namespace LoanEngine.Core;

/// <summary>
/// The full list of instalments for a loan, plus totals.
/// Encapsulation: the list is copied on the way in and exposed read-only, so nobody can alter it afterwards.
/// </summary>
public sealed class RepaymentSchedule
{
    public RepaymentSchedule(IEnumerable<Instalment> instalments)
    {
        ArgumentNullException.ThrowIfNull(instalments);
        Instalments = instalments.ToList().AsReadOnly();
    }

    public IReadOnlyList<Instalment> Instalments { get; }

    public decimal TotalPaid => Instalments.Sum(i => i.Payment);

    public decimal TotalInterest => Instalments.Sum(i => i.Interest);

    public decimal TotalPrincipal => Instalments.Sum(i => i.Principal);
}
