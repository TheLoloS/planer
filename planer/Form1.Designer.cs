namespace planer
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblRemaining;
        private System.Windows.Forms.RichTextBox txtSchedule;
        private System.Windows.Forms.Button btnPlayEndOfWork;
        private System.Windows.Forms.Button btnPlayWorkEndingSoon;
        private System.Windows.Forms.Button btnPlayStartBreak;
        private System.Windows.Forms.Button btnPlayBreakEndingSoon;
        private System.Windows.Forms.Button btnPlayStartWork;

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
        /// Initializes UI controls and layout for the main form.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 320);
            this.Text = "Harmonogram";

            // lblStatus
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 20);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(46, 17);
            this.lblStatus.Text = "Status: -";
            this.Controls.Add(this.lblStatus);

            // lblRemaining
            this.lblRemaining = new System.Windows.Forms.Label();
            this.lblRemaining.AutoSize = true;
            this.lblRemaining.Location = new System.Drawing.Point(12, 50);
            this.lblRemaining.Name = "lblRemaining";
            this.lblRemaining.Size = new System.Drawing.Size(80, 17);
            this.lblRemaining.Text = "Pozostało: -";
            this.Controls.Add(this.lblRemaining);

            // txtSchedule - multiline schedule display (RichTextBox for highlighting)
            this.txtSchedule = new System.Windows.Forms.RichTextBox();
            this.txtSchedule.Location = new System.Drawing.Point(12, 66);
            this.txtSchedule.Name = "txtSchedule";
            this.txtSchedule.Size = new System.Drawing.Size(476, 160);
            this.txtSchedule.ReadOnly = true;
            this.txtSchedule.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtSchedule.Font = new System.Drawing.Font("Consolas", 10);
            this.txtSchedule.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.txtSchedule);

            int btnTop = 240;
            int btnLeft = 12;
            int btnWidth = 220;
            int btnHeight = 28;

            // btnPlayEndOfWork
            this.btnPlayEndOfWork = new System.Windows.Forms.Button();
            this.btnPlayEndOfWork.Location = new System.Drawing.Point(btnLeft, btnTop);
            this.btnPlayEndOfWork.Size = new System.Drawing.Size(btnWidth, btnHeight);
            this.btnPlayEndOfWork.Text = "Play EndOfWork";
            this.btnPlayEndOfWork.Name = "btnPlayEndOfWork";
            this.btnPlayEndOfWork.Visible = false;
            this.btnPlayEndOfWork.Click += new System.EventHandler(this.btnPlayEndOfWork_Click);
            this.Controls.Add(this.btnPlayEndOfWork);

            // btnPlayWorkEndingSoon
            this.btnPlayWorkEndingSoon = new System.Windows.Forms.Button();
            this.btnPlayWorkEndingSoon.Location = new System.Drawing.Point(btnLeft + btnWidth + 10, btnTop);
            this.btnPlayWorkEndingSoon.Size = new System.Drawing.Size(btnWidth, btnHeight);
            this.btnPlayWorkEndingSoon.Text = "Play WorkEndingSoon";
            this.btnPlayWorkEndingSoon.Name = "btnPlayWorkEndingSoon";
            this.btnPlayWorkEndingSoon.Visible = false;
            this.btnPlayWorkEndingSoon.Click += new System.EventHandler(this.btnPlayWorkEndingSoon_Click);
            this.Controls.Add(this.btnPlayWorkEndingSoon);

            btnTop += btnHeight + 8;

            // btnPlayStartBreak
            this.btnPlayStartBreak = new System.Windows.Forms.Button();
            this.btnPlayStartBreak.Location = new System.Drawing.Point(btnLeft, btnTop);
            this.btnPlayStartBreak.Size = new System.Drawing.Size(btnWidth, btnHeight);
            this.btnPlayStartBreak.Text = "Play StartBreak";
            this.btnPlayStartBreak.Name = "btnPlayStartBreak";
            this.btnPlayStartBreak.Visible = false;
            this.btnPlayStartBreak.Click += new System.EventHandler(this.btnPlayStartBreak_Click);
            this.Controls.Add(this.btnPlayStartBreak);

            // btnPlayBreakEndingSoon
            this.btnPlayBreakEndingSoon = new System.Windows.Forms.Button();
            this.btnPlayBreakEndingSoon.Location = new System.Drawing.Point(btnLeft + btnWidth + 10, btnTop);
            this.btnPlayBreakEndingSoon.Size = new System.Drawing.Size(btnWidth, btnHeight);
            this.btnPlayBreakEndingSoon.Text = "Play BreakEndingSoon";
            this.btnPlayBreakEndingSoon.Name = "btnPlayBreakEndingSoon";
            this.btnPlayBreakEndingSoon.Visible = false;
            this.btnPlayBreakEndingSoon.Click += new System.EventHandler(this.btnPlayBreakEndingSoon_Click);
            this.Controls.Add(this.btnPlayBreakEndingSoon);

            btnTop += btnHeight + 8;

            // btnPlayStartWork
            this.btnPlayStartWork = new System.Windows.Forms.Button();
            this.btnPlayStartWork.Location = new System.Drawing.Point(btnLeft, btnTop);
            this.btnPlayStartWork.Size = new System.Drawing.Size(btnWidth, btnHeight);
            this.btnPlayStartWork.Text = "Play StartWork";
            this.btnPlayStartWork.Name = "btnPlayStartWork";
            this.btnPlayStartWork.Visible = false;
            this.btnPlayStartWork.Click += new System.EventHandler(this.btnPlayStartWork_Click);
            this.Controls.Add(this.btnPlayStartWork);
        }

        #endregion
    }
}
