"""Validate research consistency only. This command never connects to production."""
import argparse
import json
from pathlib import Path
from urllib.parse import urlparse

STATES = {"READY", "NEEDS RESEARCH", "CONFLICT", "DUPLICATE / ALREADY COVERED", "DISCONTINUED / HISTORICAL", "REJECT"}
APPROVED_IMAGES = {"PermissionGranted", "PermissionNotRequired"}


def gtin_key(value):
    if not isinstance(value, str) or len(value) not in (8, 12, 13, 14) or not value.isascii() or not value.isdigit() or not int(value):
        raise ValueError("GTIN must be a nonzero string of 8, 12, 13 or 14 ASCII digits")
    check = (10 - sum(int(d) * (3 if i % 2 == 0 else 1) for i, d in enumerate(reversed(value[:-1]))) % 10) % 10
    if check != int(value[-1]):
        raise ValueError("GTIN check digit is invalid")
    return value.zfill(14)


def validate(manifest):
    errors = []
    sources = manifest.get("sources", {})
    candidates = manifest.get("candidates", [])
    ids = [c.get("id") for c in candidates]
    if any(not i for i in ids) or len(set(ids)) != len(ids):
        errors.append("Candidate IDs must be present and unique")
    for source_id, source in sources.items():
        url = urlparse(source.get("url", ""))
        if url.scheme not in ("http", "https") or not url.netloc:
            errors.append(f"Source {source_id}: invalid URL")
        if url.netloc.lower().endswith("diapstash.com"):
            errors.append(f"Source {source_id}: discovery source cannot provide canonical facts")
        if not source.get("access_date"):
            errors.append(f"Source {source_id}: missing access date")
    for c in candidates:
        if c.get("outcome") not in STATES:
            errors.append(f"{c.get('id')}: invalid research state")
        if c.get("outcome") != "READY" and (c.get("import_allowed") or c.get("publish_allowed")):
            errors.append(f"{c.get('id')}: held candidate cannot be imported/published")
        if c.get("outcome") != "READY" and not c.get("blocker"):
            errors.append(f"{c.get('id')}: held candidate needs a precise blocker")
        for source_id in c.get("source_ids", []):
            if source_id not in sources:
                errors.append(f"{c.get('id')}: unresolved source {source_id}")
    pack_ids = set()
    ready_gtins = {}
    for pack in manifest.get("packs", []):
        pid = pack.get("id")
        if not pid or pid in pack_ids:
            errors.append("Pack research IDs must be present and unique")
        pack_ids.add(pid)
        if pack.get("candidate_id") not in ids:
            errors.append(f"{pid}: unresolved candidate")
        if pack.get("outcome") not in STATES:
            errors.append(f"{pid}: invalid pack state")
        proposal = pack.get("proposal", {})
        count = proposal.get("count")
        if isinstance(count, bool) or not isinstance(count, int) or count <= 0:
            errors.append(f"{pid}: pack quantity must be a positive integer")
        try:
            key = gtin_key(proposal.get("gtin"))
        except ValueError as error:
            errors.append(f"{pid}: {error}")
            key = None
        referenced = pack.get("evidence_source_ids", []) + proposal.get("identifier_sources", []) + proposal.get("spec_sources", [])
        for source_id in referenced:
            if source_id not in sources:
                errors.append(f"{pid}: unresolved evidence source {source_id}")
        permission = proposal.get("image_permission_status", "Unknown")
        if proposal.get("image_copied") and permission not in APPROVED_IMAGES:
            errors.append(f"{pid}: unapproved image cannot be copied")
        if pack.get("outcome") == "READY":
            for field in ("manufacturer", "brand", "product", "product_type", "size", "packaging_type", "variant_scope"):
                if not proposal.get(field):
                    errors.append(f"{pid}: READY pack missing {field}")
            if not pack.get("evidence_source_ids") or not proposal.get("identifier_sources"):
                errors.append(f"{pid}: READY pack needs identity and exact-pack identifier evidence")
            duplicate_check = pack.get("duplicate_check", {})
            if duplicate_check.get("status") != "CLEAR" or not duplicate_check.get("checked_at_utc") or not duplicate_check.get("inventory_reference"):
                errors.append(f"{pid}: READY pack requires complete, dated production duplicate check")
            if key in ready_gtins:
                errors.append(f"{pid}: equivalent GTIN already assigned to READY pack {ready_gtins[key]}")
            ready_gtins[key] = pid
        elif pack.get("import_allowed") or pack.get("publish_allowed"):
            errors.append(f"{pid}: held pack cannot be imported/published")
        production_ids = pack.get("production_ids")
        if production_ids:
            for field in ("product_id", "variant_id", "size_id", "pack_id", "identifier_id", "audit_id"):
                if not production_ids.get(field):
                    errors.append(f"{pid}: production mapping missing {field}")
    for publication in manifest.get("publications", []):
        if publication.get("research_pack_id") not in pack_ids:
            errors.append("Publication does not map to a research pack")
    return errors


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    args = parser.parse_args()
    data = json.loads(args.manifest.read_text(encoding="utf-8-sig"))
    issues = validate(data)
    print(json.dumps({"passed": not issues, "candidates": len(data.get("candidates", [])), "packs": len(data.get("packs", [])), "errors": issues}, indent=2))
    raise SystemExit(bool(issues))
