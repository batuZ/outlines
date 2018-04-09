using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixRotation
{
    class Program
    {
        static void Main(string[] args)
        {
            Program s = new Program();
            double[] p1 = new double[] { 4, 0, 0 };
            double[] p2 = new double[] { 4, 3, 0 };
            s.Calculation(p1, p2);
        }
        //通过两点坐标求旋转矩阵
        void Calculation(double[] vectorBefore, double[] vectorAfter)
        {
            double[] rotationAxis;
            double rotationAngle;
            double[,] rotationMatrix;
            rotationAxis = CrossProduct(vectorBefore, vectorAfter);
            rotationAngle = Math.Acos(DotProduct(vectorBefore, vectorAfter) / Normalize(vectorBefore) / Normalize(vectorAfter));
            double f = rotationAngle * 180 / Math.PI;
            rotationMatrix = RotationMatrix(rotationAngle, rotationAxis);
        }

        double[] CrossProduct(double[] a, double[] b)
        {
            double[] c = new double[3];

            c[0] = a[1] * b[2] - a[2] * b[1];
            c[1] = a[2] * b[0] - a[0] * b[2];
            c[2] = a[0] * b[1] - a[1] * b[0];

            return c;
        }

        double DotProduct(double[] a, double[] b)
        {
            double result;
            result = a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

            return result;
        }

        double Normalize(double[] v)
        {
            double result;

            result = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);

            return result;
        }

        double[,] RotationMatrix(double angle, double[] u)
        {
            double norm = Normalize(u);
            double[,] rotatinMatrix = new double[3, 3];

            u[0] = u[0] / norm;
            u[1] = u[1] / norm;
            u[2] = u[2] / norm;

            rotatinMatrix[0, 0] = Math.Cos(angle) + u[0] * u[0] * (1 - Math.Cos(angle));
            rotatinMatrix[0, 1] = u[0] * u[1] * (1 - Math.Cos(angle) - u[2] * Math.Sin(angle));
            rotatinMatrix[0, 2] = u[1] * Math.Sin(angle) + u[0] * u[2] * (1 - Math.Cos(angle));

            rotatinMatrix[1, 0] = u[2] * Math.Sin(angle) + u[0] * u[1] * (1 - Math.Cos(angle));
            rotatinMatrix[1, 1] = Math.Cos(angle) + u[1] * u[1] * (1 - Math.Cos(angle));
            rotatinMatrix[1, 2] = -u[0] * Math.Sin(angle) + u[1] * u[2] * (1 - Math.Cos(angle));

            rotatinMatrix[2, 0] = -u[1] * Math.Sin(angle) + u[0] * u[2] * (1 - Math.Cos(angle));
            rotatinMatrix[2, 1] = u[0] * Math.Sin(angle) + u[1] * u[2] * (1 - Math.Cos(angle));
            rotatinMatrix[2, 2] = Math.Cos(angle) + u[2] * u[2] * (1 - Math.Cos(angle));

            return rotatinMatrix;
        }
    }
}