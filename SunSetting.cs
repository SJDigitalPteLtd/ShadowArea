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
    public class SunSetting
    {
        #region Get Sun Direction
        
        public static XYZ GetSunDirection(Autodesk.Revit.DB.View view)
        {
            SunAndShadowSettings sunSettings
              = view.SunAndShadowSettings;

            XYZ initialDirection = XYZ.BasisY;

            //double altitude = sunSettings.Altitude;

            double altitude = sunSettings.GetFrameAltitude(
              sunSettings.ActiveFrame);

            Transform altitudeRotation = Transform
              .CreateRotation(XYZ.BasisX, altitude);

            XYZ altitudeDirection = altitudeRotation
              .OfVector(initialDirection);

            //double azimuth = sunSettings.Azimuth;

            double azimuth = sunSettings.GetFrameAzimuth(
              sunSettings.ActiveFrame);

            double actualAzimuth = 2 * Math.PI - azimuth;

            Transform azimuthRotation = Transform
              .CreateRotation(XYZ.BasisZ, actualAzimuth);

            XYZ sunDirection = azimuthRotation.OfVector(
              altitudeDirection);

            return sunDirection;
        }
        #endregion
        
    }
}
