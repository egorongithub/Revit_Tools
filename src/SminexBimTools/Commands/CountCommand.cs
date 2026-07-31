using Autodesk.Revit.Attributes;
using SminexBimTools.Core;

namespace SminexBimTools.Commands
{
    /// <summary>
    /// Кнопка «Количество» — число выделенных элементов
    /// с разбивкой по категориям.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CountCommand : SumCommandBase
    {
        protected override MeasureKind Kind => MeasureKind.Count;
    }
}
