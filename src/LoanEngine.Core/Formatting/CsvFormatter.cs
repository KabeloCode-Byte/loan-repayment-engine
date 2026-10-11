using System.Globalization;
using System.Text;

namespace LoanEngine.Core;

/// <summary>Comma-separated output that opens in Excel or Power BI. Amounts use a full stop as decimal separator.</summary>
public sealed class CsvFormatter : IScheduleFormatter
{
    public string Format(Loan loan, RepaymentSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(loan);
        ArgumentNullException.ThrowIfNull(schedule);

        var text = new StringBuilder();
        text.AppendLine("Number,DueDate,Payment,Interest,Principal,ClosingBalance");

        foreach (Instalment row in schedule.Instalments)
        {
            text.AppendLine(string.Join(
                ",",
                row.Number.ToString(CultureInfo.InvariantCulture),
                row.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Amount(row.Payment),
                Amount(row.Interest),
                Amount(row.Principal),
                Amount(row.ClosingBalance)));
        }

        return text.ToString();
    }

    private static string Amount(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);
}
