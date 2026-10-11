using LoanEngine.Core;
using Xunit;

namespace LoanEngine.Tests;

public class ValidationTests
{
    private static readonly DateOnly Start = new(2025, 1, 15);
    private static readonly IInterestStrategy Strategy = new MonthlyRateInterest();

    [Fact]
    public void Rejects_a_principal_that_is_not_positive_or_too_large()
    {
        foreach (decimal bad in new[] { 0m, -1m, Loan.MaxPrincipal + 1m })
        {
            Assert.Throws<InvalidLoanException>(() => new AmortisingLoan(bad, 0.10m, 12, Start, Strategy));
        }
    }

    [Fact]
    public void Rejects_a_principal_with_fractions_of_a_cent()
    {
        Assert.Throws<InvalidLoanException>(() => new AmortisingLoan(100.005m, 0.10m, 12, Start, Strategy));
    }

    [Fact]
    public void Rejects_an_interest_rate_outside_zero_to_one()
    {
        foreach (decimal bad in new[] { -0.01m, 1.01m })
        {
            Assert.Throws<InvalidLoanException>(() => new AmortisingLoan(1_000m, bad, 12, Start, Strategy));
        }
    }

    [Fact]
    public void Rejects_a_term_outside_the_allowed_range()
    {
        foreach (int bad in new[] { 0, -5, Loan.MaxTermMonths + 1 })
        {
            Assert.Throws<InvalidLoanException>(() => new InterestOnlyLoan(1_000m, 0.10m, bad, Start, Strategy));
        }
    }

    [Fact]
    public void Rejects_a_missing_interest_strategy()
    {
        Assert.Throws<ArgumentNullException>(() => new AmortisingLoan(1_000m, 0.10m, 12, Start, null!));
    }

    [Fact]
    public void Rejects_a_balloon_percentage_outside_zero_to_one()
    {
        foreach (decimal bad in new[] { 0m, -0.1m, 1m, 1.5m })
        {
            Assert.Throws<InvalidLoanException>(() => new BalloonLoan(1_000m, 0.10m, 12, Start, Strategy, bad));
        }
    }
}

public class LoanFactoryTests
{
    private static readonly DateOnly Start = new(2025, 1, 15);

    private static LoanRequest Request() => new(100_000m, 0.12m, 12, Start) { BalloonPercentage = 0.30m };

    [Fact]
    public void Creates_the_class_that_matches_the_requested_type()
    {
        Assert.IsType<AmortisingLoan>(LoanFactory.Create(LoanType.Amortising, Request()));
        Assert.IsType<InterestOnlyLoan>(LoanFactory.Create(LoanType.InterestOnly, Request()));
        Assert.IsType<BalloonLoan>(LoanFactory.Create(LoanType.Balloon, Request()));
    }

    [Fact]
    public void Uses_the_monthly_rate_strategy_when_none_is_given()
    {
        Loan loan = LoanFactory.Create(LoanType.Amortising, Request());

        Assert.IsType<MonthlyRateInterest>(loan.InterestStrategy);
    }

    [Fact]
    public void Keeps_the_strategy_that_was_requested()
    {
        LoanRequest request = Request() with { InterestStrategy = new Actual365Interest() };
        Loan loan = LoanFactory.Create(LoanType.Amortising, request);

        Assert.IsType<Actual365Interest>(loan.InterestStrategy);
    }

    [Fact]
    public void A_balloon_loan_needs_a_balloon_percentage()
    {
        LoanRequest request = Request() with { BalloonPercentage = null };

        Assert.Throws<InvalidLoanException>(() => LoanFactory.Create(LoanType.Balloon, request));
    }

    [Fact]
    public void Business_rule_errors_still_surface_through_the_factory()
    {
        LoanRequest request = Request() with { Principal = -5m };

        Assert.Throws<InvalidLoanException>(() => LoanFactory.Create(LoanType.Amortising, request));
    }
}
