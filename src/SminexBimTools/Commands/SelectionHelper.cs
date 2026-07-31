using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace SminexBimTools.Commands
{
    internal static class SelectionHelper
    {
        /// <summary>
        /// Возвращает выделенные элементы; если ничего не выделено — предлагает
        /// выбрать. Возвращает null, если пользователь отменил выбор.
        /// </summary>
        public static ICollection<ElementId> GetTargets(UIDocument uidoc)
        {
            ICollection<ElementId> ids = uidoc.Selection.GetElementIds();
            if (ids != null && ids.Count > 0)
                return ids;

            try
            {
                IList<Reference> references = uidoc.Selection.PickObjects(
                    ObjectType.Element,
                    "Выберите элементы и нажмите «Готово»");
                return references.Select(r => r.ElementId).ToList();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
        }
    }
}
