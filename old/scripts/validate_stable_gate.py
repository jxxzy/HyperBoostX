import json
from collections import Counter
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MATRIX = ROOT / "docs" / "registry" / "requirement-traceability.json"
COMPLETE = "IMPLEMENTED_VERIFIED"
REQUIRED_INVENTORY = {
    "primaryRoutes": 73,
    "prototypeSubfeatures": 376,
    "galleryImageReferences": 81,
    "legacyActionMapEntries": 606,
}


def main():
    if not MATRIX.is_file():
        raise SystemExit("Stable gate failed: requirement traceability matrix is missing.")
    matrix = json.loads(MATRIX.read_text(encoding="utf-8-sig"))
    mismatches = {
        name: (expected, matrix.get("inventory", {}).get(name))
        for name, expected in REQUIRED_INVENTORY.items()
        if matrix.get("inventory", {}).get(name) != expected
    }
    if mismatches:
        raise SystemExit(f"Stable gate failed: source inventories do not match required coverage: {mismatches}")

    requirements = matrix.get("requirements", [])
    trace_fields = {
        "requirementId", "featureId", "uiComponent", "command", "backendHandler",
        "provider", "verification", "test", "evidence", "status",
    }
    malformed = [
        item.get("requirementId", "<missing ID>")
        for item in requirements
        if not trace_fields.issubset(item) or not item.get("requirementId") or not item.get("featureId")
    ]
    if malformed:
        raise SystemExit(f"Stable gate failed: {len(malformed)} requirements lack traceability fields.")
    if len({item["requirementId"] for item in requirements}) != len(requirements):
        raise SystemExit("Stable gate failed: requirement IDs are not unique.")

    incomplete = [
        item for item in requirements
        if item.get("mandatory", True) and (
            item.get("status") != COMPLETE
            or any(item.get(field) in {None, "", "NOT_MAPPED", "NOT_VERIFIED", "NOT_RECORDED"}
                   for field in ("uiComponent", "command", "backendHandler", "provider", "verification", "test", "evidence"))
        )
    ]
    if incomplete:
        counts = Counter(item.get("status", "MISSING_STATUS") for item in incomplete)
        print(f"Stable gate FAILED: {len(incomplete)} mandatory requirements are not {COMPLETE}.")
        print("Incomplete status counts: " + ", ".join(f"{key}={value}" for key, value in sorted(counts.items())))
        for item in incomplete[:20]:
            print(f"- {item.get('requirementId')}: {item.get('status')} — {item.get('requirement', '')[:160]}")
        raise SystemExit(1)

    print(f"Stable gate PASSED: all {len(requirements)} mandatory requirements have implementation and verification evidence.")


if __name__ == "__main__":
    main()
