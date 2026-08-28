using System;
using Autodesk.Revit.DB;

namespace SminexBimTools.Core
{
    /// <summary>
    /// Вид измерения, по которому выполняется суммирование.
    /// </summary>
    public enum MeasureKind
    {
        Volume,
        Area,
        Length,
        Mass,
        Count
    }

    public static class MeasureKindExtensions
    {
        /// <summary>Название измерения для интерфейса.</summary>
        public static string DisplayName(this MeasureKind kind)
        {
            switch (kind)
            {
                case MeasureKind.Volume: return "Объем";
                case MeasureKind.Area: return "Площадь";
                case MeasureKind.Length: return "Длина";
                case MeasureKind.Mass: return "Масса";
                case MeasureKind.Count: return "Количество";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        /// <summary>Заголовок итоговой строки результата.</summary>
        public static string SummaryTitle(this MeasureKind kind)
        {
            switch (kind)
            {
                case MeasureKind.Volume: return "Суммарный объем";
                case MeasureKind.Area: return "Суммарная площадь";
                case MeasureKind.Length: return "Суммарная длина";
                case MeasureKind.Mass: return "Суммарная масса";
                case MeasureKind.Count: return "Общее количество";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        /// <summary>Единица измерения результата.</summary>
        public static string Unit(this MeasureKind kind)
        {
            switch (kind)
            {
                case MeasureKind.Volume: return "м³";
                case MeasureKind.Area: return "м²";
                case MeasureKind.Length: return "м";
                case MeasureKind.Mass: return "кг";
                case MeasureKind.Count: return "шт";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        /// <summary>
        /// «Родной» системный параметр Revit этой проверки. Используется только
        /// аналитикой настроек — чтобы распознать в правиле локализованное имя
        /// системного параметра; при подсчете системные параметры находятся
        /// по имени, как любые другие.
        /// </summary>
        public static BuiltInParameter FallbackBuiltInParameter(this MeasureKind kind)
        {
            switch (kind)
            {
                case MeasureKind.Volume: return BuiltInParameter.HOST_VOLUME_COMPUTED;
                case MeasureKind.Area: return BuiltInParameter.HOST_AREA_COMPUTED;
                case MeasureKind.Length: return BuiltInParameter.CURVE_ELEM_LENGTH;
                // У «Массы» системного запасного параметра нет.
                default: return BuiltInParameter.INVALID;
            }
        }
    }
}
