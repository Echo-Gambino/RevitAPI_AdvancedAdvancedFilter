namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Element = Autodesk.Revit.DB.Element;
    using Category = Autodesk.Revit.DB.Category;
    using FamilyInstance = Autodesk.Revit.DB.FamilyInstance;
    using Family = Autodesk.Revit.DB.Family;
    using Document = Autodesk.Revit.DB.Document;

    class ElementPickerInterface
    {
        #region Fields

        private Panel panel;
        private TreeView treeView;

        #endregion

        #region Parameters

        public Panel Panel
        {
            get { return this.panel; }
        }

        public TreeView TreeView
        {
            get { return this.treeView; }
        }

        #endregion

        #region Public Methods

        public ElementPickerInterface(
            Panel panel,
            TreeView treeView
            )
        {
            this.panel = panel;
            this.treeView = treeView;
        }

        public List<TreeNode> ToList(TreeNodeCollection collection)
        {
            List<TreeNode> list = new List<TreeNode>();

            foreach (TreeNode node in collection)
                list.Add(node);

            return list;
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


        public void UpdateAfterCheck(TreeViewEventArgs e)
        {
            // Update their children if it has any
            if (e.Node.Nodes.Count > 0)
                UpdateChildNodes(e.Node, e.Node.Checked);

            // Update their parents if it has any
            if (e.Node.Parent != null)
                UpdateParentNodes(e.Node.Parent, e.Node.Checked);

            return;
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

        #region PrivateMethods
        
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
        

        private void UpdateChildNodes(TreeNode node, bool isChecked)
        {
            foreach (TreeNode n in node.Nodes)
            {
                // Update the status of n
                n.Checked = isChecked;
                // If n has children, then update them too
                if (n.Nodes.Count > 0)
                    UpdateChildNodes(n, isChecked);
            }
        }

        private void UpdateParentNodes(TreeNode parent, bool isChecked)
        {
            // Try to get out of the method as soon as it encounters 
            // a node with the same state as the parent.
            foreach (TreeNode n in parent.Nodes)
            {
                if (n.Checked == parent.Checked) return;
            }

            // If all its children are of the opposite status,
            // change the parent's Checked status
            parent.Checked = !parent.Checked;

            // If parent's parent isn't null, continue the recursion up
            if (parent.Parent != null)
                UpdateParentNodes(parent.Parent, isChecked);

            return;
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
