using Autodesk.Revit.Attributes;
using SminexBimTools.Core;

namespace SminexBimTools.Commands
{
    /// <summary>Кнопка «Длина» — сумма длин выделенных элементов.</summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class LengthCommand : SumCommandBase
    {
        protected override MeasureKind Kind => MeasureKind.Length;
    }
}
