using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Standard_Log_Generator_03_2025_02
{

    public class ElementColorInfo
    {
        public string Status { get; set; }
        public string Color { get; set; }
    }

    public class ElementColorResult
    {
        public string Status { get; set; }
        public string ExpectedColor { get; set; }
        public string ActualColor { get; set; }
        public bool ColorMatch { get; set; }
    }

    [Transaction(TransactionMode.Manual)]
    internal class VerifyModel : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                string jsonFilePath = string.Empty;
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                
                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
                {
                    Title = "Select a file",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                // Show the file dialog and capture the result
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Retrieve the selected file's full path
                    jsonFilePath = openFileDialog.FileName;
                    Console.WriteLine("Selected file path: " + jsonFilePath);
                }
                else
                {
                    MessageBox.Show("No file selected from the diaolg.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                #region Code for setting the path hard-Coded where the latest JSON will be get from the folder automatically.
                //string folderPath = Path.GetDirectoryName(GlobalVariable.filePath);

                //// Get the latest JSON file in the specified directory
                //jsonFilePath = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly)
                //    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                //    .FirstOrDefault();

                #endregion

                // Check if the file exists
                if (!File.Exists(jsonFilePath))
                {
                    message = $"File does not exist: {jsonFilePath}";
                    return Result.Failed;
                }

                Dictionary<string, ElementColorInfo> jsonDict;
                Dictionary<string, ElementColorResult> results = new Dictionary<string, ElementColorResult>();

                try
                {
                    string json = File.ReadAllText(jsonFilePath);
                    jsonDict = JsonConvert.DeserializeObject<Dictionary<string, ElementColorInfo>>(json);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", "Failed to parse JSON: " + ex.Message);
                    return Result.Failed;
                }

                List<string> matchingElements = new List<string>();
                List<string> nonMatchingElements = new List<string>();

                foreach (var kvp in jsonDict)
                {
                    long idValue;
                    if (!long.TryParse(kvp.Key, out idValue))
                        continue;

                    ElementId elementId = new ElementId(idValue);
                    Element element = doc.GetElement(elementId);
                    if (element == null)
                        continue;

                    Options geomOptions = new Options
                    {
                        ComputeReferences = true,
                        IncludeNonVisibleObjects = false,
                        DetailLevel = ViewDetailLevel.Fine
                    };

                    GeometryElement geomElem = element.get_Geometry(geomOptions);
                    if (geomElem == null)
                        continue;

                    var expectedColor = ColorFromHex(kvp.Value.Color);
                    bool hasMatchingFace = false;
                    string actualColor = null;

                    foreach (GeometryObject geomObj in geomElem)
                    {
                        Solid solid = geomObj as Solid;
                        if (solid == null || solid.Faces.IsEmpty)
                            continue;

                        foreach (Face face in solid.Faces)
                        {
                            ElementId matId = face.MaterialElementId;
                            if (matId == ElementId.InvalidElementId)
                                continue;

                            Material material = doc.GetElement(matId) as Material;
                            if (material == null)
                                continue;

                            Autodesk.Revit.DB.Color faceColor = material.Color;
                            actualColor = $"#{faceColor.Red:X2}{faceColor.Green:X2}{faceColor.Blue:X2}";

                            if (ColorsMatch(expectedColor, faceColor))
                            {
                                hasMatchingFace = true;
                                break;
                            }
                        }

                        if (hasMatchingFace)
                            break;
                    }

                    results[kvp.Key] = new ElementColorResult
                    {
                        Status = kvp.Value.Status,
                        ExpectedColor = kvp.Value.Color,
                        ActualColor = actualColor ?? "No color found",
                        ColorMatch = hasMatchingFace
                    };

                    string elementInfo = $"ElementId: {elementId.Value} | Name: {element.Name} | Expected: {kvp.Value.Color} | Actual: {actualColor}";
                    if (hasMatchingFace)
                    {
                        matchingElements.Add(elementInfo);
                    }
                    else
                    {
                        nonMatchingElements.Add(elementInfo);
                    }
                }

                // Create Results folder in the JSON directory if it doesn't exist
                string resultsFolder = Path.Combine(Path.GetDirectoryName(jsonFilePath), "Results");
                if (!Directory.Exists(resultsFolder))
                {
                    Directory.CreateDirectory(resultsFolder);
                }

                // Save results back to JSON
                string outputJson = JsonConvert.SerializeObject(results, Formatting.Indented);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string outputPath = Path.Combine(resultsFolder,
                    $"★_{Path.GetFileNameWithoutExtension(jsonFilePath)}_results_{timestamp}_★.json");

                File.WriteAllText(outputPath, outputJson);
                // File.WriteAllText(jsonFilePath, "-------------------------------------------------");

                // Display results
                StringBuilder resultMessage = new StringBuilder();
                resultMessage.AppendLine($"Total elements checked: {results.Count}");
                resultMessage.AppendLine($"Matching elements: {matchingElements.Count}");
                resultMessage.AppendLine($"Non-matching elements: {nonMatchingElements.Count}");
                resultMessage.AppendLine();
                resultMessage.AppendLine("Results have been saved to:");
                resultMessage.AppendLine(outputPath);

                TaskDialog.Show("Color Verification Results", resultMessage.ToString());

                if (nonMatchingElements.Count > 0)
                {
                    TaskDialog.Show("Non-matching Elements",
                        string.Join(Environment.NewLine, nonMatchingElements));
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private System.Windows.Media.Color ColorFromHex(string hexColor)
        {
            if (hexColor.StartsWith("#"))
                hexColor = hexColor.Substring(1);

            return System.Windows.Media.Color.FromRgb(
                byte.Parse(hexColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber)
            );
        }

        private bool ColorsMatch(System.Windows.Media.Color expected, Autodesk.Revit.DB.Color actual)
        {
            const int tolerance = 30; // Color matching tolerance
            return Math.Abs(expected.R - actual.Red) <= tolerance &&
                   Math.Abs(expected.G - actual.Green) <= tolerance &&
                   Math.Abs(expected.B - actual.Blue) <= tolerance;
        }
    }
}
