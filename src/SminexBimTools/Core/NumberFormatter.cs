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

            // После перевода из внутренних единиц Revit (футов) значение 0,8325
            // может храниться как 0,83249999999999… — двоичное число чуть меньше
            // середины, и округление double уводит его вниз. Преобразование в
            // decimal оставляет 15 значащих цифр и само отсекает этот шум
            // (0,83249999999999… -> ровно 0,8325; 520,48249999999996 -> 520,4825),
            // после чего округление идет в десятичной арифметике — так же,
            // как значение видит пользователь в Revit.
            string text;
            if (Math.Abs(value) < 7.9e28)
            {
                decimal exact = (decimal)value;
                text = Math.Round(exact, decimalPlaces, mode).ToString("N" + decimalPlaces, culture);
            }
            else
            {
                text = Math.Round(value, decimalPlaces, mode).ToString("N" + decimalPlaces, culture);
            }

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
