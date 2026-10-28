namespace AlphaQuant.Core;

public static class ComparisonValuation
{
    public static ValuationResult Calculate(CompanyRef company, ComparisonFundamentals financials, MarketPrice? price,
        DateTimeOffset now, CapitalStructureEvidence? capital = null, EquityBasisEvidence? basis = null)
    {
        FinancialValue pe = ComparisonAnalyzer.Missing("Trailing P/E", "x", "A fresh, matching USD IEX price and reviewed EPS/share-class/split basis are required.");
        FinancialValue evSales = ComparisonAnalyzer.Missing("EV / TTM sales", "x", "A fresh matching price and explicit reviewed capital-structure evidence are required.");
        decimal? ev = null;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var usable = price is not null && price.Symbol.Equals(company.Ticker, StringComparison.OrdinalIgnoreCase)
            && price.Currency == "USD" && price.Price > 0 && price.EventAt <= now.AddSeconds(5)
            && price.ReceivedAt <= now.AddSeconds(5) && now - price.EventAt <= TimeSpan.FromSeconds(60)
            && now - price.ReceivedAt <= TimeSpan.FromSeconds(60);
        if (usable && basis is not null && basis.SupportedSimpleDomesticNonFinancialUsGaapUsd
            && basis.Cik == company.Cik && basis.Symbol == company.Ticker
            && basis.SplitAdjustedThrough == today && !string.IsNullOrWhiteSpace(basis.ReviewReference)
            && financials.TtmDilutedEps.Available && financials.TtmDilutedEps.Value > 0
            && financials.TtmDilutedEps.End is { } epsEnd && epsEnd <= today && today.DayNumber - epsEnd.DayNumber <= 135)
        {
            pe = ComparisonAnalyzer.Calculate("Trailing P/E", "x", financials.TtmDilutedEps.Start,
                financials.TtmDilutedEps.End, financials.TtmDilutedEps.Inputs, () => price!.Price / financials.TtmDilutedEps.Value!.Value);
        }
        if (usable && capital is not null && capital.SimpleSingleClass && capital.SupportedDomesticNonFinancialUsGaapUsd && capital.Cik == company.Cik
            && capital.Symbol == company.Ticker && capital.SplitAdjustedThrough == today
            && capital.SharesDate <= today && capital.BalanceSheetDate <= today
            && today.DayNumber - capital.SharesDate.DayNumber <= 135
            && today.DayNumber - capital.BalanceSheetDate.DayNumber <= 135
            && capital.OutstandingShares > 0 && capital.Debt >= 0 && capital.Cash >= 0
            && capital.PreferredEquity >= 0 && capital.NonControllingInterests >= 0
            && !string.IsNullOrWhiteSpace(capital.FilingReference) && !string.IsNullOrWhiteSpace(capital.ReviewReference)
            && financials.TtmRevenue.Available && financials.TtmRevenue.Value > 0
            && financials.TtmRevenue.End is { } revenueEnd && revenueEnd <= today && today.DayNumber - revenueEnd.DayNumber <= 135)
        {
            evSales = ComparisonAnalyzer.Calculate("EV / TTM sales", "x", financials.TtmRevenue.Start,
                financials.TtmRevenue.End, financials.TtmRevenue.Inputs, () =>
                {
                    var value = price!.Price * capital.OutstandingShares + capital.Debt + capital.PreferredEquity
                        + capital.NonControllingInterests - capital.Cash;
                    var ratio = value / financials.TtmRevenue.Value!.Value;
                    ev = value;
                    return ratio;
                });
        }
        return new(ComparisonAnalyzer.DefinitionVersion, pe, evSales, ev, price, capital, basis);
    }
}
