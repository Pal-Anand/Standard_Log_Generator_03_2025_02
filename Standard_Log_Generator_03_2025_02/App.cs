
using Autodesk.Revit.UI;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Standard_Log_Generator_03_2025_02
{
    public class App : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            RibbonPanel panel = RibbonPanel(application);

            //// Get here the cwd 
            //string currentDirectory = Directory.GetCurrentDirectory();
            //// base directory
            //string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //// projects base directory
            //string projectBaseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);


            // Get the assembly path of application where it is been complied too..
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            // Add Standard File Generator button to the ribbon panel
            if (panel.AddItem(new PushButtonData("Standard File Generator", "Create Std. File", thisAssemblyPath, "Standard_Log_Generator_03_2025_02.Standard_File_Generator")) is PushButton pushButton)
            {
                pushButton.ToolTip = "CM Standard File Generator";
                //Uri uri = new Uri("pack://application:,,,/Standard_Log_Generator_03_2025_02;component/Resource/JSON_Generator.ico");
                //BitmapImage bitmap = new BitmapImage(uri);
                //pushButton.LargeImage = bitmap;
            }

            // Add Verify Model button
            if (panel.AddItem(new PushButtonData("VerifyModel", "Verification", thisAssemblyPath, "Standard_Log_Generator_03_2025_02.VerifyModel")) is PushButton verifyButton)
            {
                verifyButton.ToolTip = "Verify model elements against saved color standards";
                //Uri uri = new Uri("pack://application:,,,/Standard_Log_Generator_03_2025_02;component/Resource/Verify_Model.ico");
                ////Uri uri = new Uri(Path.Combine(Path.GetDirectoryName(thisAssemblyPath), "Resource", "Verify_Model.ico"));
                //BitmapImage bitmap = new BitmapImage(uri);
                //verifyButton.LargeImage = bitmap;
            }

            return Result.Succeeded;
        }


        public RibbonPanel RibbonPanel(UIControlledApplication application)
        {
            string tab = "CM_Automation_Testing";
            RibbonPanel ribbonPanel = null;
            try
            {
                application.CreateRibbonTab(tab);
                application.CreateRibbonPanel(tab, "CM Automation");

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            List<RibbonPanel> panels = application.GetRibbonPanels(tab);
            foreach (RibbonPanel panel in panels.Where(p => p.Name == "CM Automation"))
            {
                ribbonPanel = panel;
            }
            return ribbonPanel;
        }
    }

}
