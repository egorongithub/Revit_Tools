using System;
using System.Globalization;

namespace SminexBimTools.Core
{
    public static class NumberFormatter
    {
        /// <summary>
        /// Форматирует число с разделителями групп и заданным числом знаков
        /// после запятой, убирая незначащие нули в конце.
        /// </summary>
        public static string Format(double value, int decimalPlaces)
        {
            decimalPlaces = Math.Max(0, Math.Min(6, decimalPlaces));
            CultureInfo culture = CultureInfo.CurrentCulture;

            string text = Math.Round(value, decimalPlaces).ToString("N" + decimalPlaces, culture);

            if (decimalPlaces > 0)
            {
                string separator = culture.NumberFormat.NumberDecimalSeparator;
                if (text.Contains(separator))
                    text = text.TrimEnd('0').TrimEnd(separator.ToCharArray());
            }

            return text;
        }
    }
}
