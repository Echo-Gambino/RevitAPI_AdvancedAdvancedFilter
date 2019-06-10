namespace AdvAdvFilter
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using FilterMode = AdvAdvFilter.Common.FilterMode;

    class OptionFilterController : Controller
    {
        #region Fields

        private RadioButton selectionRadioButton;
        private RadioButton viewRadioButton;
        private RadioButton projectRadioButton;

        #endregion Fields

        #region Parameters

        public RadioButton SelectionRadioButton
        {
            get { return this.selectionRadioButton; }
        }

        public RadioButton ViewRadioButton
        {
            get { return this.viewRadioButton; }
        }

        public RadioButton ProjectRadioButton
        {
            get { return this.projectRadioButton; }
        }

        #endregion Parameters

        #region Essential Functions

        public OptionFilterController(
            Panel panel,
            RadioButton selection,
            RadioButton view,
            RadioButton project
            ) : base(panel)
        {
            this.selectionRadioButton = selection ?? throw new ArgumentNullException();
            this.viewRadioButton = view ?? throw new ArgumentNullException();
            this.projectRadioButton = project ?? throw new ArgumentNullException();

            this.selectionRadioButton.Text = "Selection";
            this.viewRadioButton.Text = "View";
            this.projectRadioButton.Text = "Project";
        }

        #endregion Essential Functions

        #region Controls

        /// <summary>
        /// Reset the filter's radiobuttons without triggering their respective event handlers
        /// </summary>
        /// <param name="eventHandlers"></param>
        public void Reset(List<System.EventHandler> eventHandlers)
        {
            this.selectionRadioButton.CheckedChanged -= eventHandlers[0];
            this.viewRadioButton.CheckedChanged -= eventHandlers[1];
            this.projectRadioButton.CheckedChanged -= eventHandlers[2];

            this.selectionRadioButton.Checked = false;
            this.viewRadioButton.Checked = true;
            this.projectRadioButton.Checked = false;

            this.selectionRadioButton.CheckedChanged += eventHandlers[0];
            this.viewRadioButton.CheckedChanged += eventHandlers[1];
            this.projectRadioButton.CheckedChanged += eventHandlers[2];
        }

        /// <summary>
        /// Enable the radio buttons
        /// </summary>
        public override void Enable()
        {
            this.selectionRadioButton.Enabled = true;
            this.viewRadioButton.Enabled = true;
            this.projectRadioButton.Enabled = true;
        }

        /// <summary>
        /// Disable the radio buttons
        /// </summary>
        public override void Disable()
        {
            this.selectionRadioButton.Enabled = false;
            this.viewRadioButton.Enabled = false;
            this.projectRadioButton.Enabled = false;
        }

        #endregion Controls

        #region Getters

        /// <summary>
        /// Get the state of the filters
        /// </summary>
        /// <returns></returns>
        public FilterMode GetFilterState()
        {
            FilterMode output = FilterMode.Invalid;

            if (this.selectionRadioButton.Checked)
                output = FilterMode.Selection;
            else if (this.viewRadioButton.Checked)
                output = FilterMode.View;
            else if (this.projectRadioButton.Checked)
                output = FilterMode.Project;

            return output;
        }

        #endregion Getters

    }
}
