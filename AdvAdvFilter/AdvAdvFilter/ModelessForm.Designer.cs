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
            this.ElementSelectionPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // ElementSelectionPanel
            // 
            this.ElementSelectionPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.ElementSelectionPanel.Controls.Add(this.ElementSelectionTreeView);
            this.ElementSelectionPanel.Controls.Add(this.ElementSelectionLabel);
            this.ElementSelectionPanel.Location = new System.Drawing.Point(12, 12);
            this.ElementSelectionPanel.MinimumSize = new System.Drawing.Size(300, 250);
            this.ElementSelectionPanel.Name = "ElementSelectionPanel";
            this.ElementSelectionPanel.Size = new System.Drawing.Size(300, 442);
            this.ElementSelectionPanel.TabIndex = 6;
            // 
            // ElementSelectionTreeView
            // 
            this.ElementSelectionTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ElementSelectionTreeView.CheckBoxes = true;
            this.ElementSelectionTreeView.Location = new System.Drawing.Point(0, 27);
            this.ElementSelectionTreeView.Name = "ElementSelectionTreeView";
            this.ElementSelectionTreeView.Size = new System.Drawing.Size(300, 415);
            this.ElementSelectionTreeView.TabIndex = 1;
            this.ElementSelectionTreeView.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.ElementSelectionTreeView_AfterExpand);
            this.ElementSelectionTreeView.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.ElementSelectionTreeView_AfterCollapse);
            this.ElementSelectionTreeView.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.ElementSelectionTreeView_AfterCheck);
            this.ElementSelectionTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ElementSelectionTreeView_AfterSelect);
            // 
            // ElementSelectionLabel
            // 
            this.ElementSelectionLabel.AutoSize = true;
            this.ElementSelectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ElementSelectionLabel.Location = new System.Drawing.Point(0, 0);
            this.ElementSelectionLabel.Name = "ElementSelectionLabel";
            this.ElementSelectionLabel.Size = new System.Drawing.Size(97, 24);
            this.ElementSelectionLabel.TabIndex = 0;
            this.ElementSelectionLabel.Text = "Elements";
            // 
            // TestLabel
            // 
            this.TestLabel.AutoSize = true;
            this.TestLabel.Location = new System.Drawing.Point(360, 39);
            this.TestLabel.Name = "TestLabel";
            this.TestLabel.Size = new System.Drawing.Size(35, 13);
            this.TestLabel.TabIndex = 7;
            this.TestLabel.Text = "label1";
            // 
            // ModelessForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(830, 466);
            this.Controls.Add(this.TestLabel);
            this.Controls.Add(this.ElementSelectionPanel);
            this.MinimumSize = new System.Drawing.Size(500, 350);
            this.Name = "ModelessForm";
            this.Text = "ModelessForm";
            this.Load += new System.EventHandler(this.ModelessForm_Load);
            this.ElementSelectionPanel.ResumeLayout(false);
            this.ElementSelectionPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Panel ElementSelectionPanel;
        private System.Windows.Forms.Label ElementSelectionLabel;
        private System.Windows.Forms.TreeView ElementSelectionTreeView;
        private System.Windows.Forms.Label TestLabel;
    }
}