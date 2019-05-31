namespace AdvAdvFilter
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Drawing;

    class ActionAxisController
    {
        #region Fields

        private Panel panel;
        private Label label;
        private TextBox textBox;

        #endregion Fields

        #region Parameters

        public Panel Panel
        {
            get { return this.panel; }
        }

        public Label Label
        {
            get { return this.label; }
        }

        public TextBox TextBox
        {
            get { return this.textBox; }
        }

        public string Output
        {
            get { return textBox.Text; }
        }

        #endregion Parameters

        public ActionAxisController(
            Panel panel,
            Label label,
            TextBox textBox
            )
        {
            this.panel = panel ?? throw new ArgumentNullException();
            this.label = label ?? throw new ArgumentNullException();
            this.textBox = textBox ?? throw new ArgumentNullException();
        }

        public void Reset()
        {
            this.textBox.Text = "";
            this.textBox.ForeColor = Color.Black;
        }

    }
}
