using System.Xml.Serialization;

namespace SminexBimTools.Settings
{
    /// <summary>Откуда разрешено брать параметр.</summary>
    public enum ParameterSource
    {
        /// <summary>По общему порядку поиска (тип/экземпляр — как задано в настройках).</summary>
        Auto,

        /// <summary>Только из экземпляра.</summary>
        Instance,

        /// <summary>Только из типа.</summary>
        Type
    }

    /// <summary>Шаг общего порядка поиска.</summary>
    public enum SearchStage
    {
        Type,
        Instance,
        System
    }

    /// <summary>
    /// В какой единице трактовать значение безразмерного (числового/текстового)
    /// параметра. Auto — брать как есть, без пересчета.
    /// </summary>
    public enum RawUnit
    {
        Auto,
        Millimeters,
        Centimeters,
        Meters,
        SquareMillimeters,
        SquareCentimeters,
        SquareMeters,
        CubicMillimeters,
        Liters,
        CubicMeters
    }

    /// <summary>Метки и коэффициенты пересчета безразмерных значений в м/м²/м³.</summary>
    public static class RawUnits
    {
        public static string Label(RawUnit unit)
        {
            switch (unit)
            {
                case RawUnit.Millimeters: return "мм";
                case RawUnit.Centimeters: return "см";
                case RawUnit.Meters: return "м";
                case RawUnit.SquareMillimeters: return "мм²";
                case RawUnit.SquareCentimeters: return "см²";
                case RawUnit.SquareMeters: return "м²";
                case RawUnit.CubicMillimeters: return "мм³";
                case RawUnit.Liters: return "л";
                case RawUnit.CubicMeters: return "м³";
                default: return "Авто";
            }
        }

        public static RawUnit FromLabel(string label)
        {
            switch (label)
            {
                case "мм": return RawUnit.Millimeters;
                case "см": return RawUnit.Centimeters;
                case "м": return RawUnit.Meters;
                case "мм²": return RawUnit.SquareMillimeters;
                case "см²": return RawUnit.SquareCentimeters;
                case "м²": return RawUnit.SquareMeters;
                case "мм³": return RawUnit.CubicMillimeters;
                case "л": return RawUnit.Liters;
                case "м³": return RawUnit.CubicMeters;
                default: return RawUnit.Auto;
            }
        }

        /// <summary>Коэффициент пересчета значения в итоговую единицу (м, м², м³).</summary>
        public static double Factor(RawUnit unit)
        {
            switch (unit)
            {
                case RawUnit.Millimeters: return 0.001;
                case RawUnit.Centimeters: return 0.01;
                case RawUnit.SquareMillimeters: return 1e-6;
                case RawUnit.SquareCentimeters: return 1e-4;
                case RawUnit.CubicMillimeters: return 1e-9;
                case RawUnit.Liters: return 0.001;
                default: return 1.0; // м, м², м³, Авто
            }
        }
    }

    /// <summary>
    /// Правило поиска: имя параметра, место, откуда его разрешено читать,
    /// и единица для трактовки безразмерных значений.
    /// </summary>
    public class ParameterRule
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public ParameterSource Source { get; set; } = ParameterSource.Auto;

        /// <summary>
        /// Единица безразмерного значения. Применяется, только если параметр
        /// оказался числом/текстом без размерности: значение трактуется
        /// в этой единице и переводится в м/м²/м³. Auto — как есть.
        /// </summary>
        [XmlAttribute]
        public RawUnit Unit { get; set; } = RawUnit.Auto;

        public ParameterRule()
        {
        }

        public ParameterRule(string name, ParameterSource source = ParameterSource.Auto)
        {
            Name = name;
            Source = source;
        }
    }
}
