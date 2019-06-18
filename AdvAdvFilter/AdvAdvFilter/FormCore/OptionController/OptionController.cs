namespace AdvAdvFilter
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using FilterMode = AdvAdvFilter.Common.FilterMode;

    class OptionController : Controller
    {
        #region Fields

        OptionVisibilityController visibility;
        OptionFilterController filter;
        OptionHideNodeController hide;

        #endregion Fields

        #region Parameters

        public OptionVisibilityController VisibilityController
        {
            get { return this.visibility; }
        }

        public OptionFilterController FilterController
        {
            get { return this.filter; }
        }

        public OptionHideNodeController HideController
        {
            get { return this.hide; }
        }

        public List<string> HideNodesList
        {
            get { return this.hide.CategoryTypes; }
            set { this.hide.CategoryTypes = value; }
        }

        public List<string> HiddenNodes
        {
            get { return this.hide.CategoryHidden; }
        }

        #endregion Parameters

        #region Essential Functions

        public OptionController(
            Panel panel,
            OptionVisibilityController visibility,
            OptionFilterController filter,
            OptionHideNodeController hide
            ) : base(panel)
        {
            this.visibility = visibility ?? throw new ArgumentNullException();
            this.filter = filter ?? throw new ArgumentNullException();
            this.hide = hide ?? throw new ArgumentNullException();
        }

        #endregion Essential Functions

        #region Controls

        public void Reset(
            List<System.EventHandler> visibilityHandler,
            List<System.EventHandler> filterHandler
            )
        {
            this.visibility.Reset(visibilityHandler);
            this.filter.Reset(filterHandler);
        }

        public override void Enable()
        {
            this.visibility.Enable();
            this.filter.Enable();
        }

        public override void Disable()
        {
            this.visibility.Disable();
            this.filter.Disable();
        }

        #region Hide Controls

        public void UpdateHideNodeList(ElementSet set)
        {
            // Get all the categoryTypes
            IEnumerable<string> categoryTypes
                = from KeyValuePair<string, ElementSet> kvp in set.Branch
                  select kvp.Key;

            // Set categoryTypes to the HideNodesList
            this.HideNodesList = categoryTypes.ToList();
            // Display the HideNodeList by updating it
            this.ShowHideNodeList();
        }

        public void ShowHideNodeList()
        {
            this.hide.Show();
        }

        public void ToggleCheck(int index)
        {
            this.hide.ToggleCheck(index);
        }

        #endregion Hide Controls

        #endregion Controls

        #region Getters

        public bool GetVisibilityState()
        {
            return this.visibility.IsChecked();
        }

        public FilterMode GetFilterState()
        {
            return this.filter.GetFilterState();
        }

        #endregion Getters

    }
}
