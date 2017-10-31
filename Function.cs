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
    public class Function
    {
        #region Get Solid
        // Get IList Solid of GeometryElement
        public static IList<Solid> GetSolid(GeometryElement geometryelement)
        {
            IList<Solid> Ilist_Solid = new List<Solid>();
            foreach (GeometryObject geometryobject in geometryelement)
            {
                Solid solid = geometryobject as Solid;
                if (solid != null && solid.Volume > 1e-3)
                {
                    Ilist_Solid.Add(solid);
                }
                
                GeometryInstance geometryinstance = geometryobject as GeometryInstance;
                if (geometryinstance != null)
                {
                    GeometryElement geometry_element = geometryinstance.GetInstanceGeometry();
                    Ilist_Solid = GetSolid(geometry_element);
                }
            }
            return Ilist_Solid;
        }
        #endregion

        #region Join All Solid
        

        public static Solid JoinAllSolid(IEnumerable<Solid> Ilist_Solid)
        {
            Solid All_Solid = null;
            foreach (Solid solid in Ilist_Solid)
            {
                if (All_Solid == null)
                {
                    All_Solid = solid;
                }
                else
                {
                    try
                    {
                        All_Solid = BooleanOperationsUtils.ExecuteBooleanOperation(All_Solid, solid, BooleanOperationsType.Union);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return All_Solid;
        }
        #endregion
        
        #region  Get Plane of Windows

        public static Plane GetPlane (Element ele_window, Document doc, Autodesk.Revit.Creation.Application creapp, Autodesk.Revit.Creation.Document credoc)
        {

            FamilyInstance fi_window = ele_window as FamilyInstance;
            Wall wa_host = fi_window.Host as Wall;
            
            XYZ normal = fi_window.FacingOrientation;
            double wa_width = wa_host.Width;
            
            Transform offset = Transform.CreateTranslation((wa_width / normal.GetLength()) * normal);

            CurveLoop wi_curveloop = Autodesk.Revit.DB.IFC.ExporterIFCUtils.
                GetInstanceCutoutFromWall(doc, wa_host, fi_window, out normal);

            CurveArray wi_curve_array = creapp.NewCurveArray();

            foreach (Curve curve in wi_curveloop)
            {
                wi_curve_array.Append(curve.CreateTransformed(offset));
            }
            
            Plane plane = creapp.NewPlane(wi_curve_array); 

            return plane;
        }

        #endregion
        
        #region Get Solid of Window
        

        public static Solid SolidCurve(Element ele_window, Document doc)
        {

            FamilyInstance fi_window = ele_window as FamilyInstance;
            Wall wa_host = fi_window.Host as Wall;
            
            XYZ normal = fi_window.FacingOrientation;
            double wa_width = wa_host.Width+5;

            IList<CurveLoop> Li_curveloop = new List<CurveLoop>();

            CurveLoop wi_curveloop = Autodesk.Revit.DB.IFC.ExporterIFCUtils.
                GetInstanceCutoutFromWall(doc, wa_host, fi_window, out normal);
            
            Li_curveloop.Add(wi_curveloop);

            IList<Solid> Solid_Curve = new List<Solid>();

            Solid solid_curve1 = GeometryCreationUtilities.CreateExtrusionGeometry(
                   Li_curveloop, normal, wa_host.Width);

            Solid solid_curve2 = GeometryCreationUtilities.CreateExtrusionGeometry(
                   Li_curveloop, -normal, wa_host.Width);

            Solid_Curve.Add(solid_curve1);
            Solid_Curve.Add(solid_curve2);

            Solid SolidCurve = JoinAllSolid(Solid_Curve);

            return SolidCurve;
        }

        #endregion

        #region Intersect Solid

        public static Solid IntersectSolid(Solid shadow, Solid window)
        {
            Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
                shadow, window, BooleanOperationsType.Intersect);

            return intersection;
        }
        #endregion

        #region Face Area

        public static double FaceMaxArea(Solid intersection)
        {
            double facemax = 0.0;
            foreach (Face face in intersection.Faces)
            {

                if (face.Area > facemax)
                {
                    facemax = face.Area;
                }
            }
            return facemax;
        }

        #endregion
    }
}
