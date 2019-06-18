namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using System.Windows.Forms;

    class OptionHideNodeController
    {
        #region Field

        private Panel panel;
        private CheckedListBox hiddenNodeList;

        private Dictionary<string, bool> categoryTypeStatus;
        private List<string> curCategoryTypes;

        #endregion Field

        #region Parameters

        public List<string> CategoryTypes
        {
            get
            {
                return this.curCategoryTypes;
            }
            set
            {
                if (value == null) throw new ArgumentNullException("OptionHideNodeController.CategoryTypes");

                foreach (string type in value)
                {
                    if (!categoryTypeStatus.ContainsKey(type))
                    {
                        categoryTypeStatus.Add(type, false);
                    }
                }
                this.curCategoryTypes = value;
            }
        }

        public List<string> CategoryHidden
        {
            get
            {
                List<string> output = new List<string>();

                foreach (KeyValuePair<string, bool> kvp in this.categoryTypeStatus)
                {
                    if (kvp.Value) output.Add(kvp.Key);
                }

                return output;
            }

        }

        #endregion Parameters

        public OptionHideNodeController(
            Panel panel,
            CheckedListBox checkedListBox
            )
        {
            this.panel = panel;
            this.hiddenNodeList = checkedListBox;

            categoryTypeStatus = new Dictionary<string, bool>();
            curCategoryTypes = new List<string>();
        }

        public void Show()
        {
            CheckedListBox.ObjectCollection items = this.hiddenNodeList.Items;

            List<object> nodesToRemove
                = new List<object> (
                    from object item in items
                    where (!this.curCategoryTypes.Contains(item.ToString()))
                    select item);

            foreach (object node in nodesToRemove)
            {
                items.Remove(node);
            }

            foreach (string type in this.curCategoryTypes)
            {
                if (!items.Contains(type)) items.Add(type);
            }
        }
    }
}
