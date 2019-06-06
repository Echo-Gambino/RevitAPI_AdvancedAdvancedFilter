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

        // Fields related to movement
        private List<ElementId> movElements;
        private bool copyAndShift;
        private List<int> coords;

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

        #endregion Parameters

        public DataController()
        {
            this.allElements = new List<ElementId>();
            this.selElements = new List<ElementId>();
            // Fields related to movement
            this.movElements = new List<ElementId>();
            this.copyAndShift = true;
            this.coords = new List<int>() { 0, 0, 0 };
        }

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

    class NodeData
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

    class ElementSet
    {
        private Dictionary<string, ElementSet> branch;
        private HashSet<ElementId> set;

        public HashSet<ElementId> Set
        {
            get { return this.set; }
            set { this.set = value; }
        }

        public ElementSet()
        {
            branch = new Dictionary<string, ElementSet>();
            set = new HashSet<ElementId>();
        }

        /// <summary>
        /// Get a specific element set from branch
        /// </summary>
        public ElementSet GetElementSet(string key)
        {
            if (!branch.ContainsKey(key))
                return null;
            return branch[key];
        }

        /// <summary>
        /// Add a 'branch' onto the dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ElementSet AddBranch(string key)
        {
            if (key == null)
                throw new ArgumentNullException();
            else if (branch.ContainsKey(key))
                throw new ArgumentException();

            ElementSet newBranch = new ElementSet();

            branch.Add(key, newBranch);

            return newBranch;
        }

        /// <summary>
        /// Recursively get a union set of their children
        /// </summary>
        public void RecursiveUpdateCache()
        {
            if (branch.Count == 0) return;

            HashSet<ElementId> tmpSet = new HashSet<ElementId>();

            foreach (KeyValuePair<string, ElementSet> kvp in branch)
            {
                kvp.Value.RecursiveUpdateCache();
                tmpSet.UnionWith(kvp.Value.Set);
            }

            this.set = tmpSet;
        }
     
    }

    class ElementTree
    {

        private enum depth
        {
            CategoryType = 0,
            Category = 1,
            Family = 2,
            ElementType = 3,
            Instance = 4,
            Invalid = -1
        };

        private ElementSet setTree;

        private Dictionary<ElementId, TreeNode> elementIdNodes;

        private List<string> heirarchy;

        private Document doc;

        
        public ElementTree(Document doc)
        {
            this.doc = doc;

            this.elementIdNodes = new Dictionary<ElementId, TreeNode>();

            this.setTree = new ElementSet();
        }

        #region Construct elementTree

        public void AppendList(List<ElementId> elementIds)
        {
            List<NodeData> nodesToAdd = new List<NodeData>();

            // Add to elementIdNodes first
            foreach (ElementId id in elementIds)
            {
                if (this.elementIdNodes.ContainsKey(id)) continue;

                // Generate Nodedata to be put into node, and the node itself
                NodeData data = GenerateNodeData(id, this.doc);
                TreeNode node = new TreeNode();

                node.Name = data.Id.ToString();
                node.Text = node.Name;
                node.Tag = data;

                // Add elementId to listOfElementIds to be added in the tree
                nodesToAdd.Add(data);
                // Add elementid and its corresponding node in this.elementIdNodes
                this.elementIdNodes.Add(id, node);
            }

            // Update the internal tree structure
            AddToTree(nodesToAdd, setTree, depth.CategoryType);

        }

        private void AddToTree(List<NodeData> nodes, ElementSet set, depth depth)
        {
            depth nextDepth;
            ElementSet nextSet;
            Dictionary<string, List<NodeData>> grouping = new Dictionary<string, List<NodeData>>();

            if (depth == depth.Invalid)
            {
                throw new ArgumentException();
            }
            else if (depth == depth.Instance)
            {
                // current depth is the last depth, set nextDepth to depth.Invalid
                nextDepth = depth.Invalid;
            }
            else
            {
                // Get next depth down
                nextDepth = (depth)((int)depth + 1);
            }

            string key;
            string depthStr = depth.ToString();
            foreach (NodeData n in nodes)
            {
                key = n.GetParameter(depthStr);

                if (!grouping.ContainsKey(key))
                {
                    grouping.Add(key, new List<NodeData>());                    
                }

                grouping[key].Add(n);
                
            }

            foreach (KeyValuePair<string, List<NodeData>> kvp in grouping)
            {                
                ElementSet s = set.GetElementSet();

            }

            // Add the data into the set
            foreach (NodeData node in nodes)
            {
                set.Set.Add(node.Id);
            }

            if (depth == depth.Instance)
            {
            }
            else
            {


            }




        }

        /*
        private void ElementTreeAdd(
            List<Element> elements,
            Dictionary<string, object> tree,
            List<string> heirarchy
            );
        */
        #endregion Construct elementTree

        #region Auxiliary Functions

        private NodeData GenerateNodeData(ElementId elementId, Document doc)
        {
            // Fail the execution if GenerateNodeData has elementId or doc as null
            if ((elementId == null) || (doc == null))
                throw new ArgumentNullException();

            // Get elementId's element
            Element element = doc.GetElement(elementId);

            // If resulting element is null, fail the process, as it should always return not null
            if (element == null) throw new InvalidOperationException();

            // Generate NodeData
            NodeData data = new NodeData();

            // Set ElementId
            data.Id = elementId;

            // Set fields related to category
            Category category = element.Category;
            if (category != null)
            {
                data.CategoryType = category.CategoryType.ToString();
                data.Category = category.ToString();
            }

            // Set fields related to elementType
            ElementId typeId = element.GetTypeId();
            if (typeId != null)
            {
                ElementType elementType = doc.GetElement(typeId) as ElementType;
                if (elementType != null)
                {
                    data.Family = elementType.FamilyName;
                    data.ElementType = elementType.Name;
                }
            }

            return data;
        }

        #endregion Auxiliary Functions

    }


}
