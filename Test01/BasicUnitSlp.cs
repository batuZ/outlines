using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test01
{
    class BasicUnitSlp
    {
        int xOff, yOff;
        int xSize, ySize;
        double demNodata, slopeNodata;
        double pixel_x, pixel_y;
        double[] bufferArr;
        public BasicUnitSlp(int xoff, int yoff, int xsize, int ysize, double DemNodata, double slopeNodata, double[] buffer, double Pixel_X, double Pixel_Y)
        {
            this.xOff = xoff;
            this.yOff = yoff;
            this.xSize = xsize;
            this.ySize = ysize;
            this.demNodata = DemNodata;
            this.slopeNodata = slopeNodata;
            this.pixel_x = Pixel_X;
            this.pixel_y = Pixel_Y;
            this.bufferArr = buffer;

        }
        /// <summary>
        /// 返回数组除未包含dem数据外围一圈轮廓
        /// </summary>
        /// <returns>返回一个(xSize-2)*(ySize-2)长度的数组</returns>
        public double[] Calculate()
        {
            double[] res = new double[(xSize - 2) * (ySize - 2)];

            for (int y = 1; y < ySize - 1; y++)
            {
                for (int x = 1; x < xSize - 1; x++)
                {
                    if (Math.Abs(bufferArr[y * xSize + x] - demNodata) < 0.000001)
                        res[(y - 1) * (xSize - 2) + (x - 1)] = slopeNodata;
                    else
                        res[(y - 1) * (xSize - 2) + (x - 1)] = CalcuSlope(Get9Arr(bufferArr, xSize, x, y));
                }
            }
            return res;
        }

        private double[] Get9Arr(double[] arr, int xSize, int CenterX, int CenterY)
        {
            double[] r = new double[9];
            r[0] = arr[(CenterY - 1) * xSize + CenterX - 1];
            r[1] = arr[(CenterY - 1) * xSize + CenterX];
            r[2] = arr[(CenterY - 1) * xSize + CenterX + 1];
            r[3] = arr[(CenterY) * xSize + CenterX - 1];
            r[4] = arr[(CenterY) * xSize + CenterX];
            r[5] = arr[(CenterY) * xSize + CenterX + 1];
            r[6] = arr[(CenterY + 1) * xSize + CenterX - 1];
            r[7] = arr[(CenterY + 1) * xSize + CenterX];
            r[8] = arr[(CenterY + 1) * xSize + CenterX + 1];

            return r;
        }

        /// <summary>
        /// 根据3X3方格计算坡度
        /// </summary>
        /// <param name="InArray"></param>
        /// <returns></returns>
        private double CalcuSlope(double[] InArray)
        {
            if (InArray.Length != 9)
                throw new RankException("InArray");
            double left = InArray[0] + 2 * InArray[3] + InArray[6];
            double right = InArray[2] + 2 * InArray[5] + InArray[8];
            double bot = InArray[6] + 2 * InArray[7] + InArray[8];
            double top = InArray[0] + 2 * InArray[1] + InArray[2];

            double _weSlp = (right - left) / (8 * pixel_x);
            double _snSlp = (bot - top) / (8 * pixel_y);

            return 180 / Math.PI * Math.Atan(Math.Sqrt(_weSlp * _weSlp + _snSlp * _snSlp));
        }
    }
}
