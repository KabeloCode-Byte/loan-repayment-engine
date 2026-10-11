namespace LoanEngine.Core;

/// <summary>
/// Equal monthly instalments. Early instalments are mostly interest, later ones mostly principal.
/// </summary>
public class AmortisingLoan : Loan
{
    public AmortisingLoan(
        decimal principal,
        decimal annualInterestRate,
        int termMonths,
        DateOnly startDate,
        IInterestStrategy interestStrategy)
        : base(principal, annualInterestRate, termMonths, startDate, interestStrategy)
    {
    }

    public override string ProductName => "Amortising loan";

    public override decimal RegularPayment
    {
        get
        {
            decimal monthlyRate = AnnualInterestRate / 12m;

            if (monthlyRate == 0m)
            {
                return Money.Round((Principal - BalloonAmount) / TermMonths);
            }

            // Standard annuity formula, adjusted for a lump sum due at the end:
            //   payment = (P - B / g) * r / (1 - 1 / g)   where g = (1 + r)^n
            decimal growth = Power(1m + monthlyRate, TermMonths);
            decimal discount = 1m / growth;
            return Money.Round((Principal - BalloonAmount * discount) * monthlyRate / (1m - discount));
        }
    }

    /// <summary>
    /// Lump sum left to pay with the final instalment. Zero here; <see cref="BalloonLoan"/> overrides it,
    /// which lets it reuse every other line of this class.
    /// </summary>
    protected virtual decimal BalloonAmount => 0m;

    protected override decimal PrincipalForInstalment(decimal regularPayment, decimal interest) =>
        regularPayment - interest;

    private static decimal Power(decimal value, int exponent)
    {
        decimal result = 1m;
        for (int i = 0; i < exponent; i++)
        {
            result *= value;
        }

        return result;
    }
}
