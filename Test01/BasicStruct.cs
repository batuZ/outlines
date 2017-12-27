using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test01
{
    class BasicStruct
    {
        private static BasicStruct _instance;
        public static BasicStruct Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BasicStruct();
                return _instance;
            }
        }
        public BlockType GetBlockType(int xSize, int ySize, int X, int Y)
        {
            int _Ds_x = xSize;
            int _Ds_y = ySize;
            if (X == 0)
            {
                if (Y == 0)
                    return BlockType.Angular_LeftTop;
                else if (Y == (_Ds_y - 1))
                    return BlockType.Angular_LeftBot;
                else
                    return BlockType.Edge_Left;
            }
            else if (X == (_Ds_x - 1))
            {
                if (Y == 0)
                    return BlockType.Angular_RightTop;
                else if (Y == (_Ds_y - 1))
                    return BlockType.Angular_RightBot;
                else
                    return BlockType.Edge_Right;
            }
            else if (Y == 0)
                return BlockType.Edge_Top;
            else if (Y == (_Ds_y - 1))
                return BlockType.Edge_Bot;
            else
                return BlockType.Center;
        }
        public double[] GetNBHval(double[] arr, int X, int Y, int arrXsize, int arrYsize, BlockType MyBlockType)
        {
            double[] _resArray;
            if (MyBlockType == BlockType.Angular_LeftTop)
            {
                _resArray = new double[4];
                _resArray = ReadArr(arr, arrXsize, X, Y, 2, 2);
            }
            else if (MyBlockType == BlockType.Angular_LeftBot)
            {
                _resArray = new double[4];
                _resArray = ReadArr(arr, arrXsize, X, Y - 1, 2, 2);
            }
            else if (MyBlockType == BlockType.Angular_RightTop)
            {
                _resArray = new double[4];
                _resArray = ReadArr(arr, arrXsize, X - 1, Y, 2, 2);
            }
            else if (MyBlockType == BlockType.Angular_RightBot)
            {
                _resArray = new double[4];
                _resArray = ReadArr(arr, arrXsize, X - 1, Y - 1, 2, 2);
            }
            else if (MyBlockType == BlockType.Edge_Left)
            {
                _resArray = new double[6];
                _resArray = ReadArr(arr, arrXsize, X, Y - 1, 2, 3);
            }
            else if (MyBlockType == BlockType.Edge_Right)
            {
                _resArray = new double[6];
                _resArray = ReadArr(arr, arrXsize, X - 1, Y - 1, 2, 3);
            }
            else if (MyBlockType == BlockType.Edge_Top)
            {
                _resArray = new double[6];
                _resArray = ReadArr(arr, arrXsize, X - 1, Y, 3, 2);
            }
            else if (MyBlockType == BlockType.Edge_Bot)
            {
                _resArray = new double[6];
                _resArray = ReadArr(arr, arrXsize, X - 1, Y - 1, 3, 2);
            }
            else
            {
                _resArray = new double[9];
                _resArray = ReadArr(arr, arrXsize, X - 1, Y - 1, 3, 3);
            }

            return _resArray;
        }

        public double[] ReadArr(double[] arr, int arrXsize, int xoff, int yoff, int Xsize, int Ysize)
        {
            double[] result = new double[Xsize * Ysize];
            int Cnt = 0;
            for (int j = yoff; j < yoff + Ysize; j++)
            {
                for (int i = xoff; i < xoff + Xsize; i++)
                {
                    result[Cnt] = arr[j * arrXsize + i];
                    Cnt++;
                }
            }
            return result;
        }

        public double[] Clockwise_Nbhd(double[] NbhdArray, BlockType Type)
        {
            double[] _clockWiseArray;
            if (Type == BlockType.Angular_LeftTop || Type == BlockType.Angular_LeftBot ||
                Type == BlockType.Angular_LeftBot || Type == BlockType.Angular_LeftBot)
            {
                _clockWiseArray = new double[4];
                _clockWiseArray[0] = NbhdArray[0];
                _clockWiseArray[1] = NbhdArray[1];
                _clockWiseArray[2] = NbhdArray[3];
                _clockWiseArray[3] = NbhdArray[2];
            }

            else if (Type == BlockType.Edge_Left || Type == BlockType.Edge_Right)
            {
                _clockWiseArray = new double[6];
                _clockWiseArray[0] = NbhdArray[0];
                _clockWiseArray[1] = NbhdArray[1];
                _clockWiseArray[2] = NbhdArray[3];
                _clockWiseArray[3] = NbhdArray[5];
                _clockWiseArray[4] = NbhdArray[4];
                _clockWiseArray[5] = NbhdArray[2];
            }
            else if (Type == BlockType.Edge_Left || Type == BlockType.Edge_Right)
            {
                _clockWiseArray = new double[6];
                _clockWiseArray[0] = NbhdArray[0];
                _clockWiseArray[1] = NbhdArray[1];
                _clockWiseArray[2] = NbhdArray[2];
                _clockWiseArray[3] = NbhdArray[5];
                _clockWiseArray[4] = NbhdArray[4];
                _clockWiseArray[5] = NbhdArray[3];
            }
            else
            {
                _clockWiseArray = new double[8];
                _clockWiseArray[0] = NbhdArray[0];
                _clockWiseArray[1] = NbhdArray[1];
                _clockWiseArray[2] = NbhdArray[2];
                _clockWiseArray[3] = NbhdArray[5];
                _clockWiseArray[4] = NbhdArray[8];
                _clockWiseArray[5] = NbhdArray[7];
                _clockWiseArray[6] = NbhdArray[6];
                _clockWiseArray[7] = NbhdArray[3];
            }

            return _clockWiseArray;
        }

        public OrderPoint ReflectMyPoint(int value, OrderPoint centerPnt)
        {
            OrderPoint myPnt = new OrderPoint();
            switch (value)
            {
                case 0:
                    myPnt.X = centerPnt.X - 1;
                    myPnt.Y = centerPnt.Y - 1;
                    return myPnt;
                case 1:
                    myPnt.X = centerPnt.X;
                    myPnt.Y = centerPnt.Y - 1;
                    return myPnt;
                case 2:
                    myPnt.X = centerPnt.X + 1;
                    myPnt.Y = centerPnt.Y - 1;
                    return myPnt;
                case 3:
                    myPnt.X = centerPnt.X + 1;
                    myPnt.Y = centerPnt.Y;
                    return myPnt;
                case 4:
                    myPnt.X = centerPnt.X + 1;
                    myPnt.Y = centerPnt.Y + 1;
                    return myPnt;
                case 5:
                    myPnt.X = centerPnt.X;
                    myPnt.Y = centerPnt.Y + 1;
                    return myPnt;
                case 6:
                    myPnt.X = centerPnt.X - 1;
                    myPnt.Y = centerPnt.Y + 1;
                    return myPnt;
                case 7:
                    myPnt.X = centerPnt.X - 1;
                    myPnt.Y = centerPnt.Y;
                    return myPnt;
                default:
                    return myPnt;
            }
        }
    }
}
