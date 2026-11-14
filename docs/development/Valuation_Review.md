# Structured valuation review (review-ui-1, S06)

The Data connections page supplies explicit session-only forms for the unchanged
comparison-1.1 valuation contract. Existing strict JSON import remains supported.
Entry, import and checked assertions are not financial certification. No facts,
class scope, capital completeness or zero claims are approved automatically.

## Input and source contract

Choose a company still in the peer selection. Its CIK and symbol identify both
equity and capital reviews. Enter a canonical SEC accession and a review note
covering pages, class/split checks, additional source references and capital
composition. Filing buttons open a computed HTTPS SEC link only on user action.
Selected calculation-input facts are shown with exact decimal values, units,
periods, filing/accession and retrieval state. They may be cached or incomplete;
they do not substitute for independent inspection of the complete disclosures.

Every review requires affirmative US domestic non-financial US-GAAP/USD scope and
simple common-class/compatible diluted-EPS assertions. Split review must cover the
UTC evaluation date. Capital input additionally requires explicit actual outstanding
common shares (not diluted weighted-average EPS shares), share/balance-sheet dates,
USD debt, eligible cash, preferred equity and NCI, plus a complete-claim assertion.
Every numeric field is initially blank; only an explicit reviewed `0` is zero.
Use individual shares and full USD amounts, no millions/grouping separators/unit
suffixes. Invariant decimals reject negative claims, nonfinite input and overflow.

Dates use yyyy-MM-dd. Future dates are invalid. Current capital evidence must be
at most 135 days old, including the 135th day. These reproduce the existing
baseline's current-valuation gates, not a new calculation definition. Historical
or unsupported inputs remain unavailable; the form never guesses a replacement.

## Atomic session behavior

Apply first validates the entire draft, then atomically replaces both review lists
for the selected CIK; invalid/partial input preserves all prior applied reviews.
Equity-only Apply explicitly clears that CIK's prior capital review, withholding
EV/sales; other issuers are retained. Clear draft and Clear applied for company
are separate actions. Changing the selected issuer clears only its draft.
Successful replacement/clear invalidates currently displayed prices/valuations;
comparison refresh must still satisfy source, TTM, class and fresh-price guards.
Negative enterprise value is not clamped or presented as a recommendation.

The bridge remains `eligible price × actual outstanding common shares + debt +
preferred equity + NCI − approved cash`. No quote is entered by the form. An
eligible dated IEX quote and compatible positive TTM denominator are still required
by ComparisonValuation. The summary distinguishes actual shares from diluted EPS.

Reviewed-equity/capital objects and drafts are not written to SQLite, cache,
exports or backups. No market-data storage rights have been established. App
shutdown clears the applied store and structured draft. Restart begins without
reviews. Existing JSON import replaces the entire reviewed document as before;
its success/clear refreshes the form's displayed session state and invalidates prices.

## Validation evidence and remaining gates

Cloud Release build and synthetic tests exercise independent 20× P/E and 6,300 EV
with 4.6666666666666666666666666667 EV/sales, negative EV, stale quotes, explicit
zeros, incomplete replacements, unknown scope/class, conflicting identities,
future/stale dates and the 135/136-day boundary. Additional checks cover JSON
roundtrip, source precision (0.0046 USD/shares), exact cash decimals, per-company
clear, other-issuer retention and production shutdown clearing. No real issuer,
quote, provider call or qualified independent finance review is claimed.

The Avalonia headless fixture renders empty, missing-claim, capital-field,
explicit-zero and applied/source states in light/dark and a 648x560 research
viewport, including input focus. Actual native Windows scaling/accessibility,
live source reconciliation, signing and clean-Windows acceptance remain pending.
See the stage ledger for exact final run counts and remote-delivery status.
