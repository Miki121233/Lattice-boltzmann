
namespace Lattice_boltzmann
{
    partial class Form1
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
            pictureBox1 = new System.Windows.Forms.PictureBox();
            this.startButton = new System.Windows.Forms.Button();
            this.utworzCzasteczkeButton = new System.Windows.Forms.Button();
            this.utworz10CzasteczkeButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pictureBox1.Location = new System.Drawing.Point(32, 17);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(572, 386);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(635, 30);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(145, 37);
            this.startButton.TabIndex = 1;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // utworzCzasteczkeButton
            // 
            this.utworzCzasteczkeButton.Location = new System.Drawing.Point(635, 113);
            this.utworzCzasteczkeButton.Name = "utworzCzasteczkeButton";
            this.utworzCzasteczkeButton.Size = new System.Drawing.Size(145, 37);
            this.utworzCzasteczkeButton.TabIndex = 2;
            this.utworzCzasteczkeButton.Text = "Utwórz cząsteczkę";
            this.utworzCzasteczkeButton.UseVisualStyleBackColor = true;
            this.utworzCzasteczkeButton.Click += new System.EventHandler(this.utworzCzasteczkeButton_Click);
            // 
            // utworz10CzasteczkeButton
            // 
            this.utworz10CzasteczkeButton.Location = new System.Drawing.Point(635, 156);
            this.utworz10CzasteczkeButton.Name = "utworz10CzasteczkeButton";
            this.utworz10CzasteczkeButton.Size = new System.Drawing.Size(145, 37);
            this.utworz10CzasteczkeButton.TabIndex = 3;
            this.utworz10CzasteczkeButton.Text = "Utwórz 10 cząsteczkę";
            this.utworz10CzasteczkeButton.UseVisualStyleBackColor = true;
            this.utworz10CzasteczkeButton.Click += new System.EventHandler(this.utworz10CzasteczkeButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.utworz10CzasteczkeButton);
            this.Controls.Add(this.utworzCzasteczkeButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(pictureBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public static System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button utworzCzasteczkeButton;
        private System.Windows.Forms.Button utworz10CzasteczkeButton;
    }
}

