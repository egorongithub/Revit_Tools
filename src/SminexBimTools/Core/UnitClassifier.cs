using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace SminexBimTools.Core
{
    /// <summary>Физическое измерение значения параметра.</summary>
    public enum ValueDimension
    {
        /// <summary>Безразмерное: число, целое, текст, а также спеки без метрических единиц (углы, расходы и т.п.).</summary>
        None,
        Length,
        Area,
        Volume,
        Mass
    }

    /// <summary>
    /// Классификация типа данных параметра по его допустимым единицам:
    /// всё, что измеряется в метрах, — длина (включая «Размер воздуховода»,
    /// «Размер трубы», толщины изоляции и т.п.), в м² — площадь, в м³ —
    /// объем, в кг — масса. Прочее — None. Без классификации сырое значение
    /// таких параметров уходило бы наружу во внутренних единицах Revit
    /// (600 мм высоты воздуховода показывались как 1,969 фута).
    /// </summary>
    public static class UnitClassifier
    {
#if REVIT2020 || REVIT2021
        private static readonly Dictionary<UnitType, ValueDimension> Cache = new Dictionary<UnitType, ValueDimension>();

#pragma warning disable CS0618
        public static ValueDimension Classify(UnitType unitType)
        {
            if (Cache.TryGetValue(unitType, out ValueDimension cached))
                return cached;

            ValueDimension dimension = ValueDimension.None;
            try
            {
                // Измерение определяется конвертируемостью первой допустимой
                // единицы типа данных в метры/м²/м³/кг: несовместимые единицы
                // Revit конвертировать отказывается.
                IList<DisplayUnitType> validUnits = UnitUtils.GetValidDisplayUnits(unitType);
                if (validUnits.Count > 0)
                {
                    DisplayUnitType sample = validUnits[0];
                    if (CanConvert(sample, DisplayUnitType.DUT_CUBIC_METERS)) dimension = ValueDimension.Volume;
                    else if (CanConvert(sample, DisplayUnitType.DUT_SQUARE_METERS)) dimension = ValueDimension.Area;
                    else if (CanConvert(sample, DisplayUnitType.DUT_METERS)) dimension = ValueDimension.Length;
                    else if (CanConvert(sample, DisplayUnitType.DUT_KILOGRAMS_MASS)) dimension = ValueDimension.Mass;
                }
            }
            catch
            {
                dimension = ValueDimension.None;
            }

            Cache[unitType] = dimension;
            return dimension;
        }

        private static bool CanConvert(DisplayUnitType from, DisplayUnitType to)
        {
            try
            {
                UnitUtils.Convert(1.0, from, to);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Переводит внутреннее значение Revit в метрическую единицу измерения.</summary>
        public static double ConvertFromInternal(double value, ValueDimension dimension)
        {
            switch (dimension)
            {
                case ValueDimension.Volume: return UnitUtils.ConvertFromInternalUnits(value, DisplayUnitType.DUT_CUBIC_METERS);
                case ValueDimension.Area: return UnitUtils.ConvertFromInternalUnits(value, DisplayUnitType.DUT_SQUARE_METERS);
                case ValueDimension.Length: return UnitUtils.ConvertFromInternalUnits(value, DisplayUnitType.DUT_METERS);
                case ValueDimension.Mass: return UnitUtils.ConvertFromInternalUnits(value, DisplayUnitType.DUT_KILOGRAMS_MASS);
                default: return value;
            }
        }
#pragma warning restore CS0618
#else
        private static readonly Dictionary<ForgeTypeId, ValueDimension> Cache = new Dictionary<ForgeTypeId, ValueDimension>();

        public static ValueDimension Classify(ForgeTypeId spec)
        {
            if (spec == null)
                return ValueDimension.None;
            if (Cache.TryGetValue(spec, out ValueDimension cached))
                return cached;

            ValueDimension dimension = ValueDimension.None;
            try
            {
                if (UnitUtils.IsMeasurableSpec(spec))
                {
                    // Измерение определяется конвертируемостью первой допустимой
                    // единицы типа данных в метры/м²/м³/кг: несовместимые единицы
                    // Revit конвертировать отказывается.
                    IList<ForgeTypeId> validUnits = UnitUtils.GetValidUnits(spec);
                    if (validUnits.Count > 0)
                    {
                        ForgeTypeId sample = validUnits[0];
                        if (CanConvert(sample, UnitTypeId.CubicMeters)) dimension = ValueDimension.Volume;
                        else if (CanConvert(sample, UnitTypeId.SquareMeters)) dimension = ValueDimension.Area;
                        else if (CanConvert(sample, UnitTypeId.Meters)) dimension = ValueDimension.Length;
                        else if (CanConvert(sample, UnitTypeId.Kilograms)) dimension = ValueDimension.Mass;
                    }
                }
            }
            catch
            {
                dimension = ValueDimension.None;
            }

            Cache[spec] = dimension;
            return dimension;
        }

        private static bool CanConvert(ForgeTypeId from, ForgeTypeId to)
        {
            try
            {
                UnitUtils.Convert(1.0, from, to);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Переводит внутреннее значение Revit в метрическую единицу измерения.</summary>
        public static double ConvertFromInternal(double value, ValueDimension dimension)
        {
            switch (dimension)
            {
                case ValueDimension.Volume: return UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.CubicMeters);
                case ValueDimension.Area: return UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.SquareMeters);
                case ValueDimension.Length: return UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Meters);
                case ValueDimension.Mass: return UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Kilograms);
                default: return value;
            }
        }
#endif

        /// <summary>Единица результата для измерения («м», «м²», «м³», «кг»).</summary>
        public static string UnitLabel(ValueDimension dimension)
        {
            switch (dimension)
            {
                case ValueDimension.Volume: return "м³";
                case ValueDimension.Area: return "м²";
                case ValueDimension.Length: return "м";
                case ValueDimension.Mass: return "кг";
                default: return null;
            }
        }

        /// <summary>Измерение, ожидаемое проверкой.</summary>
        public static ValueDimension ExpectedDimension(MeasureKind kind)
        {
            switch (kind)
            {
                case MeasureKind.Volume: return ValueDimension.Volume;
                case MeasureKind.Area: return ValueDimension.Area;
                case MeasureKind.Length: return ValueDimension.Length;
                case MeasureKind.Mass: return ValueDimension.Mass;
                default: return ValueDimension.None;
            }
        }
    }
}
