namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Drawing;

    using Element = Autodesk.Revit.DB.Element;
    using ElementId = Autodesk.Revit.DB.ElementId;
    using Category = Autodesk.Revit.DB.Category;
    using FamilyInstance = Autodesk.Revit.DB.FamilyInstance;
    using Family = Autodesk.Revit.DB.Family;
    using Document = Autodesk.Revit.DB.Document;
    using ElementType = Autodesk.Revit.DB.ElementType;

    using Depth = AdvAdvFilter.Common.Depth;

    class AdvTreeNode : TreeNode
    {
        public int numCheckedLeafs;
        public int totalLeafs;

        public AdvTreeNode()
        {
            this.numCheckedLeafs = 0;
            this.totalLeafs = 0;
        }

        public string UpdateCounter()
        {            
            this.Text = string.Format("[ {0}/{1} ] {2}", this.numCheckedLeafs, this.totalLeafs, this.Name);
            return this.Text;
        }

        public void SelfDeterminedCheck()
        {
            if (this.numCheckedLeafs >= this.totalLeafs)
            {
                this.Checked = true;
            }
            else
            {
                this.Checked = false;
            }
        }

    }

    class LeafTreeNode : AdvTreeNode
    {
        #region Fields

        private NodeData data;

        #endregion Fields

        #region Parameters

        public ElementId ElementId
        {
            get { return data.Id; }
            set { this.data.Id = value; }
        }

        public string CategoryType
        {
            get { return this.data.CategoryType; }
            set { this.data.CategoryType = AlwaysGetString(value, "CategoryType"); }
        }

        public string Category
        {
            get { return this.data.Category; }
            set { this.data.Category = AlwaysGetString(value, "Category"); }
        }

        public string Family
        {
            get { return this.data.Family; }
            set { this.data.Family = AlwaysGetString(value, "Family"); }
        }

        public string ElementType
        {
            get { return this.data.ElementType; }
            set { this.data.ElementType = AlwaysGetString(value, "ElementType"); }
        }

        #endregion Parameters

        public LeafTreeNode(NodeData data)
        {
            this.data = data ?? throw new ArgumentNullException();
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
                    parameterValue = this.CategoryType;
                    break;
                case "Category":
                    parameterValue = this.Category;
                    break;
                case "Family":
                    parameterValue = this.Family;
                    break;
                case "ElementType":
                    parameterValue = this.ElementType;
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
        // private List<LeafTreeNode> leafNodes;
        private Dictionary<ElementId, LeafTreeNode> leafNodes;

        private HashSet<ElementId> elementIdsInTree;

        TreeViewController treeController;

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

        public List<ElementId> NodesToAdd
        {
            get { return this.treeController.NodesToAdd; }
            set { this.treeController.NodesToAdd = value; }
        }

        public List<ElementId> NodesToDel
        {
            get { return this.treeController.NodesToDel; }
            set { this.treeController.NodesToDel = value; }
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
            // this.leafNodes = new List<LeafTreeNode>();
            this.leafNodes = new Dictionary<ElementId, LeafTreeNode>();
            this.elementIdsInTree = new HashSet<ElementId>();

            this.treeController = new TreeViewController(treeView);
        }

        #region Update TreeView Structure

        /// <summary>
        /// Updates the treeView's structure
        /// </summary>
        /// <param name="rCon"></param>
        public void CommitTree(
            TreeStructure tree,
            HashSet<ElementId> newElementIds = null
            )
        {
            if (tree == null)
                throw new ArgumentNullException();

            // Modify the change lists if the corresponding argument isn't null
            if (newElementIds != null)
            {
                HashSet<ElementId> oldElementIds = this.treeController.CurElementIds;
                if (oldElementIds == null) throw new ArgumentNullException("oldElementIds");

                var addList
                    = from ElementId id in newElementIds
                      where (!oldElementIds.Contains(id))
                      select id;
                var delList
                    = from ElementId id in oldElementIds
                      where (!newElementIds.Contains(id))
                      select id;
                this.treeController.NodesToAdd = addList.ToList();
                this.treeController.NodesToDel = delList.ToList();
            }

            // Commit the following changes
            this.treeController.CommitChanges(tree);

            return;
        }

        #region TreeNode Removal

        private void TreeStructureAdd_New(
            List<LeafTreeNode> leafNodes,
            TreeNodeCollection treeNodes,
            Depth depth,
            Depth lowestDepth = Depth.Instance
            )
        {
            if (depth == lowestDepth)
            {
                foreach (LeafTreeNode node in leafNodes)
                {
                    treeNodes.Add(node);
                    this.leafNodes.Add(node.ElementId, node);
                }
                return;
            }

            Depth nextDepth;
            SortedDictionary<string, List<LeafTreeNode>> grouping;

            nextDepth = GetNextDepth(depth, lowestDepth);

            grouping = new SortedDictionary<string, List<LeafTreeNode>>();

            // Construct the dictionary to group the leaves together by a certain parameter,
            // this is so that we don't send the whole list down in every concievable branch
            string paramName = depth.ToString();
            string paramValue;
            foreach (LeafTreeNode leaf in leafNodes)
            {
                paramValue = leaf.GetParameter(paramName);

                if (!grouping.ContainsKey(paramValue))
                    grouping.Add(paramValue, new List<LeafTreeNode>());

                grouping[paramValue].Add(leaf);
            }

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
                    // branch.realText = kvp.Key;
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
                TreeStructureAdd_New(kvp.Value, branch.Nodes, nextDepth);
                // TreeStructureAdd(kvp.Value, treeNodes[kvp.Key].Nodes, newHeirarchy);
            }
        }

        private void TreeStructureRem_New(
            List<LeafTreeNode> leafNodes,
            TreeNodeCollection treeNodes,
            Depth depth,
            Depth lowestDepth = Depth.Instance
            )
        {
            if (depth == lowestDepth)
            {
                foreach (LeafTreeNode node in leafNodes)
                {
                    treeNodes.Remove(node);
                    this.leafNodes.Remove(node.ElementId);
                }
                return;
            }

            Depth nextDepth;
            SortedDictionary<string, List<LeafTreeNode>> grouping;

            nextDepth = GetNextDepth(depth, lowestDepth);

            grouping = new SortedDictionary<string, List<LeafTreeNode>>();

            // Construct the dictionary to group the leaves together by a certain parameter,
            // this is so that we don't send the whole list down in every concievable branch
            string paramName = depth.ToString();
            string paramValue;
            foreach (LeafTreeNode leaf in leafNodes)
            {
                paramValue = leaf.GetParameter(paramName);

                if (!grouping.ContainsKey(paramValue))
                    grouping.Add(paramValue, new List<LeafTreeNode>());

                grouping[paramValue].Add(leaf);
            }

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
                        this.leafNodes.Remove(leaf.ElementId);
                    }

                    continue;
                }

                // Update counter
                BranchTreeNode branch = treeNodes[kvp.Key] as BranchTreeNode;
                branch.totalLeafs -= kvp.Value.Count;
                branch.UpdateCounter();

                // Recurse into the node that has the node name of kvp.Key
                TreeStructureRem_New(kvp.Value, treeNodes[kvp.Key].Nodes, nextDepth);

                if (treeNodes[kvp.Key].Nodes.Count == 0)
                {
                    treeNodes[kvp.Key].Remove();
                }
            }
        }

        /// <summary>
        /// Removes the nodes from leafNodes and all branch nodes if all
        /// their children was removed as a consequence to this.
        /// </summary>
        /// <param name="leafNodes"></param>
        /// <param name="treeNodes"></param>
        /// <param name="heirarchy"></param>
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
                    this.leafNodes.Remove(leaf.ElementId);
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
                        this.leafNodes.Remove(leaf.ElementId);
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

        /// <summary>
        /// Removes leaf nodes in the argument leafNodes from TreeNodeCollection treeNodes
        /// and keeps the integrity of tree struture by removing branch nodes that do not
        /// have any leafNodes below it
        /// </summary>
        /// <param name="leafNodes"></param>
        /// <param name="treeNodes"></param>
        private void RemLeafNodes(
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
            TreeStructureRem(leafNodes, treeNodes, heirarchy);
        }

        /// <summary>
        /// From this.leafNodes, get a sub-list of leafNodes that has
        /// the elementIds of IEnumberable elementIds
        /// </summary>
        /// <param name="elementIds"></param>
        /// <returns></returns>
        private List<LeafTreeNode> RetrieveLeafNodes(
            IEnumerable<ElementId> elementIds)
        {
            List<LeafTreeNode> retrievedLeaves = new List<LeafTreeNode>();

            retrievedLeaves = (from ElementId id in elementIds
                              select this.leafNodes[id]).ToList();
            /*
            // Gets IEnumerable<LeafTreeNode> using a LINQ statement
            IEnumerable < LeafTreeNode > leavesToRemove
                = from LeafTreeNode in this.leafNodes
                  where elementIds.Contains(LeafTreeNode.ElementId)
                  select LeafTreeNode;

            // If resulting LINQ statement is null...
            if (leavesToRemove == null)
            {
                // Set retrievedLeaves to a new list
                retrievedLeaves = new List<LeafTreeNode>();
            }
            else
            {
                // Set retrievedLeaves to be leavesToRemove but in a List
                retrievedLeaves = leavesToRemove.ToList();
            }
            */
            return retrievedLeaves;
        }

        #endregion TreeNode Removal

        private int TallyUpSelectedElements(
            TreeNode node,
            Depth depth,
            Depth lowestDepth = Depth.Instance)
        {
            if (depth == lowestDepth)
            {
                MessageBox.Show(node.FullPath);
                return 0;
            }

            Depth nextDepth = GetNextDepth(depth, lowestDepth);

            foreach (TreeNode n in node.Nodes)
            {
                TallyUpSelectedElements(n, depth);
            }
            return 0;
        }

        public void UpdateTreeViewStructure_New(
            HashSet<ElementId> newElementIds,
            TreeStructure treeStructure
            )
        {
            if (newElementIds == null) throw new ArgumentNullException();

            lock (treeLock)
            {
                HashSet<ElementId> elementsToRem = new HashSet<ElementId>(elementIdsInTree);
                HashSet<ElementId> elementsToAdd = new HashSet<ElementId>(newElementIds);

                elementsToRem.ExceptWith(newElementIds);
                elementsToAdd.ExceptWith(elementIdsInTree);

                List<TreeNode> nodesToRem = GetTreeNodes(elementsToRem, treeStructure);
                List<TreeNode> nodesToAdd = GetTreeNodes(elementsToAdd, treeStructure);

                RemFromTreeStructure(nodesToRem, this.treeView.Nodes, Depth.CategoryType);
                AddToTreeStructure(nodesToAdd, this.treeView.Nodes, Depth.CategoryType);

                foreach (TreeNode n in this.treeView.Nodes)
                {
                    TallyUpSelectedElements(n, Depth.CategoryType);
                }

                elementIdsInTree = newElementIds;
            }
        }

        public List<TreeNode> GetTreeNodes(HashSet<ElementId> elementIds, TreeStructure tree)
        {
            List<TreeNode> treeNodes = new List<TreeNode>();

            foreach (ElementId id in elementIds)
            {
                treeNodes.Add(tree.ElementIdNodes[id]);
            }

            return treeNodes;
        }

        #region TreeNode Rem

        private void RemFromTreeStructure(
            List<TreeNode> leafNodes,
            TreeNodeCollection treeNodes,
            Depth depth,
            Depth lowestDepth = Depth.Instance
            )
        {
            if (depth == lowestDepth)
            {
                foreach (TreeNode node in leafNodes)
                    treeNodes.Remove(node);
                return;
            }

            Depth nextDepth;
            SortedDictionary<string, List<TreeNode>> grouping;

            nextDepth = GetNextDepth(depth, lowestDepth);

            grouping = GetGroupingByTreeNode(leafNodes, depth.ToString());

            grouping.OrderBy(key => key.Key);
            TreeNodeCollection nextCollection;
            foreach (KeyValuePair<string, List<TreeNode>> kvp in grouping)
            {
                nextCollection = treeNodes[kvp.Key].Nodes;

                RemFromTreeStructure(kvp.Value, nextCollection, nextDepth);

                if (treeNodes[kvp.Key].Nodes.Count == 0)
                {
                    treeNodes.RemoveByKey(kvp.Key);
                }
            }
        }

        #endregion TreeNode Rem

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
                    this.leafNodes.Add(leaf.ElementId, leaf);
                }
                return;
            }
            HashSet<LeafTreeNode> leafHash = new HashSet<LeafTreeNode>();
            
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
                    // branch.realText = kvp.Key;
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
            TreeStructure tree
            )
        {
            // Construct leafNodes to add onto the treeView
            List<LeafTreeNode> leafNodesList = new List<LeafTreeNode>();
            LeafTreeNode leafNode;
            NodeData data;

            foreach (ElementId eId in elementIds)
            {
                data = tree.ElementIdNodes[eId].Tag as NodeData;
                leafNode = new LeafTreeNode(data);
                // Add leafNode into the leafNodesToAdd
                leafNodesList.Add(leafNode);
            }

            return leafNodesList;
        }

        /// <summary>
        /// Using the argument element and elementType, create a leafTreeNode
        /// and fill its parameters using the given information
        /// </summary>
        /// <param name="element"></param>
        /// <param name="elementType"></param>
        /// <returns></returns>
        private LeafTreeNode ConstructLeafNode(
            Element element,
            ElementType elementType,
            TreeStructure tree
            )
        {
            if (element == null)
                throw new ArgumentNullException();

            LeafTreeNode leaf = new LeafTreeNode(tree.ElementIdNodes[element.Id].Tag as NodeData);

            // Get leaf's essential information
            leaf.ElementId = element.Id;
            leaf.Text = element.Id.ToString();
            // leaf.realText = element.Id.ToString();
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

        #region TreeNode Add

        private void AddToTreeStructure(
            List<TreeNode> leafNodes,
            TreeNodeCollection treeNodes,
            Depth depth,
            Depth lowestDepth = Depth.Instance
            )
        {
            if (depth == lowestDepth)
            {
                foreach (TreeNode node in leafNodes)
                    treeNodes.Add(node);
                return;
            }

            Depth nextDepth;
            SortedDictionary<string, List<TreeNode>> grouping;

            nextDepth = GetNextDepth(depth, lowestDepth);

            grouping = GetGroupingByTreeNode(leafNodes, depth.ToString());

            grouping.OrderBy(key => key.Key);
            TreeNodeCollection nextCollection;
            foreach(KeyValuePair<string, List<TreeNode>> kvp in grouping)
            {
                if (!treeNodes.ContainsKey(kvp.Key))
                {
                    TreeNode branch = new TreeNode();
                    branch.Name = kvp.Key;
                    branch.Text = kvp.Key;
                    treeNodes.Add(branch);
                }

                nextCollection = treeNodes[kvp.Key].Nodes;

                AddToTreeStructure(kvp.Value, nextCollection, nextDepth);
            }

        }

        /// <summary>
        /// Gets the next lowest depth of the given depth and returns Depth.Invalid if depth == lowest
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="lowest"></param>
        /// <returns></returns>
        private Depth GetNextDepth(Depth depth, Depth lowest)
        {
            Depth next;

            if (depth == Depth.Invalid)
                throw new ArgumentException();
            else if (depth == lowest)
                next = Depth.Invalid;
            else
                next = (Depth)((int)depth + 1);

            return next;
        }

        private SortedDictionary<string, List<TreeNode>> GetGroupingByTreeNode(List<TreeNode> nodes, string paramName)
        {
            SortedDictionary<string, List<TreeNode>> grouping = new SortedDictionary<string, List<TreeNode>>();

            string key;
            foreach (TreeNode n in nodes)
            {
                key = (n.Tag as NodeData).GetParameter(paramName);

                if (!grouping.ContainsKey(key))
                    grouping.Add(key, new List<TreeNode>());

                grouping[key].Add(n);
            }

            return grouping;
        }

        #endregion TreeNode Add

        #region CategoryType Node Setup

        /// <summary>
        /// Set up the category type nodes in the treeView
        /// </summary>
        /// <param name="nodes"></param>
        private void UpdateCategoryTypeNodes(TreeNodeCollection nodes)
        {
            // For each categoryType node...
            foreach (TreeNode n in nodes)
            {
                // Expand the its tree if it isn't already
                if (!n.IsExpanded)
                    n.Expand();
            }
        }

        #endregion CategoryType Node Setup

        #endregion Update TreeView Structure

        #region Refresh Selection
        /*
        public void RefreshSelection(TreeNodeCollection treeNodes)
        {
            foreach (BranchTreeNode branch in treeNodes)
            {
                

            }

            return;
        }
        */
        #endregion Refresh Selection

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

            lock (treeLock)
            {
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
        }

        #endregion Collapse and Expand Nodes

        #region Update TreeView Upon Check

        /// <summary>
        /// Update the treeView's checked status when the 
        /// argument e's checked status has recently changed.
        /// </summary>
        /// <param name="e"></param>
        public void UpdateAfterCheck(AdvTreeNode e)
        {
            // Recursively go up the tree until you reach the top (node.Parent == null)
            // then select the node which its Parent is null
            AdvTreeNode GetRoot(AdvTreeNode node)
            {
                AdvTreeNode tmpRoot = null;

                if (node == null) return null;

                tmpRoot = GetRoot(node.Parent as AdvTreeNode);

                if (tmpRoot == null)
                    tmpRoot = node;

                return tmpRoot;
            }

            if (e == null) return;

            lock (treeLock)
            {
                // Update their children if it has any
                if (e.Nodes.Count > 0)
                    UpdateChildNodes(e, e.Checked);

                // Update their parents if it has any
                if (e.Parent != null)
                    UpdateParentNodes(e.Parent as AdvTreeNode, e.Checked);

                // Get the root of the TreeNode
                AdvTreeNode root = GetRoot(e);

                // UpdateCheckedCounters(root);
                UpdateCounter(root);
            }

            return;
        }

        /// <summary>
        /// Update the nodes's children by the nodes's
        /// checked status recursively until it reaches the bottom
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isChecked"></param>
        private void UpdateChildNodes(AdvTreeNode node, bool isChecked)
        {
            if (node == null) return;

            // For every node's children...
            foreach (AdvTreeNode n in node.Nodes)
            {
                // If the child's check status is the same as the changes, then continue                
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

        /// <summary>
        /// Update the parents of the node's checked status by recursively going up
        /// until it reaches the top of the tree.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="isChecked"></param>
        private void UpdateParentNodes(AdvTreeNode parent, bool isChecked)
        {
            bool isAllChecked(TreeNodeCollection collection)
            {
                foreach (AdvTreeNode n in collection)
                    if (!n.Checked) return false;
                return true;
            }
            // If parent is null, then immediately exit
            if (parent == null) return;

            // Check the parent node if all of the parent's children are checked
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
                    if (!this.leafNodes.ContainsKey(elementId))
                    {
                        updateSuccess = false;
                        break;
                    }
                }

                if (updateSuccess)
                {
                    LeafTreeNode node;
                    foreach (ElementId elementId in selected)
                    {
                        node = this.leafNodes[elementId];
                        node.Checked = !node.Checked;
                        UpdateAfterCheck(node);
                        UpdateLabelTotals();

                    }
                }
            }
            return updateSuccess;
        }

        #endregion Update TreeView Upon Revit Selection

        #region Get Selection From TreeView

        /// <summary>
        /// Get the elementIds of all those that are selected in the list
        /// </summary>
        /// <returns></returns>
        public List<ElementId> GetSelectedElementIds()
        {
            List<ElementId> output = null;
            Dictionary<ElementId, LeafTreeNode> leafNodes = null;
            int max = 5;

            // While max is still greater than 0...
            while (max > 0)
            {
                // Get a copy of this.leafNodes without dataraces
                lock (treeLock)
                {
                    leafNodes = new Dictionary<ElementId, LeafTreeNode>(this.leafNodes);
                }

                // max counter decremented (signifying the program will make one attempt)
                max -= 1;
                try
                {
                    // LINQ statement: Get all the elementIds of the leafNodes that are 'Checked'
                    IEnumerable<ElementId> selected
                                = from LeafTreeNode leaf in leafNodes
                                  where leaf.Checked
                                  select leaf.ElementId;
                    // Convert the resulting LINQ statement into a list
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
            TreeNodeCollection collection = null;
            int numberChecked = 0;

            lock (treeLock)
            {
                collection = node.Nodes;

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
                // If all of their the node's children are checked, then check itself, else uncheck
                node.SelfDeterminedCheck();
            }

            // Pass the total up to the caller
            return numberChecked;
        }

        public void UpdateTotals()
        {
            lock (treeLock)
            {
                UpdateLabelTotals();
            }
        }

        /// <summary>
        /// Updates this.totalLabel to show the user how many elements are
        /// selected out of the total elements in the treeView.
        /// </summary>
        private void UpdateLabelTotals()
        {
            TreeNodeCollection collection = null;

            int total = 0;
            int max = 0;

            collection = treeView.Nodes;

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
