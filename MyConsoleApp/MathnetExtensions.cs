using System;
using MathNet.Numerics.Differentiation;

namespace MyConsoleApp
{
    public static class MathnetExtensions
    {
        /*
        public static System.Numerics.Matrix4x4 ToJacobianMatrix(this System.Numerics.Vector3 vector)
        {
            NumericalJacobian jacobian = new NumericalJacobian();
            jacobian.Evaluate()
            
        }
        */
        static double[] DoubleArray(System.Numerics.Vector3 vector)
        {
            return new double[] { vector.X, vector.Y, vector.Z };
        }
        
    }
}
