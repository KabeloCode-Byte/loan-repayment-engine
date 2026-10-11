namespace LoanEngine.Core;

/// <summary>
/// Strategy pattern: how interest builds up over one repayment period.
/// A loan is given a strategy and never needs to know which one it has.
/// </summary>
public interface IInterestStrategy
{
    /// <summary>Short name shown in descriptions and reports.</summary>
    string Name { get; }

    /// <summary>Interest for one period, before rounding to cents.</summary>
    /// <param name="balance">Outstanding principal at the start of the period.</param>
    /// <param name="annualRate">Annual rate as a fraction (0.115 means 11.5%).</param>
    /// <param name="periodStart">Previous due date, or the loan start date for the first period.</param>
    /// <param name="periodEnd">Due date of this instalment.</param>
    decimal CalculateInterest(decimal balance, decimal annualRate, DateOnly periodStart, DateOnly periodEnd);
}
