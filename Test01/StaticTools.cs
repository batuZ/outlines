using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test01
{
    static class StaticTools
    {
        #region GDAL/OGR 类的扩展方法

        /// <summary>
        /// 删除Featuer后更新Layer,Layer会被释放
        /// </summary>
        /// <param name="myDS"></param>
        public static void deleteFeatUpdate(this OSGeo.OGR.DataSource myDS)
        {
            string a = "REPACK " + myDS.GetLayerByIndex(0).GetName();
            myDS.ExecuteSQL(a, null, "");
            myDS.Dispose();
        }

        /// <summary>
        /// 通过Layer名删除Layer
        /// </summary>
        /// <param name="myDS"></param>
        /// <param name="layerName"></param>
        /// <returns></returns>
        public static bool deleteLayerByName(this OSGeo.OGR.DataSource myDS, string layerName)
        {
            bool isDelete = false;
            int layerCount = myDS.GetLayerCount();
            for (int i = 0; i < layerCount; i++)
            {
                OSGeo.OGR.Layer itm = myDS.GetLayerByIndex(i);
                if (itm.GetName() == layerName)
                {
                    itm.Dispose();
                    myDS.DeleteLayer(i);
                    isDelete = true;
                    break;
                }
                else
                    itm.Dispose();
            }
            return isDelete;
        }

        /// <summary>
        /// 是否被释放
        /// </summary>
        /// <param name="myDS"></param>
        /// <returns></returns>
        public static bool IsDispose(this OSGeo.GDAL.Dataset myDS)
        {
            try
            {
                int e = myDS.RasterCount;
                return false;
            }
            catch { return true; }
        }

        public static void claenPoint(this OSGeo.OGR.Layer resLayer, double jiaodu, int cishu)
        {
            int featCount = resLayer.GetFeatureCount(0);

            for (int i = 0; i < featCount; i++)
            {
                OSGeo.OGR.Feature oriFeat = resLayer.GetFeature(i);
                OSGeo.OGR.Geometry oriGeom = oriFeat.GetGeometryRef();
                OSGeo.OGR.Geometry subGeom = oriGeom.GetGeometryRef(0);

                int pointCount = subGeom.GetPointCount();

                Point[] aFeat = new Point[pointCount];

                for (int c = 0; c < pointCount; c++)
                {
                    aFeat[c].X = subGeom.GetX(c);
                    aFeat[c].Y = subGeom.GetY(c);
                    aFeat[c].Z = subGeom.GetZ(c);
                }

                OSGeo.OGR.Geometry newGeom = null;
                if (aFeat.Length > cishu * 3)
                {
                    newGeom = JID(aFeat, jiaodu, cishu);
                }
                else
                {
                    oriFeat.Dispose();
                    continue;
                }
                if (newGeom != null)
                {
                    oriFeat.SetGeometry(newGeom);
                    resLayer.SetFeature(oriFeat);
                }
                oriFeat.Dispose();
            }
        }
        /// <summary>
        /// 三点夹角的判定条件,输出为满足条件的成员的ID所组成的ID数组
        /// </summary>
        /// <param name="aFeat"></param>
        /// <returns></returns>
        private static OSGeo.OGR.Geometry JID(Point[] aFeat, double userSet, int seleTime)
        {
            List<Point[]> pjGroupL = new List<Point[]>();
            List<Point[]> zjGroupL = new List<Point[]>();

            List<Point> pjGroup = new List<Point>();
            List<Point> zjGroup = new List<Point>();

            for (int i = 0; i < aFeat.Length; i++)
            {
                int frontId, thisId, backId;
                bool[] yon = new bool[seleTime];
                for (int t = 1; t <= seleTime; t++)
                {
                    frontId = i < t ? aFeat.Length - 1 + i - t : i - t;

                    thisId = i;

                    backId = i > aFeat.Length - 1 - t ? i - (aFeat.Length - 1) + t : backId = i + t;

                    double jiaodu = cosCalculator(aFeat[frontId], aFeat[thisId], aFeat[backId]);

                    yon[t - 1] = jiaodu > userSet;
                }

                if (yon.Contains(true))
                {
                    pjGroup.Add(aFeat[i]);
                }
                else
                {
                    zjGroup.Add(aFeat[i]);
                }
            }

            OSGeo.OGR.Geometry outGeom = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbPolygon);
            OSGeo.OGR.Geometry subGeom = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbLinearRing);

            for (int g = 0; g < zjGroup.Count(); g++)
            {
                Point a = zjGroup[g];
                subGeom.AddPoint(a.X, a.Y, a.Z);
            }
            if (subGeom.GetPointCount() < 4)
            {
                return null;
            }
            subGeom.CloseRings();
            outGeom.AddGeometry(subGeom);
            return outGeom;
        }
        private static double cosCalculator(Point p1, Point p, Point p2)   /// 求夹角
        {
            double fenzi = (p1.X - p.X) * (p2.X - p.X) + (p1.Y - p.Y) * (p2.Y - p.Y);
            double fenmu = Math.Sqrt((p1.X - p.X) * (p1.X - p.X) + (p1.Y - p.Y) * (p1.Y - p.Y)) * Math.Sqrt((p2.X - p.X) * (p2.X - p.X) + (p2.Y - p.Y) * (p2.Y - p.Y));
            double cosValue = fenzi / fenmu;
            double acosV = Math.Acos(cosValue) * 180 / Math.PI;
            return acosV;
        }

        #endregion

        /// <summary>
        /// 判断两个Featuer是否重复，两外接矩形相同位置的边差小于1时为true
        /// </summary>
        /// <param name="ori"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public static bool isSame(OSGeo.OGR.Feature ori, OSGeo.OGR.Feature next, double maxCha = 1)
        {
            OSGeo.OGR.Envelope oriEnve = new OSGeo.OGR.Envelope();
            ori.GetGeometryRef().GetEnvelope(oriEnve);
            OSGeo.OGR.Envelope nextEnve = new OSGeo.OGR.Envelope();
            next.GetGeometryRef().GetEnvelope(nextEnve);

            if (Math.Abs(oriEnve.MaxX - nextEnve.MaxX) < maxCha && //外接矩形差
                Math.Abs(oriEnve.MaxY - nextEnve.MaxY) < maxCha &&
                Math.Abs(oriEnve.MinX - nextEnve.MinX) < maxCha &&
                Math.Abs(oriEnve.MinY - nextEnve.MinY) < maxCha)
                return true;
            else
                return false;
        }

        public static bool isIntersect(OSGeo.OGR.Envelope oriEnve, OSGeo.OGR.Envelope nextEnve)
        {
            if (oriEnve.MaxX < nextEnve.MinX ||
                oriEnve.MinX > nextEnve.MaxX ||
                oriEnve.MaxY < nextEnve.MinY ||
                oriEnve.MinY > nextEnve.MaxY)
                return false;
            else
                return true;
        }

        /// <summary>
        /// 标准差
        /// </summary>
        /// <param name="dzxLayer"></param>
        /// <param name="aue"></param>
        /// <param name="bzc"></param>
        public static void getBZC(OSGeo.OGR.Layer dzxLayer, out double aue, out double bzc)
        {
            //获取Featuer数
            int featCount = dzxLayer.GetFeatureCount(0);

            // 1 拿到每个Featuer的Value
            double[] values = new double[featCount];
            for (int i = 0; i < featCount; i++)
            {
                OSGeo.OGR.Feature fileFeat = dzxLayer.GetFeature(i);
                values[i] = fileFeat.GetFieldAsDouble("EVE");
                fileFeat.Dispose();
            }
            // 2 求Values的平均值
            aue = values.Average();

            // 3 求values与平均值差的平方和
            double pingFangHe = 0;
            for (int i = 0; i < featCount; i++)
            {
                pingFangHe += (values[i] - aue) * (values[i] - aue);
            }
            // 4 每个值与平均值的差相加,除Featuer数.再开方,得到标准差
            bzc = Math.Sqrt(pingFangHe / featCount);
        }

        /// <summary>
        ///  Array Index to geoSpace,index(索引)，xSize(图像X轴栅格数量),不通用！
        /// </summary>
        /// <param name="index"></param>
        /// <param name="subRasterOff_Size">
        /// [0] offx
        /// [1] offy
        /// [2] xSize
        /// [3] ySize
        /// </param>
        /// <param name="Transfrom"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void indexToGeoSpace(int index, int[] subRasterOff_Size, double[] Transfrom, out double x, out double y)
        {
            //通过索引获得当前值在的sub图像中的坐标
            int subPixel = (index + 1) % subRasterOff_Size[2];
            int subLine = index / subRasterOff_Size[2];
            //sub索引加off图像坐标，获得 当前值所在的全局图像坐标
            int Pixel = subPixel + subRasterOff_Size[0];
            int Line = subLine + subRasterOff_Size[1];
            // 从像素空间转换到地理空间
            imageToGeoSpace(Transfrom, Pixel, Line, out x, out y);
        }

        /// <summary>
        /// 从像素空间转换到地理空间
        /// </summary>
        /// <param name="adfGeoTransform">影像坐标变换参数</param>
        /// <param name="pixel">像素所在行</param>
        /// <param name="line">像素所在列</param>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        public static void imageToGeoSpace(double[] Tran, int pixel, int line, out double X, out double Y)
        {
            X = Tran[0] + pixel * Tran[1] + line * Tran[2];
            Y = Tran[3] + pixel * Tran[4] + line * Tran[5];
        }

        /// <summary>
        /// 从地理空间转换到像素空间
        /// </summary>
        /// <param name="adfGeoTransform">影像坐标变化参数</param>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="pixel">像素所在行</param>
        /// <param name="line">像素所在列</param>
        public static void geoToImageSpace(double[] Tran, double x, double y, out int pixel, out int line)
        {
            line = (int)((y * Tran[1] - x * Tran[4] + Tran[0] * Tran[4] - Tran[3] * Tran[1]) / (Tran[5] * Tran[1] - Tran[2] * Tran[4]));
            pixel = (int)((x - Tran[0] - line * Tran[2]) / Tran[1]);
        }
        public static void msgLine(string msg)
        {
            Console.WriteLine(msg);
        }
        /// <summary>
        /// 退格输出
        /// </summary>
        private static int maxMsgCount = 0;
        public static void msgBack(string msg)
        {
            maxMsgCount = maxMsgCount > msg.Length ? maxMsgCount : msg.Length;
            Console.Write(msg + new string('\0', maxMsgCount - msg.Length));
            Console.Write(new string('\b', maxMsgCount));
        }
        /// <summary>
        /// 进度条
        /// </summary>
        /// <param name="p"></param>
        public static void progress(int p, string msg = "")
        {
            if (p >= 0 && p <= 100)
            {
                Console.Write(new string('▁', 25) + $" {p} % {msg}");
                Console.Write(new string('\b', 60 + msg.Length));
                Console.Write(new string('█', p / 4));
                Console.Write(new string('\b', p / 2));
                if (p == 100) Console.Write('\n');
            }
        }
    }

    class BtsThread
    {
        List<Thread> tasks;
        List<Thread> subTask;
        int counts;
        string msg;
        /// <summary>
        /// 任务并行数量
        /// </summary>
        /// <param name="thSize"></param>
        public BtsThread(int thSize)
        {
            counts = thSize;
            tasks = new List<Thread>();
            subTask = new List<Thread>();
        }
        public void GoGoGo()
        {
            while (subTask.Count < counts || subTask.Remove(subTask.Find(t => t.ThreadState == ThreadState.Stopped)))
            {
                Thread waitTask = tasks.Find(t => t.ThreadState == ThreadState.Unstarted);
                waitTask.Start();
                subTask.Add(waitTask);
            }
            waitEND();
        }
        public void AddTask(Thread t)
        {
            tasks.Add(t);
        }
        private void waitEND()
        {
            while (tasks.Exists(t => t.ThreadState == ThreadState.Unstarted || t.ThreadState == ThreadState.Running))
            {
                if (msg != null) Console.WriteLine(msg);
                Thread.Sleep(1000);
            }
        }

    }
}
