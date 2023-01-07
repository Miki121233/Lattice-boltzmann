using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Lattice_boltzmann
{
    public class Rysowanie
    {
        public static Graphics g;
        public static Pen penBlue = new System.Drawing.Pen(Color.Blue, 3);
        //public static Pen penWhite = new System.Drawing.Pen(Color.White, 3);
        public static Pen penDefault = new System.Drawing.Pen(Color.FromArgb(240, 240, 240), 3);
        public static List<Czasteczka> czasteczka = new List<Czasteczka>();
        public static int rozmiarCzasteczki = 5;

        public static void nowaCzasteczka(PictureBox pictureBox1)
        {
            Random random = new Random();
            int x = random.Next(pictureBox1.Location.X, pictureBox1.Size.Width);
            int y = random.Next(pictureBox1.Location.Y, pictureBox1.Size.Height);
            int kierunek = random.Next(1, 5);
            g = pictureBox1.CreateGraphics();
            RectangleF rectangleF = new RectangleF(x,y, rozmiarCzasteczki, rozmiarCzasteczki);
            czasteczka.Add(new Czasteczka(x,y, kierunek));
            g.DrawEllipse(penBlue,rectangleF);
        }

        public static void rysujCzasteczke(Czasteczka czasteczka, RectangleF rectangleF, PictureBox pictureBox1)
        {
            g = pictureBox1.CreateGraphics();
            //RectangleF rectangleF = new RectangleF(czasteczka.Lokalizacja.X, czasteczka.Lokalizacja.Y, 5, 5); 
            g.DrawEllipse(penBlue, rectangleF);
        }

        public static void wyczyscCzasteczke(Czasteczka czasteczka, PictureBox pictureBox1)
        {
            g = pictureBox1.CreateGraphics();
            RectangleF rectangleF = new RectangleF(czasteczka.Lokalizacja.X, czasteczka.Lokalizacja.Y, rozmiarCzasteczki, rozmiarCzasteczki);
            g.DrawEllipse(penDefault, rectangleF);
        }

        public static void linia(PictureBox pictureBox1)
        {
            g = pictureBox1.CreateGraphics();
            g.DrawLine(penBlue, 0, 0, 100, 100);

        }
    }
}
