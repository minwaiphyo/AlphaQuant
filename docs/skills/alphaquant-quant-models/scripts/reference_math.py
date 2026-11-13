"""Independent numeric reference, not AlphaQuant eligibility or production logic.

Uses only standard-library Decimal. Inputs are explicit reviewed numbers. It does
not select SEC facts, establish compatible periods or validate price freshness.
"""

import argparse
import json
import sys
from decimal import Decimal, DecimalException, localcontext


class ReferenceInputError(ValueError):
    pass


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ReferenceInputError("Duplicate input fields are not allowed.")
        result[key] = value
    return result


def number(value):
    if type(value) not in (str, int):
        raise ReferenceInputError("Use decimal strings or integers, not binary floats or booleans.")
    if len(str(value)) > 128:
        raise ReferenceInputError("Numeric input is too long.")
    try:
        result = Decimal(value)
    except DecimalException as exc:
        raise ReferenceInputError("Invalid decimal input.") from exc
    if not result.is_finite() or len(result.as_tuple().digits) > 40 or abs(result.adjusted()) > 50:
        raise ReferenceInputError("A finite bounded decimal is required.")
    return result


def fields(document, required):
    if not isinstance(document, dict) or set(document) != set(required):
        raise ReferenceInputError("Supply exactly the documented fields, including explicit zero claims.")


def capital(document):
    values = {key: number(document[key]) for key in ("shares", "debt", "cash", "preferred", "nci")}
    if values["shares"] <= 0 or any(values[key] < 0 for key in ("debt", "cash", "preferred", "nci")):
        raise ReferenceInputError("Shares must be positive and all claim/cash amounts nonnegative.")
    return values


def evaluate(document):
    if not isinstance(document, dict):
        raise ReferenceInputError("Supply one JSON object.")
    with localcontext() as context:
        context.prec = 50
        context.Emax = 999
        context.Emin = -999
        kind = document.get("kind")
        if kind == "ttm-flow-bridge":
            fields(document, ("kind", "full_year", "current_ytd", "prior_ytd"))
            output = {"ttm": number(document["full_year"]) + number(document["current_ytd"]) - number(document["prior_ytd"])}
        elif kind == "eps-four-quarters":
            fields(document, ("kind", "quarters"))
            quarters = document["quarters"]
            if not isinstance(quarters, list) or len(quarters) != 4:
                raise ReferenceInputError("Supply four directly reported discrete EPS quarters, not a YTD bridge.")
            output = {"ttm_eps": sum((number(q) for q in quarters), Decimal(0))}
        elif kind == "multiples":
            fields(document, ("kind", "price", "ttm_eps", "ttm_revenue", "shares", "debt", "cash", "preferred", "nci"))
            c = capital(document)
            price, eps, revenue = (number(document[key]) for key in ("price", "ttm_eps", "ttm_revenue"))
            if price <= 0:
                raise ReferenceInputError("The numeric price must be positive.")
            ev = price * c["shares"] + c["debt"] + c["preferred"] + c["nci"] - c["cash"]
            output = {"enterprise_value": ev, "pe": price / eps if eps > 0 else None,
                      "ev_sales": ev / revenue if revenue > 0 else None}
        elif kind == "end-year-fcff-dcf":
            fields(document, ("kind", "fcff", "wacc", "terminal_growth", "terminal_fcff_next", "shares", "debt", "cash", "preferred", "nci"))
            cash_flows = document["fcff"]
            if not isinstance(cash_flows, list) or not 1 <= len(cash_flows) <= 10:
                raise ReferenceInputError("Supply 1-10 explicit annual FCFF values.")
            c = capital(document)
            wacc, growth, terminal_next = (number(document[key]) for key in ("wacc", "terminal_growth", "terminal_fcff_next"))
            if wacc <= 0 or wacc <= growth:
                raise ReferenceInputError("This reference model requires positive WACC greater than terminal growth.")
            discount = Decimal(1) + wacc
            explicit_pv = sum((number(cf) / discount ** year for year, cf in enumerate(cash_flows, 1)), Decimal(0))
            terminal_value = terminal_next / (wacc - growth)
            terminal_pv = terminal_value / discount ** len(cash_flows)
            ev = explicit_pv + terminal_pv
            equity = ev + c["cash"] - c["debt"] - c["preferred"] - c["nci"]
            output = {"explicit_pv": explicit_pv, "terminal_value": terminal_value, "terminal_pv": terminal_pv,
                      "enterprise_value": ev, "common_equity_value": equity, "per_share": equity / c["shares"]}
        else:
            raise ReferenceInputError("Unknown reference calculation kind.")
        return {"numeric_reference_only": True,
                "not_validated": ["issuer_scope", "source_periods", "source_or_market_rights", "price_freshness", "review_accuracy", "terminal_economics"],
                "values": {key: str(value) if value is not None else None for key, value in output.items()}}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", help="JSON file, otherwise reads stdin. Decimal values should be strings.")
    args = parser.parse_args()
    try:
        if args.input:
            with open(args.input, encoding="utf-8-sig") as source:
                raw = source.read(65537)
        else:
            raw = sys.stdin.read(65537)
        if len(raw) > 65536:
            raise ReferenceInputError("Input exceeds 64 KiB.")
        document = json.loads(raw, object_pairs_hook=unique_object)
        print(json.dumps(evaluate(document), indent=2))
        return 0
    except (ReferenceInputError, DecimalException, json.JSONDecodeError, OSError, UnicodeError) as exc:
        # Never echo supplied files or values, which may contain private research.
        print("Reference calculation rejected: " + (str(exc) if isinstance(exc, ReferenceInputError) else "invalid input or numeric operation."), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
