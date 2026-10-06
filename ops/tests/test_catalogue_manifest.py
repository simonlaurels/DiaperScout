import copy
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("catalogue_manifest", ROOT / "ops/catalogue_manifest.py")
validator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validator)


class ManifestSafetyTests(unittest.TestCase):
    def setUp(self):
        self.data = json.loads((ROOT / "docs/research/catalogue-population-2026-10-04.json").read_text(encoding="utf-8"))

    def ready(self):
        p = self.data["packs"][0]
        p["outcome"] = "READY"
        p["duplicate_check"] = {"status": "CLEAR", "checked_at_utc": "2026-10-04T00:00:00Z", "inventory_reference": "fixture"}
        return p

    def test_saved_manifest_is_consistent(self):
        self.assertEqual([], validator.validate(self.data))

    def test_leading_zero_equivalence(self):
        self.assertEqual(validator.gtin_key("4052199297002"), validator.gtin_key("04052199297002"))

    def test_bad_check_digit_and_numeric_value_rejected(self):
        for value in ("4052199297003", 4052199297002, "00000000", "123456789"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                validator.gtin_key(value)

    def test_held_pack_cannot_import(self):
        self.data["packs"][0]["outcome"] = "NEEDS RESEARCH"
        self.data["packs"][0]["import_allowed"] = True
        self.assertTrue(validator.validate(self.data))

    def test_held_candidate_cannot_publish(self):
        self.data["candidates"][0]["publish_allowed"] = True
        self.assertTrue(validator.validate(self.data))

    def test_unknown_permission_image_cannot_be_copied(self):
        self.data["packs"][0]["proposal"]["image_copied"] = True
        self.assertTrue(validator.validate(self.data))

    def test_ready_requires_duplicate_check(self):
        self.data["packs"][0]["outcome"] = "READY"
        self.data["packs"][0].pop("duplicate_check", None)
        self.assertTrue(validator.validate(self.data))

    def test_ready_requires_exact_pack_evidence(self):
        self.ready()["proposal"]["identifier_sources"] = []
        self.assertTrue(validator.validate(self.data))

    def test_conflicting_padded_ready_assignment_rejected(self):
        first = self.ready()
        second = copy.deepcopy(first)
        second["id"] = "different-pack"
        second["proposal"]["gtin"] = "04052199297002"
        second["proposal"]["size"] = "Large"
        self.data["packs"].append(second)
        self.assertTrue(validator.validate(self.data))

    def test_sources_must_resolve(self):
        self.data["packs"][0]["evidence_source_ids"] = ["missing"]
        self.assertTrue(validator.validate(self.data))

    def test_duplicate_candidate_ids_rejected(self):
        self.data["candidates"].append(copy.deepcopy(self.data["candidates"][0]))
        self.assertTrue(validator.validate(self.data))

    def test_production_ids_must_map_every_entity(self):
        self.ready()["production_ids"] = {"product_id": "only-product"}
        self.assertTrue(validator.validate(self.data))

    def test_discovery_cannot_be_a_factual_source(self):
        self.data["sources"]["H6"]["url"] = "https://diapstash.com/catalog/types/1"
        self.assertTrue(validator.validate(self.data))


if __name__ == "__main__":
    unittest.main()
