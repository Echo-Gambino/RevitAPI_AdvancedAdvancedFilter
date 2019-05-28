namespace AdvAdvFilter
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using FilterMode = AdvAdvFilter.DataController.FilterMode;

    class OptionController : Controller
    {
        #region Fields

        OptionVisibilityController visibility;
        OptionFilterController filter;

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

        #endregion Parameters

        #region Essential Functions

        public OptionController(
            Panel panel,
            OptionVisibilityController visibility,
            OptionFilterController filter
            ) : base(panel)
        {
            this.visibility = visibility ?? throw new ArgumentNullException();
            this.filter = filter ?? throw new ArgumentNullException();
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
