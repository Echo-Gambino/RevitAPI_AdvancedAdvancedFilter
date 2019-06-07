namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.UsingCommandData)]
    class ModelessFormCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            try
            {
                // Set up the documents
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
                List<Element> elementList = selectedIds.Select(id => doc.GetElement(id)).ToList();

                Main.ActiveExtrApp.CreateModelessForm(commandData, elementList);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ErrorReport.Report(ex);

                return Result.Failed;
            }
        }

    }
}
