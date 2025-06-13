using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Standard_Log_Generator_03_2025_02
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<int, ElementData> selectedElements;
        private UIDocument uiDoc;
        private string selectedColorStatus = string.Empty;
        private ExternalEvent sectionBoxEvent;
        private SectionBoxHandler sectionBoxHandler;
        public MainWindow(Dictionary<int, string> elements, UIApplication uiapp)
        {
            InitializeComponent();

            selectedElements = new Dictionary<int, ElementData>();
            uiDoc = uiapp.ActiveUIDocument;
            sectionBoxHandler = new SectionBoxHandler();
            sectionBoxEvent = ExternalEvent.Create(sectionBoxHandler);

            Document doc = uiDoc.Document;
            if ((string.IsNullOrEmpty(GlobalVariable.filePath) || string.IsNullOrWhiteSpace(GlobalVariable.filePath)) &&
                (string.IsNullOrEmpty(GlobalVariable.fileName) || string.IsNullOrWhiteSpace(GlobalVariable.fileName)))
            {
                fileNameAndFilePathToSave();
            }

            try
            {
                //// Populate the ListBox with selected element IDs
                //foreach (var elemId in selectedElements.Keys)
                //{
                //    ElementListBox.Items.Add(elemId);
                //}

                // First populate the selectedElements dictionary using the input elements
                foreach (var kvp in elements)
                {
                    int elemId = kvp.Key;
                    Element element = doc.GetElement(new ElementId(elemId));

                    if (element != null)
                    {
                        // Add to ListBox
                        ElementListBox.Items.Add(elemId);

                        // Initialize ElementData for each element
                        selectedElements[elemId] = new ElementData
                        {
                            Status = string.Empty,
                            Color = "#FFFFFF" // Default color (white)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while populating the element list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selectedItem = (ComboBoxItem)ColorComboBox.SelectedItem;
            selectedColorStatus = (string)selectedItem.Tag;

            // Update the color for all selected elements based on the status
            string colorForStatus = StatusColors.GetColorForStatus(selectedColorStatus);
            foreach (var key in selectedElements.Keys)
            {
                selectedElements[key].Status = selectedColorStatus;
                selectedElements[key].Color = colorForStatus;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            #region old reference
            //try
            //{
            //    string fileName = globalVariable.fileName;
            //    if (!string.IsNullOrEmpty(selectedColorStatus) && !string.IsNullOrEmpty(fileName))
            //    {
            //        // Update dictionary with color status
            //        var keys = selectedElements.Keys.ToList();
            //        foreach (var key in keys)
            //        {
            //            selectedElements[key] = selectedColorStatus;
            //        }

            //        string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            //        Dictionary<int, string> existingElements = new Dictionary<int, string>();

            //        // Check if the file exists and load existing data
            //        if (File.Exists(filePath))
            //        {
            //            try
            //            {
            //                string existingJson = File.ReadAllText(filePath);
            //                existingElements = JsonConvert.DeserializeObject<Dictionary<int, string>>(existingJson);
            //            }
            //            catch (Exception ex)
            //            {
            //                MessageBox.Show($"An error occurred while reading the existing file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //            }
            //        }

            //        // Merge existing data with new data
            //        foreach (var key in selectedElements.Keys)
            //        {
            //            existingElements[key] = selectedElements[key];
            //        }

            //        try
            //        {
            //            string json = JsonConvert.SerializeObject(existingElements, Formatting.Indented);
            //            File.WriteAllText(filePath, json);

            //            MessageBox.Show("Data saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            //        }
            //        catch (UnauthorizedAccessException uaEx)
            //        {
            //            MessageBox.Show($"Access denied: {uaEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //        }
            //        catch (PathTooLongException ptEx)
            //        {
            //            MessageBox.Show($"Path too long: {ptEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //        }
            //        catch (IOException ioEx)
            //        {
            //            MessageBox.Show($"IO error: {ioEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //        }
            //        catch (Exception ex)
            //        {
            //            MessageBox.Show($"An error occurred while saving the data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //        }
            //    }
            //    else
            //    {
            //        MessageBox.Show("Please select a color and enter a file name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //}
            #endregion

            try
            {
                Dictionary<string, ElementData> existingData = new Dictionary<string, ElementData>();

                if (!string.IsNullOrEmpty(selectedColorStatus) && !string.IsNullOrEmpty(GlobalVariable.fileName))
                {
                    Dictionary<string, ElementData> formattedElements = new Dictionary<string, ElementData>();


                    // Convert the dictionary to the required format
                    foreach (var kvp in selectedElements)
                    {
                        formattedElements[kvp.Key.ToString()] = kvp.Value;
                    }

                    // If JSON contains data, add it to formattedElements dictionary
                    if (File.Exists(GlobalVariable.filePath) && new FileInfo(GlobalVariable.filePath).Length > 0)
                    {
                        string content = File.ReadAllText(GlobalVariable.filePath);
                        JObject jsonObject = JObject.Parse(content);

                        foreach (var kvp in jsonObject)
                        {
                            ElementData elementData = kvp.Value.ToObject<ElementData>();
                            if (elementData != null)
                            {
                                existingData[kvp.Key] = elementData;
                            }
                        }

                        foreach (var kvp in existingData)
                        {
                            formattedElements[kvp.Key] = kvp.Value;
                        }
                    }

                    try
                    {
                        string json = JsonConvert.SerializeObject(formattedElements, Formatting.Indented);
                        
                        File.WriteAllText(GlobalVariable.filePath, json);
                        MessageBox.Show("Data saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while saving the data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    this.Close();
                }
                else
                {
                    MessageBox.Show("Please select a color and file name not created in the folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ElementListBox.SelectedItem != null)
                {
                    int selectedId = (int)ElementListBox.SelectedItem;
                    if (selectedElements.ContainsKey(selectedId))
                    {
                        selectedElements.Remove(selectedId);
                        ElementListBox.Items.Remove(selectedId);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while removing the element: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ElementListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ElementListBox.SelectedItem != null)
                {
                    int selectedId = (int)ElementListBox.SelectedItem;
                    sectionBoxHandler.setParameter(new ElementId(selectedId), uiDoc.Application);
                    sectionBoxEvent.Raise();
                    HighlightSingleElement(selectedId);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while highlighting the element: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HighlightSingleElement(int elementId)
        {

            try
            {
                Document doc = uiDoc.Document;
                //SectionBoxHandler selection_SectionBox = new SectionBoxHandler();
                //sectionbox_event = ExternalEvent.Create(selection_SectionBox);
                Element element = doc.GetElement(new ElementId(elementId));
                if (element != null)
                {
                    IList<ElementId> elementIds = new List<ElementId> { element.Id };
                    uiDoc.Selection.SetElementIds(elementIds);
                }
                else
                {
                    MessageBox.Show("Element not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while highlighting the element: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void HighlightElements(List<int> lstele)
        {
            try
            {

                IList<ElementId> elementIds = lstele.Select(id => new ElementId(id)).ToList();
                uiDoc.Selection.SetElementIds(elementIds);
                uiDoc.RefreshActiveView();

            }
            catch (Exception exx)
            {

            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to cancel? Any unsaved changes will be lost.", "Confirm Cancel", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }
        private void ExistingFileSaveCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // to browse the folder to save

            //System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            //{
            //    RootFolder = Environment.SpecialFolder.MyDocuments
            //};
            //System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
            //if (result != System.Windows.Forms.DialogResult.OK)
            //{
            //    MessageBox.Show("No folder selected. Please select a folder to save the report.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            //string cmAppFolder = Path.Combine(folderDialog.SelectedPath, "CM_app_Report");

            //if (!Directory.Exists(cmAppFolder))
            //{
            //    Directory.CreateDirectory(cmAppFolder);
            //}
            //// Get Revit file name and current date/time
            //string revitFileName = Path.GetFileNameWithoutExtension(uiDoc.Document.Title);
            //string dateTime = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            //string jsonFileName = $"{revitFileName}_{dateTime}.json";

            //string filePath = Path.Combine(cmAppFolder, jsonFileName);
            ////Set the file name and file path to the setter
            ////GlobalVariable globalVariable = new GlobalVariable();
            //GlobalVariable.fileName = jsonFileName;
            //GlobalVariable.filePath = filePath;
        }

        private void ExistingFileSaveCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // File selection dialog for existing JSON files
            //Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            //{
            //    DefaultExt = ".json",
            //    Filter = "JSON files (*.json)|*.json",
            //    InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CM_app_Report")
            //};

            //if (openFileDialog.ShowDialog() == true)
            //{
            //    GlobalVariable.fileName = Path.GetFileName(openFileDialog.FileName);
            //    GlobalVariable.filePath = openFileDialog.FileName;
            //    GlobalVariable.folderToSave = Path.GetDirectoryName(openFileDialog.FileName);

            //    // Optional: Load existing data from the selected file
            //    try
            //    {
            //        string jsonContent = File.ReadAllText(openFileDialog.FileName);
            //        var existingData = JsonConvert.DeserializeObject<Dictionary<string, ElementData>>(jsonContent);
            //        // You might want to process the existing data here
            //        MessageBox.Show("Existing file selected successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show($"Error reading the selected file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //}
        }

        private void ExistingFileButton_Click(object sender, RoutedEventArgs e) {

            //File selection dialog for existing JSON files

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".json",
                Filter = "JSON files (*.json)|*.json",
                InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CM_app_Report")
            };

            if (openFileDialog.ShowDialog() == true)
            {
                GlobalVariable.fileName = Path.GetFileName(openFileDialog.FileName);
                GlobalVariable.filePath = openFileDialog.FileName;
                GlobalVariable.folderToSave = Path.GetDirectoryName(openFileDialog.FileName);

                // Optional: Load existing data from the selected file
                try
                {
                    string jsonContent = File.ReadAllText(openFileDialog.FileName);
                    var existingData = JsonConvert.DeserializeObject<Dictionary<string, ElementData>>(jsonContent);
                    // You might want to process the existing data here
                    MessageBox.Show("Existing file selected successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading the selected file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void CreateNewFileButton_Click(object sender, RoutedEventArgs e) {


            //to browse the folder to save

            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                RootFolder = Environment.SpecialFolder.MyDocuments
            };
            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK)
            {
                MessageBox.Show("No folder selected. Please select a folder to save the report.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string cmAppFolder = Path.Combine(folderDialog.SelectedPath, "CM_app_Report");

            if (!Directory.Exists(cmAppFolder))
            {
                Directory.CreateDirectory(cmAppFolder);
            }
            // Get Revit file name and current date/time
            string revitFileName = Path.GetFileNameWithoutExtension(uiDoc.Document.Title);
            string dateTime = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string jsonFileName = $"{revitFileName}_{dateTime}.json";

            string filePath = Path.Combine(cmAppFolder, jsonFileName);
            //Set the file name and file path to the setter
            //GlobalVariable globalVariable = new GlobalVariable();
            GlobalVariable.fileName = jsonFileName;
            GlobalVariable.filePath = filePath;

            

        }
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            //if (ExistingFileSaveCheckBox.IsChecked == true)
            //{
            //    ExistingFileSaveCheckBox_Checked(sender, e);
            //}
            //else
            //{
            //    ExistingFileSaveCheckBox_Unchecked(sender, e);
            //}

            #region folder code implementation.....
            //// to browse the folder to save

            //System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            //{
            //    RootFolder = Environment.SpecialFolder.MyDocuments
            //};
            //System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
            //if (result != System.Windows.Forms.DialogResult.OK)
            //{
            //    MessageBox.Show("No folder selected. Please select a folder to save the report.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            //string cmAppFolder = Path.Combine(folderDialog.SelectedPath, "CM_app_Report");

            //if (!Directory.Exists(cmAppFolder))
            //{
            //    Directory.CreateDirectory(cmAppFolder);
            //}
            //// Get Revit file name and current date/time
            //string revitFileName = Path.GetFileNameWithoutExtension(uiDoc.Document.Title);
            //string dateTime = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            //string jsonFileName = $"{revitFileName}_{dateTime}.json";

            //string filePath = Path.Combine(cmAppFolder, jsonFileName);
            ////Set the file name and file path to the setter
            ////GlobalVariable globalVariable = new GlobalVariable();
            //GlobalVariable.fileName = jsonFileName;
            //GlobalVariable.filePath = filePath;
            #endregion
        }

        private void AddMoreElementsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<int> ids = selectedElements.Keys.ToList();
                HighlightElements(ids);
                IList<Reference> selectedReferences = uiDoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, "Select more elements");

                // Get the selected elements and their IDs
                foreach (var reference in selectedReferences)
                {
                    Element element = uiDoc.Document.GetElement(reference);
                    if (!selectedElements.ContainsKey(element.Id.IntegerValue))
                    {
                        // Use the current selected status and its corresponding color
                        string colorForStatus = string.IsNullOrEmpty(selectedColorStatus)
                            ? "#FFFFFF" // Default white if no status selected
                            : StatusColors.GetColorForStatus(selectedColorStatus);

                        selectedElements.Add(element.Id.IntegerValue, new ElementData
                        {
                            Status = selectedColorStatus,
                            Color = colorForStatus
                        });
                        ElementListBox.Items.Add(element.Id.IntegerValue);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while adding more elements: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void fileNameAndFilePathToSave()
        {
            string initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);


            string cmAppFolder = Path.Combine(initialDirectory, "CM_app_Report");
            GlobalVariable.folderToSave = cmAppFolder;

            if (!Directory.Exists(cmAppFolder))
            {
                Directory.CreateDirectory(cmAppFolder);
            }
            // Get Revit file name and current date/time
            string revitFileName = Path.GetFileNameWithoutExtension(uiDoc.Document.Title);
            string dateTime = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string jsonFileName = $"{revitFileName}_{dateTime}.json";

            GlobalVariable.filePath = Path.Combine(cmAppFolder, jsonFileName);
            //Set the file name and file path to the setter
            GlobalVariable.fileName = jsonFileName;

        }


    }
    // <summary>
    /// Class to handle status colors and their corresponding RGB value
    public static class StatusColors
    {
        // Dictionary to store status and their corresponding RGB colors
        private static readonly Dictionary<string, (int R, int G, int B)> StatusColorMap = new Dictionary<string, (int R, int G, int B)>
    {
       { "FullyComplete", (137, 186, 46) },        // Green
        { "NeedsReview", (255, 165, 0)}, // Orange optional
        { "PartiallyComplete", (0, 0, 255) },    // Yellow
        { "Incomplete", (128, 128, 128) },    // Grey
        { "CheckQuality", (223, 0, 0) }       // Red
    };

        // Convert RGB to Hex and get color for status
        public static string GetColorForStatus(string status)
        {
            if (StatusColorMap.TryGetValue(status, out var rgb))
            {
                return $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}";
            }
            return "#000000"; // Default black if status not found
        }

        // Get all available statuses
        public static IEnumerable<string> GetAllStatuses()
        {
            return StatusColorMap.Keys;
        }
    }


    //<summary>
    /// Class to hold element data including status and color
    public class ElementData
    {
        public string Status { get; set; }
        public string Color { get; set; }
    }

}
