using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Standard_Log_Generator_03_2025_02
{
    public class SectionBoxHandler : IExternalEventHandler
    {
        private ElementId _elementId;
        private UIApplication _application;
        private UIDocument uidoc;
        private const double Tolerance = 1000 / 304.8; //converting 100mm to feet
        private UIView _view;


        public void setParameter(ElementId elementId, UIApplication application)
        {
            _elementId = elementId;
            _application = application;
        }
        public void Execute(UIApplication app)
        {
            try
            {
                View3D view = null;
                uidoc = app.ActiveUIDocument;
                Document doc = uidoc.Document;
                view = doc.ActiveView as View3D;
                if (view.IsSectionBoxActive)
                {
                    using (Transaction tran = new Transaction(doc))
                    {
                        tran.Start("Closing Section Box");
                        view.IsSectionBoxActive = false;
                        TransactionStatus status = tran.Commit();
                    }
                }
                Element element = doc.GetElement(_elementId);


                if (view != null && view.CanModifyViewDiscipline())
                {
                    using (Transaction trans = new Transaction(doc, "Apply Section Box"))
                    {
                        trans.Start();
                        //Element element = doc.GetElement(_elementId);
                        //BoundingBoxXYZ boundingBox = doc.GetElement(_elementId).get_BoundingBox(view);
                        BoundingBoxXYZ elementBoundingBox = element.get_BoundingBox(view);
                        if (elementBoundingBox != null)
                        {
                            // Adjust bounding box with tolerance
                            XYZ min = elementBoundingBox.Min;
                            XYZ max = elementBoundingBox.Max;
                            // Applying tolerance..
                            min = new XYZ(min.X - Tolerance, min.Y - Tolerance, min.Z - Tolerance);
                            max = new XYZ(max.X + Tolerance, max.Y + Tolerance, max.Z + Tolerance);

                            BoundingBoxXYZ sectionBox = new BoundingBoxXYZ
                            {
                                Min = min,
                                Max = max
                            };

                            view.SetSectionBox(sectionBox);

                        }

                        trans.Commit();
                    }
                    // Center the bounding box in view...
                    CenterViewOnElement(view, element);
                }
                else
                {
                    TaskDialog.Show("Error", "The active view must be able to modify the section box.");
                }

            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
            }

        }

        private void CenterViewOnElement(View3D view, Element element)
        {
            try
            {
                UIDocument uidoc = _application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Get the element's bounding box in the active view
                BoundingBoxXYZ boundingBox = element.get_BoundingBox(view);

                if (boundingBox != null)
                {
                    // Calculate the center of the bounding box
                    XYZ center = (boundingBox.Min + boundingBox.Max) / 2;

                    // Get the active UIView
                    IList<UIView> uiviews = uidoc.GetOpenUIViews();
                    UIView uiView = uiviews.FirstOrDefault(uv => uv.ViewId.Equals(view.Id));

                    if (uiView != null)
                    {
                        // Set the zoom and center the view on the element's bounding box
                        XYZ min = boundingBox.Min;
                        XYZ max = boundingBox.Max;
                        BoundingBoxXYZ newBox = new BoundingBoxXYZ
                        {
                            Min = new XYZ(min.X, min.Y, min.Z),
                            Max = new XYZ(max.X, max.Y, max.Z)
                        };

                        uiView.ZoomToFit();
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"An error occurred while centering the view: {ex.Message}");
            }
        }

        public string GetName()
        {
            return "Section box Handler";
        }
    }
}
