using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Lattice_boltzmann
{
    public class Rysowanie
    {
        public static Graphics g;
        public static Pen pen1 = new System.Drawing.Pen(Color.Blue, 3);
        public List<Czasteczka> czasteczka = new List<Czasteczka>();

        public static void nowaCzasteczka(PictureBox pictureBox1)
        {
            g = pictureBox1.CreateGraphics();
            RectangleF rectangleF = new RectangleF(1,2,5,5);
            czasteczka.Add(new Czasteczka());
            g.DrawEllipse(pen1,rectangleF);
        }

        public static void linia(PictureBox pictureBox1)
        {
            g = pictureBox1.CreateGraphics();
            g.DrawLine(pen1, 0, 0, 100, 100);

        }
    }
}
