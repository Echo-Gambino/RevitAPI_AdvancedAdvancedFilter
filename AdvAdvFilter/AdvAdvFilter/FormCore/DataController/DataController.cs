namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    using TreeNode = System.Windows.Forms.TreeNode;
    using TreeNodeCollection = System.Windows.Forms.TreeNodeCollection;

    using FilterMode = AdvAdvFilter.Common.FilterMode;

    /// <summary>
    /// DataController acts as a layer between the ModelessForm and the Revit Software
    ///     to make it more convenient to update and retrieve data, and provides
    ///     information if the update has made any significant change to the data.
    /// </summary>
    public class DataController
    {

        #region DataTypes
        /*
        public enum FilterMode
        {
            Selection = 0,
            View = 1,
            Project = 2,
            Custom = 3,
            Invalid = -1
        }
        */

        #endregion DataTypes

        #region Fields

        private List<ElementId> allElements;
        private List<ElementId> selElements;

        // Fields related to movement
        private List<ElementId> movElements;
        private bool copyAndShift;
        private List<int> coords;
        // 
        private TreeStructure elementTree;

        #endregion Fields

        #region Parameters

        public List<ElementId> AllElements
        {
            // get { return this.allElements; }
            get { return this.elementTree.SubSet.ToList<ElementId>();  }
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

        public bool CopyAndShift
        {
            get { return this.copyAndShift; }
            set { this.copyAndShift = value; }
        }

        public List<int> Coords
        {
            get
            {
                List<int> output;
                if (this.coords == null)
                {
                    output = new List<int>() { 0, 0, 0 };
                }
                else if (this.coords.Count != 3)
                {
                    output = new List<int>() { 0, 0, 0 };
                }
                else
                {
                    output = this.coords;
                }
                return output;
            }
            set
            {
                if (value == null)
                {
                    this.coords.Clear();
                }
                else if (value.Count != 3)
                {
                    throw new ArgumentException();
                }
                else
                {
                    this.coords = value;
                }                        
            }
        }

        public TreeStructure ElementTree
        {
            get { return this.elementTree; }
        }

        #endregion Parameters

        #region Constructor

        public DataController(Document doc)
        {
            if (doc == null) throw new ArgumentNullException();

            this.allElements = new List<ElementId>();
            this.selElements = new List<ElementId>();
            // Fields related to movement
            this.movElements = new List<ElementId>();
            this.copyAndShift = true;
            this.coords = new List<int>() { 0, 0, 0 };

            this.elementTree = new TreeStructure(doc);
        }

        #endregion Constructor

        #region Controls

        public void SetMode(FilterMode mode)
        {
            this.elementTree.SetSubSet(mode);
        }

        #endregion Controls

        #region AllElements Operations

        public void SetAllElements(List<ElementId> elementIds)
        {
            this.elementTree.ClearAll();
            this.elementTree.AppendList(elementIds);
        }

        public void ClearAllElements()
        {
            this.elementTree.ClearAll();
        }

        public void AddToAllElements(List<ElementId> elementIds)
        {
            this.elementTree.AppendList(elementIds);
        }

        public void RemoveFromAllElements(List<ElementId> elementIds)
        {
            this.elementTree.RemoveList(elementIds);
        }

        public void GetAllElements()
        {
            List<ElementId> keyList = this.elementTree.ElementIdNodes.Keys.ToList();
        }


        #endregion AllElements Operations

        #region Auxiliary Methods

        /// <summary>
        /// Updates the elements of this.allElements with newAllElements
        /// </summary>
        /// <param name="newAllElements"></param>
        /// <returns>true if this.allElements != newAllElements, else false</returns>
        public bool UpdateAllElements(List<ElementId> newAllElements)
        {
            bool listChanged = false;

            // Perform tests to see if the list has been changed
            if ((newAllElements == null) && (this.allElements == null))
            {
                listChanged = false; // If both are null, then they didn't change
            }
            else if (((newAllElements == null) && (this.allElements != null))
                || ((newAllElements != null) && (this.allElements == null)))
            {
                listChanged = true; // If one of them is null and the other not, then it changed
            }
            else
            {
                // Perform a LINQ? statement
                // listChanged = (!newAllElements.All(this.allElements.Contains));
                listChanged = (!newAllElements.SequenceEqual(this.allElements));
            }

            // If listChanged, then...
            if (listChanged)
            {
                if (newAllElements != null)
                {
                    // If newAllElements isn't null, then update this.allElements
                    this.allElements = newAllElements;
                }
                else
                {
                    // If its null, then simply clear all items from this.allElements
                    this.allElements.Clear();
                }
            }

            return listChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newSelElements"></param>
        /// <returns></returns>
        public bool UpdateSelElements(List<ElementId> newSelElements)
        {
            // Check if the list has changed by doing something of a LINQ? statement
            bool listChanged = (!newSelElements.All(this.selElements.Contains));

            // If the list has detected a change, then update this.selElements
            if (listChanged)
                this.selElements = newSelElements;

            return listChanged;
        }

        /// <summary>
        /// Checks if two lists of ElementId have the same contents (ignore ordering)
        /// </summary>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public bool IsListEqual(List<ElementId> list1, List<ElementId> list2)
        {
            bool b1 = list1.All(e => list2.Contains(e));
            bool b2 = list2.All(e => list1.Contains(e));
            bool b3 = (list1.Count == list2.Count);

            bool result = b1 && b2 && b3;

            return result;
        }

        #endregion Auxiliary Methods


    }

}
