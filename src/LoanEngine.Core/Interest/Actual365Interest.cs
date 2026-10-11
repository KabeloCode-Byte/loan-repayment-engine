namespace LoanEngine.Core;

/// <summary>Interest follows the real number of days in the period, using a 365-day year.</summary>
public sealed class Actual365Interest : IInterestStrategy
{
    public string Name => "actual/365";

    public decimal CalculateInterest(decimal balance, decimal annualRate, DateOnly periodStart, DateOnly periodEnd)
    {
        int days = periodEnd.DayNumber - periodStart.DayNumber;
        return balance * annualRate * days / 365m;
    }
}
