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
using RevitTessellated;
#endregion

namespace AtlasShadowArea
{
    public class Projection
    {
        #region Shadow Analyzer

        public static Solid Shadow (Solid solid, Plane plane, XYZ sundirection)
        {
            ExtrusionAnalyzer exA = ExtrusionAnalyzer.Create(solid, plane, sundirection);

            Face face = exA.GetExtrusionBase();

            PlanarFace planface = face as PlanarFace;

            Solid solid_ex = GeometryCreationUtilities.CreateExtrusionGeometry(
                face.GetEdgesAsCurveLoops(), -planface.FaceNormal, 10 / 304.8);
            
            return solid_ex;
        }

        #endregion

        #region Geometry Element project to Plane

        public static Solid GetShadow(GeometryElement geoEle, Element material, Plane plane, XYZ sundirection,Document doc)
        {
            IList<Solid> AllSolid = Function.GetSolid(geoEle);

            IList<Solid> AllShadow = new List<Solid>();

            foreach (Solid Solid in AllSolid)
            {
                IList<Solid> SplitSolid = SolidUtils.SplitVolumes(Solid);

                IList<Solid> Shadow_Solid = new List<Solid>();
                
                foreach (Solid splitSolid in SplitSolid)
                {
                    int offset = 0;
                    bool a = true;
                    int i = 0;
                    while (a)
                    {
                        try
                        {
                            if (i == 0)
                            {
                                Solid TriangSolid = RevitTessellated.Tessallated.TessallatedtoSolid(splitSolid, material);
                                Solid shadow = Projection.Shadow(TriangSolid, plane, sundirection);
                                Shadow_Solid.Add(shadow);
                            }
                            else if ((0<i) && (i<=2))
                            {
                                Solid TriangSolid = RevitTessellated.Tessallated.TessallatedSolid(splitSolid, material, offset);
                                Solid shadow = Projection.Shadow(TriangSolid, plane, sundirection);
                                Shadow_Solid.Add(shadow);
                            }
                            else 
                            {
                                Solid shadow = Projection.Shadow(splitSolid, plane, sundirection);
                                Shadow_Solid.Add(shadow);
                            }

                            a = false;
                        }
                        catch
                        {
                            offset++;
                            i++;
                            if (i== 4)
                            {
                                a = false;
                            }
                            else
                            {
                                a = true;
                            }
                        }
                    }

                }

                Solid Shadow = Function.JoinAllSolid(Shadow_Solid);
                AllShadow.Add(Shadow);
            }
            Solid RealShadow = Function.JoinAllSolid(AllShadow);


            return RealShadow;
        }

        #endregion


    }
}
