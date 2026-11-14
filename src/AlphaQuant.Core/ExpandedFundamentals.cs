namespace AlphaQuant.Core;

public sealed record ExpandedFinancialMetric(string DefinitionVersion, FinancialValue Result);

public static class ExpandedFundamentals
{
    public const string Version = "expanded-1";
    public static IReadOnlyList<string> Tags { get; } = ["GrossProfit", "NetIncomeLoss", "AssetsCurrent", "LiabilitiesCurrent", "Assets", "StockholdersEquity", "StockholdersEquityIncludingPortionAttributableToNoncontrollingInterest", "PaymentsToAcquirePropertyPlantAndEquipment", "IncomeTaxExpenseBenefit", "IncomeLossFromContinuingOperationsBeforeIncomeTaxesExtraordinaryItemsNoncontrollingInterest", "LongTermDebtAndFinanceLeaseObligationsIncludingCurrentMaturities", "ShortTermBorrowings", "CashAndCashEquivalentsAtCarryingValue", "InterestAndDebtExpense", "AccountsReceivableNetCurrent", "InventoryNet", "CommonStockSharesOutstanding", "WeightedAverageNumberOfDilutedSharesOutstanding", "PaymentsOfDividendsCommonStock", "PaymentsForRepurchaseOfCommonStock", "DividendsPerShareCommonStock", "LongTermDebtMaturitiesRepaymentsOfPrincipalInNextTwelveMonths", "LongTermDebtMaturitiesRepaymentsOfPrincipalInYearTwo", "LongTermDebtMaturitiesRepaymentsOfPrincipalInYearThree", "LongTermDebtMaturitiesRepaymentsOfPrincipalInYearFour", "LongTermDebtMaturitiesRepaymentsOfPrincipalInYearFive"];
    public static IReadOnlyList<string> Names { get; } = ["Gross margin", "Net margin", "Current ratio", "Diluted EPS growth", "Operating-profit growth", "Revenue CAGR (3 years)", "Free cash flow (OCF less capex)", "Cash conversion", "Return on equity", "Return on invested capital", "Asset turnover", "Receivables / revenue", "Inventory / revenue", "Net debt", "Interest coverage", "Common shares outstanding", "Weighted-average diluted shares", "Dilution (weighted shares)", "Common dividends paid", "Common buybacks paid", "Common dividends per share", "Debt principal due within 12 months", "Debt principal due in year 2", "Debt principal due in year 3", "Debt principal due in year 4", "Debt principal due in year 5"];

    public static string Formula(string name) => Version + " · " + (name switch
    {
        "Gross margin" => "GrossProfit / positive revenue × 100.",
        "Net margin" => "Parent NetIncomeLoss / positive revenue × 100.",
        "Current ratio" => "Nonnegative current assets / positive current liabilities at the same date.",
        "Free cash flow (OCF less capex)" => "Operating cash flow − nonnegative PPE purchases; not FCFF.",
        "Cash conversion" => "Operating cash flow / positive parent net income.",
        "Return on equity" => "Parent net income / positive average beginning/end parent equity × 100.",
        "Return on invested capital" => "EBIT × (1 − valid effective tax rate) / positive average consolidated invested capital × 100. See expanded-1 debt/lease policy.",
        "Asset turnover" => "Revenue / positive average beginning/end assets.",
        "Receivables / revenue" => "Period-end net receivables / positive annual revenue.",
        "Inventory / revenue" => "Period-end net inventory / positive annual revenue.",
        "Net debt" => "Explicit long-term debt/finance leases + short-term borrowing − cash; no missing-as-zero.",
        "Interest coverage" => "Operating income / positive interest and debt expense.",
        "Revenue CAGR (3 years)" => "(Positive revenue / same-tag revenue three contiguous annual intervals earlier)^(1/3) − 1, ×100.",
        "Diluted EPS growth" or "Operating-profit growth" or "Dilution (weighted shares)" => "(Current / strictly positive compatible prior annual value − 1) ×100. Share/split comparability requires review.",
        _ => "Explicit reported fact for the labelled period/unit; no component inference or undisclosed zero. See Expanded_Fundamentals.md."
    });

