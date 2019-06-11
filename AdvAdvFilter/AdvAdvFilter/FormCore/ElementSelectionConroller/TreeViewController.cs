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
                    this.nodesToDel = nodesToDelete.ToList();
                }
            }
        }

        public HashSet<ElementId> CurElementIds { get { return this.curElementIds; } }

        #endregion Parameters

        public TreeViewController(TreeView treeView)
        {
            this.treeView = treeView ?? throw new ArgumentNullException("treeView");

            this.leafNodes = new Dictionary<ElementId, TreeNode>();
            this.curElementIds = new HashSet<ElementId>();
        }

        #region Update TreeView Structure

        public void CommitChanges(TreeStructure tree, bool keepChangeLists = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("cur " + this.curElementIds.Count.ToString());
            sb.AppendLine("del " + this.NodesToDel.Count.ToString());
            sb.AppendLine("add " + this.NodesToAdd.Count.ToString());
            MessageBox.Show(sb.ToString());

            Remove(this.NodesToDel, this.leafNodes);
            Append(this.NodesToAdd, tree);

            if (!keepChangeLists)
            {
                this.NodesToDel = null;
                this.NodesToAdd = null;
            }
        }

        #endregion Update TreeView Structure

        #region AddNodes

        private void Remove(
            List<ElementId> elementIds,
            Dictionary<ElementId, TreeNode> nodeDict
            )
        {
            if (elementIds.Count == 0) return;

            lock (treeLock)
            {
                DelNodesInTree(elementIds, this.treeView.Nodes, nodeDict, Depth.CategoryType);
            }
        }

        private void Append(
            List<ElementId> elementIds,
            TreeStructure tree
            )
        {
            if (elementIds.Count == 0) return;

            lock (treeLock)
            {
                AddNodesToTree(elementIds, this.treeView.Nodes, tree, Depth.CategoryType);
            }
        }

        private void AddNodesToTree(
            List<ElementId> elementIds,
            TreeNodeCollection treeNodes,
            TreeStructure tree,
            Depth depth,
            Depth lowestDepth = Depth.Instance)
        {
            if (depth == lowestDepth)
            {
                TreeNode node;
                foreach (ElementId id in elementIds)
                {
                    node = CloneTreeNode(tree.ElementIdNodes[id]);

                    treeNodes.Add(node);

                    this.curElementIds.Add(id);
                    this.leafNodes.Add(id, node);
                }
                return;
            }

            Depth nextDepth;
            SortedDictionary<string, List<ElementId>> grouping;

            nextDepth = GetNextDepth(depth, lowestDepth);
            grouping = GetNextGrouping(elementIds, tree.ElementIdNodes, depth.ToString());

            grouping.OrderBy(key => key.Key);
            foreach (KeyValuePair<string, List<ElementId>> kvp in grouping)
            {
                TreeNode branch;
                // If treeNodes doesn't have a node with the name of the
                // selected parameter value of a group of leaves,
                // then make a node that has that name
                if (!treeNodes.ContainsKey(kvp.Key))
                {
                    branch = new TreeNode();
                    branch.Name = kvp.Key;
                    branch.Text = kvp.Key;
                    treeNodes.Add(branch);
                }
                else
                {
                    branch = treeNodes[kvp.Key];
                }

                // Recurse into the node that has the node name of kvp.Key
                AddNodesToTree(kvp.Value, branch.Nodes, tree, nextDepth);
            }
        }

        private void DelNodesInTree(
            List<ElementId> elementIds,
            TreeNodeCollection treeNodes,
            Dictionary<ElementId, TreeNode> nodeDict,
            Depth depth,
            Depth lowestDepth = Depth.Instance)
        {
            if (depth == lowestDepth)
            {
                TreeNode node;
                foreach (ElementId id in elementIds)
                {
                    node = nodeDict[id];

                    treeNodes.Remove(node);

                    this.curElementIds.Remove(id);
                    this.leafNodes.Remove(id);
                }
                return;
            }

            Depth nextDepth;
            SortedDictionary<string, List<ElementId>> grouping;

            nextDepth = GetNextDepth(depth, lowestDepth);
            grouping = GetNextGrouping(elementIds, nodeDict, depth.ToString());

            grouping.OrderBy(key => key.Key);
            foreach (KeyValuePair<string, List<ElementId>> kvp in grouping)
            {
                TreeNode branch;
                // If treeNodes doesn't have a node with the name of the
                // selected parameter value of a group of leaves, skip it
                if (!treeNodes.ContainsKey(kvp.Key))
                {
                    foreach (ElementId id in kvp.Value)
                    {
                        this.curElementIds.Remove(id);
                        this.leafNodes.Remove(id);
                    }
                    continue;
                }
                else
                {
                    branch = treeNodes[kvp.Key];
                }

                // Recurse into the node that has the node name of kvp.Key
                DelNodesInTree(kvp.Value, branch.Nodes, nodeDict, nextDepth);

                if (treeNodes[kvp.Key].Nodes.Count == 0)
                {
                    treeNodes[kvp.Key].Remove();
                }
            }

        }

        #endregion AddNodes

        #region Auxiliary Methods

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
                throw new ArgumentException();
            else if (depth == lowest)
                next = Depth.Invalid;
            else
                next = (Depth)((int)depth + 1);

            return next;
        }

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
                node = nodeDict[id];
                data = node.Tag as NodeData;
                if (data == null) throw new NullReferenceException("data");

                paramValue = data.GetParameter(paramName);

                if (!grouping.ContainsKey(paramValue))
                    grouping.Add(paramValue, new List<ElementId>());

                grouping[paramValue].Add(id);
            }

            return grouping;
        }

        #endregion Auxiliary Methods
    }
}
