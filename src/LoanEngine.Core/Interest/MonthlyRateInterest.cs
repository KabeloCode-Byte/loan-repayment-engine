namespace LoanEngine.Core;

/// <summary>Every month earns one twelfth of the annual rate, whatever the length of the month.</summary>
public sealed class MonthlyRateInterest : IInterestStrategy
{
    public string Name => "monthly rate";

    public decimal CalculateInterest(decimal balance, decimal annualRate, DateOnly periodStart, DateOnly periodEnd) =>
        balance * annualRate / 12m;
}
