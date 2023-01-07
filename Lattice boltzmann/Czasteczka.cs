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
        public int Kierunek { get; set; } // 1-lewo, 2-gora, 3-prawo, 4-dol
        public Point Lokalizacja { get; set; }
        
        public void ruch()
        {

            switch (Kierunek)
            {
                case 1: // lewo
                    {
                        Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                        RectangleF rectangleF = new RectangleF(Lokalizacja.X - Szybkosc, Lokalizacja.Y, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                        Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);
                        Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);

                    }
                    break;
                case 2: // gora
                    {
                        Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                        RectangleF rectangleF = new RectangleF(Lokalizacja.X, Lokalizacja.Y + Szybkosc, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                        Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);
                        Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                    }
                    break;
                case 3: // prawo
                    {
                        Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                        RectangleF rectangleF = new RectangleF(Lokalizacja.X + Szybkosc, Lokalizacja.Y, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                        Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);
                        Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                    }
                    break;
                case 4: //dol
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
}
