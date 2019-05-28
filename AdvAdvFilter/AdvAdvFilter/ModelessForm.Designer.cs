namespace AdvAdvFilter
{
    partial class ModelessForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ElementSelectionPanel = new System.Windows.Forms.Panel();
            this.ElementSelectionTreeView = new System.Windows.Forms.TreeView();
            this.ElementSelectionLabel = new System.Windows.Forms.Label();
            this.TestLabel = new System.Windows.Forms.Label();
            this.SidePanel = new System.Windows.Forms.Panel();
            this.ActionPanel = new System.Windows.Forms.Panel();
            this.ActionShiftButton = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.ActionShiftRadioButton0 = new System.Windows.Forms.RadioButton();
            this.ActionShiftRadioButton1 = new System.Windows.Forms.RadioButton();
            this.ActionShiftPanel2 = new System.Windows.Forms.Panel();
            this.ActionShiftTextBox2 = new System.Windows.Forms.TextBox();
            this.ActionShiftLabel2 = new System.Windows.Forms.Label();
            this.ActionShiftPanel1 = new System.Windows.Forms.Panel();
            this.ActionShiftTextBox1 = new System.Windows.Forms.TextBox();
            this.ActionShiftLabel1 = new System.Windows.Forms.Label();
            this.ActionShiftPanel0 = new System.Windows.Forms.Panel();
            this.ActionShiftTextBox0 = new System.Windows.Forms.TextBox();
            this.ActionShiftLabel0 = new System.Windows.Forms.Label();
            this.ActionLabel = new System.Windows.Forms.Label();
            this.OptionPanel = new System.Windows.Forms.Panel();
            this.OptionFilterPanel = new System.Windows.Forms.Panel();
            this.OptionFilterRadioButton2 = new System.Windows.Forms.RadioButton();
            this.OptionFilterRadioButton1 = new System.Windows.Forms.RadioButton();
            this.OptionFilterRadioButton0 = new System.Windows.Forms.RadioButton();
            this.OptionFilterLabel = new System.Windows.Forms.Label();
            this.OptionVisibilityPanel = new System.Windows.Forms.Panel();
            this.OptionVisibilityLabel = new System.Windows.Forms.Label();
            this.OptionVisibilityCheckBox = new System.Windows.Forms.CheckBox();
            this.OptionLabel = new System.Windows.Forms.Label();
            this.ElementSelectionPanel.SuspendLayout();
            this.SidePanel.SuspendLayout();
            this.ActionPanel.SuspendLayout();
            this.panel1.SuspendLayout();
            this.ActionShiftPanel2.SuspendLayout();
            this.ActionShiftPanel1.SuspendLayout();
            this.ActionShiftPanel0.SuspendLayout();
            this.OptionPanel.SuspendLayout();
            this.OptionFilterPanel.SuspendLayout();
            this.OptionVisibilityPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // ElementSelectionPanel
            // 
            this.ElementSelectionPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.ElementSelectionPanel.Controls.Add(this.ElementSelectionTreeView);
            this.ElementSelectionPanel.Controls.Add(this.ElementSelectionLabel);
            this.ElementSelectionPanel.Location = new System.Drawing.Point(12, 12);
            this.ElementSelectionPanel.MinimumSize = new System.Drawing.Size(300, 300);
            this.ElementSelectionPanel.Name = "ElementSelectionPanel";
            this.ElementSelectionPanel.Size = new System.Drawing.Size(300, 390);
            this.ElementSelectionPanel.TabIndex = 6;
            // 
            // ElementSelectionTreeView
            // 
            this.ElementSelectionTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ElementSelectionTreeView.CheckBoxes = true;
            this.ElementSelectionTreeView.Location = new System.Drawing.Point(0, 3);
            this.ElementSelectionTreeView.Name = "ElementSelectionTreeView";
            this.ElementSelectionTreeView.Size = new System.Drawing.Size(300, 368);
            this.ElementSelectionTreeView.TabIndex = 1;
            this.ElementSelectionTreeView.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.ElementSelectionTreeView_AfterCheck);
            this.ElementSelectionTreeView.BeforeCollapse += new System.Windows.Forms.TreeViewCancelEventHandler(this.ElementSelectionTreeView_BeforeCollapse);
            this.ElementSelectionTreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.ElementSelectionTreeView_BeforeExpand);
            this.ElementSelectionTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ElementSelectionTreeView_AfterSelect);
            // 
            // ElementSelectionLabel
            // 
            this.ElementSelectionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ElementSelectionLabel.AutoSize = true;
            this.ElementSelectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ElementSelectionLabel.Location = new System.Drawing.Point(3, 374);
            this.ElementSelectionLabel.Name = "ElementSelectionLabel";
            this.ElementSelectionLabel.Size = new System.Drawing.Size(137, 16);
            this.ElementSelectionLabel.TabIndex = 0;
            this.ElementSelectionLabel.Text = "Total Selected Items: ";
            // 
            // TestLabel
            // 
            this.TestLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.TestLabel.AutoSize = true;
            this.TestLabel.Location = new System.Drawing.Point(681, 12);
            this.TestLabel.Name = "TestLabel";
            this.TestLabel.Size = new System.Drawing.Size(35, 13);
            this.TestLabel.TabIndex = 7;
            this.TestLabel.Text = "label1";
            // 
            // SidePanel
            // 
            this.SidePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SidePanel.Controls.Add(this.ActionPanel);
            this.SidePanel.Controls.Add(this.OptionPanel);
            this.SidePanel.Location = new System.Drawing.Point(315, 12);
            this.SidePanel.MaximumSize = new System.Drawing.Size(400, 1000);
            this.SidePanel.MinimumSize = new System.Drawing.Size(250, 300);
            this.SidePanel.Name = "SidePanel";
            this.SidePanel.Size = new System.Drawing.Size(289, 390);
            this.SidePanel.TabIndex = 8;
            // 
            // ActionPanel
            // 
            this.ActionPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ActionPanel.Controls.Add(this.ActionShiftButton);
            this.ActionPanel.Controls.Add(this.panel1);
            this.ActionPanel.Controls.Add(this.ActionShiftPanel2);
            this.ActionPanel.Controls.Add(this.ActionShiftPanel1);
            this.ActionPanel.Controls.Add(this.ActionShiftPanel0);
            this.ActionPanel.Controls.Add(this.ActionLabel);
            this.ActionPanel.Location = new System.Drawing.Point(4, 173);
            this.ActionPanel.Name = "ActionPanel";
            this.ActionPanel.Size = new System.Drawing.Size(282, 214);
            this.ActionPanel.TabIndex = 1;
            // 
            // ActionShiftButton
            // 
            this.ActionShiftButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ActionShiftButton.Location = new System.Drawing.Point(3, 186);
            this.ActionShiftButton.Name = "ActionShiftButton";
            this.ActionShiftButton.Size = new System.Drawing.Size(276, 25);
            this.ActionShiftButton.TabIndex = 8;
            this.ActionShiftButton.Text = "Defaults";
            this.ActionShiftButton.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.ActionShiftRadioButton0);
            this.panel1.Controls.Add(this.ActionShiftRadioButton1);
            this.panel1.Location = new System.Drawing.Point(3, 102);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(276, 50);
            this.panel1.TabIndex = 7;
            // 
            // ActionShiftRadioButton0
            // 
            this.ActionShiftRadioButton0.AutoSize = true;
            this.ActionShiftRadioButton0.Location = new System.Drawing.Point(6, 5);
            this.ActionShiftRadioButton0.Name = "ActionShiftRadioButton0";
            this.ActionShiftRadioButton0.Size = new System.Drawing.Size(64, 17);
            this.ActionShiftRadioButton0.TabIndex = 5;
            this.ActionShiftRadioButton0.TabStop = true;
            this.ActionShiftRadioButton0.Text = "Relative";
            this.ActionShiftRadioButton0.UseVisualStyleBackColor = true;
            // 
            // ActionShiftRadioButton1
            // 
            this.ActionShiftRadioButton1.AutoSize = true;
            this.ActionShiftRadioButton1.Location = new System.Drawing.Point(6, 28);
            this.ActionShiftRadioButton1.Name = "ActionShiftRadioButton1";
            this.ActionShiftRadioButton1.Size = new System.Drawing.Size(66, 17);
            this.ActionShiftRadioButton1.TabIndex = 6;
            this.ActionShiftRadioButton1.TabStop = true;
            this.ActionShiftRadioButton1.Text = "Absolute";
            this.ActionShiftRadioButton1.UseVisualStyleBackColor = true;
            // 
            // ActionShiftPanel2
            // 
            this.ActionShiftPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ActionShiftPanel2.Controls.Add(this.ActionShiftTextBox2);
            this.ActionShiftPanel2.Controls.Add(this.ActionShiftLabel2);
            this.ActionShiftPanel2.Location = new System.Drawing.Point(3, 75);
            this.ActionShiftPanel2.Name = "ActionShiftPanel2";
            this.ActionShiftPanel2.Size = new System.Drawing.Size(276, 26);
            this.ActionShiftPanel2.TabIndex = 4;
            // 
            // ActionShiftTextBox2
            // 
            this.ActionShiftTextBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ActionShiftTextBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ActionShiftTextBox2.Location = new System.Drawing.Point(20, 3);
            this.ActionShiftTextBox2.MinimumSize = new System.Drawing.Size(100, 20);
            this.ActionShiftTextBox2.Name = "ActionShiftTextBox2";
            this.ActionShiftTextBox2.Size = new System.Drawing.Size(253, 20);
            this.ActionShiftTextBox2.TabIndex = 4;
            // 
            // ActionShiftLabel2
            // 
            this.ActionShiftLabel2.AutoSize = true;
            this.ActionShiftLabel2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ActionShiftLabel2.Location = new System.Drawing.Point(3, 3);
            this.ActionShiftLabel2.MinimumSize = new System.Drawing.Size(15, 20);
            this.ActionShiftLabel2.Name = "ActionShiftLabel2";
            this.ActionShiftLabel2.Size = new System.Drawing.Size(20, 20);
            this.ActionShiftLabel2.TabIndex = 1;
            this.ActionShiftLabel2.Text = "Z :";
            this.ActionShiftLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ActionShiftPanel1
            // 
            this.ActionShiftPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ActionShiftPanel1.Controls.Add(this.ActionShiftTextBox1);
            this.ActionShiftPanel1.Controls.Add(this.ActionShiftLabel1);
            this.ActionShiftPanel1.Location = new System.Drawing.Point(3, 49);
            this.ActionShiftPanel1.Name = "ActionShiftPanel1";
            this.ActionShiftPanel1.Size = new System.Drawing.Size(276, 26);
            this.ActionShiftPanel1.TabIndex = 3;
            // 
            // ActionShiftTextBox1
            // 
            this.ActionShiftTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ActionShiftTextBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ActionShiftTextBox1.Location = new System.Drawing.Point(20, 3);
            this.ActionShiftTextBox1.MinimumSize = new System.Drawing.Size(100, 20);
            this.ActionShiftTextBox1.Name = "ActionShiftTextBox1";
            this.ActionShiftTextBox1.Size = new System.Drawing.Size(253, 20);
            this.ActionShiftTextBox1.TabIndex = 4;
            // 
            // ActionShiftLabel1
            // 
            this.ActionShiftLabel1.AutoSize = true;
            this.ActionShiftLabel1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ActionShiftLabel1.Location = new System.Drawing.Point(3, 3);
            this.ActionShiftLabel1.MinimumSize = new System.Drawing.Size(15, 20);
            this.ActionShiftLabel1.Name = "ActionShiftLabel1";
            this.ActionShiftLabel1.Size = new System.Drawing.Size(20, 20);
            this.ActionShiftLabel1.TabIndex = 1;
            this.ActionShiftLabel1.Text = "Y :";
            this.ActionShiftLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ActionShiftPanel0
            // 
            this.ActionShiftPanel0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ActionShiftPanel0.Controls.Add(this.ActionShiftTextBox0);
            this.ActionShiftPanel0.Controls.Add(this.ActionShiftLabel0);
            this.ActionShiftPanel0.Location = new System.Drawing.Point(3, 23);
            this.ActionShiftPanel0.Name = "ActionShiftPanel0";
            this.ActionShiftPanel0.Size = new System.Drawing.Size(276, 26);
            this.ActionShiftPanel0.TabIndex = 2;
            // 
            // ActionShiftTextBox0
            // 
            this.ActionShiftTextBox0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ActionShiftTextBox0.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ActionShiftTextBox0.Location = new System.Drawing.Point(20, 3);
            this.ActionShiftTextBox0.MinimumSize = new System.Drawing.Size(100, 20);
            this.ActionShiftTextBox0.Name = "ActionShiftTextBox0";
            this.ActionShiftTextBox0.Size = new System.Drawing.Size(253, 20);
            this.ActionShiftTextBox0.TabIndex = 4;
            // 
            // ActionShiftLabel0
            // 
            this.ActionShiftLabel0.AutoSize = true;
            this.ActionShiftLabel0.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ActionShiftLabel0.Location = new System.Drawing.Point(3, 3);
            this.ActionShiftLabel0.MinimumSize = new System.Drawing.Size(15, 20);
            this.ActionShiftLabel0.Name = "ActionShiftLabel0";
            this.ActionShiftLabel0.Size = new System.Drawing.Size(20, 20);
            this.ActionShiftLabel0.TabIndex = 1;
            this.ActionShiftLabel0.Text = "X :";
            this.ActionShiftLabel0.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ActionLabel
            // 
            this.ActionLabel.AutoSize = true;
            this.ActionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ActionLabel.Location = new System.Drawing.Point(4, 4);
            this.ActionLabel.Name = "ActionLabel";
            this.ActionLabel.Size = new System.Drawing.Size(104, 16);
            this.ActionLabel.TabIndex = 0;
            this.ActionLabel.Text = "Shift Selected";
            // 
            // OptionPanel
            // 
            this.OptionPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OptionPanel.Controls.Add(this.OptionFilterPanel);
            this.OptionPanel.Controls.Add(this.OptionVisibilityPanel);
            this.OptionPanel.Controls.Add(this.OptionLabel);
            this.OptionPanel.Location = new System.Drawing.Point(4, 4);
            this.OptionPanel.Name = "OptionPanel";
            this.OptionPanel.Size = new System.Drawing.Size(282, 163);
            this.OptionPanel.TabIndex = 0;
            // 
            // OptionFilterPanel
            // 
            this.OptionFilterPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OptionFilterPanel.Controls.Add(this.OptionFilterRadioButton2);
            this.OptionFilterPanel.Controls.Add(this.OptionFilterRadioButton1);
            this.OptionFilterPanel.Controls.Add(this.OptionFilterRadioButton0);
            this.OptionFilterPanel.Controls.Add(this.OptionFilterLabel);
            this.OptionFilterPanel.Location = new System.Drawing.Point(3, 68);
            this.OptionFilterPanel.Name = "OptionFilterPanel";
            this.OptionFilterPanel.Size = new System.Drawing.Size(276, 95);
            this.OptionFilterPanel.TabIndex = 3;
            // 
            // OptionFilterRadioButton2
            // 
            this.OptionFilterRadioButton2.AutoSize = true;
            this.OptionFilterRadioButton2.Checked = true;
            this.OptionFilterRadioButton2.Location = new System.Drawing.Point(6, 19);
            this.OptionFilterRadioButton2.Name = "OptionFilterRadioButton2";
            this.OptionFilterRadioButton2.Size = new System.Drawing.Size(95, 17);
            this.OptionFilterRadioButton2.TabIndex = 3;
            this.OptionFilterRadioButton2.TabStop = true;
            this.OptionFilterRadioButton2.Text = "Current Project";
            this.OptionFilterRadioButton2.UseVisualStyleBackColor = true;
            this.OptionFilterRadioButton2.CheckedChanged += new System.EventHandler(this.OptionFilterRadioButton2_CheckedChanged);
            // 
            // OptionFilterRadioButton1
            // 
            this.OptionFilterRadioButton1.AutoSize = true;
            this.OptionFilterRadioButton1.Location = new System.Drawing.Point(6, 42);
            this.OptionFilterRadioButton1.Name = "OptionFilterRadioButton1";
            this.OptionFilterRadioButton1.Size = new System.Drawing.Size(85, 17);
            this.OptionFilterRadioButton1.TabIndex = 2;
            this.OptionFilterRadioButton1.Text = "Current View";
            this.OptionFilterRadioButton1.UseVisualStyleBackColor = true;
            this.OptionFilterRadioButton1.CheckedChanged += new System.EventHandler(this.OptionFilterRadioButton1_CheckedChanged);
            // 
            // OptionFilterRadioButton0
            // 
            this.OptionFilterRadioButton0.AutoSize = true;
            this.OptionFilterRadioButton0.Location = new System.Drawing.Point(6, 65);
            this.OptionFilterRadioButton0.Name = "OptionFilterRadioButton0";
            this.OptionFilterRadioButton0.Size = new System.Drawing.Size(106, 17);
            this.OptionFilterRadioButton0.TabIndex = 1;
            this.OptionFilterRadioButton0.Text = "Current Selection";
            this.OptionFilterRadioButton0.UseVisualStyleBackColor = true;
            this.OptionFilterRadioButton0.CheckedChanged += new System.EventHandler(this.OptionFilterRadioButton0_CheckedChanged);
            // 
            // OptionFilterLabel
            // 
            this.OptionFilterLabel.AutoSize = true;
            this.OptionFilterLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OptionFilterLabel.Location = new System.Drawing.Point(3, 0);
            this.OptionFilterLabel.Name = "OptionFilterLabel";
            this.OptionFilterLabel.Size = new System.Drawing.Size(53, 15);
            this.OptionFilterLabel.TabIndex = 0;
            this.OptionFilterLabel.Text = "Filter By:";
            // 
            // OptionVisibilityPanel
            // 
            this.OptionVisibilityPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OptionVisibilityPanel.Controls.Add(this.OptionVisibilityLabel);
            this.OptionVisibilityPanel.Controls.Add(this.OptionVisibilityCheckBox);
            this.OptionVisibilityPanel.Location = new System.Drawing.Point(3, 23);
            this.OptionVisibilityPanel.Name = "OptionVisibilityPanel";
            this.OptionVisibilityPanel.Size = new System.Drawing.Size(276, 39);
            this.OptionVisibilityPanel.TabIndex = 2;
            // 
            // OptionVisibilityLabel
            // 
            this.OptionVisibilityLabel.AutoSize = true;
            this.OptionVisibilityLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OptionVisibilityLabel.Location = new System.Drawing.Point(3, 1);
            this.OptionVisibilityLabel.Name = "OptionVisibilityLabel";
            this.OptionVisibilityLabel.Size = new System.Drawing.Size(53, 15);
            this.OptionVisibilityLabel.TabIndex = 2;
            this.OptionVisibilityLabel.Text = "Visibility:";
            // 
            // OptionVisibilityCheckBox
            // 
            this.OptionVisibilityCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.OptionVisibilityCheckBox.AutoSize = true;
            this.OptionVisibilityCheckBox.Location = new System.Drawing.Point(6, 19);
            this.OptionVisibilityCheckBox.Name = "OptionVisibilityCheckBox";
            this.OptionVisibilityCheckBox.Size = new System.Drawing.Size(103, 17);
            this.OptionVisibilityCheckBox.TabIndex = 1;
            this.OptionVisibilityCheckBox.Text = "Hide unselected";
            this.OptionVisibilityCheckBox.UseVisualStyleBackColor = true;
            this.OptionVisibilityCheckBox.CheckedChanged += new System.EventHandler(this.OptionVisibilityCheckBox_CheckedChanged);
            // 
            // OptionLabel
            // 
            this.OptionLabel.AutoSize = true;
            this.OptionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OptionLabel.Location = new System.Drawing.Point(4, 4);
            this.OptionLabel.Name = "OptionLabel";
            this.OptionLabel.Size = new System.Drawing.Size(61, 16);
            this.OptionLabel.TabIndex = 0;
            this.OptionLabel.Text = "Options";
            // 
            // ModelessForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(728, 411);
            this.Controls.Add(this.SidePanel);
            this.Controls.Add(this.TestLabel);
            this.Controls.Add(this.ElementSelectionPanel);
            this.MinimumSize = new System.Drawing.Size(600, 450);
            this.Name = "ModelessForm";
            this.Text = "ModelessForm";
            this.Load += new System.EventHandler(this.ModelessForm_Load);
            this.ElementSelectionPanel.ResumeLayout(false);
            this.ElementSelectionPanel.PerformLayout();
            this.SidePanel.ResumeLayout(false);
            this.ActionPanel.ResumeLayout(false);
            this.ActionPanel.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ActionShiftPanel2.ResumeLayout(false);
            this.ActionShiftPanel2.PerformLayout();
            this.ActionShiftPanel1.ResumeLayout(false);
            this.ActionShiftPanel1.PerformLayout();
            this.ActionShiftPanel0.ResumeLayout(false);
            this.ActionShiftPanel0.PerformLayout();
            this.OptionPanel.ResumeLayout(false);
            this.OptionPanel.PerformLayout();
            this.OptionFilterPanel.ResumeLayout(false);
            this.OptionFilterPanel.PerformLayout();
            this.OptionVisibilityPanel.ResumeLayout(false);
            this.OptionVisibilityPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Panel ElementSelectionPanel;
        private System.Windows.Forms.Label ElementSelectionLabel;
        private System.Windows.Forms.TreeView ElementSelectionTreeView;
        private System.Windows.Forms.Label TestLabel;
        private System.Windows.Forms.Panel SidePanel;
        private System.Windows.Forms.Panel ActionPanel;
        private System.Windows.Forms.Panel OptionPanel;
        private System.Windows.Forms.Panel ActionShiftPanel0;
        private System.Windows.Forms.Label ActionShiftLabel0;
        private System.Windows.Forms.Label ActionLabel;
        private System.Windows.Forms.CheckBox OptionVisibilityCheckBox;
        private System.Windows.Forms.Label OptionLabel;
        private System.Windows.Forms.Button ActionShiftButton;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton ActionShiftRadioButton0;
        private System.Windows.Forms.RadioButton ActionShiftRadioButton1;
        private System.Windows.Forms.Panel ActionShiftPanel2;
        private System.Windows.Forms.TextBox ActionShiftTextBox2;
        private System.Windows.Forms.Label ActionShiftLabel2;
        private System.Windows.Forms.Panel ActionShiftPanel1;
        private System.Windows.Forms.TextBox ActionShiftTextBox1;
        private System.Windows.Forms.Label ActionShiftLabel1;
        private System.Windows.Forms.TextBox ActionShiftTextBox0;
        private System.Windows.Forms.Panel OptionVisibilityPanel;
        private System.Windows.Forms.Label OptionVisibilityLabel;
        private System.Windows.Forms.Panel OptionFilterPanel;
        private System.Windows.Forms.RadioButton OptionFilterRadioButton2;
        private System.Windows.Forms.RadioButton OptionFilterRadioButton1;
        private System.Windows.Forms.RadioButton OptionFilterRadioButton0;
        private System.Windows.Forms.Label OptionFilterLabel;
    }
}