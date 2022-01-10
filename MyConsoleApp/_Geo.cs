using System;
using System.Numerics;
namespace MyConsoleApp
{

    public interface IWMM
    {
        Data Get(double latitude, double longitude, DateTime date);
    }

    public class _Geo : IWMM
    {
        static _Geo _instance;
        public static _Geo Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new _Geo();
                }

                return _instance;
            }
        }

        public Data Get(double latitude, double longitude, DateTime date)
        {
            var magCalc = new Geo.Geomagnetism.WmmGeomagnetismCalculator();
            var cordinate = new Geo.Coordinate(latitude, longitude);
            var geoMagRes = magCalc.TryCalculate(cordinate, date);

            var r = geoMagRes;

            return new Data(r.X, r.Y, r.Z, r.TotalIntensity, r.Inclination, r.Declination);
        }
    }

    public class Data
    {

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public double MagnitudeTesla { get; set; } // is nano, div 1000 to get microtesla
        public double MagnitudeMicroTesla { get; set; } //.. MagnitudeGauss?

        public double Inclinition { get; set; }
        public double Declination { get; set; }

        double ToMicro { get; } = 1000;

        public Data(double x, double y, double z, double magnitude, double inclination, double declination)
        {
            X = x / ToMicro; //x / ToMicro;
            Y = y / ToMicro;
            Z = z / ToMicro;
            // Logic.Utils.Log.Message("Magnitude: " + magnitude); // is in nanotesla
            MagnitudeTesla = magnitude;
            MagnitudeMicroTesla = magnitude / 1000;
            Inclinition = inclination;
            Declination = declination;

            // extra
            var ToNED = Quaternion.CreateFromAxisAngle(Vector3.UnitY, Extensions.ToRad(180)) * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Extensions.ToRad(90));
            var ToENU = Quaternion.Inverse(ToNED);
            MagRefENU = Vector3.Transform(new Vector3((float)X, (float)Y, (float)Z), ToENU);
        }
        public Vector3 MagRefENU { get; set; }
    }
}
