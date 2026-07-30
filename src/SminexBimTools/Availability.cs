using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace SminexBimTools
{
    /// <summary>Команда доступна только при открытом документе.</summary>
    public class DocumentRequiredAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return applicationData != null
                && applicationData.ActiveUIDocument != null
                && applicationData.ActiveUIDocument.Document != null;
        }
    }

    /// <summary>Команда доступна всегда, в том числе без открытого документа.</summary>
    public class AlwaysAvailable : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return true;
        }
    }
}
