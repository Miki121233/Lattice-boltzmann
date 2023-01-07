using System;
using System.Windows.Forms;

namespace Lattice_boltzmann
{
    public partial class Form1 : Form
    {
        
        public Form1()
        {
            InitializeComponent();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < Rysowanie.czasteczka.Count; i++)
            {
                Rysowanie.czasteczka[i].ruch();
            }
            
        }

        private void utworzCzasteczkeButton_Click(object sender, EventArgs e)
        {
            Rysowanie.nowaCzasteczka(pictureBox1);
        }

        private void utworz10CzasteczkeButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 10; i++) { Rysowanie.nowaCzasteczka(pictureBox1); }
        }
    }
}
