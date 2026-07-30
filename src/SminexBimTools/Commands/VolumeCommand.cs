using Autodesk.Revit.Attributes;
using SminexBimTools.Core;

namespace SminexBimTools.Commands
{
    /// <summary>Кнопка «Объем» — сумма объёмов выделенных элементов.</summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class VolumeCommand : SumCommandBase
    {
        protected override MeasureKind Kind => MeasureKind.Volume;
    }
}
