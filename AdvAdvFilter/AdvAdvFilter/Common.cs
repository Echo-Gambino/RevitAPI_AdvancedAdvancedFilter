using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvAdvFilter
{
    using Autodesk.Revit.DB;

    class CommonMethods
    {
        public static string GetElementCategory(Element element, Document doc)
        {
            string txt = "";

            try
            {
                ElementId eId = element.Category.Id;
                Category c = Category.GetCategory(doc, eId);
                txt = c.Name;
                // string txt = element.Category.Name;
            }
            catch (Exception ex)
            {
                txt = ex.Message;
            }
            txt = "Dummy THICC";

            return txt;
        }

        public static string GetElementFamily(Element element, Document doc)
        {
            string txt = "\t\t";

            try
            {
                ElementId eId = element.GetTypeId();
                ElementType type = doc.GetElement(eId) as ElementType;
                txt = type.FamilyName;
            }
            catch (Exception ex)
            {
                txt = ex.Message;
            }

            return txt;
        }

        public static string GetElementType(Element element)
        {
            string txt = element.Name;
            return txt;
        }

        public static string GetElementInstanceId(Element element)
        {
            // string txt = element.UniqueId;
            string txt = element.Id.ToString();
            return txt;
        }

    }
}