    public static IReadOnlyList<ExpandedFinancialMetric> Analyze(IReadOnlyList<RawFact> facts, DateOnly asOf, IReadOnlyList<AnnualFundamentals> history)
    {
        var eligible = facts.Where(f => f.Filed <= asOf && f.End <= asOf && f.Form is "10-K" or "10-K/A" && (f.Start is null || f.Start <= f.End)).ToArray();
        var output = new List<ExpandedFinancialMetric>();
        foreach (var year in history)
        {
            var start = year.Start; var end = year.End; var beginning = start.AddDays(-1);
            RawFact? Select(string tag, DateOnly? from, DateOnly to, string unit = "USD")
            {
                var matches = eligible.Where(f => f.Concept == tag && f.Unit == unit && f.Start == from && f.End == to).OrderByDescending(f => f.Filed).ToArray();
                if (matches.Length == 0) return null;
                var latest = matches.Where(f => f.Filed == matches[0].Filed).ToArray();
                return latest.Select(f => f.Value).Distinct().Count() == 1 ? latest.OrderBy(f => f.Accession, StringComparer.Ordinal).First() : null;
            }
            RawFact? Flow(string tag, string unit = "USD") => Select(tag, start, end, unit);
            RawFact? Balance(string tag, bool prior = false, string unit = "USD") => Select(tag, null, prior ? beginning : end, unit);
            var revenueFact = year.Revenue.Inputs.FirstOrDefault()?.Fact;
            RawFact? PriorFlow(string tag, string unit = "USD")
            {
                var periods = eligible.Where(f => f.Concept == tag && f.Unit == unit && f.End == beginning && f.Start is not null && f.End.DayNumber - f.Start.Value.DayNumber is >= 349 and <= 379).Select(f => f.Start).Distinct().ToArray();
                return periods.Length == 1 ? Select(tag, periods[0], beginning, unit) : null;
            }
            void Metric(string name, string unit, RawFact?[] required, Func<decimal[], decimal?> calculate)
            {
                FinancialValue value;
                if (required.Any(f => f is null)) value = ComparisonAnalyzer.Missing(name, unit, "Exact compatible expanded-1 inputs are missing or conflicting; inspect disclosed period/scope and note-dependent data.");
                else
                {
                    var inputs = required.Select(f => new FactContribution(f!)).ToArray();
                    try
                    {
                        var result = calculate(required.Select(f => f!.Value).ToArray());
                        value = result is null ? ComparisonAnalyzer.Missing(name, unit, "expanded-1 denominator, sign, tax or capital policy is not satisfied.") : new(name, result, unit, start, end, inputs);
                    }
                    catch (Exception ex) when (ex is OverflowException or DivideByZeroException or ArithmeticException)
                    { value = ComparisonAnalyzer.Missing(name, unit, "expanded-1 calculation exceeds its numeric domain."); }
                }
                output.Add(new(Version, value with { Start = start, End = end, EvidenceReferences = ["Definition: docs/requirements/Expanded_Fundamentals.md (expanded-1); latest-filed research, not point-in-time or class/split certification."] }));
            }
            void Ratio(string name, RawFact? a, RawFact? b, decimal scale = 1m, string unit = "x") => Metric(name, unit, [a,b], v => v[1] > 0 ? v[0]/v[1]*scale : null);
            void PositiveFlow(string name,string tag,string unit="USD") => Metric(name,unit,[Flow(tag,unit)],v=>v[0]>=0?v[0]:null);
            var rev = revenueFact; var net = Flow("NetIncomeLoss"); var op = Flow(ComparisonConcepts.OperatingIncome); var ocf = Flow(ComparisonConcepts.OperatingCashFlow);
            Ratio("Gross margin",Flow("GrossProfit"),rev,100,"%"); Ratio("Net margin",net,rev,100,"%");
            Metric("Current ratio","x",[Balance("AssetsCurrent"),Balance("LiabilitiesCurrent")],v=>v[0]>=0&&v[1]>0?v[0]/v[1]:null);
            foreach (var (name,tag,unit) in new[]{("Diluted EPS growth",ComparisonConcepts.DilutedEps,"USD/shares"),("Operating-profit growth",ComparisonConcepts.OperatingIncome,"USD"),("Dilution (weighted shares)","WeightedAverageNumberOfDilutedSharesOutstanding","shares")})
                Metric(name,"%",[Flow(tag,unit),PriorFlow(tag,unit)],v=>v[1]>0?(v[0]/v[1]-1)*100:null);
            var oldRevenue = rev is null ? null : eligible.Where(f=>f.Concept==rev.Concept && f.Unit=="USD" && f.Start is not null && f.End.DayNumber-f.Start.Value.DayNumber is >=349 and <=379).Where(f=>Math.Abs((end.DayNumber-f.End.DayNumber)-1095)<=21).Select(f=>f.End).Distinct().ToArray();
            RawFact? cagrBase=null;
            if (oldRevenue is {Length:1} && rev is not null)
            {
                var chain=beginning; var valid=true;
                for(var i=0;i<3;i++)
                {
                    var candidates=eligible.Where(f=>f.Concept==rev.Concept&&f.Unit=="USD"&&f.End==chain&&f.Start is not null&&f.End.DayNumber-f.Start.Value.DayNumber is >=349 and <=379).Select(f=>f.Start).Distinct().ToArray();
                    if(candidates.Length!=1){valid=false;break;}
                    var selected=Select(rev.Concept,candidates[0],chain); if(selected is null){valid=false;break;}
                    if(i==2)cagrBase=selected;chain=candidates[0]!.Value.AddDays(-1);
                }
                if(!valid)cagrBase=null;
            }
            Metric("Revenue CAGR (3 years)","%",[rev,cagrBase],v=>v[0]>0&&v[1]>0?(decimal)(Math.Pow((double)(v[0]/v[1]),1.0/3)-1)*100:null);
            Metric("Free cash flow (OCF less capex)","USD",[ocf,Flow("PaymentsToAcquirePropertyPlantAndEquipment")],v=>v[1]>=0?v[0]-v[1]:null);
            Ratio("Cash conversion",ocf,net);
            Metric("Return on equity","%",[net,Balance("StockholdersEquity",true),Balance("StockholdersEquity")],v=>v[1]>0&&v[2]>0?v[0]/((v[1]+v[2])/2)*100:null);
            Metric("Asset turnover","x",[rev,Balance("Assets",true),Balance("Assets")],v=>v[1]>0&&v[2]>0?v[0]/((v[1]+v[2])/2):null);
            Ratio("Receivables / revenue",Balance("AccountsReceivableNetCurrent"),rev); Ratio("Inventory / revenue",Balance("InventoryNet"),rev);
            RawFact?[] Debt(bool prior) => [Balance("LongTermDebtAndFinanceLeaseObligationsIncludingCurrentMaturities",prior),Balance("ShortTermBorrowings",prior),Balance("CashAndCashEquivalentsAtCarryingValue",prior)];
            Metric("Net debt","USD",Debt(false),v=>v.All(n=>n>=0)?v[0]+v[1]-v[2]:null);
            Metric("Return on invested capital","%",[op,Flow("IncomeTaxExpenseBenefit"),Flow("IncomeLossFromContinuingOperationsBeforeIncomeTaxesExtraordinaryItemsNoncontrollingInterest"),Balance("StockholdersEquityIncludingPortionAttributableToNoncontrollingInterest",true),..Debt(true),Balance("StockholdersEquityIncludingPortionAttributableToNoncontrollingInterest"),..Debt(false)],v=>
            {
                if(v[2]<=0||v[1]<0||v[1]>v[2]||v.Skip(4).Take(3).Concat(v.Skip(8)).Any(n=>n<0))return null;
                var first=v[3]+v[4]+v[5]-v[6];var last=v[7]+v[8]+v[9]-v[10];
                return first>0&&last>0?v[0]*(1-v[1]/v[2])/((first+last)/2)*100:null;
            });
            Ratio("Interest coverage",op,Flow("InterestAndDebtExpense"));
            Metric("Common shares outstanding","shares",[Balance("CommonStockSharesOutstanding",unit:"shares")],v=>v[0]>0?v[0]:null);
            PositiveFlow("Weighted-average diluted shares","WeightedAverageNumberOfDilutedSharesOutstanding","shares");
            PositiveFlow("Common dividends paid","PaymentsOfDividendsCommonStock"); PositiveFlow("Common buybacks paid","PaymentsForRepurchaseOfCommonStock");PositiveFlow("Common dividends per share","DividendsPerShareCommonStock","USD/shares");
            var maturityTags=Tags.Where(t=>t.StartsWith("LongTermDebtMaturities",StringComparison.Ordinal)).ToArray();
            for(var i=0;i<maturityTags.Length;i++) Metric(i==0?"Debt principal due within 12 months":$"Debt principal due in year {i+1}","USD",[Balance(maturityTags[i])],v=>v[0]>=0?v[0]:null);
        }
        return output;
    }
}
