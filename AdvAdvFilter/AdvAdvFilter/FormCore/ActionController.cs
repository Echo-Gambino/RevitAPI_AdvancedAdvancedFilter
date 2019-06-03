namespace AdvAdvFilter
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using ActionMode = AdvAdvFilter.ActionModeController.ActionMode;

    class ActionController
    {

        #region Fields

        // Windows forms elements
        private Panel panel;
        private Button resetButton;
        private Button shiftButton;
        private Label promptLabel;
        // Custom made controllers
        private ActionAxisController xAxis;
        private ActionAxisController yAxis;
        private ActionAxisController zAxis;
        private ActionModeController mode;

        #endregion Fields

        #region Parameters

        public string X
        {
            get { return this.xAxis.Output; }
        }

        public string Y
        {
            get { return this.yAxis.Output; }
        }

        public string Z
        {
            get { return this.zAxis.Output; }
        }

        public ActionMode Mode
        {
            get { return this.mode.GetMode(); }
        }

        #endregion Parameters

        public ActionController(
            Panel panel,
            Button resetButton,
            Button shiftButton,
            Label promptLabel,
            List<ActionAxisController> xyz,
            ActionModeController mode
            )
        {
            if (xyz.Count != 3) throw new ArgumentException();
            
            this.panel = panel ?? throw new ArgumentNullException();
            this.resetButton = resetButton ?? throw new ArgumentNullException();
            this.shiftButton = shiftButton ?? throw new ArgumentNullException();
            this.promptLabel = promptLabel ?? throw new ArgumentNullException();

            this.xAxis = xyz[0];
            this.yAxis = xyz[1];
            this.zAxis = xyz[2];

            this.mode = mode ?? throw new ArgumentNullException();
        }

        #region Controls

        public void Reset()
        {
            this.xAxis.Reset();
            this.yAxis.Reset();
            this.zAxis.Reset();
            this.mode.Reset();

            this.DisableDefaults();
            this.DisableShift();
            this.DisablePrompt();
        }

        public void EnableDefaults()
        {
            this.resetButton.Enabled = true;
        }

        public void DisableDefaults()
        {
            this.resetButton.Enabled = false;
        }

        public void EnableShift()
        {
            this.shiftButton.Enabled = true;
        }

        public void DisableShift()
        {
            this.shiftButton.Enabled = false;
        }

        public void EnablePrompt()
        {
            this.promptLabel.Visible = true;
        }

        public void DisablePrompt()
        {
            this.promptLabel.Visible = false;
        }

        public void PromptCustom(string text)
        {
            if (this.promptLabel.Text != text)
            {
                if (!this.promptLabel.Visible)
                    this.promptLabel.Visible = true;

                this.promptLabel.Text = text;
            }
            else
            {
                if (!this.promptLabel.Visible)
                    this.promptLabel.Visible = true;
            }
        }

        public void PromptNotValidElements()
        {
            StringBuilder errorMsg = new StringBuilder();
            List<string> movableCategories = new List<string>()
            {
                "Structural Columns",
                "Structural Connections",
                "Structural Framing",
                "Generic Models",
                "Stacked Walls",
                "Curtain Panels",
                "Walls",
                "Floors"
            };

            errorMsg.Append("Please select valid elements to move\n");
            errorMsg.Append("Valid categories of elements:\n");
            foreach (string c in movableCategories)
            {
                errorMsg.Append(String.Format("  {0}\n", c));
            }

            this.PromptCustom(errorMsg.ToString());
        }

        public void PromptNotValidShifts()
        {
            StringBuilder errorMsg = new StringBuilder();

            errorMsg.Append("The input(s) of shifting the element(s) isn't valid\n");
            errorMsg.Append("Please try again.\n");

            this.PromptCustom(errorMsg.ToString());
        }

        #endregion Controls

        #region Supplementary

        public bool TryGetAllInputs(out List<int> xyz, out bool shiftRelative)
        {
            bool extractionSuccessful = false;

            string xValue = this.xAxis.Output;
            string yValue = this.yAxis.Output;
            string zValue = this.zAxis.Output;
            int value;

            ActionMode mode = this.mode.GetMode();
            List<int> newXYZ = new List<int>();

            bool newShiftRelative = true;

            if (Int32.TryParse(xValue, out value))
            {
                newXYZ.Add(value);
                if (Int32.TryParse(yValue, out value))
                {
                    newXYZ.Add(value);
                    if (Int32.TryParse(zValue, out value))
                    {
                        newXYZ.Add(value);
                        if (mode == ActionMode.Relative)
                        {
                            newShiftRelative = true;
                            extractionSuccessful = true;
                        }
                        else if (mode == ActionMode.Absolute)
                        {
                            newShiftRelative = false;
                            extractionSuccessful = true;
                        }
                    }
                }
            }

            if (extractionSuccessful)
            {
                xyz = newXYZ;
                shiftRelative = newShiftRelative;
            }
            else
            {
                xyz = new List<int>();
                shiftRelative = false;
            }

            return extractionSuccessful;
        }

        public bool IsAllAxisEmpty()
        {
            bool xEmpty = (this.xAxis.Output == "");
            bool yEmpty = (this.yAxis.Output == "");
            bool zEmpty = (this.zAxis.Output == "");

            return xEmpty && yEmpty && zEmpty;
        }

        public bool IsModeDefault()
        {
            ActionMode defaultMode = ActionMode.Relative;
            return (this.mode.GetMode() == defaultMode);
        }

        #endregion Supplementary
    }
}
