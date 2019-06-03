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

        public bool UpdateSelectedNodes(ICollection<ElementId> selected)
        {
            bool selectedChanged = false;

            lock (treeLock)
            {
                List<ElementId> currSelection = selected.ToList<ElementId>();

                foreach (LeafTreeNode leaf in this.leafNodes)
                {
                    if (selected.Contains(leaf.ElementId))
                    {
                        if (!leaf.Checked)
                        {
                            leaf.Checked = true;
                            UpdateAfterCheck(leaf);
                            UpdateTotalSelectedItemsLabel();

                            selectedChanged = true;
                        }
                    }
                    else
                    {
                        if (leaf.Checked)
                        {
                            leaf.Checked = false;
                            UpdateAfterCheck(leaf);
                            UpdateTotalSelectedItemsLabel();

                            selectedChanged = true;
                        }
                    }
                }
            }

            return selectedChanged;
        }

        public bool UpdateSelectedLeaves(ICollection<ElementId> selected)
        {
            bool updateSuccess = true;

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
                            UpdateTotalSelectedItemsLabel();
                        }
                    }
                    else
                    {
                        if (leaf.Checked)
                        {
                            leaf.Checked = !leaf.Checked;
                            UpdateAfterCheck(leaf);
                            UpdateTotalSelectedItemsLabel();
                        }
                    }
                }
            }

            return updateSuccess;
        }

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

        #region Unused Functions

        public List<TreeNode> ToList(TreeNodeCollection collection)
        {
            List<TreeNode> list = new List<TreeNode>();

            foreach (TreeNode node in collection)
                list.Add(node);

            return list;
        }

        public void ResetView()
        {
            this.categoryTypes = GetCategoryNodes();

            foreach (BranchTreeNode node in this.categoryTypes)
            {
                if (!node.IsExpanded)
                    node.Expand();
                node.Collapse();
                node.isAllCollapsed = true;
            }
            return;
        }

        public List<BranchTreeNode> GetCategoryNodes()
        {
            TreeNodeCollection collection = this.treeView.Nodes;
            List<TreeNode> nodeList = this.ToList(collection);

            List<BranchTreeNode> output = new List<BranchTreeNode>();

            foreach (TreeNode node in nodeList)
            {
                if (node is BranchTreeNode cNode)
                    output.Add(cNode);
            }

            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        private List<LeafTreeNode> GetNewLeafNodes(List<ElementId> elements)
        {
            List<LeafTreeNode> output = null;

            if (elements == null)
                return output;

            output = new List<LeafTreeNode>();

            LeafTreeNode leaf = null;
            foreach (ElementId i in elements)
            {
                leaf = new LeafTreeNode();
                leaf.ElementId = i;
                output.Add(leaf);
            }

            return output;
        }

        #endregion Unused Functions

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

                SetupCheckedCounter(treeView.Nodes);

                UpdateTotalSelectedItemsLabel();
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
        /// <param name="branch"></param>
        public void ToggleCollapse(BranchTreeNode branch)
        {
            TreeNodeCollection subBranches = branch.Nodes;

            bool allCollapsed = true;

            foreach (AdvTreeNode sub in subBranches)
            {
                if (sub.IsExpanded)
                {
                    allCollapsed = false;
                    break;
                }
            }

            if (allCollapsed)
            {
                branch.ExpandAll();
            }
            else
            {
                foreach (AdvTreeNode sub in subBranches)
                {
                    sub.Collapse();
                }
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

        #region Update When Check

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

            // Update their children if it has any
            if (e.Nodes.Count > 0)
                UpdateChildNodes(e, e.Checked);

            // Update their parents if it has any
            if (e.Parent != null)
                UpdateParentNodes(e.Parent as AdvTreeNode, e.Checked);

            AdvTreeNode root = GetRoot(e);

            UpdateCheckedCounters(root);

            return;
        }

        private void UpdateChildNodes(AdvTreeNode node, bool isChecked)
        {
            if (node == null)
                return;

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

        #endregion Update When Check

        #region Update Counters

        private int UpdateCheckedCounters(AdvTreeNode node)
        {
            int output = 0;

            if (node.Name == "Instance")
            {
                if (node.Checked)
                    output = 1;
                else
                    output = 0;
            }
            else
            {
                foreach (AdvTreeNode n in node.Nodes)
                {
                    output += UpdateCheckedCounters(n);
                }

                node.numCheckedLeafs = output;
                node.UpdateCounter();

            }

            return output;
        }

        public void UpdateTotalSelectedItemsLabel()
        {
            int total = 0;
            int max = 0;

            TreeNodeCollection nodes = treeView.Nodes;

            foreach (AdvTreeNode n in nodes)
            {
                total += UpdateCheckedCounters(n);
                max += n.totalLeafs;
            }

            this.totalLabel.Text = string.Format("Total Selected Items: {0} / {1}", total, max);
        }

        #endregion Update Counters

        #region TMP STOW

        public void SetLeafTreeNode(List<ElementId> elements)
        {
            if (this.leafNodes == null)
                this.leafNodes = new List<LeafTreeNode>();

            IEnumerable<ElementId> newElementIds;

            if (this.leafNodes.Count != 0)
            {
                IEnumerable<LeafTreeNode> nodesWithValidElements
                        = from LeafTreeNode node in this.leafNodes
                          where elements.Contains(node.ElementId)
                          select node;

                IEnumerable<ElementId> leafNodeElementIds
                        = from LeafTreeNode node in this.leafNodes
                          select node.ElementId;

                newElementIds
                        = from ElementId eId in elements
                          where (!leafNodeElementIds.Contains<ElementId>(eId))
                          select eId;
            }
            else
            {
                newElementIds = elements;
            }

            LeafTreeNode newNode = null;

            foreach (ElementId eId in newElementIds)
            {
                newNode = new LeafTreeNode();
                newNode.ElementId = eId;
                this.leafNodes.Add(newNode);
            }
        }

        public void BuildTreeView()
        {
            TreeNodeCollection nodes = this.TreeView.Nodes;
            nodes.Clear();

            if (this.leafNodes == null)
                return;

            foreach (LeafTreeNode n in this.leafNodes)
            {
                nodes.Add(n);
            }

        }

        public void TestSomething()
        {
            foreach (LeafTreeNode n in this.leafNodes)
            {
                if (n.Checked)
                    n.Checked = false;
                else
                    n.Checked = true;
            }
        }

        public void SetElementsToTreeView(List<Element> elements, Document doc)
        {
            string getTXT(Element e)
            {               
                string txt = "";
                try
                {
                    txt += CommonMethods.GetElementCategory(e, doc) + " / ";
                    txt += CommonMethods.GetElementFamily(e, doc) + " / ";
                    txt += CommonMethods.GetElementType(e) + " / ";
                    txt += CommonMethods.GetElementInstanceId(e);
                }
                catch (Exception ex)
                {
                    txt = ex.Message;
                }

                return txt;
            }

            bool isNodeInElements(string name)
            {
                foreach (Element e in elements)
                {

                    if (name == getTXT(e))
                        return true;
                }
                return false;
            }

            TreeNodeCollection nodes = treeView.Nodes;

            IEnumerable<TreeNode> nodesInElements
                        = from TreeNode n in nodes
                          where isNodeInElements(n.Text)
                          select n;

            if (nodesInElements.Count<TreeNode>() == elements.Count)
                return;

            nodes.Clear();

            foreach (Element e in elements)
            {
                // string txt = e.Category.Name;
                string txt = getTXT(e);

                TreeNode n = new TreeNode();
                n.Name = txt;
                n.Text = txt;
                nodes.Add(n);
            }

        }


        public List<TreeNode> GetListOfCheckedLeaves(TreeNodeCollection collection)
        {
            return GetLeaves(collection, true);
        }

        public List<TreeNode> GetListOfAllLeaves(TreeNodeCollection collection)
        {
            return GetLeaves(collection, false);
        }

        public int GetNumberOfCheckedLeafs(TreeNodeCollection collection)
        {
            List<TreeNode> checkedLeaves = GetListOfCheckedLeaves(collection);
            return checkedLeaves.Count;
        }

        #endregion

        #endregion

        #region PrivateMethods

        private string getElementCategory(Element element)
        {
            string txt = element.Category.Name;
            return txt;
        }

        private string getElementFamily(Element element)
        {
            string txt = "\t\t";

            FamilyInstance famInst = element as FamilyInstance;
            if (famInst != null)
            {
                Family fam = famInst.Symbol.Family;
                txt = fam.Name;
            }
            else
            {
                Autodesk.Revit.DB.ElementId eType = element.GetTypeId();
                //Autodesk.Revit.DB.ElementType type =
                //Type eType = element.GetType();
                //txt = eType.Name;
                txt = "idk";
            }

            return txt;
        }

        private string getElementType(Element element)
        {
            string txt = element.Name;
            return txt;
        }

        private string getElementInstanceId(Element element)
        {
            string txt = element.UniqueId;
            return txt;
        }

        private List<TreeNode> ElementsToNodes(List<Element> elements)
        {
            List<TreeNode> categories = new List<TreeNode>();

            foreach (Element e in elements)
            {
                TreeNode node = new TreeNode();
                Category category = e.Category;

                node.Name = category.Name;
                node.Text = category.Name;

                categories.Add(node);
            }

            return categories;
        }
        
        private List<TreeNode> GetLeaves(TreeNodeCollection collection, bool pickChecked)
        {
            // Get a new empty list
            List<TreeNode> allLeaves = new List<TreeNode>();

            foreach (TreeNode node in collection)
            {
                // if node doesn't have any children (leaf node)...
                if (node.Nodes.Count == 0)
                {
                    // if the method is only selecting checked nodes and
                    // the node isn't check, continue onto the next node
                    if (pickChecked && (!node.Checked))
                        continue;
                    // Add the node onto the list
                    allLeaves.Add(node);
                }
                // if the node has children (non-leaf node)...
                else
                {
                    // Recurse down, with node.Nodes being the collection to be evaluated
                    List<TreeNode> Leaves = this.GetLeaves(node.Nodes, pickChecked);
                    // Add Leaves onto allLeaves
                    allLeaves = (allLeaves.Concat(Leaves)).ToList<TreeNode>();
                }
            }

            // Pass allLeaves up as a return
            return allLeaves;
        }

        #endregion

    }

}
