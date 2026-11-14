import importlib.util
import hashlib
import json
from pathlib import Path
import tempfile
import unittest

SPEC = importlib.util.spec_from_file_location(
    "notices", Path(__file__).resolve().parents[1] / "Collect-DistributionNotices.py")
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class DistributionNoticesTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.publish = self.root / "publish"
        self.publish.mkdir()
        self.cache = self.root / "packages"
        self.assets = self.root / "assets.json"
        self.assets.write_text(json.dumps({"packageFolders": {str(self.cache): {}}}))

    def package(self, name, text="Upstream copyright and full license\n"):
        folder = self.cache / name.lower() / "1.0"
        folder.mkdir(parents=True)
        (folder / f"{name}.nuspec").write_text(
            '<package><metadata><license type="file">LICENSE.txt</license>'
            '<copyright>Upstream</copyright></metadata></package>')
        (folder / "LICENSE.txt").write_text(text)
        return folder

    def graph(self, libraries):
        (self.publish / "AlphaQuant.Desktop.deps.json").write_text(json.dumps({"libraries": libraries}))

    def test_exact_graph_includes_runtime_not_unused_cache(self):
        self.package("Dependency")
        self.package("Microsoft.NETCore.App.Runtime.win-x64", "Runtime notices\n")
        self.package("BuildOnly")
        self.graph({"Dependency/1.0": {"type": "package"},
                    "runtimepack.Microsoft.NETCore.App.Runtime.win-x64/1.0": {"type": "runtimepack"},
                    "AlphaQuant.Desktop/1.0": {"type": "project"}})
        result = MODULE.collect(self.publish, self.assets)
        self.assertEqual(len(result["dependencies"]), 2)
        self.assertEqual(result["status"], "review_required")
        texts = [p.read_text() for p in (self.publish / "licenses").rglob("LICENSE.txt")]
        self.assertCountEqual(texts, ["Upstream copyright and full license\n", "Runtime notices\n"])
        self.assertNotIn("BuildOnly", json.dumps(result))
        self.assertNotIn(str(self.cache), json.dumps(result))
        with self.assertRaisesRegex(ValueError, "already exists"):
            MODULE.collect(self.publish, self.assets)

    def test_missing_dependency_fails_before_output(self):
        self.graph({"Missing/1.0": {"type": "package"}})
        with self.assertRaisesRegex(ValueError, "Missing restored"):
            MODULE.collect(self.publish, self.assets)
        self.assertFalse((self.publish / "licenses").exists())

    def test_expression_without_text_is_flagged_not_cleared(self):
        p = self.package("FontWrapper")
        (p / "LICENSE.txt").unlink()
        (p / "FontWrapper.nuspec").write_text(
            '<package><metadata><license type="expression">MIT</license></metadata></package>')
        self.graph({"FontWrapper/1.0": {"type": "package"}})
        item = MODULE.collect(self.publish, self.assets)["dependencies"][0]
        self.assertTrue(item["reviewRequired"])
        self.assertEqual(item["licenseFiles"], [])
        self.assertIn("no license text", item["reviewReason"])

    def test_declared_license_cannot_escape_package(self):
        p = self.package("Escaping")
        (p / "Escaping.nuspec").write_text(
            '<package><metadata><license type="file">../../outside.txt</license></metadata></package>')
        self.graph({"Escaping/1.0": {"type": "package"}})
        with self.assertRaisesRegex(ValueError, "Invalid declared license"):
            MODULE.collect(self.publish, self.assets)

    def test_upstream_notice_integrity_and_provenance(self):
        self.package("Dependency")
        self.graph({"Dependency/1.0": {"type": "package"}})
        source = self.root / "upstream"
        source.mkdir()
        text = b"Upstream license text\n"
        (source / "LICENSE.txt").write_bytes(text)
        (source / "sources.json").write_text(json.dumps({"sources": [{
            "file": "LICENSE.txt", "sha256": hashlib.sha256(text).hexdigest(),
            "identities": ["Dependency/1.0"], "url": "https://example.invalid/pinned/LICENSE"}]}))
        (source / "LICENSE.txt").write_bytes(b"tampered")
        with self.assertRaisesRegex(ValueError, "checksum mismatch"):
            MODULE.collect(self.publish, self.assets, source)
        self.assertFalse((self.publish / "licenses").exists())
        (source / "LICENSE.txt").write_bytes(text)
        item = MODULE.collect(self.publish, self.assets, source)["dependencies"][0]
        self.assertEqual(len(item["upstreamSources"]), 1)
        self.assertEqual(len(item["licenseFiles"]), 2)
        self.assertEqual((self.publish / item["licenseFiles"][1]).read_bytes(), text)


if __name__ == "__main__":
    unittest.main()
