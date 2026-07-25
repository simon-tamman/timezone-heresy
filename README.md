# Mercury Estimate

A deliberately awkward .NET CLI that takes an estimate expressed as an Earth range and converts only its upper bound into the corresponding Mercury unit.

Examples:

```text
3-6 days   -> 3 Earth days to 0.0341 Mercury solar days
3-6 weeks  -> 3 Earth weeks to 0.0341 Mercury weeks
3-6 months -> 3 Earth months to 24.9116 Mercury orbital months
3-6 years  -> 3 Earth years to 24.9116 Mercury orbital years
```

## Definitions

The application uses these explicit definitions:

- **Earth day:** 24 hours.
- **Mercury solar day:** 175.942 Earth days.
- **Mercury week:** 7 Mercury solar days (≈ 1,231.6 Earth days). Invented by this tool.
- **Earth mean year:** 365.2425 Earth days.
- **Mercury orbital year:** 87.9691 Earth days.
- **Planetary month:** one twelfth of the relevant planet's year.

Mercury has no standard civil calendar, so “Mercury week” and “Mercury month” are arithmetic conventions invented by this tool. The CLI labels months as **Mercury orbital month** to make that visible.

The intentionally awkward result is real: one Mercury solar day is approximately two Mercury orbital years.

## Run

```bash
dotnet run --project MercuryEstimate.csproj -- "3-6 days"
```

You can also omit the estimate and enter it interactively:

```bash
dotnet run --project MercuryEstimate.csproj
```

Accepted input forms include:

```text
3-6 days
1-2 weeks
2 to 5 months
1–2 years
1.5-3.25 months
```

## Options

```bash
# Explain the conversion definitions
dotnet run -- "3-6 days" --explain

# Machine-readable output
dotnet run -- "3-6 days" --json

# Usage information
dotnet run -- --help
```

## Disclosure

Written by a bot.
