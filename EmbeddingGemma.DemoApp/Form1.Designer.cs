namespace EmbeddingGemma.DemoApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SearchBox = new TextBox();
            SearchButton = new Button();
            ResultGridView = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)ResultGridView).BeginInit();
            SuspendLayout();
            // 
            // SearchBox
            // 
            SearchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            SearchBox.Font = new Font("Segoe UI", 12F);
            SearchBox.Location = new Point(12, 16);
            SearchBox.Name = "SearchBox";
            SearchBox.Size = new Size(695, 29);
            SearchBox.TabIndex = 1;
            // 
            // SearchButton
            // 
            SearchButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            SearchButton.Font = new Font("Segoe UI", 10F);
            SearchButton.Location = new Point(713, 16);
            SearchButton.Name = "SearchButton";
            SearchButton.Size = new Size(75, 29);
            SearchButton.TabIndex = 2;
            SearchButton.Text = "Search";
            SearchButton.UseVisualStyleBackColor = true;
            SearchButton.Click += SearchButton_Click;
            // 
            // ResultGridView
            // 
            ResultGridView.AllowUserToAddRows = false;
            ResultGridView.AllowUserToDeleteRows = false;
            ResultGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ResultGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllHeaders;
            ResultGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            ResultGridView.Dock = DockStyle.Bottom;
            ResultGridView.Location = new Point(0, 51);
            ResultGridView.Name = "ResultGridView";
            ResultGridView.ReadOnly = true;
            ResultGridView.Size = new Size(800, 399);
            ResultGridView.TabIndex = 3;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(ResultGridView);
            Controls.Add(SearchButton);
            Controls.Add(SearchBox);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "EmbeddingGemma Test";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)ResultGridView).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView ResultGridView;
        private TextBox SearchBox;
        private Button SearchButton;
    }
}
