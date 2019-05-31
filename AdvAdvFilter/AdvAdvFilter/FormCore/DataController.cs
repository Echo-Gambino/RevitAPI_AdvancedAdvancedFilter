namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    /// <summary>
    /// DataController acts as a layer between the ModelessForm and the Revit Software
    ///     to make it more convenient to update and retrieve data, and provides
    ///     information if the update has made any significant change to the data.
    /// </summary>
    class DataController
    {

        #region DataTypes

        public enum FilterMode
        {
            Selection = 0,
            View = 1,
            Project = 2,
            Invalid = -1
        }

        #endregion DataTypes

        #region Fields

        private List<ElementId> allElements;
        private List<ElementId> selElements;
        private List<ElementId> movElements;

        #endregion Fields

        #region Parameters

        public List<ElementId> AllElements
        {
            get { return this.allElements; }
        }

        public List<ElementId> SelElements
        {
            set { this.selElements = value; }
            get { return this.selElements; }
        }

        public List<ElementId> MovElements
        {
            set
            {
                if (value == null)
                    this.movElements.Clear();
                else if (value.Count == 0)
                    this.movElements.Clear();
                else
                    this.movElements = value;
            }
            get
            {
                if (this.movElements == null)
                    this.movElements = new List<ElementId>();
                return this.movElements;
            }
        }

        #endregion Parameters

        public DataController()
        {
            this.allElements = new List<ElementId>();
            this.selElements = new List<ElementId>();
            this.movElements = new List<ElementId>();
        }

        public bool UpdateAllElements(List<ElementId> newAllElements)
        {
            bool listChanged = false;

            if ((newAllElements == null) && (this.allElements == null))
            {
                listChanged = false;
            }
            else if (((newAllElements == null) && (this.allElements != null))
                || ((newAllElements != null) && (this.allElements == null)))
            {
                listChanged = true;
            }
            else
            {
                // listChanged = (!newAllElements.All(this.allElements.Contains));
                listChanged = (!newAllElements.SequenceEqual(this.allElements));
            }

            if (listChanged)
            {
                if (newAllElements != null)
                {
                    this.allElements = newAllElements;
                }
                else
                {
                    this.allElements.Clear();
                }
            }

            return listChanged;
        }

        public bool UpdateSelElements(List<ElementId> newSelElements)
        {
            bool listChanged = (!newSelElements.All(this.selElements.Contains));

            if (listChanged)
                this.selElements = newSelElements;

            return listChanged;
        }
    }
}
