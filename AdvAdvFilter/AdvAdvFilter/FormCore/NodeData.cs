namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Autodesk.Revit.DB;

    public class NodeData
    {
        #region Fields

        private ElementId id;
        private string categoryType;
        private string category;
        private string family;
        private string elementType;

        #endregion Fields

        #region Parameters

        public ElementId Id
        {
            get { return this.id; }
            set { this.id = value; }
        }

        public string CategoryType
        {
            get { return this.categoryType; }
            set { this.categoryType = AlwaysGetString(value, "CategoryType"); }
        }

        public string Category
        {
            get { return this.category; }
            set { this.category = AlwaysGetString(value, "Category"); }
        }

        public string Family
        {
            get { return this.family; }
            set { this.family = AlwaysGetString(value, "Family"); }
        }

        public string ElementType
        {
            get { return this.elementType; }
            set { this.elementType = AlwaysGetString(value, "ElementType"); }
        }

        #endregion Parameters

        public NodeData()
        {
            this.Id = null;
            this.CategoryType = null;
            this.Category = null;
            this.Family = null;
            this.ElementType = null;
        }

        /// <summary>
        /// Returns string input if its not null,
        /// else it returns "null" or "No {fieldName}" if fieldName is not null
        /// </summary>
        /// <param name="input"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private string AlwaysGetString(string input, string fieldName = null)
        {
            if (input == null)
            {
                if (fieldName == "")
                {
                    input = "null";
                }
                else
                {
                    input = "No " + fieldName;
                }
            }
            return input;
        }

        /// <summary>
        /// Attempt to get the parameter value using a parameter key,
        /// if the parameter key cannot be found, return "null" instead
        /// </summary>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        public string GetParameter(string parameterName)
        {
            string parameterValue = "";

            switch (parameterName)
            {
                case "CategoryType":
                    parameterValue = this.categoryType;
                    break;
                case "Category":
                    parameterValue = this.category;
                    break;
                case "Family":
                    parameterValue = this.family;
                    break;
                case "ElementType":
                    parameterValue = this.elementType;
                    break;
                default:
                    parameterValue = "null";
                    break;
            }
            return parameterValue;
        }

    }

}
