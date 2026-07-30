using System;
using System.Globalization;

namespace SminexBimTools.Core
{
    public static class NumberFormatter
    {
        /// <summary>
        /// Форматирует число с разделителями групп и заданным числом знаков
        /// после запятой, убирая незначащие нули в конце.
        /// При <paramref name="roundUp"/> последняя цифра округляется вверх,
        /// как в Revit (0,8325 → 0,833); иначе — к чётному (0,8325 → 0,832).
        /// </summary>
        public static string Format(double value, int decimalPlaces, bool roundUp = true)
        {
            decimalPlaces = Math.Max(0, Math.Min(6, decimalPlaces));
            CultureInfo culture = CultureInfo.CurrentCulture;

            MidpointRounding mode = roundUp ? MidpointRounding.AwayFromZero : MidpointRounding.ToEven;

            // После перевода из внутренних единиц Revit значение 0,8325 может
            // храниться как 0,83249999999999… — тогда правило половинки не
            // срабатывает и число уходит вниз. Предварительное округление до
            // 9 знаков убирает этот двоичный шум (так же поступает и Revit),
            // не влияя на отображаемые 0–6 знаков.
            double cleaned = Math.Round(value, 9, MidpointRounding.AwayFromZero);

            string text = Math.Round(cleaned, decimalPlaces, mode).ToString("N" + decimalPlaces, culture);

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
