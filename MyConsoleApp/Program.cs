using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using g3;


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


        // simulation

        public static Vector3 TestBias { get; } = new Vector3(-20, 110, -375);
        public static Vector3 TestStretch { get; } = new Vector3(-0.8f, 1.2f, -2f);

        public static int Count { get; } = 1200;

        //static IEnumerable<Vector3> EarthPoints { get; } = CreateDataPoints(Earth.Length());

        static IEnumerable<Vector3> EarthPoints = new SphericalFibonacciPointSet(Count).ToPointToSystemVectors();

        static Vector3 EarthSphereCenter { get; } = Earth - Earth / 2;

        static IEnumerable<Vector3> DataPoints { get; } = EarthPoints.Select(e => e.Normalize(Earth.Length()));

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //EarthPoints.Select(e => e.Normalize(Earth.Length())).Printable().Write("./earth.csv");

            //DataPoints.AddHardIronDistortion(TestBias).Select(Calculate).Printable().Write("./data.csv");
            
            Console.WriteLine("Done");
            Console.WriteLine(Earth);
            Console.WriteLine(Center(Earth, 0.5f));

            /*
            var geo = _Geo.Instance.Get(57.7089, 11.9746, DateTime.Now);


            var raw = new Vector3((float)geo.X, (float)geo.Y, (float)geo.Z);


            Console.WriteLine("------");
            Console.WriteLine(raw);
            Console.WriteLine(geo.MagRefENU);
            */
        }




        static Vector3 Center(Vector3 vector, float weight)
        {

            var opposite = vector.RotateOpposite();
            Console.WriteLine(opposite);
            var lerpCenter = Vector3.Lerp(vector,opposite , weight);
            Console.WriteLine("lerpCenter: " + lerpCenter);
            return lerpCenter;
        }
        
        static Vector3 Last { get; set; } = Vector3.One;
        static List<Vector3> MeanCenter { get; } = new List<Vector3>();
        static Vector3 Calculate(Vector3 p)
        {
            var centerP = Center(p, (Earth/p).Length());
            Console.WriteLine("p: " + p);
            return p - centerP;

        }

        public static Vector3 EarthMean { get; } = Earth.Sphere((int)Earth.Length()).Mean();
        public static Vector3 PMean(Vector3 v)
        {
            return v.Sphere((int)Earth.Length()).Mean();
        }

        static void SoftIronCalculatePoint(Vector3 p)
        {

            /*
            // 2.3599968, 0.072999954, 2.850000
            var diff = p1 - p2;
            Console.WriteLine(diff);
            */


        }

        static List<Vector3> CreateDataPoints(float norm)
        {
            return Vector3.Zero.Sphere(1200).Select(e => e * norm).ToList();
        }


        // use 'CreateEarthDataPoints' wwith extension methods instead
        /*
        static List<Vector3> CreateHardIronBiasedDataPoints()
        {
            return Vector3.Zero.Sphere(1, 1200).Select(e => e * Earth.Length()).ToList().Select(e => e + TestBias).ToList();
        }

        public static Vector3 SemiAxises { get; } = new Vector3(-0.5f, -0.8f, 1.2f);


        static List<Vector3> CreateSoftIronBiasedDataPoints()
        {
            return Vector3.Zero.Sphere(1, 1200).Select(e => (e * Earth.Length()) / SemiAxises).ToList();
        }
        */


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
        public static float Yaw(this Quaternion q)
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
            var sphere = new List<Vector3>();

            var phi = Math.PI * (3 - Math.Sqrt(0.5));

            for(int i = 0; i < count; i++ )
            {
                var y = 1 - (i / (count - 1)) * 2;
                var radius = Math.Sqrt(1 - y * y);

                var theta = phi * i;

                var x = Math.Cos(theta) * radius;
                var z = Math.Sin(theta) * radius;

                sphere.Add(new Vector3((float) x, (float)y, (float)z));

            }


            // https://stackoverflow.com/questions/9600801/evenly-distributing-n-points-on-a-sphere

            return sphere;
        }

        /*
        static List<Vector> DistributedVector4(this Vector3 vector, int count)
        {
            var xValues = count / 3;
            var yValues = count / 3;
            var zValues = count / 3;

            var increaseX = ToRad(360 / xValues);
            var increaseY = ToRad(360 / yValues);
            var increaseZ = ToRad(360 / zValues);

            var xQ = Quaternion.CreateFromAxisAngle(Vector3.UnitX, increaseX);
            var yQ = Quaternion.CreateFromAxisAngle(Vector3.UnitY, increaseY);
            var zQ = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, increaseZ);

            for(int i = new )
        }
        */
        static Quaternion RandomQuaternion()
        {

            float x, y, z, u, v, w, s;
            do { x = Next; y = Next; z = x * x + y * y; } while (z > 1);
            do { u = Next; v = Next; w = u * u + v * v; } while (w > 1);
            s = (float)Math.Sqrt((1 - z) / w);
            return new Quaternion(x, y, s * u, s * v);

            // from: https://stackoverflow.com/questions/31600717/how-to-generate-a-random-quaternion-quickly
        }

        public static IEnumerable<PrintableVector3> Printable(this IEnumerable<Vector3> data)
        {
            return data.Select(d => new PrintableVector3(d.X, d.Y, d.Z));
        }

        // can't write 'Vector3' directly with 'CSVHelper' nuget
        public static void Write(this IEnumerable<PrintableVector3> data, string path)
        {
            CsvConfiguration configuration = new CsvConfiguration(CultureInfo.InvariantCulture);
            configuration.HasHeaderRecord = false;
            using (var writer = new StreamWriter(path))
            using (var csv = new CsvWriter(writer, configuration))
            {
                csv.WriteRecords(data);
            }
        }

        public static Vector3 Normalize(this Vector3 v, float norm = 1)
        {
            // note there is an axis angle as well
            return v * norm / v.Length();
        }

        public static Vector3 Sum(this IEnumerable<Vector3> vectors)
        {
            Vector3 sum = Vector3.Zero;
            vectors.ToList().ForEach(v => sum += v);
            return sum;
        }

        public static Vector3 Subtract(this IEnumerable<Vector3> vectors, Vector3 s)
        {
            Vector3 t = Vector3.Zero;
            vectors.ToList().ForEach(v => t += v - s);
            return t;
        }

        public static Quaternion Sum(this IEnumerable<Quaternion> vectors)
        {
            Quaternion sum = Quaternion.Identity;
            vectors.ToList().ForEach(v => sum += v);
            return sum;
        }

        public static Quaternion Subtract(this List<Quaternion> vectors)
        {
            Quaternion t = Quaternion.Identity;
            vectors.ForEach(v => t -= v);
            return t;
        }
        public static Vector3 AxisX(this Vector3 vector)
        {
            return new Vector3(vector.X);
        }
        public static Vector3 AxisY(this Vector3 vector)
        {
            return new Vector3(vector.Y);
        }
        public static Vector3 AxisZ(this Vector3 vector)
        {
            return new Vector3(vector.Z);
        }

        public static Vector3 ClambedAxises(this Vector3 v1, Vector3 to)
        {
            var cX = Vector3.Clamp(v1.AxisX(), -to.AxisX(), to.AxisX());
            var cY = Vector3.Clamp(v1.AxisY(), -to.AxisY(), to.AxisY());
            var cZ = Vector3.Clamp(v1.AxisZ(), -to.AxisZ(), to.AxisZ());

            return new Vector3(cX.X, cY.Y, cZ.Z);
        }

        //public static List<Vector> Substract()

        // https://stackoverflow.com/questions/52584715/how-can-i-convert-a-quaternion-to-an-angle?rq=1
        public static Vector3 Axis(this Quaternion q)
        {

            var a = AngleRads(q);

            var ax = q.X / Math.Sin(Math.Acos(a));
            var ay = q.Y / Math.Sin(Math.Acos(a));
            var az = q.Z / Math.Sin(Math.Acos(a));

            return new Vector3((float)ax, (float)ay, (float)az);
        }

        public static Matrix4x4 Sum(this List<Matrix4x4> matrices)
        {
            Matrix4x4 sum = Matrix4x4.Identity;
            matrices.ForEach(m => sum += m);
            return sum;
        }

        public static IEnumerable<Vector3> AddSoftIronDistiortion(this IEnumerable<Vector3> vector, Vector3 semiAxises)
        {
            return vector.Select(e => (e) / semiAxises);
        }

        public static IEnumerable<Vector3> AddHardIronDistortion(this IEnumerable<Vector3> vector, Vector3 hardIron)
        {
            return vector.Select(e => (e) + hardIron);
        }

        public static Vector3 Mean(this IEnumerable<Vector3> vectors)
        {
            var count = vectors.Count();
            return vectors.Sum() / count;
        }

        public static float AngleRads(this Quaternion quaternion)
        {
            return (float)(Math.Acos(quaternion.W) * 2);
            // https://stackoverflow.com/questions/52584715/how-can-i-convert-a-quaternion-to-an-angle?rq=1
        }

        public static float Slope(this Vector3 v1, Vector3 v2)
        {
            var dx = v1.X - v2.X;
            var dy = v1.Y - v2.Y;

            return dy / dx;
        }

        public static IEnumerable<Vector3> ReadDistortedMag()
        {
            
            var text = File.ReadAllLines("/Users/lelelo1/Projects/MyConsoleApp/MyConsoleApp/RealDistortedMag.txt");

            var readings = text.Select(s =>
            {
                var line = s.Split(" ");
                var x = float.Parse(line[1]);
                var y = float.Parse(line[2]);
                var z = float.Parse(line[3]);

                return new Vector3(x, y, z);
            });


            return readings;

        }

        public static List<Vector3> ToPointToSystemVectors(this SphericalFibonacciPointSet sphericalFibonacciPointSet)
        {
            var list = new List<Vector3>();
            for(int i = 0; i < sphericalFibonacciPointSet.Count; i++)
            {
                var p = sphericalFibonacciPointSet.Point(i);

                list.Add(new Vector3((float)p.x, (float)p.y, (float)p.z));
            }

            return list;
        }

        public static Vector3 RotateOpposite(this Vector3 vector)
        {
            /*
            var radsRotate = Extensions.ToRad(180);

            var qX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, radsRotate);
            var qY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, radsRotate);
            var qZ = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, radsRotate);

            return vector.Rotate(qX).Rotate(qY).Rotate(qZ);
            */
            //return Vector3.Negate(vector);
            //return vector.Rotate(Quaternion.CreateFromAxisAngle(vector, Extensions.ToRad(180)));

            return Vector3.Zero; // vector.Rotate(new Quaternion(vector, ToRad(10)));
        }

        public static Vector3 ToNearest(this IEnumerable<Vector3> vectors, Vector3 vector)
        {
            return vectors.OrderByDescending(v => Vector3.Distance(v, vector)).Last();
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

    public class ReadTyrexMagData
    {
        [Index(0)]
        public float A { get; }
        [Index(1)]
        public float B { get; }
        [Index(2)]
        public float C { get; }
        [Index(3)]
        public float D { get; }

        public ReadTyrexMagData(float a, float b, float c, float d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public Vector3 Get()
        {
            return new Vector3(B, C, D);
        }
    }
}
