using System;
using System.Drawing;
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

        private void testButton_Click(object sender, EventArgs e)
        {
            Graphics g = pictureBox1.CreateGraphics();
            Pen pen = new Pen(Color.Red,3);
            Point p1 = new Point(0, 0);
            Point p2 = new Point(582-50, 386-50);
            g.DrawLine(pen ,p1,p2);
        }
    }
}
