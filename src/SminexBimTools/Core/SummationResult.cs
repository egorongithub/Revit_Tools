using System.Collections.Generic;

namespace SminexBimTools.Core
{
    /// <summary>
    /// Результат суммирования по выделенным элементам.
    /// </summary>
    public class SummationResult
    {
        /// <summary>Итоговая сумма (в метрах, м², м³ или штуках).</summary>
        public double Total { get; set; }

        /// <summary>Всего элементов было выделено.</summary>
        public int TotalElements { get; set; }

        /// <summary>Сколько элементов учтено в сумме.</summary>
        public int Counted { get; set; }

        /// <summary>
        /// Сколько элементов дали значение из безразмерного параметра
        /// (число/целое/текст) — оно просуммировано как есть, без перевода единиц.
        /// </summary>
        public int RawCount { get; set; }

        /// <summary>Описания элементов, у которых параметр не найден.</summary>
        public List<string> Skipped { get; } = new List<string>();

        /// <summary>Разбивка суммы по категориям: имя категории → итог.</summary>
        public Dictionary<string, CategoryTotal> Categories { get; } = new Dictionary<string, CategoryTotal>();

        /// <summary>
        /// Разбивка по параметрам-источникам: «Имя (источник, единица)» → итог.
        /// </summary>
        public Dictionary<string, CategoryTotal> ByParameter { get; } = new Dictionary<string, CategoryTotal>();
    }

    public class CategoryTotal
    {
        public double Sum { get; set; }
        public int Count { get; set; }
    }
}
