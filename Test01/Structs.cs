using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test01
{
    //点\单多边形\单线
    struct Point
    {
        public double X;
        public double Y;
        public double Z;
    }


    public class OrderPoint
    {
        public int X;
        public int Y;
        public Toward Direct;//定义指向下一点的朝向
        public int pID;//简化节点的时候需要用到

        public OrderPoint()
        { }
        public OrderPoint(int _X, int _Y)
        {
            X = _X;
            Y = _Y;
        }
    }
    /// <summary>
    /// 八邻域朝向（从左上角1开始）
    /// </summary>
    public enum Toward
    {
        Left_top = 0,
        Top = 1,
        Right_top = 2,
        Right = 3,
        Right_bot = 4,
        Bot = 5,
        Left_bot = 6,
        Left = 7
    }
    /// <summary>
    /// Angular = 4表示与之相邻的像素块有4块（含本身）
    /// </summary>
    public enum BlockType : int
    {
        Angular_LeftTop = 1,
        Angular_RightTop = 3,
        Angular_LeftBot = 5,
        Angular_RightBot = 7,
        Edge_Top = 2,
        Edge_Right = 4,
        Edge_Bot = 6,
        Edge_Left = 8,
        Center = 9
    }
    public class LineSelfSect
    {
        private static LineSelfSect _instance;
        public static LineSelfSect Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new LineSelfSect();
                return _instance;
            }
        }
        /// <summary>
        /// 判断自相交图形
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public List<OrderPoint> RingedList(List<OrderPoint> list)
        {
            foreach (OrderPoint item in list)
            {
                int _itemIndex = list.IndexOf(item);
                List<OrderPoint> findlist = list.FindAll((a) =>
                {
                    if (Math.Abs(a.X - item.X) < 3 &&
                        Math.Abs(a.Y - item.Y) < 3 &&
                        Math.Abs(list.IndexOf(a) - list.IndexOf(item)) > 25)
                        return true;
                    else
                        return false;
                });
                if (findlist.Count > 0)
                {
                    int min = list.IndexOf(findlist[0]);
                    if (min < _itemIndex)
                        return list.GetRange(min, _itemIndex - min);
                    else
                        return list.GetRange(_itemIndex, min - _itemIndex);
                }
            }
            return null;
        }
    }

    #region for MinOutLine
    struct Point_qt
    {
        public double X;
        public double Y;
        public double Z;
        public double Rela_Ang;
        public int PID;

        public Point_qt(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Rela_Ang = 2;
            this.PID = -1;
        }
        public Point_qt(double x, double y, double z, double Rela_Ang)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Rela_Ang = Rela_Ang;
            this.PID = -1;
        }

        public Point_qt(double Rela_Ang)
        {
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
            this.Rela_Ang = Rela_Ang;
            this.PID = -1;
        }
    }
    struct Edge_qt
    {
        public Point_qt FromNode;
        public Point_qt ToNode;
        public Point_qt minXNode;
        public Point_qt MaxXNode;
        public double MaxY;
        public double minX;
        public double MaxX;
        public double Area_Enve;
    }
    class Vec2
    {
        public double X;
        public double Y;
        public double Length { get { return Math.Sqrt(X * X + Y * Y); } }

        public Vec2() { }
        public Vec2(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public Vec2 GetCrossVec2()
        {
            Vec2 vec = new Vec2(Y, -X);
            return vec;
        }
        public Vec2 GetUnitVec2()
        {
            Vec2 vec = new Vec2();
            vec.X = this.X / this.Length;
            vec.Y = this.Y / this.Length;
            return vec;
        }

        public static Vec2 operator *(Vec2 lhs, double rhs)
        {
            Vec2 vec = new Vec2();
            vec.X = lhs.X * rhs;
            vec.Y = lhs.Y * rhs;
            return vec;
        }

        public static double operator *(Vec2 lhs, Vec2 rhs)
        {
            double result;
            result = lhs.X * rhs.Y - lhs.Y * rhs.X;
            return result;
        }
    }
    #endregion

    /// <summary>
    /// 筛选要素时，先分区归类，减少遍历数量
    /// </summary>
    class CutBox
    {
        public List<int> pdxIDs;
        public List<int> dzxIDs;
        public CutBox()
        {
            pdxIDs = new List<int>();
            dzxIDs = new List<int>();
        }
    }
}
