using LoanEngine.Core;
using Xunit;

namespace LoanEngine.Tests;

public class AmortisingLoanTests
{
    private static readonly DateOnly Start = new(2025, 1, 15);

    private static AmortisingLoan StandardLoan() =>
        new(100_000m, 0.12m, 12, Start, new MonthlyRateInterest());

    [Fact]
    public void Regular_payment_matches_the_standard_annuity_figure()
    {
        // R100,000 at 12% over 12 months is a well-known R8,884.88 a month.
        Assert.Equal(8_884.88m, StandardLoan().RegularPayment);
    }

    [Fact]
    public void First_instalment_splits_into_interest_and_principal()
    {
        Instalment first = StandardLoan().GenerateSchedule().Instalments[0];

        Assert.Equal(1, first.Number);
        Assert.Equal(new DateOnly(2025, 2, 15), first.DueDate);
        Assert.Equal(8_884.88m, first.Payment);
        Assert.Equal(1_000.00m, first.Interest);       // 1% of 100,000
        Assert.Equal(7_884.88m, first.Principal);
        Assert.Equal(92_115.12m, first.ClosingBalance);
    }

    [Fact]
    public void Final_instalment_clears_the_remaining_balance_exactly()
    {
        RepaymentSchedule schedule = StandardLoan().GenerateSchedule();
        Instalment last = schedule.Instalments[^1];

        Assert.Equal(12, schedule.Instalments.Count);
        Assert.Equal(0m, last.ClosingBalance);
        Assert.Equal(8_884.85m, last.Payment);          // a few cents less, absorbing the rounding
    }

    [Fact]
    public void Totals_add_up()
    {
        RepaymentSchedule schedule = StandardLoan().GenerateSchedule();

        Assert.Equal(100_000m, schedule.TotalPrincipal);
        Assert.Equal(6_618.53m, schedule.TotalInterest);
        Assert.Equal(106_618.53m, schedule.TotalPaid);
    }

    [Fact]
    public void Zero_interest_loan_splits_the_principal_evenly()
    {
        var loan = new AmortisingLoan(12_000m, 0m, 12, Start, new MonthlyRateInterest());
        RepaymentSchedule schedule = loan.GenerateSchedule();

        Assert.Equal(1_000.00m, loan.RegularPayment);
        Assert.Equal(0m, schedule.TotalInterest);
        Assert.Equal(12_000m, schedule.TotalPaid);
    }

    [Fact]
    public void Describe_summarises_the_loan()
    {
        Assert.Equal(
            "Amortising loan: R100,000.00 at 12% over 12 months (monthly rate)",
            StandardLoan().Describe());
    }
}
