namespace LoanEngine.Core;

/// <summary>Thrown when loan details break a business rule (for example a negative principal).</summary>
public sealed class InvalidLoanException : Exception
{
    public InvalidLoanException(string message) : base(message)
    {
    }

    public InvalidLoanException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
