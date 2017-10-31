#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;
#endregion

namespace AtlasShadowArea
{
    class App : IExternalApplication
    {
        public string TabName = "SBJ Tools";
        static string _path = System.Reflection.Assembly.GetExecutingAssembly().Location;

        public Result OnStartup(UIControlledApplication a)
        {
            a.CreateRibbonTab(TabName);

            // Create a ribbon panel
            RibbonPanel m_projectPanel = a.CreateRibbonPanel(TabName, " Solar Analysis ");

            // Create button1
            PushButtonData button1 = new PushButtonData("ATLAS.SBJ", "Shadow Area Pro",
                _path, "AtlasShadowArea.ShadedArea");
            button1.ToolTip = "Calculate and illustrate the area of shadow cast from element to one specific face";
            BitmapImage button1_Image = new BitmapImage(new Uri("pack://application:,,,/AtlasShadowArea;component/Resources/sun.png"));
            button1.LargeImage = button1_Image;

            m_projectPanel.AddItem(button1);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}
