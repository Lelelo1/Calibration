using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using CsvHelper;
using CsvHelper.Configuration;
namespace MyConsoleApp
{
    class Program
    {

        // in mciroteslas from: https://www.ngdc.noaa.gov/geomag/calculators/magcalc.shtml#igrfwmm
        static float NormNorth { get; } = 16.1011f;
        static float NormEast { get; } = 1.2612f;
        static float NormVertical { get; } = -48.3479f;

        // 1 radian 57deg
        // full cirlce: 6.31f rad
        static float Rads { get; } = 1; // 6.31f; fullcircle
        public static Vector3 Earth { get; } = new Vector3(NormEast, NormNorth, NormVertical);
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Earth.Sphere(10000).Printable().Write();



            // with one:
            // 3.8460116, 7.2826767, 11.321257>
            // <0.027889848, -0.05362284, 0.025732994>
        }
        // test adding and removed vectors prior to and after rotation 

        // https://www.nxp.com/docs/en/application-note/AN4246.pdf
        public static Matrix4x4 SoftIronMatrix(double rads)
        {
            var matrix = new Matrix4x4(
                (float)Math.Cos(rads), (float)Math.Sin(rads), 0, 0,
                (float)-Math.Sin(rads), (float)Math.Cos(rads), 0f, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
            return matrix;
        }
    }

    

    static class Extensions
    {
        public static double Yaw(this Quaternion q)
        {
            q = Quaternion.Normalize(new Quaternion(0, 0, q.Z, q.W));
            return (float)(2 * Math.Acos(q.W));
        }

        static float floatPi = (float)Math.PI;
        public static float ToRad(float deg)
        {
            return deg * (floatPi / 180);
        }
        public static float ToDeg(float rad)
        {
            return rad * (180 / floatPi);
        }

        public static Vector3 Rotate(this Vector3 v1, Quaternion q)
        {
            return Vector3.Transform(v1, Matrix4x4.CreateFromQuaternion(q));
        }

        public static Vector3 Rotate(this Vector3 v1, Matrix4x4 m)
        {
            return Vector3.Transform(v1, m);
        }

        public static Quaternion QuaternionBetween(Vector3 v1, Vector3 v2)
        {
            Quaternion q;
            Vector3 a = Vector3.Cross(v1, v2);
            var w = Math.Sqrt((v1.Length() * v1.Length()) * (v2.Length() * v2.Length())) + Vector3.Dot(v1, v2);

            return Quaternion.Normalize(new Quaternion(a, (float)w));

            // from: https://stackoverflow.com/questions/1171849/finding-quaternion-representing-the-rotation-from-one-vector-to-another
        }

        static Random Random { get; } = new Random();
        static float Next => (float)Random.NextDouble();

        public static Vector3 RandomRotate(Vector3 v)
        {
            return Vector3.Transform(v, RandomQuaternion());
        }

        public static List<Vector3> Sphere(this Vector3 vector, int count = 10000)
        {
            var list = new List<Vector3>();
            for(var i = 0; i < count; i ++)
            {
                list.Add(RandomSpherePoint(vector, 1));
            }
            return list;
        }

        static Vector3 RandomSpherePoint(Vector3 vector, int radius)
        {
            var u = new Random().NextDouble();
            var v = new Random().NextDouble();
            var theta = 2 * Math.PI * u;
            var phi = Math.Acos(2 * v - 1);
            var x = vector.X + (radius * Math.Sin(phi) * Math.Cos(theta));
            var y = vector.Y + (radius * Math.Sin(phi) * Math.Sin(theta));
            var z = vector.Z + (radius * Math.Cos(phi));
            return new Vector3((float)x, (float)y, (float)z);
        }


        static Quaternion RandomQuaternion()
        {

            float x, y, z, u, v, w, s;
            do { x = Next; y = Next; z = x * x + y * y; } while (z > 1);
            do { u = Next; v = Next; w = u * u + v * v; } while (w > 1);
            s = (float)Math.Sqrt((1 - z) / w);
            return new Quaternion(x, y, s * u, s * v);

            // from: https://stackoverflow.com/questions/31600717/how-to-generate-a-random-quaternion-quickly
        }

        public static List<PrintableVector3> Printable(this List<Vector3> data)
        {
            return data.Select(d => new PrintableVector3(d.X, d.Y, d.Z)).ToList();
        } 

        // can't write 'Vector3' directly with 'CSVHelper' nuget
        public static void Write(this List<PrintableVector3> data)
        {
            CsvConfiguration configuration = new CsvConfiguration(CultureInfo.InvariantCulture);
            configuration.HasHeaderRecord = false;
            using (var writer = new StreamWriter("./data.csv"))
            using (var csv = new CsvWriter(writer, configuration))
            {
                csv.WriteRecords(data);
            }
        }


    }

    public class PrintableVector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public PrintableVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
