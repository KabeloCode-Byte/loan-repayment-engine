namespace LoanEngine.Core;

public enum LoanType
{
    Amortising,
    InterestOnly,
    Balloon,
}

/// <summary>Everything needed to create a loan. Optional settings are only used by the loan types that need them.</summary>
public sealed record LoanRequest(decimal Principal, decimal AnnualInterestRate, int TermMonths, DateOnly StartDate)
{
    /// <summary>Required for <see cref="LoanType.Balloon"/>: 0.30 means 30% of the principal is due at the end.</summary>
    public decimal? BalloonPercentage { get; init; }

    /// <summary>Defaults to <see cref="MonthlyRateInterest"/> when not set.</summary>
    public IInterestStrategy? InterestStrategy { get; init; }
}

/// <summary>
/// Factory pattern: one place decides which concrete class to build, so calling code
/// can ask for a loan by type and receive the abstract <see cref="Loan"/>.
/// </summary>
public static class LoanFactory
{
    public static Loan Create(LoanType type, LoanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        IInterestStrategy strategy = request.InterestStrategy ?? new MonthlyRateInterest();

        return type switch
        {
            LoanType.Amortising => new AmortisingLoan(
                request.Principal, request.AnnualInterestRate, request.TermMonths, request.StartDate, strategy),

            LoanType.InterestOnly => new InterestOnlyLoan(
                request.Principal, request.AnnualInterestRate, request.TermMonths, request.StartDate, strategy),

            LoanType.Balloon => new BalloonLoan(
                request.Principal, request.AnnualInterestRate, request.TermMonths, request.StartDate, strategy,
                request.BalloonPercentage
                    ?? throw new InvalidLoanException("A balloon loan needs a BalloonPercentage.")),

            _ => throw new InvalidLoanException($"Unknown loan type: {type}."),
        };
    }
}
