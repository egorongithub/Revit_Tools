using Autodesk.Revit.Attributes;
using SminexBimTools.Core;

namespace SminexBimTools.Commands
{
    /// <summary>
    /// Кнопка «Количество» — сумма значений параметра «Количество» выделенных
    /// элементов; элементы без такого параметра считаются за 1 штуку.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CountCommand : SumCommandBase
    {
        protected override MeasureKind Kind => MeasureKind.Count;
    }
}
