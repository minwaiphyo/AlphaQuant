import copy
import importlib.util
import json
import subprocess
import sys
import unittest
from decimal import Decimal, localcontext
from fractions import Fraction
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / "docs/skills/alphaquant-quant-models/scripts/reference_math.py"
SPEC = importlib.util.spec_from_file_location("reference_math", SCRIPT)
MATH = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MATH)


def multiples():
    return dict(kind="multiples", price="60", ttm_eps="3", ttm_revenue="1350000000",
                shares="100000000", debt="400000000", cash="100000000", preferred="0", nci="0")


def dcf():
    return dict(kind="end-year-fcff-dcf", fcff=["110", "121"], wacc="0.10", terminal_growth="0.03",
                terminal_fcff_next="124.63", shares="100", debt="100", cash="20", preferred="10", nci="5")


class ReferenceArithmeticTests(unittest.TestCase):
    def test_additive_ttm_worked_bridge(self):
        result = MATH.evaluate(dict(kind="ttm-flow-bridge", full_year="1200", current_ytd="1000", prior_ytd="850"))
        self.assertEqual("1350", result["values"]["ttm"])
        self.assertTrue(result["numeric_reference_only"])
        self.assertIn("source_periods", result["not_validated"])

    def test_eps_uses_four_supplied_quarters(self):
        self.assertEqual(Decimal("3"), Decimal(MATH.evaluate(dict(kind="eps-four-quarters", quarters=["0.5", "0.75", "0.75", "1"]))["values"]["ttm_eps"]))

    def test_eps_ytd_bridge_is_not_an_eps_operation(self):
        with self.assertRaises(MATH.ReferenceInputError):
            MATH.evaluate(dict(kind="eps-four-quarters", full_year="3", current_ytd="2.5", prior_ytd="2"))

    def test_eps_requires_exactly_four_quarters(self):
        with self.assertRaises(MATH.ReferenceInputError):
            MATH.evaluate(dict(kind="eps-four-quarters", quarters=["1", "1", "1"]))

    def test_multiples_match_worked_equity_and_enterprise_bridge(self):
        values = MATH.evaluate(multiples())["values"]
        self.assertEqual(Decimal("20"), Decimal(values["pe"]))
        self.assertEqual(Decimal("6300000000"), Decimal(values["enterprise_value"]))
        with localcontext() as ctx:
            ctx.prec = 50
            self.assertLess(abs(Decimal(values["ev_sales"]) - Decimal(14) / Decimal(3)), Decimal("1e-40"))

    def test_missing_claim_is_rejected_not_defaulted(self):
        for field in ("debt", "cash", "preferred", "nci"):
            document = multiples()
            del document[field]
            with self.subTest(field=field), self.assertRaises(MATH.ReferenceInputError):
                MATH.evaluate(document)

    def test_nonpositive_denominators_remain_unavailable(self):
        document = multiples()
        document.update(ttm_eps="-3", ttm_revenue="0")
        values = MATH.evaluate(document)["values"]
        self.assertIsNone(values["pe"])
        self.assertIsNone(values["ev_sales"])

    def test_compatible_split_preserves_multiples_and_capitalization(self):
        original = MATH.evaluate(multiples())["values"]
        split = multiples()
        split.update(price="30", ttm_eps="1.5", shares="200000000")
        observed = MATH.evaluate(split)["values"]
        for field in original:
            self.assertEqual(Decimal(original[field]), Decimal(observed[field]), field)

    def test_negative_enterprise_value_is_not_clamped(self):
        document = multiples()
        document.update(shares="1", debt="0", cash="1000")
        self.assertEqual(Decimal("-940"), Decimal(MATH.evaluate(document)["values"]["enterprise_value"]))

    def test_dcf_matches_independent_rational_worked_results(self):
        values = MATH.evaluate(dcf())["values"]
        # Hand-derived rational expectations, not a second discounting loop.
        expected = {"explicit_pv": Fraction(200), "terminal_pv": Fraction(10300, 7),
                    "enterprise_value": Fraction(11700, 7), "common_equity_value": Fraction(11035, 7),
                    "per_share": Fraction(2207, 140)}
        with localcontext() as ctx:
            ctx.prec = 50
            for field, value in expected.items():
                target = Decimal(value.numerator) / Decimal(value.denominator)
                self.assertLess(abs(Decimal(values[field]) - target), Decimal("1e-40"), field)

    def test_dcf_rejects_singular_terminal_denominator(self):
        document = dcf()
        document["terminal_growth"] = document["wacc"]
        with self.assertRaises(MATH.ReferenceInputError):
            MATH.evaluate(document)

    def test_dcf_bounds_forecast_horizon(self):
        document = dcf()
        document["fcff"] = ["1"] * 11
        with self.assertRaises(MATH.ReferenceInputError):
            MATH.evaluate(document)

    def test_dcf_respects_explicit_terminal_cash_flow(self):
        document = dcf()
        document["terminal_fcff_next"] = "0"
        values = MATH.evaluate(document)["values"]
        self.assertEqual(Decimal("200"), Decimal(values["enterprise_value"]))

    def test_dcf_negative_equity_is_retained(self):
        document = dcf()
        document["debt"] = "10000"
        self.assertLess(Decimal(MATH.evaluate(document)["values"]["common_equity_value"]), 0)

    def test_dcf_scales_money_and_shares_without_changing_per_share(self):
        document = dcf()
        scaled = copy.deepcopy(document)
        scaled["fcff"] = [str(Decimal(x) * 10) for x in document["fcff"]]
        for key in ("terminal_fcff_next", "shares", "debt", "cash", "preferred", "nci"):
            scaled[key] = str(Decimal(scaled[key]) * 10)
        self.assertEqual(MATH.evaluate(document)["values"]["per_share"], MATH.evaluate(scaled)["values"]["per_share"])

    def test_nonfinite_float_boolean_and_unbounded_numbers_rejected(self):
        for value in ("NaN", "Infinity", "1e1000", True, 0.1):
            with self.subTest(value=value), self.assertRaises(MATH.ReferenceInputError):
                MATH.number(value)

    def test_cli_success_and_safe_rejection(self):
        result = subprocess.run([sys.executable, str(SCRIPT)], input=json.dumps(multiples()), text=True, capture_output=True)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("20", json.loads(result.stdout)["values"]["pe"])
        result = subprocess.run([sys.executable, str(SCRIPT)], input='{"kind":"private-research-do-not-echo"}', text=True, capture_output=True)
        self.assertEqual(2, result.returncode)
        self.assertNotIn("private-research", result.stderr)
        self.assertEqual("", result.stdout)

    def test_cli_rejects_duplicate_fields(self):
        result = subprocess.run([sys.executable, str(SCRIPT)], input='{"kind":"ttm-flow-bridge","full_year":"1200","current_ytd":"1000","prior_ytd":"850","prior_ytd":"0"}', text=True, capture_output=True)
        self.assertEqual(2, result.returncode)
        self.assertEqual("", result.stdout)

    def test_cli_bounds_input_before_parsing(self):
        result = subprocess.run([sys.executable, str(SCRIPT)], input=" " * 65537, text=True, capture_output=True)
        self.assertEqual(2, result.returncode)
        self.assertIn("64 KiB", result.stderr)


if __name__ == "__main__":
    unittest.main()
