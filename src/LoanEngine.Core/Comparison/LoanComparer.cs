namespace LoanEngine.Core;

/// <summary>Headline figures for one loan option.</summary>
public sealed record LoanSummary(string Description, decimal FirstPayment, decimal TotalInterest, decimal TotalPaid);

/// <summary>
/// Polymorphism in action: this class takes any mix of loan types, calls the same methods on each,
/// and never checks which concrete class it is dealing with.
/// </summary>
public static class LoanComparer
{
    /// <summary>Summaries ordered from cheapest to most expensive in total interest.</summary>
    public static IReadOnlyList<LoanSummary> Compare(IEnumerable<Loan> loans)
    {
        ArgumentNullException.ThrowIfNull(loans);

        return loans
            .Select(loan =>
            {
                RepaymentSchedule schedule = loan.GenerateSchedule();
                return new LoanSummary(
                    loan.Describe(),
                    schedule.Instalments[0].Payment,
                    schedule.TotalInterest,
                    schedule.TotalPaid);
            })
            .OrderBy(summary => summary.TotalInterest)
            .ToList()
            .AsReadOnly();
    }
}
