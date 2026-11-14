"""Collect evidence from the actual publish graph; never certify license clearance.

Uses only Python 3 standard library. No network, assembly execution or credentials.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import xml.etree.ElementTree as ET


def collect(publish_dir, assets_path, supplemental_dir=None):
    publish = Path(publish_dir).resolve()
    assets = json.loads(Path(assets_path).read_text(encoding="utf-8-sig"))
    deps = json.loads((publish / "AlphaQuant.Desktop.deps.json").read_text(encoding="utf-8-sig"))
    roots = [Path(p) for p in assets["packageFolders"]]
    supplemental = Path(supplemental_dir) if supplemental_dir else Path(__file__).with_name("distribution-licenses")
    source_manifest = supplemental / "sources.json"
    source_data = json.loads(source_manifest.read_text(encoding="utf-8")) if source_manifest.exists() else {"sources": []}
    destination = publish / "licenses"
    outputs = [destination, publish / "dependency-inventory.json", publish / "THIRD-PARTY-NOTICES.txt"]
    if any(p.exists() for p in outputs):
        raise ValueError("Notice output already exists; use a fresh publish directory.")
    # Resolve all inputs before creating output. Missing packages are an error,
    # not silently omitted obligations. The inventory contains no local cache paths.
    resolved = []
    for identity, item in sorted(deps["libraries"].items()):
        if item["type"] not in ("package", "runtimepack"):
            continue
        package, version = identity.split("/", 1)
        if item["type"] == "runtimepack":
            package = package.removeprefix("runtimepack.")
        if any(part in ("", ".", "..") or not all(c.isalnum() or c in "._-+" for c in part)
               for part in (package, version)):
            raise ValueError("Unsafe dependency identity")
        folder = next((root / package.lower() / version.lower() for root in roots
                       if (root / package.lower() / version.lower()).is_dir()), None)
        if folder is None:
            raise ValueError(f"Missing restored package: {identity}")
        nuspecs = list(folder.glob("*.nuspec"))
        if len(nuspecs) != 1:
            raise ValueError(f"Expected one nuspec: {identity}")
        metadata = {}
        for node in ET.parse(nuspecs[0]).getroot().iter():
            name = node.tag.rsplit("}", 1)[-1]
            if name in ("license", "copyright", "projectUrl", "licenseUrl"):
                metadata[name] = node.text or ""
                if name == "license":
                    metadata["licenseType"] = node.get("type", "unknown")
        files = []
        declared = metadata.get("license") if metadata.get("licenseType") == "file" else None
        if declared:
            candidate = (folder / declared).resolve()
            if not candidate.is_relative_to(folder.resolve()) or not candidate.is_file():
                raise ValueError(f"Invalid declared license file: {identity}")
            files.append(candidate)
        for candidate in sorted(folder.rglob("*")):
            if candidate.is_file() and any(token in candidate.name.lower()
                                           for token in ("license", "notice", "copying", "ofl")):
                if candidate.suffix.lower() in ("", ".txt", ".md", ".rst", ".html"):
                    files.append(candidate)
        files = sorted(set(p.resolve() for p in files))
        if any(not p.is_relative_to(folder.resolve()) for p in files):
            raise ValueError(f"License symlink escapes package: {identity}")
        extras = []
        for source in source_data["sources"]:
            if identity not in source["identities"]:
                continue
            candidate = (supplemental / source["file"]).resolve()
            if not candidate.is_relative_to(supplemental.resolve()) or not candidate.is_file():
                raise ValueError(f"Invalid supplemental license file: {identity}")
            if hashlib.sha256(candidate.read_bytes()).hexdigest() != source["sha256"]:
                raise ValueError(f"Supplemental license checksum mismatch: {identity}")
            extras.append((candidate, source))
        resolved.append((identity, metadata, folder.resolve(), files, extras))
    destination.mkdir()
    inventory = []
    for identity, metadata, folder, files, extras in resolved:
        copied = []
        for source in files:
            target = destination / identity.replace("/", "-") / source.relative_to(folder)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, target)
            copied.append(target.relative_to(publish).as_posix())
        provenance = []
        for source_file, source in extras:
            target = destination / identity.replace("/", "-") / "upstream" / source_file.name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source_file, target)
            copied.append(target.relative_to(publish).as_posix())
            provenance.append(source)
        inventory.append({"identity": identity, "metadata": metadata, "licenseFiles": copied,
                          "upstreamSources": provenance,
                          "reviewRequired": True,
                          "reviewReason": "Confirm bundled/transitive obligations and embedded assets."
                          if copied else "Package has no license text; obtain and retain upstream notices."})
    report = {"schemaVersion": 1, "status": "review_required", "dependencies": inventory,
              "upstreamEvidence": source_data}
    (publish / "dependency-inventory.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    lines = ["AlphaQuant third-party license evidence", "",
             "INCOMPLETE AUDIT: review_required. This development package is not cleared for recruiter distribution.",
             "Repository MIT terms do not relicense third-party components or financial data.",
             "See dependency-inventory.json and licenses/ for exact package-supplied evidence.", ""]
    for item in inventory:
        lines.extend([item["identity"], json.dumps(item["metadata"], ensure_ascii=False),
                      "License files: " + (", ".join(item["licenseFiles"]) or "NONE — upstream review required"), ""])
    (publish / "THIRD-PARTY-NOTICES.txt").write_text("\n".join(lines), encoding="utf-8")
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--publish-dir", required=True)
    parser.add_argument("--assets", required=True)
    args = parser.parse_args()
    result = collect(args.publish_dir, args.assets)
    print(f"Collected {len(result['dependencies'])} dependency records; license clearance remains pending.")
