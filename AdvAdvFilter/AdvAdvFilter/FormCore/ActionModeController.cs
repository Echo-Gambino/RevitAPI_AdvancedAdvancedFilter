namespace AdvAdvFilter
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    class ActionModeController
    {

        #region DataType

        public enum ActionMode
        {
            None = 0,
            ShiftOnly = 1,
            CopyAndShift = 2,
            Invalid = -1
        }

        #endregion DataType

        #region Fields

        private Panel panel;
        private RadioButton shift;
        private RadioButton copyAndShift;

        #endregion Fields

        public ActionModeController(
            Panel panel,
            RadioButton shift,
            RadioButton copyAndShift
            )
        {
            this.panel = panel ?? throw new ArgumentNullException();
            this.shift = shift ?? throw new ArgumentNullException();
            this.copyAndShift = copyAndShift ?? throw new ArgumentNullException();
        }

        #region Controls

        public void Reset()
        {
            this.shift.Checked = true;
            this.copyAndShift.Checked = false;
        }

        #endregion Controls

        #region Getters

        public ActionMode GetMode()
        {
            ActionMode mode = ActionMode.None;
            bool relChecked = shift.Checked;
            bool absChecked = copyAndShift.Checked;

            // Check if the two radio buttons are in different states
            if (relChecked != absChecked)
            {
                // Check if relChecked is checked
                if (relChecked)
                {
                    // relChecked is true, then the mode is 'relative'
                    mode = ActionMode.ShiftOnly;
                }
                else
                {
                    // If relChecked isn't true, mode must then be 'absolute'
                    mode = ActionMode.CopyAndShift;
                }
            }
            else
            {
                if (relChecked)
                {
                    // If relChecked and absChecked is the same and true, then set mode to invalid
                    mode = ActionMode.Invalid;
                }
                else
                {
                    // If relChecked and absChecked is the same and false, then set mode to none
                    mode = ActionMode.None;
                }
            }

            return mode;
        }

        #endregion Getters
    }
}
