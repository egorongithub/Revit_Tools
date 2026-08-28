using Autodesk.Revit.Attributes;
using SminexBimTools.Core;

namespace SminexBimTools.Commands
{
    /// <summary>Кнопка «Масса» — сумма масс выделенных элементов.</summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MassCommand : SumCommandBase
    {
        protected override MeasureKind Kind => MeasureKind.Mass;
    }
}
