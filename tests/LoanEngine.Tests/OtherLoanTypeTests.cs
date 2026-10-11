using LoanEngine.Core;
using Xunit;

namespace LoanEngine.Tests;

public class InterestOnlyLoanTests
{
    private static readonly DateOnly Start = new(2025, 1, 15);

    [Fact]
    public void Pays_only_interest_until_the_final_instalment()
    {
        var loan = new InterestOnlyLoan(100_000m, 0.12m, 12, Start, new MonthlyRateInterest());
        RepaymentSchedule schedule = loan.GenerateSchedule();

        for (int i = 0; i < 11; i++)
        {
            Assert.Equal(1_000.00m, schedule.Instalments[i].Payment);
            Assert.Equal(0m, schedule.Instalments[i].Principal);
            Assert.Equal(100_000m, schedule.Instalments[i].ClosingBalance);
        }

        Instalment last = schedule.Instalments[^1];
        Assert.Equal(101_000.00m, last.Payment);
        Assert.Equal(100_000m, last.Principal);
        Assert.Equal(0m, last.ClosingBalance);
        Assert.Equal(12_000.00m, schedule.TotalInterest);
    }
}

public class BalloonLoanTests
{
    private static readonly DateOnly Start = new(2025, 1, 15);

    private static BalloonLoan ThirtyPercentBalloon() =>
        new(100_000m, 0.12m, 12, Start, new MonthlyRateInterest(), 0.30m);

    [Fact]
    public void Monthly_payment_is_lower_than_a_fully_amortising_loan()
    {
        var plain = new AmortisingLoan(100_000m, 0.12m, 12, Start, new MonthlyRateInterest());

        Assert.Equal(6_519.42m, ThirtyPercentBalloon().RegularPayment);
        Assert.True(ThirtyPercentBalloon().RegularPayment < plain.RegularPayment);
    }

    [Fact]
    public void Final_instalment_includes_the_balloon()
    {
        RepaymentSchedule schedule = ThirtyPercentBalloon().GenerateSchedule();
        Instalment beforeLast = schedule.Instalments[^2];
        Instalment last = schedule.Instalments[^1];

        Assert.Equal(36_157.78m, beforeLast.ClosingBalance);   // what is still owed going into the last month
        Assert.Equal(36_519.36m, last.Payment);
        Assert.Equal(0m, last.ClosingBalance);
        Assert.Equal(8_232.98m, schedule.TotalInterest);
    }

    [Fact]
    public void Describe_extends_the_base_description()
    {
        Assert.Equal(
            "Balloon loan: R100,000.00 at 12% over 12 months (monthly rate), balloon 30% (R30,000.00)",
            ThirtyPercentBalloon().Describe());
    }

    [Fact]
    public void A_balloon_loan_can_be_used_wherever_an_amortising_loan_is_expected()
    {
        AmortisingLoan asParent = ThirtyPercentBalloon();
        Loan asBase = ThirtyPercentBalloon();

        Assert.Equal(asParent.RegularPayment, asBase.RegularPayment);
        Assert.Equal(0.30m, ThirtyPercentBalloon().BalloonPercentage);
    }
}

public class InterestStrategyTests
{
    [Fact]
    public void Actual365_uses_the_real_number_of_days_in_each_period()
    {
        // Starting on 31 January, the first due date is 28 February: 28 days.
        var start = new DateOnly(2025, 1, 31);
        var loan = new AmortisingLoan(100_000m, 0.12m, 12, start, new Actual365Interest());
        RepaymentSchedule schedule = loan.GenerateSchedule();

        Assert.Equal(new DateOnly(2025, 2, 28), schedule.Instalments[0].DueDate);
        Assert.Equal(920.55m, schedule.Instalments[0].Interest);      // 100,000 x 12% x 28 / 365
        Assert.Equal(new DateOnly(2026, 1, 31), schedule.Instalments[^1].DueDate);
        Assert.Equal(6_567.24m, schedule.TotalInterest);
        Assert.Equal(0m, schedule.Instalments[^1].ClosingBalance);
    }

    [Fact]
    public void Different_strategies_give_different_costs_for_the_same_loan()
    {
        var start = new DateOnly(2025, 1, 31);
        var monthly = new AmortisingLoan(100_000m, 0.12m, 12, start, new MonthlyRateInterest());
        var actual = new AmortisingLoan(100_000m, 0.12m, 12, start, new Actual365Interest());

        Assert.True(actual.GenerateSchedule().TotalInterest < monthly.GenerateSchedule().TotalInterest);
    }

    [Fact]
    public void Due_dates_follow_the_start_date_without_drifting()
    {
        var loan = new InterestOnlyLoan(10_000m, 0.10m, 4, new DateOnly(2025, 1, 31), new MonthlyRateInterest());
        DateOnly[] dates = loan.GenerateSchedule().Instalments.Select(i => i.DueDate).ToArray();

        Assert.Equal(new DateOnly(2025, 2, 28), dates[0]);
        Assert.Equal(new DateOnly(2025, 3, 31), dates[1]);   // back to the 31st, not stuck on the 28th
        Assert.Equal(new DateOnly(2025, 4, 30), dates[2]);
        Assert.Equal(new DateOnly(2025, 5, 31), dates[3]);
    }
}
