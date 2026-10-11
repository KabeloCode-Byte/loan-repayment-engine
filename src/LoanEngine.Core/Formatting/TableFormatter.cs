using System.Globalization;
using System.Text;

namespace LoanEngine.Core;

/// <summary>Plain-text table for the console.</summary>
public sealed class TableFormatter : IScheduleFormatter
{
    public string Format(Loan loan, RepaymentSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(loan);
        ArgumentNullException.ThrowIfNull(schedule);

        var text = new StringBuilder();
        text.AppendLine(loan.Describe());
        text.AppendLine($"{"No.",3}  {"Due date",-10}  {"Payment",14}  {"Interest",14}  {"Principal",14}  {"Balance",14}");
        text.AppendLine(new string('-', 80));

        foreach (Instalment row in schedule.Instalments)
        {
            text.AppendLine(
                $"{row.Number,3}  {row.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),-10}  " +
                $"{Number(row.Payment),14}  {Number(row.Interest),14}  {Number(row.Principal),14}  {Number(row.ClosingBalance),14}");
        }

        text.AppendLine(new string('-', 80));
        text.AppendLine(
            $"{"Totals",3}  {string.Empty,-10}  {Number(schedule.TotalPaid),14}  " +
            $"{Number(schedule.TotalInterest),14}  {Number(schedule.TotalPrincipal),14}");
        return text.ToString();
    }

    private static string Number(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
