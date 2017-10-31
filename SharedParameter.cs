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
    public class SharedParameter
    {
        #region AddSharedParameter

        public Result AddSharedParameter (ExternalCommandData commandData,  
            string Path, string ParameterName, string gu)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Document doc = uidoc.Document;

            app.SharedParametersFilename = Path;
            //string sharedParamsFileName = app.SharedParametersFilename;

            DefinitionFile definition = app.OpenSharedParameterFile();
            DefinitionGroups groups = definition.Groups;

            DefinitionGroup group = groups.get_Item("ShadowArea");

            if (group == null)
            {
                try
                {
                    group = groups.Create("ShadowArea");
                }
                catch (Exception)
                {
                    group = null;
                }
            }

            Definitions sharepara = group.Definitions;

            //tao share option

            ExternalDefinitionCreationOptions shareoption = new ExternalDefinitionCreationOptions(ParameterName, ParameterType.Text);
            shareoption.UserModifiable = false;
            Guid guid = new Guid(gu);
            shareoption.GUID = guid;

            Definition myDefinition = group.Definitions.Create(shareoption);

            CategorySet myCategories = app.Create.NewCategorySet();
            
            Category myCategory = Category.GetCategory(doc, BuiltInCategory.OST_Windows);


            myCategories.Insert(myCategory);
            
            InstanceBinding instanceBinding = app.Create.NewInstanceBinding(myCategories);
            
            BindingMap bindingMap = doc.ParameterBindings;


            bool instanceBindOK = bindingMap.Insert(myDefinition,
                                                instanceBinding, BuiltInParameterGroup.PG_ENERGY_ANALYSIS);

            return Result.Succeeded;

        }



        #endregion
        
        public static string Path(Document doc)
        {
            // Get file Revit Path
            string path = doc.PathName;
            if (string.IsNullOrEmpty(path) || !path.Contains("\\"))
            {
                MessageBox.Show("Please Save First!");
                //return Result.Failed;
            }
            string ModelName = doc.Title;

            // Create new txt path
            string Path = path.Remove(path.IndexOf(ModelName)) + "AtlasSharedParameter.txt";
            //MessageBox.Show(Path);

            //Set SharedParameter File
            StreamWriter stream;
            stream = new StreamWriter(Path);
            stream.Close();

            return Path;
        }
    }
}
