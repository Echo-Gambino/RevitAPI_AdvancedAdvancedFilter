namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Element = Autodesk.Revit.DB.Element;
    using ElementId = Autodesk.Revit.DB.ElementId;
    using Category = Autodesk.Revit.DB.Category;
    using FamilyInstance = Autodesk.Revit.DB.FamilyInstance;
    using Family = Autodesk.Revit.DB.Family;
    using Document = Autodesk.Revit.DB.Document;
    using ElementType = Autodesk.Revit.DB.ElementType;

    class AdvTreeNode : TreeNode
    {
        public int numCheckedLeafs;
        public int totalLeafs;
        public string realText;

        public AdvTreeNode()
        {
            this.numCheckedLeafs = 0;
            this.totalLeafs = 0;
        }

        public string UpdateCounter()
        {
            this.Text = string.Format("[ {0}/{1} ] {2}", this.numCheckedLeafs, this.totalLeafs, this.realText);
            return this.Text;
        }

        public bool UpdateIsChecked(bool status)
        {
            if (this.Checked == status)
                return false;
            this.Checked = status;
            return true;
        }
    }

    class LeafTreeNode : AdvTreeNode
    {
        #region Fields

        public ElementId ElementId;

        private string categoryType;
        private string category;
        private string family;
        private string elementType;

        #endregion Fields

        #region Parameters

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

        public LeafTreeNode()
        {
            this.ElementId = null;

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

    class BranchTreeNode : AdvTreeNode
    {
        public bool isAllCollapsed;

        public BranchTreeNode()
        {
            this.isAllCollapsed = true;
        }
    }

    class ElementSelectionController
    {
        #region Fields

        private Panel panel;
        private Label totalLabel;
        private TreeView treeView;
        private List<BranchTreeNode> categoryTypes;
        private List<LeafTreeNode> leafNodes;

        private object treeLock = new object();

        #endregion

        #region Parameters

        public Panel Panel
        {
            get { return this.panel; }
        }

        public Label TotalLabel
        {
            get { return this.totalLabel; }
        }

        public TreeView TreeView
        {
            get { return this.treeView; }
        }

        #endregion

        public ElementSelectionController(
            Panel panel,
            Label totalLabel,
            TreeView treeView
            )
        {
            this.panel = panel;
            this.totalLabel = totalLabel;
            this.treeView = treeView;

            // this.categoryTypes = GetCategoryNodes();
            this.leafNodes = new List<LeafTreeNode>();
        }

        // TODO: should probably move this to datacontroller.
        /// <summary>
        /// 
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

        #region Update TreeView Structure

        public void UpdateTreeViewStructure(
            List<ElementId> newElementIds,
            RevitController rCon
            )
        {
            if ((newElementIds == null) || (rCon == null))
                throw new ArgumentNullException();

            /// Get information that will be needed to perform the necessary actions
            // Get list of old elementIds
            IEnumerable<ElementId> oldElementIds
                = from LeafTreeNode leaf in this.leafNodes
                  select leaf.ElementId;
            // Get list of elements to add onto the treeView
            IEnumerable<ElementId> addList
                = from ElementId eId in newElementIds
                  where (!oldElementIds.Contains(eId))
                  select eId;
            // Get list of elementIds to remove from the treeView
            IEnumerable<ElementId> remList
                = from ElementId eId in oldElementIds
                  where (!newElementIds.Contains(eId))
                  select eId;

            // Get list of leafNodes to add onto treeView using addList
            List<LeafTreeNode> leafNodesToAdd = ConstructLeafNodeList(addList, rCon);
            // Get list of leafNodes to remove from treeView using remList
            List<LeafTreeNode> leafNodesToRem = RetrieveLeafNodes(remList);

            /// Execute actions that will modify the treeView
            // Selectively add leaf nodes into the treeView without scrapping everything
            AddLeafNodes(leafNodesToAdd, this.treeView.Nodes);
            // Selectively remove leafnodes in the treeView
            RemLeafNodes(leafNodesToRem, this.treeView.Nodes);
            // Update the category nodes
            UpdateCategoryTypeNodes(this.treeView.Nodes);
        }

        #region TreeNode Removal

        private void TreeStructureRem(
            List<LeafTreeNode> leafNodes,
            TreeNodeCollection treeNodes,
            List<string> heirarchy
            )
        {
            if ((treeNodes == null)
                || (leafNodes == null)
                || (heirarchy == null))
            {
                throw new ArgumentNullException();
            }

            // Assumed to be at within the last branch of the heirarchy
            if (heirarchy.Count == 0)
            {
                foreach (LeafTreeNode leaf in leafNodes)
                {
                    // Remove leaf in lastBranch
                    treeNodes.Remove(leaf);
                    // Remove leaf from leafNodes list
                    this.leafNodes.Remove(leaf);
                }
                return;
            }

            SortedDictionary<string, List<LeafTreeNode>> grouping = new SortedDictionary<string, List<LeafTreeNode>>();
            string paramName = heirarchy[0];

            // Construct the dictionary to group the leaves together by a certain parameter,
            // this is so that we don't send the whole list down in every concievable branch
            string paramValue;
            foreach (LeafTreeNode leaf in leafNodes)
            {
                paramValue = leaf.GetParameter(paramName);

                if (!grouping.ContainsKey(paramValue))
                    grouping.Add(paramValue, new List<LeafTreeNode>());

                grouping[paramValue].Add(leaf);
            }

            // Clone the heirarchy but remove the first item of the list
            List<string> newHeirarchy = new List<string>(heirarchy);
            newHeirarchy.RemoveAt(0);

            grouping.OrderBy(key => key.Key);
            foreach (KeyValuePair<string, List<LeafTreeNode>> kvp in grouping)
            {
                // If treeNodes doesn't have a node with the name of the
                // selected parameter value of a group of leaves,
                // then delete all nodes with that parameter name
                if (!treeNodes.ContainsKey(kvp.Key))
                {
                    foreach (LeafTreeNode leaf in leafNodes)
                    {
                        // Remove leaf 
                        leaf.Remove();
                        // Remove leaf from leafNodes list
                        this.leafNodes.Remove(leaf);
                    }

                    continue;
                }

                // Update counter
                BranchTreeNode branch = treeNodes[kvp.Key] as BranchTreeNode;
                branch.totalLeafs -= kvp.Value.Count;
                branch.UpdateCounter();

                // Recurse into the node that has the node name of kvp.Key
                TreeStructureRem(kvp.Value, treeNodes[kvp.Key].Nodes, newHeirarchy);

                if (treeNodes[kvp.Key].Nodes.Count == 0)
                {
                    treeNodes[kvp.Key].Remove();
                }
            }

            return;
        }

        private void RemLeafNodes(
            List<LeafTreeNode> leafNodes,
            TreeNodeCollection treeNodes
            )
        {
            if (leafNodes.Count == 0) return;

            List<string> heirarchy = new List<string>()
            {
                "CategoryType",
                "Category",
                "Family",
                "ElementType"
            };

            TreeStructureRem(leafNodes, treeNodes, heirarchy);
        }

        private List<LeafTreeNode> RetrieveLeafNodes(
            IEnumerable<ElementId> elementIds)
        {
            List<LeafTreeNode> retrievedLeaves;

            IEnumerable<LeafTreeNode> leavesToRemove
                = from LeafTreeNode in this.leafNodes
                  where elementIds.Contains(LeafTreeNode.ElementId)
                  select LeafTreeNode;

            if (leavesToRemove == null)
            {
                retrievedLeaves = new List<LeafTreeNode>();
            }
            else
            {
                retrievedLeaves = leavesToRemove.ToList();
            }

            return retrievedLeaves;
        }

        #endregion TreeNode Removal

        #region TreeNode Insertion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="leafNodes"></param>
        /// <param name="treeNodes"></param>
        /// <param name="heirarchy"></param>
        private void TreeStructureAdd(
            List<LeafTreeNode> leafNodes,
            TreeNodeCollection treeNodes,
            List<string> heirarchy
            )
        {
            if ((treeNodes == null)
                || (leafNodes == null)
                || (heirarchy == null))
            {
                throw new ArgumentNullException();
            }

            // Assumed to be at within the last branch of the heirarchy
            if (heirarchy.Count == 0)
            {
                foreach (LeafTreeNode leaf in leafNodes)
                {
                    // Add leaf in lastBranch
                    treeNodes.Add(leaf);
                    // Add leaf to leafNodes list for easy access
                    this.leafNodes.Add(leaf);
                }
                return;
            }

            SortedDictionary<string, List<LeafTreeNode>> grouping = new SortedDictionary<string, List<LeafTreeNode>>();
            string paramName = heirarchy[0];

            // Construct the dictionary to group the leaves together by a certain parameter,
            // this is so that we don't send the whole list down in every concievable branch
            string paramValue;
            foreach (LeafTreeNode leaf in leafNodes)
            {
                paramValue = leaf.GetParameter(paramName);

                if (!grouping.ContainsKey(paramValue))
                    grouping.Add(paramValue, new List<LeafTreeNode>());

                grouping[paramValue].Add(leaf);
            }

            // Clone the heirarchy but remove the first item of the list
            List<string> newHeirarchy = new List<string>(heirarchy);
            newHeirarchy.RemoveAt(0);

            grouping.OrderBy(key => key.Key);
            foreach (KeyValuePair<string, List<LeafTreeNode>> kvp in grouping)
            {
                BranchTreeNode branch;
                // If treeNodes doesn't have a node with the name of the
                // selected parameter value of a group of leaves,
                // then make a node that has that name
                if (!treeNodes.ContainsKey(kvp.Key))
                {
                    branch = new BranchTreeNode();
                    branch.Name = kvp.Key;
                    branch.Text = kvp.Key;
                    branch.realText = kvp.Key;
                    branch.totalLeafs = 0;
                    treeNodes.Add(branch);
                }
                else
                {
                    branch = treeNodes[kvp.Key] as BranchTreeNode;
                }

                // Update counter
                branch.totalLeafs += kvp.Value.Count;
                branch.UpdateCounter();

                // Recurse into the node that has the node name of kvp.Key
                TreeStructureAdd(kvp.Value, branch.Nodes, newHeirarchy);
                // TreeStructureAdd(kvp.Value, treeNodes[kvp.Key].Nodes, newHeirarchy);
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="leafNodes"></param>
        /// <param name="treeNodes"></param>
        private void AddLeafNodes(
            List<LeafTreeNode> leafNodes,
            TreeNodeCollection treeNodes
            )
        {
            // Gives the method an early out if there are no leafNodes to be added
            if (leafNodes.Count == 0) return;

            List<string> heirarchy = new List<string>()
            {
                "CategoryType",
                "Category",
                "Family",
                "ElementType"
            };

            // Call the recursive method UpdateTreeStructure
            TreeStructureAdd(leafNodes, treeNodes, heirarchy);
        }

        /// <summary>
        /// Constructs a list of leafNodes from
        /// a list of elementIds and the revitController
        /// </summary>
        /// <param name="elementIds"></param>
        /// <param name="rCon"></param>
        /// <returns></returns>
        private List<LeafTreeNode> ConstructLeafNodeList(
            IEnumerable<ElementId> elementIds,
            RevitController rCon
            )
        {
            // Construct leafNodes to add onto the treeView
            List<LeafTreeNode> leafNodesList = new List<LeafTreeNode>();
            LeafTreeNode leafNode;
            Element element;

            foreach (ElementId eId in elementIds)
            {
                // Get the Revit element from elementId
                element = rCon.Doc.GetElement(eId);
                // Get the leafNode from element and elementType
                leafNode = ConstructLeafNode(element, rCon.GetElementType(element));
                // Add leafNode into the leafNodesToAdd
                leafNodesList.Add(leafNode);
            }

            return leafNodesList;
        }

        private LeafTreeNode ConstructLeafNode(
            Element element,
            ElementType elementType
            )
        {
            if (element == null)
                throw new ArgumentNullException();

            LeafTreeNode leaf = new LeafTreeNode();

            // Get leaf's essential information
            leaf.ElementId = element.Id;
            leaf.Text = element.Id.ToString();
            leaf.realText = element.Id.ToString();
            leaf.Name = element.Id.ToString();

            // Get the category from element and attempts to
            // fill out the category section of the leaf
            Category category = element.Category;
            if (category != null)
            {
                leaf.CategoryType = category.CategoryType.ToString();
                leaf.Category = category.Name;
            }

            // If elementType isn't null, then fill out the
            // leaf's Family and ElementType fields
            if (elementType != null)
            {
                leaf.Family = elementType.FamilyName;
                leaf.ElementType = elementType.Name;
            }

            return leaf;
        }

        #endregion TreeNode Insertion

        #region CategoryType Node Setup

        /// <summary>
        /// Set up the category type nodes in the treeView
        /// </summary>
        /// <param name="categoryType"></param>
        private void UpdateCategoryTypeNodes(TreeNodeCollection categoryType)
        {
            // For each categoryType node...
            foreach (BranchTreeNode cNode in categoryType)
            {
                // Expand the its tree if it isn't already
                if (!cNode.IsExpanded)
                    cNode.Expand();
            }
        }

        #endregion CategoryType Node Setup

        #endregion Update TreeView Structure

        #region Collapse and Expand Nodes

        /// <summary>
        /// If the children of the branch is all collapsed, expand all the children.
        /// else, collapse all the children
        /// </summary>
        /// <param name="branch"></param>
        public void ToggleCollapse(BranchTreeNode branch)
        {
            // Returns true if every node in the collection is collapsed, else return false
            bool AllCollapsed(TreeNodeCollection collection)
            {
                foreach (AdvTreeNode node in collection)
                    if (node.IsExpanded) return false;
                return true;
            }

            TreeNodeCollection children = branch.Nodes;

            if (AllCollapsed(children))
            {
                // Expand all nodes under the branch
                branch.ExpandAll();
            }
            else
            {
                // Individually collapse each child (without collapsing branch node)
                foreach (AdvTreeNode node in children)
                    node.Collapse();
            }
        }

        #endregion Collapse and Expand Nodes

        #region Update TreeView Upon Check

        public void UpdateAfterCheck(AdvTreeNode e)
        {
            AdvTreeNode GetRoot(AdvTreeNode node)
            {
                AdvTreeNode tmpRoot = null;

                if (node == null) return null;

                tmpRoot = GetRoot(node.Parent as AdvTreeNode);

                if (tmpRoot == null)
                    tmpRoot = node;

                return tmpRoot;
            }

            if (e == null)
                return;

            lock (treeLock)
            {
                // Update their children if it has any
                if (e.Nodes.Count > 0)
                    UpdateChildNodes(e, e.Checked);

                // Update their parents if it has any
                if (e.Parent != null)
                    UpdateParentNodes(e.Parent as AdvTreeNode, e.Checked);

                AdvTreeNode root = GetRoot(e);

                // UpdateCheckedCounters(root);
                UpdateCounter(root);
            }

            return;
        }

        private void UpdateChildNodes(AdvTreeNode node, bool isChecked)
        {
            if (node == null) return;

            foreach (AdvTreeNode n in node.Nodes)
            {
                if (n.Checked == isChecked) continue;

                // Update the status of n
                n.Checked = isChecked;

                // If n has children, then update them too
                if (n.Nodes.Count > 0)
                {
                    UpdateChildNodes(n, isChecked);
                }
            }
        }

        private void UpdateParentNodes(AdvTreeNode parent, bool isChecked)
        {
            bool isAllChecked(TreeNodeCollection collection)
            {
                foreach (AdvTreeNode n in collection)
                    if (!n.Checked) return false;
                return true;
            }

            if (parent == null) return;

            parent.Checked = isAllChecked(parent.Nodes);

            // If parent's parent isn't null, continue the recursion up
            if (parent.Parent != null)
                UpdateParentNodes(parent.Parent as AdvTreeNode, isChecked);

            return;
        }


        #endregion Update TreeView Upon Check

        #region Update TreeView Upon Revit Selection

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selected"></param>
        /// <returns></returns>
        public bool UpdateSelectedLeaves(ICollection<ElementId> selected)
        {
            bool updateSuccess = true;

            lock (treeLock)
            {
                IEnumerable<ElementId> leafNodeElementIds
                            = from LeafTreeNode leaf in this.leafNodes
                              select leaf.ElementId;

                foreach (ElementId elementId in selected)
                {
                    // If the leaf node doesn't exist in the selected elementIds,
                    // Then the leafNodes are outdated and the update won't work
                    if (!leafNodeElementIds.Contains(elementId))
                    {
                        updateSuccess = false;
                        break;
                    }
                }

                if (updateSuccess)
                {
                    foreach (LeafTreeNode leaf in this.leafNodes)
                    {
                        if (selected.Contains(leaf.ElementId))
                        {
                            if (!leaf.Checked)
                            {
                                leaf.Checked = !leaf.Checked;
                                UpdateAfterCheck(leaf);
                                // UpdateTotalSelectedItemsLabel();
                                UpdateLabelTotals();
                            }
                        }
                        else
                        {
                            if (leaf.Checked)
                            {
                                leaf.Checked = !leaf.Checked;
                                UpdateAfterCheck(leaf);
                                // UpdateTotalSelectedItemsLabel();
                                UpdateLabelTotals();
                            }
                        }
                    }
                }
            }
            return updateSuccess;
        }

        #endregion Update TreeView Upon Revit Selection

        #region Get Selection From TreeView

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<ElementId> GetSelectedElementIds()
        {
            List<ElementId> output = null;
            int max = 5;

            while (max > 0)
            {
                max -= 1;
                try
                {
                    IEnumerable<ElementId> selected
                                = from LeafTreeNode leaf in this.leafNodes
                                  where leaf.Checked
                                  select leaf.ElementId;
                    output = selected.ToList<ElementId>();
                    break;
                }
                catch (InvalidOperationException ex)
                {
                    continue;
                }
                catch (ArgumentNullException ex)
                {
                    continue;
                }
            }
            return output;
        }

        #endregion Get Selection From TreeView
        
        #region Update Selected Node Count

        /// <summary>
        /// Update the given node's selection counter along with their children as well.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public int UpdateCounter(AdvTreeNode node)
        {
            TreeNodeCollection collection = node.Nodes;
            int numberChecked = 0;
            
            // If collection.Count == 0, then that must mean that this is a leaf node
            if (collection.Count == 0)
            {
                // If node is checked set numberChecked = 1 instead of leaving it equal 0
                if (node.Checked)
                    numberChecked = 1;
                return numberChecked;
            }

            // For each AdvTreeNode in the collection
            foreach (AdvTreeNode n in collection)
            {
                // Add up the number of checked elements with a recursive call
                numberChecked += UpdateCounter(n);
            }

            // Set the total elements selected (numberChecked) to numCheckedLeafs
            node.numCheckedLeafs = numberChecked;
            // Update the node's counter
            node.UpdateCounter();

            // Pass the total up to the caller
            return numberChecked;
        }

        /// <summary>
        /// Updates this.totalLabel to show the user how many elements are
        /// selected out of the total elements in the treeView.
        /// </summary>
        public void UpdateLabelTotals()
        {
            TreeNodeCollection collection = treeView.Nodes;

            int total = 0;
            int max = 0;

            // For each node in the collection, update tally up the checked and maximum checked leaves
            foreach (AdvTreeNode node in collection)
            {
                total += node.numCheckedLeafs;
                max += node.totalLeafs;
            }

            // Update totoallabel's text
            this.totalLabel.Text = string.Format("Total Selected Items: {0} / {1}", total, max);
        }

        #endregion Update Selected Node Count

    }

}
