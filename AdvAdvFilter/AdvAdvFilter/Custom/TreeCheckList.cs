namespace AdvAdvFilter.Custom
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Drawing;

    public class TreeCheckList
    {
        #region SubClass(es)

        private class TreeNode
        {
            #region Fields

            // Panel 'self' is used to encapsulate itself and other subchildren
            private Panel self = null;
            // Label 'size' is used to let the user collapse/expand the object
            private Label size = null;
            // CheckBox 'item' an interactable checkbox
            private CheckBox item = null;
            // List<TreeNode> is a list of children of the current TreeNode
            private List<TreeNode> childItem = null;

            #endregion

            #region Parameters

            public int Height
            {
                get
                {
                    int sizeHeight = 0;
                    int itemHeight = 0;
                    int childItemHeight = 0;

                    if (size != null) sizeHeight = size.Size.Height;
                    if (item != null) itemHeight = item.Size.Height;
                    if (childItem != null)
                        foreach (TreeNode child in childItem)
                            childItemHeight = childItemHeight + child.Height;

                    return Math.Max(sizeHeight, itemHeight) + childItemHeight;
                }
            }

            #endregion

            public TreeNode(string nodeName = "Node", List<TreeNode> childList = null)
            {
                // Set all the fields to the 'default' value
                this.self = new Panel();
                this.size = new Label();
                this.item = new CheckBox();
                this.childItem = childList;

                // Add 'size' and 'item' into the panel
                this.self.Controls.Add(size);
                this.self.Controls.Add(item);

                // Set 'size' parameters
                size.Text = "";
                size.Size = new Size(0, 0);
                size.Location = new Point(size.Margin.Left, size.Margin.Top);

                // Set 'item' parameters
                item.Text = nodeName;
                item.Location = new Point(size.Width + item.Margin.Left, item.Margin.Top);

                // Set the height of the panel
                Size s = self.Size;
                s.Height = this.Height;
                self.Size = s;
            }

            private int GetPanelHeight()
            {
                int firstLayer = Math.Max(size.Size.Height, item.Size.Height);

                int subNodeLayer = 0;

                if (childItem != null)
                    subNodeLayer = 5;

                return firstLayer + subNodeLayer;
            }




        }

        #endregion

        

    }

}
