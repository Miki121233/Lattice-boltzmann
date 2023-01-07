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

        }

        private void utworzCzasteczkeButton_Click(object sender, EventArgs e)
        {
            Rysowanie.nowaCzasteczka(pictureBox1);
        }
    }
}
