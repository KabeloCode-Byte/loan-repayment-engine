using LoanEngine.Core;
using Xunit;

namespace LoanEngine.Tests;

public class ScheduleInvariantTests
{
    /// <summary>Rules that must hold for every kind of loan and every interest strategy.</summary>
    [Theory]
    [InlineData(LoanType.Amortising, false)]
    [InlineData(LoanType.Amortising, true)]
    [InlineData(LoanType.InterestOnly, false)]
    [InlineData(LoanType.InterestOnly, true)]
    [InlineData(LoanType.Balloon, false)]
    [InlineData(LoanType.Balloon, true)]
    public void Every_loan_type_produces_a_consistent_schedule(LoanType type, bool useActual365)
    {
        var request = new LoanRequest(750_000m, 0.1175m, 48, new DateOnly(2025, 3, 31))
        {
            BalloonPercentage = 0.25m,
            InterestStrategy = useActual365 ? new Actual365Interest() : new MonthlyRateInterest(),
        };

        Loan loan = LoanFactory.Create(type, request);
        RepaymentSchedule schedule = loan.GenerateSchedule();

        Assert.Equal(48, schedule.Instalments.Count);
        Assert.Equal(750_000m, schedule.TotalPrincipal);
        Assert.Equal(0m, schedule.Instalments[^1].ClosingBalance);

        decimal previousBalance = 750_000m;
        DateOnly previousDate = request.StartDate;

        for (int i = 0; i < schedule.Instalments.Count; i++)
        {
            Instalment row = schedule.Instalments[i];

            Assert.Equal(i + 1, row.Number);
            Assert.Equal(row.Interest + row.Principal, row.Payment);
            Assert.Equal(previousBalance - row.Principal, row.ClosingBalance);
            Assert.True(row.ClosingBalance >= 0m);
            Assert.True(row.DueDate > previousDate);

            previousBalance = row.ClosingBalance;
            previousDate = row.DueDate;
        }
    }

    [Fact]
    public void The_schedule_cannot_be_changed_by_the_caller()
    {
        var loan = new AmortisingLoan(10_000m, 0.10m, 6, new DateOnly(2025, 1, 1), new MonthlyRateInterest());
        RepaymentSchedule schedule = loan.GenerateSchedule();

        Assert.False(schedule.Instalments is List<Instalment>);
    }

    [Fact]
    public void Generating_the_schedule_twice_gives_the_same_result()
    {
        var loan = new BalloonLoan(10_000m, 0.10m, 6, new DateOnly(2025, 1, 1), new MonthlyRateInterest(), 0.2m);

        Assert.Equal(loan.GenerateSchedule().Instalments, loan.GenerateSchedule().Instalments);
    }
}

public class LoanComparerTests
{
    [Fact]
    public void Orders_mixed_loan_types_from_cheapest_to_most_expensive()
    {
        var request = new LoanRequest(500_000m, 0.115m, 60, new DateOnly(2025, 1, 15)) { BalloonPercentage = 0.30m };

        // Deliberately in the "wrong" order: the comparer must sort them.
        var loans = new List<Loan>
        {
            LoanFactory.Create(LoanType.InterestOnly, request),
            LoanFactory.Create(LoanType.Amortising, request),
            LoanFactory.Create(LoanType.Balloon, request),
        };

        IReadOnlyList<LoanSummary> result = LoanComparer.Compare(loans);

        Assert.Equal(3, result.Count);

        Assert.StartsWith("Amortising loan", result[0].Description);
        Assert.Equal(159_778.34m, result[0].TotalInterest);
        Assert.Equal(10_996.30m, result[0].FirstPayment);

        Assert.StartsWith("Balloon loan", result[1].Description);
        Assert.Equal(198_094.79m, result[1].TotalInterest);
        Assert.Equal(9_134.91m, result[1].FirstPayment);

        Assert.StartsWith("Interest-only loan", result[2].Description);
        Assert.Equal(287_500.20m, result[2].TotalInterest);
        Assert.Equal(4_791.67m, result[2].FirstPayment);
    }

    [Fact]
    public void Rejects_a_missing_list()
    {
        Assert.Throws<ArgumentNullException>(() => LoanComparer.Compare(null!));
    }
}

public class FormatterTests
{
    private static (Loan Loan, RepaymentSchedule Schedule) Sample()
    {
        var loan = new AmortisingLoan(100_000m, 0.12m, 12, new DateOnly(2025, 1, 15), new MonthlyRateInterest());
        return (loan, loan.GenerateSchedule());
    }

    [Fact]
    public void Csv_has_a_header_and_one_line_per_instalment()
    {
        (Loan loan, RepaymentSchedule schedule) = Sample();
        string[] lines = new CsvFormatter().Format(loan, schedule).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(13, lines.Length);
        Assert.Equal("Number,DueDate,Payment,Interest,Principal,ClosingBalance", lines[0]);
        Assert.Equal("1,2025-02-15,8884.88,1000.00,7884.88,92115.12", lines[1]);
    }

    [Fact]
    public void Table_shows_the_loan_description_and_totals()
    {
        (Loan loan, RepaymentSchedule schedule) = Sample();
        string table = new TableFormatter().Format(loan, schedule);

        Assert.Contains("Amortising loan: R100,000.00 at 12% over 12 months", table);
        Assert.Contains("Totals", table);
        Assert.Contains("106,618.53", table);
    }

    [Fact]
    public void Formatters_can_be_swapped_through_the_interface()
    {
        (Loan loan, RepaymentSchedule schedule) = Sample();
        var formatters = new List<IScheduleFormatter> { new TableFormatter(), new CsvFormatter() };

        foreach (IScheduleFormatter formatter in formatters)
        {
            Assert.False(string.IsNullOrWhiteSpace(formatter.Format(loan, schedule)));
        }
    }
}
