using System.Globalization;
using System.Text.Json;
using AlphaQuant.Core;

namespace AlphaQuant.Infrastructure;

public sealed record ParsedFacts(string? EntityName, IReadOnlyList<RawFact> Facts, int SkippedFacts);

/// <summary>
/// Parses SEC JSON into Core types. Values are read straight into decimal, never through double.
/// Malformed individual entries are skipped and counted; a malformed document throws InvalidData.
/// </summary>
public static class SecParser
{
    public static IReadOnlyList<CompanyRef> ParseDirectory(byte[] json)
    {
        using var document = Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw Invalid("The SEC ticker list was not a JSON object.");
        }

        var companies = new List<CompanyRef>();
        foreach (var property in root.EnumerateObject())
        {
            var entry = property.Value;
            if (entry.ValueKind != JsonValueKind.Object
                || !TryGetCik(entry, out var cik)
                || GetString(entry, "ticker") is not { Length: > 0 } ticker
                || GetString(entry, "title") is not { Length: > 0 } title)
            {
                continue;
            }

            companies.Add(new CompanyRef(cik, ticker, title));
        }

        if (companies.Count == 0)
        {
            throw Invalid("The SEC ticker list contained no valid companies.");
        }

        return companies;
    }

    /// <summary>Reads only the us-gaap concepts in <see cref="ConceptMap.All"/>, across all reported units.</summary>
    public static ParsedFacts ParseFacts(byte[] json)
    {
        using var document = Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw Invalid("The SEC company facts were not a JSON object.");
        }

        var facts = new List<RawFact>();
        var skipped = 0;

        if (root.TryGetProperty("facts", out var factsElement)
            && factsElement.ValueKind == JsonValueKind.Object
            && factsElement.TryGetProperty("us-gaap", out var usGaap)
            && usGaap.ValueKind == JsonValueKind.Object)
        {
            foreach (var concept in usGaap.EnumerateObject())
            {
                if (!ConceptMap.All.Contains(concept.Name)
                    || concept.Value.ValueKind != JsonValueKind.Object
                    || !concept.Value.TryGetProperty("units", out var units)
                    || units.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var unit in units.EnumerateObject())
                {
                    if (unit.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var element in unit.Value.EnumerateArray())
                    {
                        if (TryParseFact(concept.Name, unit.Name, element) is { } fact)
                        {
                            facts.Add(fact);
                        }
                        else
                        {
                            skipped++;
                        }
                    }
                }
            }
        }

        return new ParsedFacts(GetString(root, "entityName"), facts, skipped);
    }

    private static RawFact? TryParseFact(string concept, string unit, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty("val", out var val)
            || val.ValueKind != JsonValueKind.Number
            || !val.TryGetDecimal(out var value)
            || !TryGetDate(element, "end", out var end)
            || !TryGetDate(element, "filed", out var filed)
            || GetString(element, "form") is not { Length: > 0 } form
            || GetString(element, "accn") is not { Length: > 0 } accession)
        {
            return null;
        }

        DateOnly? start = null;
        if (element.TryGetProperty("start", out var startElement))
        {
            if (startElement.ValueKind != JsonValueKind.String
                || !DateOnly.TryParseExact(startElement.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStart))
            {
                return null;
            }

            start = parsedStart;
        }

        return new RawFact(concept, unit, value, start, end, form, filed, accession);
    }

    private static JsonDocument Parse(byte[] json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new DataSourceException(DataSourceErrorKind.InvalidData, "The SEC response was not valid JSON.", ex);
        }
    }

    private static DataSourceException Invalid(string message) =>
        new(DataSourceErrorKind.InvalidData, message);

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool TryGetDate(JsonElement element, string name, out DateOnly date)
    {
        date = default;
        return GetString(element, name) is { } text
            && DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryGetCik(JsonElement entry, out long cik)
    {
        cik = 0;
        if (!entry.TryGetProperty("cik_str", out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt64(out cik) && cik > 0,
            JsonValueKind.String => long.TryParse(value.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out cik) && cik > 0,
            _ => false,
        };
    }
}
