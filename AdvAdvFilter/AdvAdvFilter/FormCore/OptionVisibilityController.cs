namespace AdvAdvFilter
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    class OptionVisibilityController : Controller
    {
        #region Fields

        private CheckBox visibilityCheckBox;

        #endregion Fields

        #region Parameters

        public CheckBox VisibilityCheckBox
        {
            get { return this.visibilityCheckBox; }
        }

        #endregion Parameters

        #region Essential Functions

        public OptionVisibilityController(
            Panel panel,
            CheckBox checkBox
            ) : base(panel)
        {
            this.visibilityCheckBox = checkBox ?? throw new ArgumentNullException();
        }

        #endregion Essential Functions

        #region Controls

        public void Reset(List<System.EventHandler> eventHandler)
        {
            this.visibilityCheckBox.CheckedChanged -= eventHandler[0];
            this.visibilityCheckBox.Checked = false;
            this.visibilityCheckBox.CheckedChanged += eventHandler[0];
        }

        public override void Enable()
        {
            this.visibilityCheckBox.Enabled = true;
        }

        public override void Disable()
        {
            this.visibilityCheckBox.Enabled = false;
        }

        #endregion Controls

        #region Getters

        public bool IsChecked()
        {
            return this.visibilityCheckBox.Checked;
        }

        public void SetState(bool state)
        {
            this.visibilityCheckBox.Checked = state;
        }

        #endregion

    }
}
