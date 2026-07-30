using Autodesk.Revit.Attributes;
using SminexBimTools.Core;

namespace SminexBimTools.Commands
{
    /// <summary>Кнопка «Площадь» — сумма площадей выделенных элементов.</summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AreaCommand : SumCommandBase
    {
        protected override MeasureKind Kind => MeasureKind.Area;
    }
}
