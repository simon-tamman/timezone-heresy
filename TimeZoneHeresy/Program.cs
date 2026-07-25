using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

const decimal EarthMeanYearInDays = 365.2425m;
const decimal MercuryOrbitalYearInEarthDays = 87.9691m;
const decimal MercurySolarDayInEarthDays = 175.942m;
const decimal MercuryWeekInEarthDays = 7m * MercurySolarDayInEarthDays; // invented: 7 Mercury solar days

var options = CliOptions.Parse(args);

if (options.ShowHelp)
{
    PrintHelp();
    return 0;
}

string input;

if (!string.IsNullOrWhiteSpace(options.RangeText))
{
    input = options.RangeText;
}
else
{
    Console.Write("Enter an estimate, for example '3-6 days': ");
    input = Console.ReadLine() ?? string.Empty;
}

if (!EstimateRange.TryParse(input, out var estimate, out var error))
{
    Console.Error.WriteLine($"Error: {error}");
    Console.Error.WriteLine("Expected a range such as: 3-6 days, 1-2 weeks, 2 to 5 months, or 1–2 years.");
    return 1;
}

var result = ConvertEstimate(estimate!);

if (options.Json)
{
    var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    Console.WriteLine(json);
    return 0;
}

Console.WriteLine(result.Display);

