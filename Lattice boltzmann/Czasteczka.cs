using System.Drawing;
using System.Windows.Forms;

namespace Lattice_boltzmann
{
    public class Czasteczka
    {
        
        public Czasteczka(int x, int y, int kierunek)
        {
            Lokalizacja = new Point(x,y);
            Kierunek = kierunek;
            Szybkosc = 10;
        }
        public int Szybkosc { get; set; }
        public int Kierunek { get; set; } // -1 -lewo, -2 -gora,  1 -prawo, 2 -dol
        public Point Lokalizacja { get; set; }
        
        public void ruch()
        {

            switch (Kierunek)
            {
                case -1: // lewo
                    {
                        if (warunkiRuchu(Lokalizacja.X - Szybkosc, Lokalizacja.Y)==true)
                        { 
                            Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                            RectangleF rectangleF = new RectangleF(Lokalizacja.X - Szybkosc, Lokalizacja.Y, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                            Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);
                            Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                        }
                    }
                    break;
                case -2: // gora
                    {
                        if (warunkiRuchu(Lokalizacja.X, Lokalizacja.Y + Szybkosc) == true)
                        {
                            Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                            RectangleF rectangleF = new RectangleF(Lokalizacja.X, Lokalizacja.Y + Szybkosc, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                            Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);
                            Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                        }
                    }
                    break;
                case 1: // prawo
                    {
                        if (warunkiRuchu(Lokalizacja.X + Szybkosc, Lokalizacja.Y) == true)
                        {
                            Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                            RectangleF rectangleF = new RectangleF(Lokalizacja.X + Szybkosc, Lokalizacja.Y, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                            Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);
                            Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                        }
                    }
                    break;
                case 2: //dol
                    {
                        if (warunkiRuchu(Lokalizacja.X, Lokalizacja.Y - Szybkosc) == true)
                        {
                            Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                            RectangleF rectangleF = new RectangleF(Lokalizacja.X, Lokalizacja.Y - Szybkosc, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                            Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);
                            Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        public bool warunkiRuchu(int x, int y)
        {
            var picturebox = Form1.pictureBox1;
            if (x >= picturebox.Width - Rysowanie.rozmiarCzasteczki || y >= picturebox.Height - Rysowanie.rozmiarCzasteczki || x < 0 || y < 0) 
            {
                return false;
            }
            //Graphics g = Form1.pictureBox1.CreateGraphics();
           
            Bitmap b = new Bitmap(picturebox.ClientSize.Width, picturebox.Height);
            picturebox.DrawToBitmap(b, picturebox.ClientRectangle);
           
            
            for (int i = 0; i < Szybkosc; i++)
            {
                Color colour = b.GetPixel(x, y);
                if (colour == Color.Blue)
                {
                    Kierunek = -Kierunek;
                    ruch();
                    return false;
                }
                else
                {
                    return true;
                }
            }




            b.Dispose();
            return true;
        }

    }
}
