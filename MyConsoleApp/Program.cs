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

            var earthPoints = CreateEarthDataPoints().AddSoftIronDistiortion(SemiAxises, Earth);
            earthPoints.Printable().Write("./earth.csv");

            var earthSum = earthPoints.Sum();
            Console.WriteLine(earthSum);
            var earthMean = earthSum / 1200;




            var hardIron = CreateHardIronBiasedDataPoints();
            // hpoint = e * Earth.Length()) + TestBias
            CreateHardIronBiasedDataPoints().AddSoftIronDistiortion(SemiAxises, Earth).Select(h =>
            {
                    
                var meanCount = 100;
                var hMean = h.Sphere((int)h.Length(), meanCount).Sum() / meanCount;


                var angle = Quaternion.Normalize(Extensions.QuaternionBetween(h, Earth));//.Yaw();


                Console.WriteLine("angle: " + angle);
                var q = angle;//Quaternion.Inverse(Quaternion.Normalize(Quaternion.CreateFromAxisAngle(Earth, angle)));
                
               
                var bias = hMean;
                Console.WriteLine("bias: " + bias);

                //(h - bias).Rotate(rotationMatrix);//.Normalize(Earth.Length());
                return h;

            }).ToList().Printable().Write("./data.csv");

            //CreateSoftIronBiasedDataPoints();

            Console.WriteLine(new Vector3(-36.12f, -12.56f, -32.89f).Length());
            // with one:
            // 3.8460116, 7.2826767, 11.321257>
            // <0.027889848, -0.05362284, 0.025732994>
            var o = 2 * 6 - 4;
            Console.WriteLine("" + o);
        }

        static List<Vector3> CreateEarthDataPoints()
        {
            return Vector3.Zero.Sphere(1, 1200).Select(e => e * Earth.Length()).ToList();
        }

        public static Vector3 TestBias { get; } = new Vector3(20, -10, -375);
        static List<Vector3> CreateHardIronBiasedDataPoints()
        {
            return Vector3.Zero.Sphere(1, 1200).Select(e => e * Earth.Length()).ToList().Select(e => e + TestBias).ToList();
        }

        public static Vector3 SemiAxises { get; } = new Vector3(-0.5f, -0.8f, 1.2f);


        static List<Vector3> CreateSoftIronBiasedDataPoints()
        {
            return Vector3.Zero.Sphere(1, 1200).Select(e => (e * Earth.Length()) / SemiAxises).ToList();
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

        public static List<Vector3> Sphere(this Vector3 vector, int radius, int count = 10000)
        {
            var list = new List<Vector3>();
            for (var i = 0; i < count; i++)
            {
                list.Add(RandomSpherePoint(vector, radius));
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

        public static List<PrintableVector3> Printable(this List<Vector3> data)
        {
            return data.Select(d => new PrintableVector3(d.X, d.Y, d.Z)).ToList();
        }

        // can't write 'Vector3' directly with 'CSVHelper' nuget
        public static void Write(this List<PrintableVector3> data, string path)
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

        public static Vector3 Sum(this List<Vector3> vectors)
        {
            Vector3 sum = Vector3.Zero;
            vectors.ForEach(v => sum += v);
            return sum;
        }

        public static Quaternion Sum(this List<Quaternion> vectors)
        {
            Quaternion sum = Quaternion.Identity;
            vectors.ForEach(v => sum += v);
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

        public static float AngleRads(Quaternion q)
        {
            return (float)Math.Acos(q.W) * 2;
        }

        public static GeometRi.Sphere GeometRiSphere(this Vector3 vector)
        {
            return new GeometRi.Sphere(vector.ToGeometRiPoint(), vector.Length());
        }

        public static double[] ToDoubleVectorArray(this Vector3 vector)
        {
            return new double[] { vector.X, vector.Y, vector.Z };
        }

        public static GeometRi.Point3d ToGeometRiPoint(this Vector3 vector)
        {
            return new GeometRi.Point3d(vector.ToDoubleVectorArray());
        }

        public static Vector3 ToSystemVector(this GeometRi.Point3d point)
        {
            return new Vector3((float)point.X, (float)point.Y, (float)point.Z);
        }

        public static GeometRi.Vector3d ToGeometRiVector(this Vector3 vector)
        {
            return new GeometRi.Vector3d(vector.X, vector.Y, vector.Z);
        }

        public static Matrix4x4 Sum(this List<Matrix4x4> matrices)
        {
            Matrix4x4 sum = Matrix4x4.Identity;
            matrices.ForEach(m => sum += m);
            return sum;
        }

        public static List<Vector3> AddSoftIronDistiortion(this IEnumerable<Vector3> vector, Vector3 semiAxises, Vector3 earth)
        {
            return vector.Select(e => (e) / semiAxises).ToList();
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
