"""Project-specific skill catalog/resource checks, using only the standard library.

This is not a replacement for the skill-creator YAML validator or agent behavior
evaluation. It checks our intentionally simple manifests and maintained routing.
"""
import json
import re
from pathlib import Path


def validate(root):
    root = Path(root).resolve()
    catalog = json.loads((root / "docs/skills/skill_catalog.json").read_text(encoding="utf-8-sig"))
    names = set()
    resources = 0
    for skill in catalog["skills"]:
        name = skill["name"]
        if name in names or not re.fullmatch(r"alphaquant-[a-z0-9-]{1,53}", name):
            raise ValueError("Invalid or duplicate project skill name")
        names.add(name)
        folder = (root / skill["path"]).resolve()
        if folder != (root / "docs/skills" / name).resolve():
            raise ValueError("Noncanonical skill path")
        entry = (folder / "SKILL.md").read_text(encoding="utf-8-sig")
        frontmatter = re.match(r"\A---\r?\n(.*?)\r?\n---\r?\n", entry, re.S)
        if not frontmatter or not re.search(r"(?m)^name: " + re.escape(name) + r"\s*$", frontmatter[1]):
            raise ValueError("Skill identity/frontmatter mismatch")
        description = re.search(r"(?m)^description: (.+)$", frontmatter[1])
        if not description or len(description[1]) > 1024:
            raise ValueError("A concise scalar discovery description is required")
        for document in folder.rglob("*.md"):
            for target in re.findall(r"\]\(([^)]+)\)", document.read_text(encoding="utf-8-sig")):
                if target.startswith(("https://", "http://", "#")):
                    continue
                resource = (document.parent / target.split("#", 1)[0]).resolve()
                if not resource.is_relative_to(root) or not resource.exists():
                    raise ValueError(f"Missing or escaping resource in {document.name}: {target}")
                resources += 1
    ledger = json.loads((root / "docs/roadmap/Stage_Ledger.json").read_text(encoding="utf-8-sig"))
    stage_ids = {stage["id"] for stage in ledger["stages"]}
    if set(catalog["stage_skill_hints"]) != stage_ids:
        raise ValueError("Skill routing must account for every roadmap stage")
    for hint in catalog["stage_skill_hints"].values():
        if not hint or any(name not in names for name in hint):
            raise ValueError("Unknown or empty routing hint")
    for path in catalog["required_repository_paths"] + [catalog["evaluation_cases"]]:
        candidate = (root / path).resolve()
        if not candidate.is_relative_to(root) or not candidate.exists():
            raise ValueError("Missing authoritative project resource")
    return len(names), len(stage_ids), resources


if __name__ == "__main__":
    counts = validate(Path(__file__).resolve().parents[1])
    print(f"Validated {counts[0]} project skills, {counts[1]} stage routes and {counts[2]} linked resources.")
