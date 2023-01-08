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
            if (warunkiRuchu() == true)
            {
                switch (Kierunek)
                {
                    case -1: // lewo
                        {
                            
                            Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                            RectangleF rectangleF = new RectangleF(Lokalizacja.X - Szybkosc, Lokalizacja.Y, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                            Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);
                            Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                            
                        }
                        break;
                    case -2: // gora
                        {
                            
                            Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                            RectangleF rectangleF = new RectangleF(Lokalizacja.X, Lokalizacja.Y + Szybkosc, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                            Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);
                            Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                            
                        }
                        break;
                    case 1: // prawo
                        {
                           
                            Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                            RectangleF rectangleF = new RectangleF(Lokalizacja.X + Szybkosc, Lokalizacja.Y, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                            Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);
                            Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                            
                        }
                        break;
                    case 2: //dol
                        {
                            
                            Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                            RectangleF rectangleF = new RectangleF(Lokalizacja.X, Lokalizacja.Y - Szybkosc, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                            Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);
                            Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                            
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        public bool warunkiRuchu()
        {
            var picturebox = Form1.pictureBox1;
            /*if (Lokalizacja.X >= picturebox.Width - Rysowanie.rozmiarCzasteczki || Lokalizacja.Y >= picturebox.Height - Rysowanie.rozmiarCzasteczki || Lokalizacja.X < 0 || Lokalizacja.Y < 0 ||
                Lokalizacja.X + Szybkosc >= picturebox.Width - Rysowanie.rozmiarCzasteczki || Lokalizacja.Y + Szybkosc >= picturebox.Height - Rysowanie.rozmiarCzasteczki  ||
                 Lokalizacja.X - Szybkosc >= picturebox.Width - Rysowanie.rozmiarCzasteczki || Lokalizacja.Y - Szybkosc >= picturebox.Height - Rysowanie.rozmiarCzasteczki ||
                 Lokalizacja.X - Szybkosc < 0 || Lokalizacja.Y - Szybkosc < 0)
            {
                Kierunek = -Kierunek;
                return false;
            }*/
            //Graphics g = Form1.pictureBox1.CreateGraphics();
           
            Bitmap b = new Bitmap(picturebox.ClientSize.Width, picturebox.Height);
            picturebox.DrawToBitmap(b, picturebox.ClientRectangle);
            Color colour = new Color();
            int x=-1;
            int y=-1;
            for (int i = 0; i < Szybkosc; i++)
            {
                switch (Kierunek)
                {
                    case 1: // lewo
                        {
                            x = Lokalizacja.X - i;
                            y = Lokalizacja.Y;
                            
                        }
                        break;
                    case 2: // gora
                        {
                            x = Lokalizacja.X;
                            y = Lokalizacja.Y - i;
                            
                        }
                        break;
                    case -1: // prawo
                        {
                            x = Lokalizacja.X + i;
                            y = Lokalizacja.Y;
                            

                        }
                        break;
                    case -2: // dol
                        {
                            x = Lokalizacja.X;
                            y = Lokalizacja.Y + i;
                            
                        }
                        break;
                }

                if (x > picturebox.Width - Rysowanie.rozmiarCzasteczki || y > picturebox.Height - Rysowanie.rozmiarCzasteczki || x <= 0 || y <= 0)
                {
                    Kierunek = -Kierunek;
                    return true;
                }
                else colour = b.GetPixel(x, y);
                if (colour == Color.Blue)
                {
                    
                    Kierunek = -Kierunek;
                    ruch();
                    return true;
                }
                
            }
           
            //b.Dispose();
            return true;
        }

    }
}
