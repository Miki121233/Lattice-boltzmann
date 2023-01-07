using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lattice_boltzmann
{
    public class Czasteczka
    {
        public Czasteczka(double x, double y)
        {
            this.LokalizacjaX = x;
            this.LokalizacjaY = y;
        }
        public double Szybkosc { get; set; }
        public double Kierunek { get; set; }
        public double LokalizacjaX { get; set; }
        public double LokalizacjaY { get; set; }


    }
}
