#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Reflection;
#endregion

namespace AtlasShadowArea
{
    [Transaction(TransactionMode.Manual)]
    public class ShadedArea: IExternalCommand
    {
        public object AddSharedParameter { get; private set; }

        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Document doc = uidoc.Document;
            Autodesk.Revit.DB.View view = doc.ActiveView;

            // Creation Application and Creation Document
            Autodesk.Revit.Creation.Application creapp = app.Create;
            Autodesk.Revit.Creation.Document credoc = doc.Create;
            
            // Get All Windows & Devices
            IList<Element> All_Windows = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Windows)
                .WhereElementIsNotElementType().ToList();
            
            IList<Element> All_ShadingDevices = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WhereElementIsNotElementType()
                .Where(e => e.LookupParameter("SHADING DEVICE").AsValueString() == "Yes").ToList();
            
            // List Direction
            List<string> Direction = new List<string> { "N", "S", "W", "E", "NE", "NW", "SE", "SW" };

            // List Month and Date
            List<List<int>> Month = new List<List<int>>();
            List<int> March = new List<int> { 3, 21 };
            List<int> June = new List<int> { 6, 21 };
            List<int> December = new List<int> { 12, 23 };
            Month.Add(March);
            Month.Add(June);
            Month.Add(December);
            
            ProjectPosition projectPosition = doc.ActiveProjectLocation.get_ProjectPosition(XYZ.Zero);
            Transform rotationTransform = Transform.CreateRotation(XYZ.BasisZ, -projectPosition.Angle);
            SunAndShadowSettings sunsetting = doc.ActiveView.SunAndShadowSettings;
            
            IEnumerable<Element> material = new FilteredElementCollector(doc).
            OfClass(typeof(Material)).Cast<Material>();
            Element mat = material.First();
            
            List<string> ParameterName = new List<string> { "ET_AS_MARCH", "ET_AS_JUNE", "ET_AS_DECEMBER" };
            List<string> GUID = new List<string> { "185bef59-7765-4927-a457-ec4b95484917", "fe907238-7f79-4745-918c-b3be1af589cc", "db1bba74-2b53-4b22-ad9b-b38b52657d33" };
            
            if (All_Windows.ElementAt(0).LookupParameter(ParameterName[0]) == null)
            {
                using (Transaction ts = new Transaction(doc, "Shared Para"))
                {
                    ts.Start();
                    string path = SharedParameter.Path(doc);
                    SharedParameter sharepara = new SharedParameter();
                    sharepara.AddSharedParameter(commandData, path,  ParameterName[0], GUID[0]);
                    sharepara.AddSharedParameter(commandData, path, ParameterName[1], GUID[1]);
                    sharepara.AddSharedParameter(commandData, path, ParameterName[2], GUID[2]);
                    ts.Commit();
                }
            }

