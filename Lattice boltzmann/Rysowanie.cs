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
        public static SolidBrush brushBlue = new SolidBrush(Color.Blue);
        //public static Pen penWhite = new System.Drawing.Pen(Color.White, 3);
        public static Pen penDefault = new System.Drawing.Pen(Color.FromArgb(240, 240, 240), 3);
        public static List<Czasteczka> czasteczka = new List<Czasteczka>();
        public static int rozmiarCzasteczki = 3;

        public static void nowaCzasteczka(PictureBox pictureBox1)
        {
            Random random = new Random();
            int kierunek = random.Next(-2, 3);
        ponownaRandomizacja:
            int x = random.Next(0, pictureBox1.Size.Width - rozmiarCzasteczki);
            int y = random.Next(0, pictureBox1.Size.Height - rozmiarCzasteczki);
            for (int i = 0; i < czasteczka.Count; i++)
            {
                if ((x < czasteczka[i].Lokalizacja.X - rozmiarCzasteczki || x > czasteczka[i].Lokalizacja.X + rozmiarCzasteczki) || 
                    (y < czasteczka[i].Lokalizacja.Y - rozmiarCzasteczki || y > czasteczka[i].Lokalizacja.Y + rozmiarCzasteczki))
                {}
                else goto ponownaRandomizacja;
            }
            g = pictureBox1.CreateGraphics();
            RectangleF rectangleF = new RectangleF(x,y, rozmiarCzasteczki, rozmiarCzasteczki);
            czasteczka.Add(new Czasteczka(x,y, kierunek));
            
            g.DrawEllipse(penBlue,rectangleF);
            //g.FillRectangle(brushBlue, rectangleF);
        }

        public static void rysujCzasteczke(Czasteczka czasteczka, RectangleF rectangleF, PictureBox pictureBox1)
        {
            g = pictureBox1.CreateGraphics();
            //RectangleF rectangleF = new RectangleF(czasteczka.Lokalizacja.X, czasteczka.Lokalizacja.Y, 5, 5); 
            Color color = Color.Red;

            if (czasteczka.Szybkosc < 4)  //warunek od stezenia do koloru
            {
                color = Color.FromArgb(224, 223, 255);
            }
            else if (czasteczka.Szybkosc < 7 && czasteczka.Szybkosc >= 4)
            {
                color = Color.FromArgb(129, 125, 242);
            }
            else if (czasteczka.Szybkosc < 10 && czasteczka.Szybkosc >= 7)
            {
                color = Color.FromArgb(33, 26, 225);
            }
            else if (czasteczka.Szybkosc == 10) 
            {
                color = Color.FromArgb(6, 0, 169);
            }
            else if (czasteczka.Szybkosc > 10)
            {
                color = Color.FromArgb(4, 0, 120);
            }
                Pen penStezenie = new Pen(color, 3);

            g.DrawEllipse(penStezenie, rectangleF);
            //g.DrawEllipse(penBlue, rectangleF);
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
