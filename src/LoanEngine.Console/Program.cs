using System.Globalization;
using LoanEngine.Core;

// Usage: dotnet run --project src/LoanEngine.Console -- [principal] [annualRatePercent] [months] [--csv file.csv]
// Example: dotnet run --project src/LoanEngine.Console -- 750000 11.5 60 --csv schedule.csv

const decimal DefaultPrincipal = 500_000m;
const decimal DefaultRatePercent = 11.5m;
const int DefaultMonths = 60;
const decimal BalloonPercentage = 0.30m;

string? csvPath = null;
var positional = new List<string>();

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--csv")
    {
        if (i + 1 >= args.Length)
        {
            return Fail("--csv needs a file name.");
        }

        csvPath = args[++i];
    }
    else
    {
        positional.Add(args[i]);
    }
}

if (!TryRead(positional, 0, DefaultPrincipal, out decimal principal)
    || !TryRead(positional, 1, DefaultRatePercent, out decimal ratePercent)
    || !TryRead(positional, 2, DefaultMonths, out int months))
{
    return Fail("Principal, rate and months must be numbers, for example: 750000 11.5 60");
}

try
{
    var request = new LoanRequest(principal, ratePercent / 100m, months, DateOnly.FromDateTime(DateTime.Today))
    {
        BalloonPercentage = BalloonPercentage,
    };

    // One list holding different kinds of loan: polymorphism.
    var options = new List<Loan>
    {
        LoanFactory.Create(LoanType.Amortising, request),
        LoanFactory.Create(LoanType.Balloon, request),
        LoanFactory.Create(LoanType.InterestOnly, request),
    };

    Console.WriteLine("Loan options compared (cheapest first)");
    Console.WriteLine(new string('=', 100));
    foreach (LoanSummary summary in LoanComparer.Compare(options))
    {
        Console.WriteLine(summary.Description);
        Console.WriteLine(
            $"    first payment R{summary.FirstPayment:N2}   total interest R{summary.TotalInterest:N2}   total paid R{summary.TotalPaid:N2}");
    }

    Loan chosen = options[0];
    Console.WriteLine();
    Console.Write(new TableFormatter().Format(chosen, chosen.GenerateSchedule()));

    if (csvPath is not null)
    {
        File.WriteAllText(csvPath, new CsvFormatter().Format(chosen, chosen.GenerateSchedule()));
        Console.WriteLine($"Schedule saved to {csvPath}");
    }

    return 0;
}
catch (InvalidLoanException error)
{
    return Fail(error.Message);
}

static bool TryRead<T>(List<string> values, int index, T fallback, out T result) where T : IParsable<T>
{
    if (index >= values.Count)
    {
        result = fallback;
        return true;
    }

    return T.TryParse(values[index], CultureInfo.InvariantCulture, out result!);
}

static int Fail(string message)
{
    Console.Error.WriteLine($"Error: {message}");
    return 1;
}
