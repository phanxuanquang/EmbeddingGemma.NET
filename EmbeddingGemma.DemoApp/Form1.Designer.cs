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
            ExecutionTimeLabel = new Label();
            BrowserCombobox = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)ResultGridView).BeginInit();
            SuspendLayout();
            // 
            // SearchBox
            // 
            SearchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            SearchBox.Font = new Font("Segoe UI", 12F);
            SearchBox.Location = new Point(174, 27);
            SearchBox.Margin = new Padding(4, 5, 4, 5);
            SearchBox.Name = "SearchBox";
            SearchBox.PlaceholderText = "Enter your query";
            SearchBox.Size = new Size(843, 39);
            SearchBox.TabIndex = 1;
            // 
            // SearchButton
            // 
            SearchButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            SearchButton.Font = new Font("Segoe UI", 10F);
            SearchButton.Location = new Point(1023, 27);
            SearchButton.Margin = new Padding(4, 5, 4, 5);
            SearchButton.Name = "SearchButton";
            SearchButton.Size = new Size(107, 39);
            SearchButton.TabIndex = 2;
            SearchButton.Text = "Search";
            SearchButton.UseVisualStyleBackColor = true;
            SearchButton.Click += SearchButton_Click;
            // 
            // ResultGridView
            // 
            ResultGridView.AllowUserToAddRows = false;
            ResultGridView.AllowUserToDeleteRows = false;
            ResultGridView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ResultGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ResultGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            ResultGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            ResultGridView.Location = new Point(17, 76);
            ResultGridView.Margin = new Padding(4, 5, 4, 5);
            ResultGridView.Name = "ResultGridView";
            ResultGridView.ReadOnly = true;
            ResultGridView.RowHeadersWidth = 62;
            ResultGridView.Size = new Size(1113, 634);
            ResultGridView.TabIndex = 3;
            // 
            // ExecutionTimeLabel
            // 
            ExecutionTimeLabel.AutoSize = true;
            ExecutionTimeLabel.Dock = DockStyle.Bottom;
            ExecutionTimeLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            ExecutionTimeLabel.ForeColor = Color.DarkGreen;
            ExecutionTimeLabel.Location = new Point(0, 715);
            ExecutionTimeLabel.Name = "ExecutionTimeLabel";
            ExecutionTimeLabel.Padding = new Padding(5);
            ExecutionTimeLabel.Size = new Size(171, 35);
            ExecutionTimeLabel.TabIndex = 4;
            ExecutionTimeLabel.Text = "Execution Time: 0";
            ExecutionTimeLabel.TextAlign = ContentAlignment.BottomRight;
            // 
            // BrowserCombobox
            // 
            BrowserCombobox.DropDownStyle = ComboBoxStyle.DropDownList;
            BrowserCombobox.Font = new Font("Segoe UI", 12F);
            BrowserCombobox.FormattingEnabled = true;
            BrowserCombobox.Location = new Point(17, 27);
            BrowserCombobox.Name = "BrowserCombobox";
            BrowserCombobox.Size = new Size(150, 40);
            BrowserCombobox.Sorted = true;
            BrowserCombobox.TabIndex = 5;
            BrowserCombobox.SelectedIndexChanged += BrowserCombobox_SelectedIndexChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1143, 750);
            Controls.Add(BrowserCombobox);
            Controls.Add(ExecutionTimeLabel);
            Controls.Add(ResultGridView);
            Controls.Add(SearchButton);
            Controls.Add(SearchBox);
            Margin = new Padding(4, 5, 4, 5);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "EmbeddingGemma Test";
            Load += Form_Load;
            ((System.ComponentModel.ISupportInitialize)ResultGridView).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView ResultGridView;
        private TextBox SearchBox;
        private Button SearchButton;
        private Label ExecutionTimeLabel;
        private ComboBox BrowserCombobox;
    }
}
