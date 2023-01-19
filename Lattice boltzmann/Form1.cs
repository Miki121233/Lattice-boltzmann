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
            Graphics g = pictureBox1.CreateGraphics();
            //g.DrawLine(new Pen(Color.Black), pictureBox1.Width / 3, 0, pictureBox1.Width / 3, pictureBox1.Height);
            g.DrawLine(new Pen(Color.Black), pictureBox1.Width / 3, 0, pictureBox1.Width / 3, pictureBox1.Height/2-40);
            g.DrawLine(new Pen(Color.Black), pictureBox1.Width / 3, pictureBox1.Height / 2 + 30, pictureBox1.Width / 3, pictureBox1.Height);
            //obszar po lewej x1 = 0, x2 = width/3, y1 = height
            //obszar niedostepny 

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
            /*Graphics g = pictureBox1.CreateGraphics();
            Pen pen = new Pen(Color.Red,3);
            Point p1 = new Point(0, 0);
            Point p2 = new Point(582-50, 386-50);
            g.DrawLine(pen ,p1,p2);*/

            
            int kierunek = 1;
            int x1 = 10;
            int y1 = 10;

            int x2 = 30;
            int y2 = 10;
            Graphics g = pictureBox1.CreateGraphics();
            RectangleF rectangleF = new RectangleF(x1, y1, 3, 3);
            Rysowanie.czasteczka.Add(new Czasteczka(x1, y1, kierunek));

            g.DrawEllipse(new Pen(Color.Black), rectangleF);

            rectangleF = new RectangleF(x2, y2, 3, 3);
            Rysowanie.czasteczka.Add(new Czasteczka(x2, y2, -kierunek));

            g.DrawEllipse(new Pen(Color.Black), rectangleF);
        }

        private void deleteAllButton_Click(object sender, EventArgs e)
        {
            powtorka:
            for (int i = 0; i < Rysowanie.czasteczka.Count; i++)
            {
                Rysowanie.czasteczka.Remove(Rysowanie.czasteczka[i]);
            }
            if (Rysowanie.czasteczka.Count != 0)
                goto powtorka;
        }
    }
}
