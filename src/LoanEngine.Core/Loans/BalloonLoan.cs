namespace LoanEngine.Core;

/// <summary>
/// Smaller monthly instalments, with a percentage of the principal due as a lump sum at the end.
/// Inherits almost everything from <see cref="AmortisingLoan"/> and only supplies the balloon.
/// </summary>
public sealed class BalloonLoan : AmortisingLoan
{
    private readonly decimal _balloonPercentage;

    public BalloonLoan(
        decimal principal,
        decimal annualInterestRate,
        int termMonths,
        DateOnly startDate,
        IInterestStrategy interestStrategy,
        decimal balloonPercentage)
        : base(principal, annualInterestRate, termMonths, startDate, interestStrategy)
    {
        if (balloonPercentage <= 0m || balloonPercentage >= 1m)
        {
            throw new InvalidLoanException(
                "Balloon percentage must be greater than 0 and less than 1 (for example 0.30 for 30%).");
        }

        _balloonPercentage = balloonPercentage;
    }

    /// <summary>The balloon as a fraction of the principal: 0.30 means 30%.</summary>
    public decimal BalloonPercentage => _balloonPercentage;

    public override string ProductName => "Balloon loan";

    protected override decimal BalloonAmount => Money.Round(Principal * _balloonPercentage);

    public override string Describe() =>
        $"{base.Describe()}, balloon {(_balloonPercentage * 100m):0.##}% ({Money.Format(BalloonAmount)})";
}
