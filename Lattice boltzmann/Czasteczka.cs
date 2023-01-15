using System;
using System.Drawing;
using System.Windows.Forms;

namespace Lattice_boltzmann
{
    public class Czasteczka
    {
        
        public Czasteczka(int x, int y, int kierunek)
        {
            Random rand = new Random();

            Lokalizacja = new Point(x,y);
            Kierunek = kierunek;
            Szybkosc = rand.Next(1, 10);//10;
         
            WspolczynnikWagowy = 1/4;
            Masa = rand.Next(10,30);
            CzasRelaksacji = 1;//rand.Next(1,1000000);
            Feq = CzasRelaksacji * (1 + 3 * Szybkosc + 3 * Szybkosc * Szybkosc);
            Stezenie = Feq / WspolczynnikWagowy;
            
        }
        public int Szybkosc { get; set; }
        public int Kierunek { get; set; } // -1 -lewo, -2 -gora,  1 -prawo, 2 -dol
        public Point Lokalizacja { get; set; }

        public int Masa { get; set; }
        public double CzasRelaksacji { get; set; }
        public double Stezenie { get; set; }
        public double WspolczynnikWagowy { get; set; }
        public double Feq { get; set; } //równowaga lokalna

        // F eq = w * C -> F eq = wspolczynnik_wagowy * stezenie 
        // Fout = Fin + czasRelaksacji(Feq - Fin) => Fout = Feq
        //zmiana stezenia koloru od stezenia
        // Feq = czasRel*gestosc(1+3(wektorKierunku*predkosc) + 4.5(wektorKierunku*predkosc)^2 - 1.5 predkosc^2) =>
        // Feq = czasRel * (1 + 3 predkosc + 4.5 predkosc^2 - 1.5 predkosc^2) = czasRel * (1+3predkosc + 3predkosc^2) 

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
                            for (int i = 0; i < Rysowanie.czasteczka.Count; i++)
                            {
                                if (Rysowanie.czasteczka[i].Lokalizacja.X == (int)rectangleF.X && Rysowanie.czasteczka[i].Lokalizacja.Y == (int)rectangleF.Y)
                                { //warunek kolizji

                                    //prawo zachowania pedu
                                    int m1 = Masa; int v1 = Szybkosc;
                                    int m2 = Rysowanie.czasteczka[i].Masa; int v2 = Rysowanie.czasteczka[i].Szybkosc;
                                    int u1 = v1 * (m1 - m2) / (m1 + m2) + 2 * m2 * v2 / m1 + m2;
                                    int u2 = v2 * (m1 - m2) / (m1 + m2) + 2 * m1 * v1 / m1 + m2;
                                    Szybkosc = u1;
                                    Rysowanie.czasteczka[i].Szybkosc = u2;

                                    Rysowanie.czasteczka[i].Kierunek = -Rysowanie.czasteczka[i].Kierunek;
                                    Kierunek = -Kierunek;

                                    ruch();
                                }

                            }
                            Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                            Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);
                        }
                        break;
                    case -2: // gora
                        {
                            
                            Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                            RectangleF rectangleF = new RectangleF(Lokalizacja.X, Lokalizacja.Y + Szybkosc, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                            for (int i = 0; i < Rysowanie.czasteczka.Count; i++)
                            {
                                if (Rysowanie.czasteczka[i].Lokalizacja.X == (int)rectangleF.X && Rysowanie.czasteczka[i].Lokalizacja.Y == (int)rectangleF.Y)
                                { //warunek kolizji

                                    //prawo zachowania pedu
                                    int m1 = Masa; int v1 = Szybkosc;
                                    int m2 = Rysowanie.czasteczka[i].Masa; int v2 = Rysowanie.czasteczka[i].Szybkosc;
                                    int u1 = v1 * (m1 - m2) / (m1 + m2) + 2 * m2 * v2 / m1 + m2;
                                    int u2 = v2 * (m1 - m2) / (m1 + m2) + 2 * m1 * v1 / m1 + m2;
                                    Szybkosc = u1;
                                    Rysowanie.czasteczka[i].Szybkosc = u2;

                                    Rysowanie.czasteczka[i].Kierunek = -Rysowanie.czasteczka[i].Kierunek;
                                    Kierunek = -Kierunek;

                                    ruch();
                                }

                            }
                            Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                            Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);

                        }
                        break;
                    case 1: // prawo
                        {
                           
                            Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                            RectangleF rectangleF = new RectangleF(Lokalizacja.X + Szybkosc, Lokalizacja.Y, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                            for (int i = 0; i < Rysowanie.czasteczka.Count; i++)
                            {
                                if (Rysowanie.czasteczka[i].Lokalizacja.X == (int)rectangleF.X && Rysowanie.czasteczka[i].Lokalizacja.Y == (int)rectangleF.Y)
                                { //warunek kolizji

                                    //prawo zachowania pedu
                                    int m1 = Masa; int v1 = Szybkosc;
                                    int m2 = Rysowanie.czasteczka[i].Masa; int v2 = Rysowanie.czasteczka[i].Szybkosc;
                                    int u1 = v1 * (m1 - m2) / (m1 + m2) + 2 * m2 * v2 / m1 + m2;
                                    int u2 = v2 * (m1 - m2) / (m1 + m2) + 2 * m1 * v1 / m1 + m2;
                                    Szybkosc = u1;
                                    Rysowanie.czasteczka[i].Szybkosc = u2;

                                    Rysowanie.czasteczka[i].Kierunek = -Rysowanie.czasteczka[i].Kierunek;
                                    Kierunek = -Kierunek;

                                    ruch();
                                }

                            }
                            Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                            Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);

                        }
                        break;
                    case 2: //dol
                        {
                            
                            Rysowanie.wyczyscCzasteczke(this, Form1.pictureBox1);
                            RectangleF rectangleF = new RectangleF(Lokalizacja.X, Lokalizacja.Y - Szybkosc, Rysowanie.rozmiarCzasteczki, Rysowanie.rozmiarCzasteczki);
                            for (int i = 0; i < Rysowanie.czasteczka.Count; i++)
                            {
                                if (Rysowanie.czasteczka[i].Lokalizacja.X == (int)rectangleF.X && Rysowanie.czasteczka[i].Lokalizacja.Y == (int)rectangleF.Y)
                                { //warunek kolizji

                                    //prawo zachowania pedu
                                    int m1 = Masa; int v1 = Szybkosc;
                                    int m2 = Rysowanie.czasteczka[i].Masa; int v2 = Rysowanie.czasteczka[i].Szybkosc;
                                    int u1 = v1 * (m1 - m2) / (m1 + m2) + 2 * m2 * v2 / m1 + m2;   
                                    int u2 = v2 * (m1 - m2) / (m1 + m2) + 2 * m1 * v1 / m1 + m2;
                                    Szybkosc = u1;
                                    Rysowanie.czasteczka[i].Szybkosc = u2;

                                    Rysowanie.czasteczka[i].Kierunek = -Rysowanie.czasteczka[i].Kierunek;
                                    Kierunek = -Kierunek;

                                    ruch();
                                }

                            }
                            Lokalizacja = new Point((int)rectangleF.X, (int)rectangleF.Y);
                            Rysowanie.rysujCzasteczke(this, rectangleF, Form1.pictureBox1);

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
            picturebox.DrawToBitmap(b, picturebox.RectangleToScreen(picturebox.ClientRectangle));//picturebox.ClientRectangle);
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
           
            b.Dispose();
            return true;
        }

    }
}
