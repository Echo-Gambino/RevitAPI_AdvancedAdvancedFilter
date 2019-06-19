namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using ElementId = Autodesk.Revit.DB.ElementId;

    using Depth = AdvAdvFilter.Common.Depth;

    class ElementSelectionController
    {
        #region Fields

        private Panel panel;
        private Label totalLabel;
        private TreeView treeView;

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

        /// <summary>
        /// Change list to notify which nodes (denoted in ElementId) should be added into TreeStructure
        /// </summary>
        public List<ElementId> NodesToAdd
        {
            get { return this.treeController.NodesToAdd; }
            set { this.treeController.NodesToAdd = value; }
        }

        /// <summary>
        /// Change list to notify which nodes (denoted in ElementId) should be removed from TreeStructure
        /// </summary>
        public List<ElementId> NodesToDel
        {
            get { return this.treeController.NodesToDel; }
            set { this.treeController.NodesToDel = value; }
        }

        /// <summary>
        /// A list of all nodes (denoted in ElementId) is within the treeController
        /// </summary>
        public HashSet<ElementId> AllElementIds
        {
            get { return this.treeController.CurElementIds; }
        }

        /// <summary>
        /// A hashmap to map the ElementId to its corresponding TreeNode within the TreeView
        /// </summary>
        public Dictionary<ElementId, TreeNode> LeafNodes
        {
            get { return this.treeController.LeafNodes; }
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

            this.treeController = new TreeViewController(treeView);
        }

        #region Controls

        public void ExpandAll()
        {
            this.treeView.ExpandAll();
        }

        public void CollapseAll()
        {
            this.treeView.CollapseAll();
        }

        public void ExpandSelected()
        {
            TreeNode selected = this.treeView.SelectedNode;
            if (selected != null) selected.ExpandAll();
        }

        public void CollapseSelected()
        {
            TreeNode selected = this.treeView.SelectedNode;
            if (selected != null) selected.Collapse(false);
        }

        #endregion Controls

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
            // This method absolutely requires TreeStructure to work, throw an argument if no TreeStructure exists 
            if (tree == null) throw new ArgumentNullException();

            // Modify the change lists if the corresponding argument isn't null
            if (newElementIds != null)
            {
                // Get the 'old' selected elementIds
                HashSet<ElementId> oldElementIds = this.treeController.CurElementIds;

                // Get all the elementIds from newElementIds that are NOT from oldElementIds to be added
                IEnumerable<ElementId> addList
                    = from ElementId id in newElementIds
                      where (!oldElementIds.Contains(id))
                      select id;
                // Get all the elementIds from oldElementIds that are NOT from newElementIds to be deleted
                IEnumerable<ElementId> delList
                    = from ElementId id in oldElementIds
                      where (!newElementIds.Contains(id))
                      select id;

                // Set the treeController's change lists so that it can be used when treeController.CommitChanges is called
                this.treeController.NodesToAdd = addList.ToList();
                this.treeController.NodesToDel = delList.ToList();
            }
            /*
            var thing = from ElementId id in this.treeController.NodesToDel
                        where ("47145" == id.ToString())
                        select id;
            if (thing.Count() != 0)
                MessageBox.Show("47145 is going to be deleted!");
            */
            // Commit the following changes
            this.treeController.CommitChanges(tree);

            return;
        }

        #endregion Update TreeView Structure

        #region Update TreeView Selection
        // TODO: Need to move some of these functions over to TreeViewController

        /// <summary>
        /// Update selection by checkedStatus for the given elementIds
        /// </summary>
        /// <param name="elementIds"></param>
        /// <param name="checkedStatus"></param>
        public void UpdateSelectionByElementId(HashSet<ElementId> elementIds, bool checkedStatus)
        {
            // If there are no treeNodes, exit out immediately
            if (elementIds.Count == 0) return;

            lock (treeLock)
            {
                // Construct a list of treeNodes
                List<TreeNode> treeNodes = new List<TreeNode>();

                // Translate the ElementIds into TreeNodes
                TreeNode node;
                foreach (ElementId id in elementIds)
                {
                    if (!this.LeafNodes.ContainsKey(id)) continue;
                    // Get the node that corresponds to the elementId
                    node = this.LeafNodes[id];
                    // Add it to the list of treeNode
                    treeNodes.Add(node);
                }

                // If there are no treeNodes, then exit out immediately
                if (treeNodes.Count == 0) return;

                // Call the back-end facing update selection
                UpdateSelection(treeNodes, checkedStatus);
            }
        }

        /// <summary>
        /// Updates the given treeNodes by checkedStatus
        /// </summary>
        /// <param name="treeNodes"></param>
        /// <param name="checkedStatus"></param>
        private void UpdateSelection(List<TreeNode> treeNodes, bool checkedStatus)
        {
            // If treeNodes is null or treeNodes is empty, then throw an exception
            if (treeNodes == null)
                throw new ArgumentNullException("treeNodes");
            else if (treeNodes.Count == 0)
                throw new ArgumentException("treeNodes");

            List<TreeNode> nextNodes = new List<TreeNode>();
            bool recurse = true;

            // Check if the limit of recursion has been reached (reached the top nodes), and set a flag to stop recursion if true
            if (treeNodes[0].Parent == null) recurse = false;

            // For each TreeNode in treeNodes
            foreach (TreeNode node in treeNodes)
            {
                // Check if the node should be checked given the context of checkedStatus
                if (ShouldBeChecked(node, checkedStatus))
                {
                    // If it should be checked and it isn't already, then check it
                    if (!node.Checked) node.Checked = true;
                }
                else
                {
                    // If it shouldn't be checked and its checked, then uncheck it
                    if (node.Checked) node.Checked = false;
                }

                // If the node's parent isn't within the list of nodes to recurse up, then add the parent into the next node
                if (!nextNodes.Contains(node.Parent))
                    nextNodes.Add(node.Parent);
            }

            // If recursing is enabled, then recurse with the newly constructed list of nodes
            if (recurse) UpdateSelection(nextNodes, checkedStatus);

            return;
        }

        /// <summary>
        /// Determines whether or not the treeNode should be checked depending on checkedStatus
        /// </summary>
        /// <param name="treeNode"></param>
        /// <param name="checkedStatus"></param>
        /// <returns> True: TreeNode should be checked. False: TreeNode shouldn't be checked </returns>
        private bool ShouldBeChecked(TreeNode treeNode, bool checkedStatus)
        {
            // If the treeNode is a leaf, then it should/shouldn't be checked based on checkedStatus
            if (treeNode.Nodes.Count == 0) return checkedStatus;

            // Essentially, if ANY child within treeNode ISN'T checked, the treeNode shouldn't be checked.
            // Else, it should be checked.
            bool result = true;
            foreach (TreeNode node in treeNode.Nodes)
            {
                if (node.Checked == false)
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        #endregion Update TreeView Selection

        #region Update TreeView Counter Node

        public void RefreshAllNodeCounters(DataController data)
        {
            lock (treeLock)
            {
                treeController.RefreshNodeCounters(data);
            }
        }

        public void UpdateNodeCounter(TreeNode node, DataController data)
        {
            lock (treeLock)
            {
                treeController.UpdateNodeCounter(node, data);
            }
        }

        public void UpdateAffectedCounter(
            IEnumerable<ElementId> affected,
            DataController data)
        {
            lock (treeLock)
            {
                HashSet<TreeNode> cTypes = treeController.GetBranchNodes(affected, Depth.CategoryType);
                HashSet<TreeNode> categories = treeController.GetBranchNodes(affected, Depth.Category);

                foreach (TreeNode node in cTypes.Union(categories))
                {
                    treeController.UpdateNodeCounter(node, data);
                }
            }
        }

        #endregion Update TreeView Counter Node

        #region Update TreeView Label

        public void UpdateSelectionCounter()
        {
            int total = 0;
            int selected = 0;

            lock (treeLock)
            {
                // Get the total number of leafNodes
                total = this.LeafNodes.Count;
                // For every TreeNode in leafNode, if its checked, then add one to selected
                foreach (KeyValuePair<ElementId, TreeNode> kvp in this.LeafNodes)
                {
                    if (kvp.Value.Checked) selected += 1;
                }

                // Put it into the totalLabel.Text
                this.totalLabel.Text = String.Format("Total Selected Items: {0}/{1}", selected, total);
            }
        }

        #endregion Update TreeView Label

        #region Auxiliary Functions

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

        /// <summary>
        /// Gets the path of the node, but instead using nodes' text, it uses the nodes' names
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public List<string> GetNamePath(TreeNode node)
        {
            return treeController.GetNamePath(node, new List<string>());
        }

        #endregion Auxiliary Functions

        #region Expand Nodes

        /// <summary>
        /// Expand the nodes that are within the treeView
        /// </summary>
        /// <param name="nodes"></param>
        private void ExpandNodes(TreeNodeCollection nodes)
        {
            // For each categoryType node...
            foreach (TreeNode n in nodes)
            {
                // Expand the its tree if it isn't already
                if (!n.IsExpanded)
                    n.Expand();
            }
        }

        #endregion Expand Nodes

    }
}
