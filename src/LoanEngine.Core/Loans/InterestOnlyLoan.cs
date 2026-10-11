namespace LoanEngine.Core;

/// <summary>Only interest is paid each month. The whole principal is repaid with the final instalment.</summary>
public sealed class InterestOnlyLoan : Loan
{
    public InterestOnlyLoan(
        decimal principal,
        decimal annualInterestRate,
        int termMonths,
        DateOnly startDate,
        IInterestStrategy interestStrategy)
        : base(principal, annualInterestRate, termMonths, startDate, interestStrategy)
    {
    }

    public override string ProductName => "Interest-only loan";

    /// <summary>The monthly interest on the full principal. Indicative: the real figure depends on the interest strategy.</summary>
    public override decimal RegularPayment => Money.Round(Principal * AnnualInterestRate / 12m);

    protected override decimal PrincipalForInstalment(decimal regularPayment, decimal interest) => 0m;
}
