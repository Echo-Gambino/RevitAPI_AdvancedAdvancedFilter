namespace AdvAdvFilter
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using System.Windows.Forms;

    using Autodesk.Revit.DB;

    using Depth = AdvAdvFilter.Common.Depth;

    public class TreeViewController
    {

        #region Fields
        
        // Persistent fields
        private TreeView treeView;
        private Dictionary<ElementId, TreeNode> leafNodes;
        private HashSet<ElementId> curElementIds;
        private object treeLock = new object();
        // Temporary fields
        private List<ElementId> nodesToAdd;
        private List<ElementId> nodesToDel;

        #endregion Fields

        #region Parameters

        public List<ElementId> NodesToAdd
        {
            get { return this.nodesToAdd; }
            set
            {
                if (value == null)
                {
                    this.nodesToAdd.Clear();
                }
                else
                {
                    IEnumerable<ElementId> nodesToAdd
                        = from ElementId id in value
                          where (!this.curElementIds.Contains(id))
                          select id;
                    this.nodesToAdd = nodesToAdd.ToList();
                }
            }
        }

        public List<ElementId> NodesToDel
        {
            get { return this.nodesToDel; }
            set
            {
                if (value == null)
                {
                    this.nodesToDel.Clear();
                }
                else
                {
                    IEnumerable<ElementId> nodesToDelete
                        = from ElementId id in value
                          where this.curElementIds.Contains(id)
                          select id;
                    // IEnumerable<ElementId> nodesToDelete = this.curElementIds.Except(commonElementIds);
                    this.nodesToDel = nodesToDelete.ToList();
                }
            }
        }

        public HashSet<ElementId> CurElementIds { get { return this.curElementIds; } }

        public Dictionary<ElementId, TreeNode> LeafNodes { get { return this.leafNodes; } }

        #endregion Parameters

        public TreeViewController(TreeView treeView)
        {
            this.treeView = treeView ?? throw new ArgumentNullException("treeView");

            this.leafNodes = new Dictionary<ElementId, TreeNode>();
            this.curElementIds = new HashSet<ElementId>();
        }

        #region Update TreeView Structure

        /// <summary>
        /// Commit the changes requested from the object's NodesToDel and NodesToAdd parameters
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="keepChangeLists"></param>
        public void CommitChanges(TreeStructure tree, bool keepChangeLists = false)
        {
            // Remove and add elementIds
            Remove(this.NodesToDel, this.leafNodes);
            Append(this.NodesToAdd, tree);

            // If the argument specifies to not keep the change lists, reset it by passing null to the lists
            if (!keepChangeLists)
            {
                this.NodesToDel = null;
                this.NodesToAdd = null;
            }
        }

        #endregion Update TreeView Structure

        #region Reset Data

        /// <summary>
        /// Quickly clear out all the data from the treeView
        /// </summary>
        public void Clear()
        {
            lock (treeLock)
            {
                // Call a function to clear out all the data from the treeView
                ClearAllData();
            }
        }

        /// <summary>
        /// Quickly clear out all the data from the treeView
        /// </summary>
        private void ClearAllData()
        {
            // Clears all the treeView nodes
            this.treeView.Nodes.Clear();
            // Clears all the leafNode mappings
            this.leafNodes.Clear();
            // Clears the hashSet of currentElementIds
            this.curElementIds.Clear();
        }

        #endregion Reset Data

        #region Add Nodes Into TreeView

        /// <summary>
        /// Append the given elementIds into the treeView
        /// </summary>
        /// <param name="elementIds"></param>
        /// <param name="tree"></param>
        public void Append(
            List<ElementId> elementIds,
            TreeStructure tree
            )
        {
            // If there are no elementIds, then exit out
            if (elementIds.Count == 0) return;

            lock (treeLock)
            {
                // Call a method that will add treeNodes corresponding to the elementIds into treeView
                AddNodesToTree(elementIds, this.treeView.Nodes, tree, Depth.CategoryType);
            }
        }

        /// <summary>
        /// Adds new leaves into the treeView and contructs the branches to get there by recursion
        /// </summary>
        /// <param name="elementIds"></param>
        /// <param name="treeNodes"></param>
        /// <param name="tree"></param>
        /// <param name="depth"></param>
        /// <param name="lowestDepth"></param>
        private void AddNodesToTree(
            List<ElementId> elementIds,
            TreeNodeCollection treeNodes,
            TreeStructure tree,
            Depth depth,
            Depth lowestDepth = Depth.Instance)
        {
            // If the method currently is at the lowestDepth...
            if (depth == lowestDepth)
            {
                // For every elementId...
                TreeNode node;
                foreach (ElementId id in elementIds)
                {
                    // Get a clone of the tree's cached node
                    node = CloneTreeNode(tree.ElementIdNodes[id]);
                    // Add the node into the treeNode
                    treeNodes.Add(node);
                    // Record the elementId and node to keep track of it and for easy access
                    this.curElementIds.Add(id);
                    this.leafNodes.Add(id, node);
                }
                // Exit out immediately
                return;
            }

            Depth nextDepth;
            SortedDictionary<string, List<ElementId>> grouping;

            // Get the next depth
            nextDepth = GetNextDepth(depth, lowestDepth);
            // Get the next grouping
            grouping = GetNextGrouping(elementIds, tree.ElementIdNodes, depth.ToString());

            // Sort the dictionary in alphebetical order
            grouping.OrderBy(key => key.Key);
            foreach (KeyValuePair<string, List<ElementId>> kvp in grouping)
            {
                TreeNode branch;
                // If treeNodes doesn't have a node with the name of the parameterValue
                if (!treeNodes.ContainsKey(kvp.Key))
                {
                    // Make a TreeNode that has the name of the parameterValue
                    branch = new TreeNode();
                    branch.Name = kvp.Key;
                    branch.Text = kvp.Key;
                    // Put it into the treeView
                    treeNodes.Add(branch);
                }
                else
                {
                    // Get the treeNode from the treeView
                    branch = treeNodes[kvp.Key];
                }

                // Recurse into the node that has the node name of kvp.Key
                AddNodesToTree(kvp.Value, branch.Nodes, tree, nextDepth);
            }
        }

        #endregion Add Nodes Into TreeView

        #region Delete Nodes From TreeView

        /// <summary>
        /// Remove the given elementIds from the given treeView
        /// </summary>
        /// <param name="elementIds"></param>
        /// <param name="nodeDict"></param>
        public void Remove(
            List<ElementId> elementIds,
            Dictionary<ElementId, TreeNode> nodeDict
            )
        {
            // If there are no elementIds, then exit out
            if (elementIds.Count == 0) return;

            lock (treeLock)
            {
                if (elementIds.Count == this.curElementIds.Count)
                {
                    // If the method detects that all elementIds are being removed,
                    // take a shortcut and clear out everything
                    ClearAllData();
                }
                else
                {
                    // Else, call a method that will delete the nodes with the corresponding elementIds recursively
                    DelNodesInTree(elementIds, this.treeView.Nodes, nodeDict, Depth.CategoryType);
                }
            }
        }

        /// <summary>
        /// Deletes existing leaves in the treeView and removes the branches if not needed by recursion
        /// </summary>
        /// <param name="elementIds"></param>
        /// <param name="treeNodes"></param>
        /// <param name="nodeDict"></param>
        /// <param name="depth"></param>
        /// <param name="lowestDepth"></param>
        private void DelNodesInTree(
            List<ElementId> elementIds,
            TreeNodeCollection treeNodes,
            Dictionary<ElementId, TreeNode> nodeDict,
            Depth depth,
            Depth lowestDepth = Depth.Instance)
        {
            // If the method currently is at the lowestDepth...
            if (depth == lowestDepth)
            {
                // For every elementId...
                TreeNode node;
                foreach (ElementId id in elementIds)
                {
                    // Get the node from the node mapping
                    node = nodeDict[id];
                    // Remove the node from treeView
                    treeNodes.Remove(node);
                    // Remove the node and elementId from the records
                    this.curElementIds.Remove(id);
                    this.leafNodes.Remove(id);
                }
                // Exit out immediately
                return;
            }

            Depth nextDepth;
            SortedDictionary<string, List<ElementId>> grouping;

            // Get next depth
            nextDepth = GetNextDepth(depth, lowestDepth);
            // Get next grouping
            grouping = GetNextGrouping(elementIds, nodeDict, depth.ToString());

            // Sort the elements in alphebetical order
            grouping.OrderBy(key => key.Key);
            foreach (KeyValuePair<string, List<ElementId>> kvp in grouping)
            {
                TreeNode branch;
                // If treeNodes doesn't have a node with the name of the parameter value...
                if (!treeNodes.ContainsKey(kvp.Key))
                {
                    // Get all elements that is represented and remove them from the records
                    foreach (ElementId id in kvp.Value)
                    {
                        this.curElementIds.Remove(id);
                        this.leafNodes.Remove(id);
                    }
                    // Continue to the next iteration
                    continue;
                }
                else
                {
                    // Get the next branch node
                    branch = treeNodes[kvp.Key];
                }

                // Recurse into the node that has the node name of kvp.Key
                DelNodesInTree(kvp.Value, branch.Nodes, nodeDict, nextDepth);

                // If the current branch has no children after the recursion, remove itself as well
                if (branch.Nodes.Count == 0)
                {
                    branch.Remove();
                }
            }

        }

        #endregion Delete Nodes From TreeView

        #region Nodal Selection Counter

        /// <summary>
        /// Refresh the selection counters of all the prescribed nodes within TreeView (CategoryType and Category)
        /// </summary>
        /// <param name="data"></param>
        public void RefreshNodeCounters(DataController data)
        {
            // Step 1: Get the enumerable of tree nodes
            TreeNodeCollection collection = treeView.Nodes;

            TreeNodeCollection subCollection = null;

            // Step 2: For every TreeNode...
            foreach (TreeNode node in collection)
            {
                // Step 2.1: Get the node's nodes
                subCollection = node.Nodes;

                // Step 2.2: For every subNode...
                foreach (TreeNode subNode in subCollection)
                {
                    // Step 2.2.1: Update the nodal counter for subNode
                    UpdateNodeCounter(subNode, data);
                }

                // Step 2.3: Update the nodal counter for node
                UpdateNodeCounter(node, data);
            }
        }

        /// <summary>
        /// Updates the current counter of this node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="data"></param>
        public void UpdateNodeCounter(TreeNode node, DataController data)
        {
            int total = 0;
            int selected = 0;

            // Use DataController to get all the leaf's elementIds that is under the current node
            HashSet<ElementId> leafs = GetLeafsFromNode(node, data);

            // Set the total and initialize selected
            total = leafs.Count;
            selected = 0;

            // For each elementId, it retrieves the id from leafNodes, and adds one to selected if that node is checked.
            TreeNode l;
            foreach (ElementId id in leafs)
            {
                l = this.leafNodes[id];
                if (l.Checked) { selected += 1; }
            }

            // text = "{num selected}/{num total} {node name}"
            string text = string.Format("[{0}/{1}] {2}", selected, total, node.Name);

            // Set the node's text
            node.Text = text;
        }

        /// <summary>
        /// Get the set of the leaf's elementIds that is under the argument node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private HashSet<ElementId> GetLeafsFromNode(TreeNode node, DataController data)
        {
            // Get the tokened string that outlines the path to take in the tree to reach to the current node
            List<string> pathTokens = GetNamePath(node, new List<string>());

            // From the data controller, call the function to get ALL the element ids by path
            HashSet<ElementId> elementIds = data.GetElementIdsByPath(pathTokens);

            // Interset the set with the elementIds we currently have
            elementIds.IntersectWith(curElementIds);

            return elementIds;
        }

        #endregion Nodal Selection Counter

        #region Auxiliary Methods

        /// <summary>
        /// Returns a new, seperate node with the exact parameters as the given node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private TreeNode CloneTreeNode(TreeNode node)
        {
            if (node == null) throw new ArgumentNullException("node");
            else if ((node.Tag as NodeData) == null) throw new ArgumentNullException("node.Tag");

            TreeNode newNode = new TreeNode();

            newNode = node.Clone() as TreeNode;
            newNode.Tag = node.Tag as NodeData;

            return newNode;
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
            {
                // If depth is Invalid, then something is wrong, throw exception
                throw new ArgumentException();
            }
            else if (depth == lowest)
            {
                // If depth is at its lowest, then set the next depth to Invalid
                next = Depth.Invalid;
            }
            else
            {
                // Get the depth corresponding to the number that is one greater than the current one
                next = (Depth)((int)depth + 1);
            }

            return next;
        }

        /// <summary>
        /// Get the grouping of elementIds by a specified parameter name
        /// </summary>
        /// <param name="elementIds"></param>
        /// <param name="nodeDict"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        private SortedDictionary<string, List<ElementId>> GetNextGrouping(
            List<ElementId> elementIds,
            Dictionary<ElementId, TreeNode> nodeDict,
            string paramName
            )
        {
            SortedDictionary<string, List<ElementId>> grouping = new SortedDictionary<string, List<ElementId>>();

            string paramValue;
            TreeNode node;
            NodeData data;
            foreach (ElementId id in elementIds)
            {
                // Get the node from the elementId and node map
                node = nodeDict[id];
                // Get the NodeData from the node
                data = node.Tag as NodeData;
                // If there is no node data, something is wrong, throw exception
                if (data == null) throw new NullReferenceException("data");
                // Get parameterValue by searching NodeData's parameterName
                paramValue = data.GetParameter(paramName);
                // If parameterValue isn't already represented in the grouping, make a new one
                if (!grouping.ContainsKey(paramValue))
                    grouping.Add(paramValue, new List<ElementId>());
                // Add the elementId corresponding to the parameterValue
                grouping[paramValue].Add(id);
            }

            return grouping;
        }

        /// <summary>
        /// Returns the path of the tree structure to reach the current node but with node names
        /// </summary>
        /// <param name="node"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public List<string> GetNamePath(TreeNode node, List<string> list)
        {
            if (node == null) throw new ArgumentNullException("node");
            if (list == null) throw new ArgumentNullException("list");

            // Gets the current node name
            string name = node.Name;

            // Get the node's parent
            TreeNode parent = node.Parent;

            // If parent isn't null, then recurse up and get the is name path
            if (parent != null)
                list = GetNamePath(parent, list);

            // add the node's name into the list
            list.Add(name);

            return list;
        }

        #region Get Branch Nodes

        public HashSet<TreeNode> GetBranchNodes(IEnumerable<ElementId> elementIds, Depth depth)
        {
            IEnumerable<TreeNode> nodes
                = from ElementId id in elementIds
                  select this.leafNodes[id];

            IEnumerable<List<string>> paths
                = from TreeNode n in nodes
                  select GetNamePath(n, new List<string>());

            IEnumerable<List<string>> stubs
                = new HashSet<List<string>>(
                    from List<string> subPaths in paths
                    select GetPathFromDepth(subPaths, depth));

            IEnumerable<TreeNode> branches
                = new List<TreeNode>(
                    from List<string> stub in stubs
                    select GetTreeNodeFromStub(stub));

            return new HashSet<TreeNode>(branches);
        }

        private List<string> GetPathFromDepth(List<string> p, Depth d)
        {
            List<string> newP = new List<string>();
            int i = 0;
            switch (d)
            {
                case Depth.CategoryType:
                    i = 1;
                    break;
                case Depth.Category:
                    i = 2;
                    break;
                default:
                    i = 1;
                    break;
            }

            for (int k = 0; k < i; k++)
            {
                newP.Add(p[k]);
            }

            return newP;
        }

        private TreeNode GetTreeNodeFromStub(List<string> stub)
        {
            TreeNodeCollection col = this.treeView.Nodes;

            TreeNode node = null;
            foreach (string s in stub)
            {
                node = col[s];
                col = node.Nodes;
            }

            return node;
        }

        #endregion Get Branch Nodess

        #endregion Auxiliary Methods
    }
}