            using (TransactionGroup tg = new TransactionGroup(doc, "Calculate Shadow Area"))
            {
                tg.Start();

                foreach (string d in Direction)
                {
                    IList<Element> Windows = All_Windows.
                        Where(e => e.LookupParameter("ET_FACING").AsString() == d).ToList();
                    
                    IList<Element> Shading = All_ShadingDevices.
                        Where(e => e.LookupParameter("ET_FACING").AsString() == d).ToList();

                    //Exception

                    if (Windows.Count() == 0)
                    {
                        continue;
                    }
                    else if (Shading.Count() == 0)
                    {
                        foreach (Element w in Windows)
                        {
                            foreach (string s in ParameterName)
                            {
                                Parameter pa = w.LookupParameter(s);

                                using (Transaction t = new Transaction(doc, "Set value"))
                                {
                                    t.Start();

                                    pa.Set("N/A");

                                    t.Commit();
                                }
                            }
                            
                        }
                    }

                    else
                    {
                        // User preferences for parsing of geometry.

                        Autodesk.Revit.DB.Options option = new Options();
                        option.ComputeReferences = true; 
                        option.DetailLevel = ViewDetailLevel.Fine; 
                        option.IncludeNonVisibleObjects = false; 
                        
                        IList<GeometryElement> GeoEle_Device = new List<GeometryElement>();
                        foreach (Element el in Shading)
                        {
                            GeometryElement geoEle = el.get_Geometry(option);
                            GeoEle_Device.Add(geoEle);
                        }

                        //Get Plane of Windows
                        IList<Plane> Plane = new List<Plane>();
                        IList<int> IndexRemove = new List<int>();
                        foreach (Element wi in Windows)
                        {
                            try
                            {
                                Plane plane = Function.GetPlane(wi, doc, creapp, credoc);
                                Plane.Add(plane);
                            }
                            catch
                            {
                                int id = Windows.IndexOf(wi);
                                IndexRemove.Add(id);
                            }
                        }
                        
                        int count = 0;
                        foreach (int index in IndexRemove)
                        {
                            Windows.RemoveAt(index-count);
                            count++;
                        }
                        
                        //Get Simple Solid of Windows
                        IList<Solid> Solid_Windows = new List<Solid>();
                        foreach (Element wi in Windows)
                        {
                            Solid solid = Function.SolidCurve(wi, doc);
                            
                            Solid_Windows.Add(solid);
                        }
                        
                        //ExtrusionAnalyzer

                        int i = 0;
                        foreach (Plane plane in Plane)
                        {
                            Element window = Windows[i];

                            int j = 0;
                            foreach (List<int> DateMonth in Month)
                            {

                                IList <string> SA = new List<string>();

                                for (int h = 7; h <= 18; h++)
                                {
                                    
                                    // Set Date & Time
                                    using (Transaction t = new Transaction(doc, "Date Time"))
                                    {
                                        t.Start();
                                        

                                        //Set Sun Settings
                                        //doc.ActiveView.get_Parameter(BuiltInParameter.VIEW_GRAPH_SUN_PATH).Set(0);

                                        sunsetting.SunAndShadowType = SunAndShadowType.OneDayStudy;
                                        sunsetting.StartDateAndTime = DateTime.SpecifyKind(
                                            new DateTime(2017, DateMonth[0], DateMonth[1], h, 0, 0), DateTimeKind.Local);

                                        //doc.ActiveView.get_Parameter(BuiltInParameter.VIEW_GRAPH_SUN_PATH).Set(1);

                                        t.Commit();
                                    }
                                    
                                    //Get SunDirection

                                    XYZ SunDirection_project_north = SunSetting.GetSunDirection(view);
                                    XYZ SunDirection = rotationTransform.OfVector(SunDirection_project_north);
                                    
                                    FamilyInstance fi_wi = window as FamilyInstance;
                                    XYZ wi_vector = fi_wi.FacingOrientation;
                                    double angle = wi_vector.DotProduct(SunDirection);
                                    
                                    double ShadowArea, ShadowArea_m2 = 0.0;

                                    if (angle <= 0)
                                    {
                                        SA.Add("N/A");
                                    }
                                    else
                                    {
                                        IList<Solid> Ilist_Shadow = new List<Solid>();
                                        foreach (GeometryElement geoEle in GeoEle_Device)
                                        {
                                            Solid Shadow = Projection.GetShadow(geoEle, mat, plane, SunDirection, doc);
                                            Ilist_Shadow.Add(Shadow);
                                        }
                                        Solid Shadow_Plane = Function.JoinAllSolid(Ilist_Shadow);

                                        if (Shadow_Plane != null && Shadow_Plane.Volume > 1e-3)
                                        {
                                            Solid Wi_North = Solid_Windows[i];
                                            
                                            Solid Wi_Shadow = Function.IntersectSolid(Shadow_Plane, Wi_North);

                                            using (Transaction t = new Transaction(doc, "Show Shadow"))
                                            {
                                                t.Start();
                                                DirectShape ds_shadow = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Casework));
                                                ds_shadow.SetShape(new GeometryObject[] { Wi_Shadow });
                                                t.Commit();
                                            }

                                            if (Wi_Shadow != null && Wi_Shadow.Volume > 1e-3)
                                            {
                                                ShadowArea = Function.FaceMaxArea(Wi_Shadow);
                                                ShadowArea_m2 = Math.Round((ShadowArea * 0.3048 * 0.3048), 3);
                                            }
                                        }
                                        SA.Add(ShadowArea_m2.ToString());
                                    }
                                }
                                
                                string ET_AS = string.Join(";", SA.ToArray());

                                Parameter para = window.LookupParameter(ParameterName[j]);

                                using (Transaction t = new Transaction(doc, "Set value"))
                                {
                                    t.Start();

                                    para.Set(ET_AS);

                                    t.Commit();
                                }

                                j++;
                            }

                            i++;

                        }

                    }
                }
                
                tg.Assimilate();
            }

            //MessageBox.Show("FINISH");
            return Result.Succeeded;
        }
        
    }
}
