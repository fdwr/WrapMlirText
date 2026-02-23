namespace WrapMlirText
{
    partial class formMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(formMain));
            this.buttonWrap = new System.Windows.Forms.Button();
            this.textBoxOutput = new System.Windows.Forms.TextBox();
            this.textBoxInput = new System.Windows.Forms.TextBox();
            this.labelTokens = new System.Windows.Forms.Label();
            this.labelUnwrapped = new System.Windows.Forms.Label();
            this.labelWrapped = new System.Windows.Forms.Label();
            this.textBoxTokens = new System.Windows.Forms.TextBox();
            this.labelInputText = new System.Windows.Forms.Label();
            this.textBoxRuler = new System.Windows.Forms.TextBox();
            this.checkBoxWrap = new System.Windows.Forms.CheckBox();
            this.labelBreakFlags = new System.Windows.Forms.Label();
            this.textBoxBreakFlags = new System.Windows.Forms.TextBox();
            this.labelLineRanges = new System.Windows.Forms.Label();
            this.textBoxLineRanges = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // buttonWrap
            // 
            this.buttonWrap.Location = new System.Drawing.Point(8, 624);
            this.buttonWrap.Name = "buttonWrap";
            this.buttonWrap.Size = new System.Drawing.Size(104, 24);
            this.buttonWrap.TabIndex = 10;
            this.buttonWrap.Text = "&Wrap";
            this.buttonWrap.UseVisualStyleBackColor = true;
            this.buttonWrap.Click += new System.EventHandler(this.buttonWrap_Click);
            // 
            // textBoxOutput
            // 
            this.textBoxOutput.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxOutput.HideSelection = false;
            this.textBoxOutput.Location = new System.Drawing.Point(8, 448);
            this.textBoxOutput.Multiline = true;
            this.textBoxOutput.Name = "textBoxOutput";
            this.textBoxOutput.ReadOnly = true;
            this.textBoxOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxOutput.Size = new System.Drawing.Size(1000, 168);
            this.textBoxOutput.TabIndex = 9;
            this.textBoxOutput.Text = "(click the Wrap button)";
            this.textBoxOutput.WordWrap = false;
            // 
            // textBoxInput
            // 
            this.textBoxInput.AcceptsReturn = true;
            this.textBoxInput.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxInput.HideSelection = false;
            this.textBoxInput.Location = new System.Drawing.Point(8, 24);
            this.textBoxInput.Multiline = true;
            this.textBoxInput.Name = "textBoxInput";
            this.textBoxInput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxInput.Size = new System.Drawing.Size(1000, 168);
            this.textBoxInput.TabIndex = 0;
            this.textBoxInput.Text = resources.GetString("textBoxInput.Text");
            // 
            // labelTokens
            // 
            this.labelTokens.AutoSize = true;
            this.labelTokens.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTokens.Location = new System.Drawing.Point(8, 200);
            this.labelTokens.Name = "labelTokens";
            this.labelTokens.Size = new System.Drawing.Size(53, 13);
            this.labelTokens.TabIndex = 1;
            this.labelTokens.Text = "Tokens:";
            // 
            // labelUnwrapped
            // 
            this.labelUnwrapped.AutoSize = true;
            this.labelUnwrapped.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelUnwrapped.Location = new System.Drawing.Point(8, 8);
            this.labelUnwrapped.Name = "labelUnwrapped";
            this.labelUnwrapped.Size = new System.Drawing.Size(75, 13);
            this.labelUnwrapped.TabIndex = 3;
            this.labelUnwrapped.Text = "Unwrapped:";
            // 
            // labelWrapped
            // 
            this.labelWrapped.AutoSize = true;
            this.labelWrapped.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelWrapped.Location = new System.Drawing.Point(8, 432);
            this.labelWrapped.Name = "labelWrapped";
            this.labelWrapped.Size = new System.Drawing.Size(62, 13);
            this.labelWrapped.TabIndex = 8;
            this.labelWrapped.Text = "Wrapped:";
            // 
            // textBoxTokens
            // 
            this.textBoxTokens.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxTokens.HideSelection = false;
            this.textBoxTokens.Location = new System.Drawing.Point(8, 216);
            this.textBoxTokens.Multiline = true;
            this.textBoxTokens.Name = "textBoxTokens";
            this.textBoxTokens.ReadOnly = true;
            this.textBoxTokens.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxTokens.Size = new System.Drawing.Size(368, 168);
            this.textBoxTokens.TabIndex = 2;
            this.textBoxTokens.Text = "(click the Wrap button)";
            this.textBoxTokens.WordWrap = false;
            // 
            // labelInputText
            // 
            this.labelInputText.AutoSize = true;
            this.labelInputText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelInputText.Location = new System.Drawing.Point(8, 8);
            this.labelInputText.Name = "labelInputText";
            this.labelInputText.Size = new System.Drawing.Size(65, 13);
            this.labelInputText.TabIndex = 11;
            this.labelInputText.Text = "Input text:";
            // 
            // textBoxRuler
            // 
            this.textBoxRuler.BackColor = System.Drawing.SystemColors.Control;
            this.textBoxRuler.Enabled = false;
            this.textBoxRuler.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxRuler.Location = new System.Drawing.Point(8, 392);
            this.textBoxRuler.Margin = new System.Windows.Forms.Padding(0);
            this.textBoxRuler.Multiline = true;
            this.textBoxRuler.Name = "textBoxRuler";
            this.textBoxRuler.ReadOnly = true;
            this.textBoxRuler.Size = new System.Drawing.Size(1000, 32);
            this.textBoxRuler.TabIndex = 7;
            this.textBoxRuler.TabStop = false;
            this.textBoxRuler.Text = resources.GetString("textBoxRuler.Text");
            this.textBoxRuler.WordWrap = false;
            // 
            // checkBoxWrap
            // 
            this.checkBoxWrap.Checked = true;
            this.checkBoxWrap.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxWrap.Location = new System.Drawing.Point(80, 8);
            this.checkBoxWrap.Name = "checkBoxWrap";
            this.checkBoxWrap.Size = new System.Drawing.Size(52, 16);
            this.checkBoxWrap.TabIndex = 12;
            this.checkBoxWrap.Text = "Wrap";
            this.checkBoxWrap.UseVisualStyleBackColor = true;
            this.checkBoxWrap.CheckedChanged += new System.EventHandler(this.checkBoxWrap_CheckedChanged);
            // 
            // labelBreakFlags
            // 
            this.labelBreakFlags.AutoSize = true;
            this.labelBreakFlags.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelBreakFlags.Location = new System.Drawing.Point(384, 200);
            this.labelBreakFlags.Name = "labelBreakFlags";
            this.labelBreakFlags.Size = new System.Drawing.Size(75, 13);
            this.labelBreakFlags.TabIndex = 3;
            this.labelBreakFlags.Text = "Break flags:";
            // 
            // textBoxBreakFlags
            // 
            this.textBoxBreakFlags.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxBreakFlags.HideSelection = false;
            this.textBoxBreakFlags.Location = new System.Drawing.Point(384, 216);
            this.textBoxBreakFlags.Multiline = true;
            this.textBoxBreakFlags.Name = "textBoxBreakFlags";
            this.textBoxBreakFlags.ReadOnly = true;
            this.textBoxBreakFlags.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxBreakFlags.Size = new System.Drawing.Size(296, 168);
            this.textBoxBreakFlags.TabIndex = 4;
            this.textBoxBreakFlags.Text = "(click the Wrap button)";
            this.textBoxBreakFlags.WordWrap = false;
            // 
            // labelLineRanges
            // 
            this.labelLineRanges.AutoSize = true;
            this.labelLineRanges.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLineRanges.Location = new System.Drawing.Point(688, 200);
            this.labelLineRanges.Name = "labelLineRanges";
            this.labelLineRanges.Size = new System.Drawing.Size(77, 13);
            this.labelLineRanges.TabIndex = 5;
            this.labelLineRanges.Text = "Line ranges:";
            // 
            // textBoxLineRanges
            // 
            this.textBoxLineRanges.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxLineRanges.HideSelection = false;
            this.textBoxLineRanges.Location = new System.Drawing.Point(688, 216);
            this.textBoxLineRanges.Multiline = true;
            this.textBoxLineRanges.Name = "textBoxLineRanges";
            this.textBoxLineRanges.ReadOnly = true;
            this.textBoxLineRanges.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxLineRanges.Size = new System.Drawing.Size(320, 168);
            this.textBoxLineRanges.TabIndex = 6;
            this.textBoxLineRanges.Text = "(click the Wrap button)";
            this.textBoxLineRanges.WordWrap = false;
            // 
            // formMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ClientSize = new System.Drawing.Size(1016, 658);
            this.Controls.Add(this.textBoxLineRanges);
            this.Controls.Add(this.labelLineRanges);
            this.Controls.Add(this.textBoxBreakFlags);
            this.Controls.Add(this.labelBreakFlags);
            this.Controls.Add(this.checkBoxWrap);
            this.Controls.Add(this.textBoxRuler);
            this.Controls.Add(this.labelInputText);
            this.Controls.Add(this.textBoxTokens);
            this.Controls.Add(this.labelWrapped);
            this.Controls.Add(this.labelTokens);
            this.Controls.Add(this.buttonWrap);
            this.Controls.Add(this.textBoxOutput);
            this.Controls.Add(this.textBoxInput);
            this.Name = "formMain";
            this.Text = "MLIR Wrap Text";
            this.Load += new System.EventHandler(this.formMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonWrap;
        private System.Windows.Forms.TextBox textBoxOutput;
        private System.Windows.Forms.TextBox textBoxInput;
        private System.Windows.Forms.Label labelUnwrapped;
        private System.Windows.Forms.Label labelTokens;
        private System.Windows.Forms.Label labelWrapped;
        private System.Windows.Forms.TextBox textBoxTokens;
        private System.Windows.Forms.Label labelInputText;
        private System.Windows.Forms.TextBox textBoxRuler;
        private System.Windows.Forms.CheckBox checkBoxWrap;
        private System.Windows.Forms.Label labelBreakFlags;
        private System.Windows.Forms.TextBox textBoxBreakFlags;
        private System.Windows.Forms.Label labelLineRanges;
        private System.Windows.Forms.TextBox textBoxLineRanges;
    }
}