if (options.Explain)
{
    Console.WriteLine();
    Console.WriteLine("Conversion basis:");
    Console.WriteLine($"- 1 Mercury solar day = {MercurySolarDayInEarthDays.ToString("0.###", CultureInfo.InvariantCulture)} Earth days");
    Console.WriteLine($"- 1 Earth mean year = {EarthMeanYearInDays.ToString("0.####", CultureInfo.InvariantCulture)} Earth days");
    Console.WriteLine($"- 1 Mercury orbital year = {MercuryOrbitalYearInEarthDays.ToString("0.####", CultureInfo.InvariantCulture)} Earth days");
    Console.WriteLine("- 1 planetary month = one twelfth of that planet's year");
    Console.WriteLine();
    Console.WriteLine("Note: Mercury has no standard civil calendar. 'Mercury month' and 'Mercury week' are explicit arithmetic subdivisions invented by this tool.");
}

return 0;

ConversionResult ConvertEstimate(EstimateRange range)
{
    var mercuryMaximum = range.Unit switch
    {
        EstimateUnit.Day => range.Maximum / MercurySolarDayInEarthDays,
        EstimateUnit.Week => range.Maximum * 7m / MercuryWeekInEarthDays,
        EstimateUnit.Month =>
            range.Maximum * (EarthMeanYearInDays / 12m) /
            (MercuryOrbitalYearInEarthDays / 12m),
        EstimateUnit.Year =>
            range.Maximum * EarthMeanYearInDays /
            MercuryOrbitalYearInEarthDays,
        _ => throw new ArgumentOutOfRangeException(nameof(range.Unit))
    };

    var earthUnit = range.Unit switch
    {
        EstimateUnit.Day => Pluralize(range.Minimum, "Earth day", "Earth days"),
        EstimateUnit.Week => Pluralize(range.Minimum, "Earth week", "Earth weeks"),
        EstimateUnit.Month => Pluralize(range.Minimum, "Earth month", "Earth months"),
        EstimateUnit.Year => Pluralize(range.Minimum, "Earth year", "Earth years"),
        _ => throw new ArgumentOutOfRangeException(nameof(range.Unit))
    };

    var mercuryUnit = range.Unit switch
    {
        EstimateUnit.Day => Pluralize(mercuryMaximum, "Mercury solar day", "Mercury solar days"),
        EstimateUnit.Week => Pluralize(mercuryMaximum, "Mercury week", "Mercury weeks"),
        EstimateUnit.Month => Pluralize(mercuryMaximum, "Mercury orbital month", "Mercury orbital months"),
        EstimateUnit.Year => Pluralize(mercuryMaximum, "Mercury orbital year", "Mercury orbital years"),
        _ => throw new ArgumentOutOfRangeException(nameof(range.Unit))
    };

    var minimumText = FormatNumber(range.Minimum);
    var maximumText = FormatNumber(mercuryMaximum);

    return new ConversionResult(
        Input: range.Original,
        EarthMinimum: range.Minimum,
        EarthUnit: range.Unit.ToString().ToLowerInvariant(),
        MercuryMaximum: mercuryMaximum,
        MercuryUnit: MercuryUnitName(range.Unit),
        Display: $"{minimumText} {earthUnit} to {maximumText} {mercuryUnit}");
}

static string MercuryUnitName(EstimateUnit unit) => unit switch
{
    EstimateUnit.Day => "solar-day",
    EstimateUnit.Week => "week",
    EstimateUnit.Month => "orbital-month",
    EstimateUnit.Year => "orbital-year",
    _ => throw new ArgumentOutOfRangeException(nameof(unit))
};

static string Pluralize(decimal value, string singular, string plural) =>
    value == 1m ? singular : plural;

static string FormatNumber(decimal value)
{
    if (value == decimal.Truncate(value))
    {
        return value.ToString("0", CultureInfo.InvariantCulture);
    }

    // Up to four decimal places, without noisy trailing zeroes.
    return value.ToString("0.####", CultureInfo.InvariantCulture);
}

static void PrintHelp()
{
    Console.WriteLine("Mercury Estimate");
    Console.WriteLine();
    Console.WriteLine("Converts the upper end of an Earth estimate range into Mercury units.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- \"3-6 days\"");
    Console.WriteLine("  dotnet run -- \"2 to 5 months\" --explain");
    Console.WriteLine("  dotnet run -- \"1–2 years\" --json");
    Console.WriteLine();
    Console.WriteLine("Accepted units: days, weeks, months, years");
    Console.WriteLine();
    Console.WriteLine("Definitions:");
    Console.WriteLine("  day   -> Mercury solar day");
    Console.WriteLine("  week  -> Mercury week (invented: 7 Mercury solar days)");
    Console.WriteLine("  month -> 1/12 of the relevant planet's orbital year");
    Console.WriteLine("  year  -> Mercury orbital year");
}

internal sealed record ConversionResult(
    string Input,
    decimal EarthMinimum,
    string EarthUnit,
    decimal MercuryMaximum,
    string MercuryUnit,
    string Display);

internal sealed record EstimateRange(
    decimal Minimum,
    decimal Maximum,
    EstimateUnit Unit,
    string Original)
{
    private static readonly Regex Pattern = new(
        @"^\s*(?<minimum>\d+(?:[.,]\d+)?)\s*(?:-|–|—|to)\s*(?<maximum>\d+(?:[.,]\d+)?)\s*(?<unit>days?|weeks?|months?|years?)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TryParse(
        string input,
        out EstimateRange? estimate,
        out string error)
    {
        estimate = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "The estimate cannot be empty.";
            return false;
        }

        var match = Pattern.Match(input);
        if (!match.Success)
        {
            error = "The estimate is not in a recognised range format.";
            return false;
        }

        if (!TryParseNumber(match.Groups["minimum"].Value, out var minimum) ||
            !TryParseNumber(match.Groups["maximum"].Value, out var maximum))
        {
            error = "One or both numbers are invalid.";
            return false;
        }

        if (minimum < 0m || maximum < 0m)
        {
            error = "Estimate values cannot be negative.";
            return false;
        }

        if (minimum > maximum)
        {
            error = "The start of the range cannot be greater than the end.";
            return false;
        }

        var unitText = match.Groups["unit"].Value.ToLowerInvariant();
        var unit = unitText switch
        {
            "day" or "days" => EstimateUnit.Day,
            "week" or "weeks" => EstimateUnit.Week,
            "month" or "months" => EstimateUnit.Month,
            "year" or "years" => EstimateUnit.Year,
            _ => throw new InvalidOperationException("The regex accepted an unsupported unit.")
        };

        estimate = new EstimateRange(minimum, maximum, unit, input.Trim());
        return true;
    }

    private static bool TryParseNumber(string text, out decimal value) =>
        decimal.TryParse(
            text.Replace(',', '.'),
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);
}

internal enum EstimateUnit
{
    Day,
    Week,
    Month,
    Year
}

internal sealed record CliOptions(
    string? RangeText,
    bool Explain,
    bool Json,
    bool ShowHelp)
{
    public static CliOptions Parse(string[] args)
    {
        var explain = false;
        var json = false;
        var showHelp = false;
        var rangeParts = new List<string>();

        foreach (var arg in args)
        {
            switch (arg)
            {
                case "--explain":
                    explain = true;
                    break;
                case "--json":
                    json = true;
                    break;
                case "--help":
                case "-h":
                    showHelp = true;
                    break;
                default:
                    rangeParts.Add(arg);
                    break;
            }
        }

        return new CliOptions(
            rangeParts.Count == 0 ? null : string.Join(' ', rangeParts),
            explain,
            json,
            showHelp);
    }
}
