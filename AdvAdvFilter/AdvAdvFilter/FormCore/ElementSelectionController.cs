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
        public ElementId ElementId;

        public LeafTreeNode()
        {
            this.ElementId = null;
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

        #region Public Methods

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

        #region Selected Elements

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

        #endregion Selected Elements

        #region UpdateTreeView

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="rCon"></param>
        public void UpdateTreeView(List<ElementId> elements, RevitController rCon)
        {
            List<Type> updateList = new List<Type>()
            {
                "CategoryType".GetType(),
                typeof(Category),
                typeof(Family),
                typeof(ElementType),
                typeof(ElementId)
            };

            lock (treeLock)
            {
                this.leafNodes.Clear();

                UpdateLevel(elements, treeView.Nodes, rCon, updateList);

                SetupCategoryTypeNodes(treeView.Nodes);

                // SetupCheckedCounter(treeView.Nodes);

                foreach (AdvTreeNode node in treeView.Nodes)
                {
                    UpdateCounter(node);
                }

                // UpdateTotalSelectedItemsLabel();

                UpdateLabelTotals();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="nodes"></param>
        /// <param name="rCon"></param>
        /// <param name="updateList"></param>
        private void UpdateLevel(
            List<ElementId> elements,
            TreeNodeCollection nodes,
            RevitController rCon,
            List<Type> updateList)
        {
            if (updateList.Count == 0)
            {
                return;
            }

            Dictionary<string, AdvTreeNode> stringNodeMap = new Dictionary<string, AdvTreeNode>();
            SortedDictionary<string, List<ElementId>> group = null;
            List<TreeNode> nodesToBeDeleted = new List<TreeNode>();
            Type type = updateList[0];

            group = rCon.GroupElementIdsBy(type, elements);

            List<Type> newUpdateList = new List<Type>(updateList);
            newUpdateList.RemoveAt(0);

            foreach (TreeNode n in nodes)
            {
                // If the grouping doesn't have a key, then set the node up for deletion
                if (!group.ContainsKey(n.Text))
                {
                    nodesToBeDeleted.Add(n);
                    continue;
                }

                // Add the node to the stringNodeMap for later
                if (!stringNodeMap.ContainsKey(n.Text))
                {
                    stringNodeMap.Add(n.Text, n as AdvTreeNode);
                }

            }

            // Remove TreeNodes in nodes that are within the List<TreeNode> nodesToBeDeleted
            foreach (TreeNode n in nodesToBeDeleted)
            {
                nodes.Remove(n);
            }

            bool lastLevel = type == typeof(ElementId);

            group.OrderBy(key => key.Key);
            foreach (KeyValuePair<string, List<ElementId>> kvp in group)
            {
                string nodeName = kvp.Key;
                List<ElementId> elementGrouping = kvp.Value;

                BranchTreeNode nextNode = null;

                // Check if stringNodeMap has the nodeName as a key
                if (stringNodeMap.ContainsKey(nodeName))
                {
                    // If true, then the nodes that has the nodeName exists
                    if (lastLevel)
                    {
                        this.leafNodes.Add(stringNodeMap[nodeName] as LeafTreeNode);
                        continue;
                    }
                    nextNode = stringNodeMap[nodeName] as BranchTreeNode;
                    nextNode.totalLeafs = elementGrouping.Count;

                }
                else
                {
                    // If false, then the node with nodeName doesn't exist, create a new one
                    if (lastLevel)
                    {
                        LeafTreeNode newLeaf = new LeafTreeNode();

                        newLeaf.ElementId = kvp.Value[0];
                        newLeaf.Name = "Instance";
                        newLeaf.Text = nodeName;

                        nodes.Add(newLeaf);

                        this.leafNodes.Add(newLeaf);
                        continue;
                    }
                    else
                    {
                        nextNode = new BranchTreeNode();

                        nextNode.Name = type.ToString();
                        nextNode.realText = nodeName;
                        nextNode.totalLeafs = elementGrouping.Count;
                        nextNode.UpdateCounter();

                        nodes.Add(nextNode);
                    }
                }

                UpdateLevel(elementGrouping, nextNode.Nodes, rCon, newUpdateList);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="categoryType"></param>
        private void SetupCategoryTypeNodes(TreeNodeCollection categoryType)
        {
            // For each categoryType node...
            foreach (BranchTreeNode cNode in categoryType)
            {
                // Expand the its tree
                cNode.Expand();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private int SetupCheckedCounter(TreeNodeCollection nodes)
        {
            int output = 0;
            int tmp = 0;

            if (nodes.Count == 0)
                return output;

            foreach (AdvTreeNode n in nodes)
            {
                tmp = 0;

                if (n.Name == "Instance")
                {
                    if (n.Checked)
                    {
                        tmp = 1;
                        n.numCheckedLeafs = tmp;
                    }
                }
                else
                {
                    tmp = SetupCheckedCounter(n.Nodes);
                    n.numCheckedLeafs = tmp;
                    n.UpdateCounter();
                }

                output += tmp;
            }

            return output;
        }

        #endregion

        #endregion

        #region Functions

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

        #region Update TreeView Upoun Check

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


        #endregion Update TreeView Upoun Check

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

        #region Update TreeView Structure



        #endregion Update TreeView Structure

        #endregion Functions

    }

    /// <summary>
    /// 
    /// </summary>
    class TreeStructure
    {
        #region Data

        // TreeView data structure

        #endregion Data

        #region Functions

        // Get 


        #endregion Functions

    }

}
