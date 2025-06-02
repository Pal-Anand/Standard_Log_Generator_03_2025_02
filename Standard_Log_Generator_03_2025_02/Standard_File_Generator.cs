using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Standard_Log_Generator_03_2025_02
{
    [Transaction(TransactionMode.Manual)]
    public class Standard_File_Generator : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Prompt user to select elements
                IList<Reference> selectedReferences = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, "Select elements");

                // Get the selected elements and their IDs
                Dictionary<int, string> selectedElements = selectedReferences
                    .Select(reference => doc.GetElement(reference))
                    .ToDictionary(element => element.Id.IntegerValue, element => string.Empty);

                // Show the main window with the selected elements
                MainWindow mainWindow = new MainWindow(selectedElements, uiapp);
                mainWindow.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }


        }
    }
}
