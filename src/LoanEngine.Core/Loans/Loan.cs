using System.Globalization;

namespace LoanEngine.Core;

/// <summary>
/// Abstract base class for every kind of loan.
///
/// Abstraction:   callers work with <see cref="Loan"/> and never need to know the concrete type.
/// Encapsulation: all state is set once in the constructor, after validation, and exposed read-only.
/// Template Method: <see cref="GenerateSchedule"/> owns the repayment loop; subclasses only supply
///                  the parts that differ (<see cref="RegularPayment"/> and <see cref="PrincipalForInstalment"/>).
/// Strategy:      interest is delegated to an injected <see cref="IInterestStrategy"/>.
/// </summary>
public abstract class Loan
{
    public const decimal MaxPrincipal = 1_000_000_000_000m;
    public const int MaxTermMonths = 600;

    protected Loan(
        decimal principal,
        decimal annualInterestRate,
        int termMonths,
        DateOnly startDate,
        IInterestStrategy interestStrategy)
    {
        if (principal <= 0m || principal > MaxPrincipal)
        {
            throw new InvalidLoanException(
                $"Principal must be greater than 0 and at most {Money.Format(MaxPrincipal)}.");
        }

        if (Money.Round(principal) != principal)
        {
            throw new InvalidLoanException("Principal cannot have more than two decimal places.");
        }

        if (annualInterestRate < 0m || annualInterestRate > 1m)
        {
            throw new InvalidLoanException(
                "Annual interest rate must be between 0 and 1 (for example 0.115 for 11.5%).");
        }

        if (termMonths < 1 || termMonths > MaxTermMonths)
        {
            throw new InvalidLoanException($"Term must be between 1 and {MaxTermMonths} months.");
        }

        Principal = principal;
        AnnualInterestRate = annualInterestRate;
        TermMonths = termMonths;
        StartDate = startDate;
        InterestStrategy = interestStrategy ?? throw new ArgumentNullException(nameof(interestStrategy));
    }

    public decimal Principal { get; }

    /// <summary>Annual rate as a fraction: 0.115 means 11.5%.</summary>
    public decimal AnnualInterestRate { get; }

    public int TermMonths { get; }

    public DateOnly StartDate { get; }

    public IInterestStrategy InterestStrategy { get; }

    /// <summary>Name of the loan product, for example "Amortising loan".</summary>
    public abstract string ProductName { get; }

    /// <summary>The usual instalment. For loans with a final lump sum, the last payment is larger.</summary>
    public abstract decimal RegularPayment { get; }

    /// <summary>
    /// How much of an instalment goes to the principal.
    /// The final instalment is handled by the base class, which always clears the remaining balance.
    /// </summary>
    protected abstract decimal PrincipalForInstalment(decimal regularPayment, decimal interest);

    /// <summary>Builds the full repayment schedule. Same steps for every loan type.</summary>
    public RepaymentSchedule GenerateSchedule()
    {
        var instalments = new List<Instalment>(TermMonths);
        decimal balance = Principal;
        decimal regularPayment = RegularPayment;
        DateOnly previousDue = StartDate;

        for (int number = 1; number <= TermMonths; number++)
        {
            // Counting from the start date each time avoids drift (31 Jan, 28 Feb, 31 Mar ...).
            DateOnly dueDate = StartDate.AddMonths(number);

            decimal interest = Money.Round(
                InterestStrategy.CalculateInterest(balance, AnnualInterestRate, previousDue, dueDate));

            decimal principalPaid = number == TermMonths
                ? balance
                : Math.Clamp(PrincipalForInstalment(regularPayment, interest), 0m, balance);

            balance -= principalPaid;
            instalments.Add(new Instalment(number, dueDate, interest + principalPaid, interest, principalPaid, balance));
            previousDue = dueDate;
        }

        return new RepaymentSchedule(instalments);
    }

    /// <summary>One-line description. Subclasses can extend it by overriding and calling the base version.</summary>
    public virtual string Describe()
    {
        string rate = (AnnualInterestRate * 100m).ToString("0.##", CultureInfo.InvariantCulture);
        return $"{ProductName}: {Money.Format(Principal)} at {rate}% over {TermMonths} months ({InterestStrategy.Name})";
    }

    public override string ToString() => Describe();
}
