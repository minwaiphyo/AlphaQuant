# Source selection and reconciliation

Authority: current `docs/requirements/Financial_Definitions.md`, `src/AlphaQuant.Core/ComparisonAnalyzer.cs`, concept definitions and `src/AlphaQuant.Infrastructure/SecParser.cs`. If code and specification disagree, expose the discrepancy and test the intended version rather than picking whichever yields a number.

## Decision record

For each metric capture issuer/CIK, supported-scope evidence or unknown, concept/namespace, unit, instant or duration, start/end, form, filed/accession, retrieval/as-of date, value and multiplier. Record why alternatives lost selection. Preserve enough context to reproduce a restatement choice later.

Comparison-1 uses five annual periods with a sixth when needed for growth. Its annual/quarter duration bounds, tag priority and same-day conflict rules are defined centrally. Read those current rules rather than approximating a year as a filing's `fy` field or silently relaxing a bound to fill a gap.

## High-impact distinctions

| Input situation | Required handling |
|---|---|
| Same period in multiple filings | Apply current versioned selection and disclose changed values |
| Conflicting facts on the same filing date | Preserve an unavailable/conflict state under comparison-1 |
| Two alternative revenue tags | Choose by supported mapping, do not sum alternatives |
| Quarterly versus YTD fact | Match actual interval. Never treat cumulative YTD as a discrete quarter |
| Missing debt/preferred/NCI | Unknown, not an inferred zero claim |
| EPS versus shares | USD/share diluted earnings is not actual outstanding common shares |
| Parent versus consolidated income | Record scope and use only a compatible approved denominator |
| Latest restated history versus historical observation | Latest-filed research is not a point-in-time reconstruction |
| Unsupported issuer/class | Keep supported facts inspectable, withhold unsupported derived results |

## Reconciliation workflow

1. Select a bounded case matrix for the authorized work package. Include a normal case and the selection boundary it changes, such as amendment, fiscal mismatch or missing quarter.
2. Use isolated synthetic fixtures for default tests. When authorized, use the configured contact and paced SEC client for bounded real requests. Never commit request-identification headers or private settings.
3. Inspect the actual public filing/fact context. Reduce it to the minimum source-backed fixture and dated references needed to explain the expected result.
4. Recompute using an independently written worksheet or reference arithmetic with explicit inputs. A reference calculation does not prove period/scope validity by itself.
5. Record matches, mismatches, tolerance, absent disclosures and unsupported cases. Add a regression for a demonstrated defect, not just a copied implementation test.

External interface reference: [SEC EDGAR API](https://www.sec.gov/search-filings/edgar-application-programming-interfaces). Verify current access requirements when executing live work.
