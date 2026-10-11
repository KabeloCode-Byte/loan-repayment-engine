namespace LoanEngine.Core;

/// <summary>One line of a repayment schedule. A record, so it is immutable once created.</summary>
public sealed record Instalment(
    int Number,
    DateOnly DueDate,
    decimal Payment,
    decimal Interest,
    decimal Principal,
    decimal ClosingBalance);
