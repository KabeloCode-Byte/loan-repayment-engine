namespace LoanEngine.Core;

/// <summary>Turns a schedule into text. Add a new output format by adding a new class, with no changes elsewhere.</summary>
public interface IScheduleFormatter
{
    string Format(Loan loan, RepaymentSchedule schedule);
}
