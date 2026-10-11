using System.Globalization;

namespace LoanEngine.Core;

/// <summary>
/// Small helper so every rand amount is rounded and displayed the same way.
/// Amounts are held in <see cref="decimal"/> (never double) to avoid binary rounding errors.
/// </summary>
internal static class Money
{
    public static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static string Format(decimal value) =>
        "R" + value.ToString("N2", CultureInfo.InvariantCulture);
}
