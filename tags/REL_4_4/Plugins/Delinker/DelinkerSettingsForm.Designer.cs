namespace AutoWikiBrowser.Plugins.Delinker
{
    partial class SettingsForm
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
            this.OK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.Link = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.Skip = new System.Windows.Forms.CheckBox();
            this.Cancel = new System.Windows.Forms.Button();
            this.Subdomains = new System.Windows.Forms.CheckBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.RemoveSections = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // OK
            // 
            this.OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OK.Location = new System.Drawing.Point(245, 181);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 23);
            this.OK.TabIndex = 0;
            this.OK.Text = "OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.OK_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Link to remove:";
            // 
            // Link
            // 
            this.Link.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Link.Location = new System.Drawing.Point(98, 15);
            this.Link.Name = "Link";
            this.Link.Size = new System.Drawing.Size(302, 22);
            this.Link.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(242, 38);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(158, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Must be properly regex-escaped";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Skip
            // 
            this.Skip.AutoSize = true;
            this.Skip.Location = new System.Drawing.Point(15, 95);
            this.Skip.Name = "Skip";
            this.Skip.Size = new System.Drawing.Size(156, 17);
            this.Skip.TabIndex = 4;
            this.Skip.Text = "Skip if no links are removed";
            this.Skip.UseVisualStyleBackColor = true;
            // 
            // Cancel
            // 
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(326, 181);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 5;
            this.Cancel.Text = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            // 
            // Subdomains
            // 
            this.Subdomains.AutoSize = true;
            this.Subdomains.Location = new System.Drawing.Point(15, 72);
            this.Subdomains.Name = "Subdomains";
            this.Subdomains.Size = new System.Drawing.Size(120, 17);
            this.Subdomains.TabIndex = 6;
            this.Subdomains.Text = "Include subdomains";
            this.Subdomains.UseVisualStyleBackColor = true;
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "Remove all",
            "Remove only "});
            this.comboBox1.Location = new System.Drawing.Point(279, 70);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(121, 21);
            this.comboBox1.TabIndex = 7;
            // 
            // RemoveSections
            // 
            this.RemoveSections.AutoSize = true;
            this.RemoveSections.Location = new System.Drawing.Point(15, 118);
            this.RemoveSections.Name = "RemoveSections";
            this.RemoveSections.Size = new System.Drawing.Size(212, 17);
            this.RemoveSections.TabIndex = 8;
            this.RemoveSections.Text = "Remove emptied external links sections";
            this.RemoveSections.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(412, 216);
            this.Controls.Add(this.RemoveSections);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.Subdomains);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.Skip);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.Link);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.OK);
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Delinker settings";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Label label1;
        internal System.Windows.Forms.TextBox Link;
        private System.Windows.Forms.Label label2;
        internal System.Windows.Forms.CheckBox Skip;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.CheckBox Subdomains;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.CheckBox RemoveSections;
    }
}