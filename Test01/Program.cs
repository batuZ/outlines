using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test01
{
    class Program
    {
        static string dsmPath = @"E:\work\C-长春\DEM\changchun_dsm05.img";//  @"D:\temp\outlinetest01.img";//  
        static string shpSaveFile = @"D:\temp\asf\resClear.shp";             //  save
        static string shpSavePath = Path.GetDirectoryName(shpSaveFile);     //  C:\temp\asf\
        static string slopePath = shpSavePath + "\\a.img";                  //  C:\temp\asf\a.img

        static OSGeo.OGR.DataSource shpDataSet;             //shp文件集合DIR
        static OSGeo.GDAL.Driver gdalDriver;                //IMG文件驱动
        static OSGeo.GDAL.Dataset dsmDataset;               //dsmDataSet;
        static OSGeo.GDAL.Dataset slopeDataSet;             //slopeDataSet
        static OSGeo.OSR.SpatialReference srs;
        //用户定义参数

        static int jianG = 2;                               //生成等值线的间隔
        static double minLength = 50;                       //清理等值线的最小长度
        static double maxLength = 2600;                     //清理等值线的最大长度
        static double sec = 2;                              //2倍的标准差;1倍为68.2%;2倍为95.4%;3倍为99.8%;正态分布
        static double imgNodata = -100000;                  //dsm和slope的无效值
        static int CutWeight = 1000;                         //slope子图大小，影响计算效率
        static int OverlapWeight = 4;                       //slope子图重叠区
        static double ImprotLevel;                          //slopeLineAnge
        static bool IsDelete = true;
        static double[] dsm_Transform = new double[6];
        static int dsm_Xsize;
        static int dsm_Ysize;

        static void Main(string[] args)
        {

            OSGeo.GDAL.Gdal.AllRegister();
            OSGeo.OGR.Ogr.RegisterAll();
            //shp驱动
            OSGeo.OGR.Driver shpDataDriver = OSGeo.OGR.Ogr.GetDriverByName("ESRI shapefile");
            //shp数据源
            shpDataSet = shpDataDriver.CreateDataSource(shpSavePath, null);
            //img驱动
            gdalDriver = OSGeo.GDAL.Gdal.GetDriverByName("HFA");
            //dsm数据源
            dsmDataset = OSGeo.GDAL.Gdal.Open(dsmPath, OSGeo.GDAL.Access.GA_Update);
            //dsm数据信息
            dsmDataset.GetGeoTransform(dsm_Transform);
            dsm_Xsize = dsmDataset.RasterXSize;
            dsm_Ysize = dsmDataset.RasterYSize;
            //投影信息
            srs = dsmDataset.GetProjectionRef() == "" ? null : new OSGeo.OSR.SpatialReference(dsmDataset.GetProjectionRef());

            Stopwatch aTime = new Stopwatch(); aTime.Start();

            //1 坡度图
            buildSlope();
            StaticTools.msgLine($"完成！用时：{aTime.Elapsed.ToString()}\n"); aTime.Restart();

            //2 坡度线
            OSGeo.OGR.Layer pdx = getPDX(1);
            StaticTools.msgLine($"完成！用时：{aTime.Elapsed.ToString()}\n"); aTime.Restart();
            OSGeo.OGR.Layer slopeCleanLayer = cleanLayer(pdx, 200, 5000);
            StaticTools.msgLine($"完成！用时：{aTime.Elapsed.ToString()}\n"); aTime.Restart();

            //3 等高线
            OSGeo.OGR.Layer dzx = getDZX_();
            StaticTools.msgLine($"完成！用时：{aTime.Elapsed.ToString()}\n"); aTime.Restart();
            OSGeo.OGR.Layer dzxPolyLayer = cleanDZX(dzx);
            StaticTools.msgLine($"完成！用时：{aTime.Elapsed.ToString()}\n"); aTime.Restart();

            //4 筛选
            OSGeo.OGR.Layer selectLayer = selectionFeatuers(slopeCleanLayer, dzxPolyLayer);
            StaticTools.msgLine($"完成！用时：{aTime.Elapsed.ToString()}\n"); aTime.Restart();
            OSGeo.OGR.Layer resLayer = cleanLayer_FF(selectLayer);
            StaticTools.msgLine($"完成！用时：{aTime.Elapsed.ToString()}\n"); aTime.Restart();

            // 最小外接矩形
            getMinOutLineFromLayerToLayer(resLayer);
            StaticTools.msgLine($"完成！用时：{aTime.Elapsed.ToString()}\n"); aTime.Restart();

            //5 简化
            jianhua(resLayer, 175, 5);
            StaticTools.msgLine($"完成！用时：{aTime.Elapsed.ToString()}\n"); aTime.Restart();

            //6 高度值 未完成
            //getH(resLayer);
            //StaticTools.msgLine($"完成！用时：{aTime.Elapsed.ToString()}\n"); aTime.Restart();

            shpDataSet.Dispose();
            shpDataDriver.Dispose();
            dsmDataset.Dispose();
            gdalDriver.Dispose();

            //   aTime.Stop();
            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
        }

        #region 等高线
        /// <summary>
        /// 获取等高线
        /// </summary>
        /// <returns></returns>
        static OSGeo.OGR.Layer getDZX_()
        {
            StaticTools.msgLine("createDZX...");
            //分析数据源
            double noDataValue;                 //无效值
            int hasDataValue;                   //0不使用无效值,1使用无效值
            dsmDataset.GetRasterBand(1).GetNoDataValue(out noDataValue, out hasDataValue);
            //WKT
            string wkt = dsmDataset.GetProjectionRef();
            OSGeo.OSR.SpatialReference srs = wkt == "" ? null : new OSGeo.OSR.SpatialReference(wkt);
            //图像值属性，最大值、最小值、平均值
            double min, max, mean, std;
            dsmDataset.GetRasterBand(1).GetStatistics(0, 1, out min, out max, out mean, out std);
            //计算每级线的值，加0.5是确保取到最接近的整数，不被四舍五入
            int count = Convert.ToInt32((max - min) / jianG + 0.5);
            double[] shu = new double[count];
            for (int i = 0; i < count; i++)
                shu[i] = min + jianG * i;

            //创建空的SHP，准备塞入数据
            shpDataSet.deleteLayerByName("dzx");
            OSGeo.OGR.Layer dzxLayer = shpDataSet.CreateLayer("dzx", srs, OSGeo.OGR.wkbGeometryType.wkbMultiLineString, null);
            //必须有这两个字段，函数要往里塞值，其实没毛用
            OSGeo.OGR.FieldDefn fieldDf0 = new OSGeo.OGR.FieldDefn("LID", OSGeo.OGR.FieldType.OFTInteger);
            OSGeo.OGR.FieldDefn fieldDf1 = new OSGeo.OGR.FieldDefn("EVE", OSGeo.OGR.FieldType.OFTReal);
            dzxLayer.CreateField(fieldDf0, 1);//ID
            dzxLayer.CreateField(fieldDf1, 1);//Value

            //Band(1), 间隔, 起始高度, 分段数量, 分段值数组, 是否有无效值, 无效值, 预置图层. ID字段, 高度值字段, null , null
            OSGeo.GDAL.Gdal.ContourGenerate(dsmDataset.GetRasterBand(1), jianG, min, count, shu, hasDataValue, noDataValue, dzxLayer, 0, 1, null, null);

            if (dzxLayer.GetFeatureCount(0) > 0)
            {
                return dzxLayer;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 通过线长,值 清理等值线
        /// </summary>
        /// <param name="dzx"></param>
        /// <returns></returns>
        static OSGeo.OGR.Layer cleanDZX(OSGeo.OGR.Layer dzxLayer)
        {
            //创建poly层
            shpDataSet.deleteLayerByName("dzPoly");
            OSGeo.OGR.Layer newdzxLayer = shpDataSet.CreateLayer("dzPoly", dzxLayer.GetSpatialRef(), OSGeo.OGR.wkbGeometryType.wkbPolygon, null);

            //aue平均值，bzc标准差
            double aue, bzc;
            StaticTools.getBZC(dzxLayer, out aue, out bzc);
            //清理过大和过小的高度值，取值范围为平均值两则，2倍的标准差，约为95.4%
            double minValue = aue - bzc * sec;
            double maxValue = aue + bzc * sec;
            int FeatureCount = dzxLayer.GetFeatureCount(0);
            StaticTools.msgLine("cleanDZX...");
            for (int i = 0; i < FeatureCount; i++)
            {
                OSGeo.OGR.Feature fileFeat = dzxLayer.GetFeature(i);
                OSGeo.OGR.Geometry fileGeom = fileFeat.GetGeometryRef();

                if (fileGeom.IsRing()//判断闭合
                    && fileFeat.GetFieldAsDouble("EVE") > minValue
                    && fileFeat.GetFieldAsDouble("EVE") < maxValue //判断值
                    && fileGeom.Length() > minLength
                    && fileGeom.Length() < maxLength) //判断长度
                {
                    OSGeo.OGR.Feature newFeat = polyFeat(fileGeom);
                    newdzxLayer.CreateFeature(newFeat);
                    StaticTools.progress((i + 1) * 100 / FeatureCount, $"{i} / {FeatureCount}");
                }
            }
            StaticTools.progress(100);
            if (IsDelete)
                shpDataSet.deleteLayerByName(dzxLayer.GetName());

            return newdzxLayer;
        }
        static OSGeo.OGR.Feature polyFeat(OSGeo.OGR.Geometry lineGeom)
        {
            OSGeo.OGR.Feature polyFeat = new OSGeo.OGR.Feature(new OSGeo.OGR.FeatureDefn(""));
            OSGeo.OGR.Geometry polyGeom = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbPolygon);
            OSGeo.OGR.Geometry subGeom = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbLinearRing);
            for (int s = 0; s < lineGeom.GetPointCount(); s++)
                subGeom.AddPoint(lineGeom.GetX(s), lineGeom.GetY(s), lineGeom.GetZ(s));
            polyGeom.AddGeometry(subGeom);
            polyFeat.SetGeometry(polyGeom);
            return polyFeat;
        }

        #endregion

        #region 坡度图

        static void buildSlope()
        {
            // 创建slopeMap,设置无效值
            CreateSlopeMap();

            //Sloping
            int Maxcnt = GetCutNumberOfImg();
            StaticTools.msgLine("createSlopeMap...");
            for (int i = 0; i < Maxcnt; i++)
            {
                TKData_(i);
                StaticTools.progress((i + 1) * 100 / Maxcnt, $"{i + 1} / {Maxcnt}");
            }
        }
        /// <summary>
        /// 创建slopeMap,设置无效值
        /// </summary>
        /// <param name="DemPath"></param>
        /// <param name="OutSlpPath"></param>
        static void CreateSlopeMap()
        {
            double dsm_NoDataValue; int dsm_HasNoDataValue;
            OSGeo.GDAL.Band dsmBand = dsmDataset.GetRasterBand(1);
            dsmBand.GetNoDataValue(out dsm_NoDataValue, out dsm_HasNoDataValue);

            //调用GDal创建slope影像
            if (File.Exists(slopePath))
                gdalDriver.Delete(slopePath);
            slopeDataSet = gdalDriver.Create(slopePath, dsm_Xsize, dsm_Ysize, 1, OSGeo.GDAL.DataType.GDT_Float32, null);
            slopeDataSet.SetProjection(dsmDataset.GetProjection());
            slopeDataSet.SetGeoTransform(dsm_Transform);
            OSGeo.GDAL.Band slopeBand = slopeDataSet.GetRasterBand(1);

            //设置无效值
            if (dsm_HasNoDataValue == 0)            //dsm没有无效值
            {
                //把DSM和SLOPE全设置成用户指定的值
                dsmBand.SetNoDataValue(imgNodata);
                slopeBand.SetNoDataValue(imgNodata);
            }
            else                        //DSM有无效值时
            {
                //把SLOPE和全局变量设置成DSM的无效值
                imgNodata = dsm_NoDataValue;
                slopeBand.SetNoDataValue(dsm_NoDataValue);
            }
            // BufferOnePixel(slopeBand);
        }
        private static void BufferOnePixel(OSGeo.GDAL.Band TargetBand)
        {
            double[] readArr = new double[TargetBand.XSize];
            TargetBand.ReadRaster(0, 1, TargetBand.XSize, 1, readArr, TargetBand.XSize, 1, 0, 0);
            TargetBand.WriteRaster(0, 0, TargetBand.XSize, 1, readArr, TargetBand.XSize, 1, 0, 0);

            TargetBand.ReadRaster(0, TargetBand.YSize - 2, TargetBand.XSize, 1, readArr, TargetBand.XSize, 1, 0, 0);
            TargetBand.WriteRaster(0, TargetBand.YSize - 1, TargetBand.XSize, 1, readArr, TargetBand.XSize, 1, 0, 0);

            readArr = new double[TargetBand.YSize];
            TargetBand.ReadRaster(1, 0, 1, TargetBand.YSize, readArr, 1, TargetBand.YSize, 0, 0);
            TargetBand.WriteRaster(0, 0, 1, TargetBand.YSize, readArr, 1, TargetBand.YSize, 0, 0);

            TargetBand.ReadRaster(TargetBand.XSize - 2, 0, 1, TargetBand.YSize, readArr, 1, TargetBand.YSize, 0, 0);
            TargetBand.WriteRaster(TargetBand.XSize - 1, 0, 1, TargetBand.YSize, readArr, 1, TargetBand.YSize, 0, 0);
        }
        /// <summary>
        /// 分块数量
        /// </summary>
        /// <param name="imgPath">DSM路径</param>
        /// <param name="CutWeight">切块大小</param>
        /// <param name="OverlapWeight">块重叠区</param>
        /// <returns></returns>
        private static int GetCutNumberOfImg()
        {
            int xTimes = (dsm_Xsize - OverlapWeight) / CutWeight;
            int xRemainder = (dsm_Xsize - OverlapWeight) % CutWeight;
            int yTimes = (dsm_Ysize - OverlapWeight) / CutWeight;
            int yRemainder = (dsm_Ysize - OverlapWeight) % CutWeight;

            int ax = xTimes, by = yTimes;
            if (xTimes == 0)
                xRemainder = dsm_Xsize;
            if (yTimes == 0)
                yRemainder = dsm_Ysize;
            if (xRemainder > OverlapWeight)
                ax++;
            if (yRemainder > OverlapWeight)
                by++;

            return ax * by;
        }
        private static void TKData_(int index)
        {
            double pixel_x = Math.Abs(dsm_Transform[1]);
            double pixel_y = Math.Abs(dsm_Transform[5]);

            int xTimes = (dsm_Xsize - OverlapWeight) / CutWeight;
            int xRemainder = (dsm_Xsize - OverlapWeight) % CutWeight;
            int yTimes = (dsm_Ysize - OverlapWeight) / CutWeight;
            int yRemainder = (dsm_Ysize - OverlapWeight) % CutWeight;
            int ax = xTimes, by = yTimes;
            if (xRemainder > OverlapWeight)
                ax++;
            int x = index % ax;
            int y = index / ax;
            int xsize = CutWeight + OverlapWeight;
            int ysize = CutWeight + OverlapWeight;
            if (x * CutWeight + xsize > dsm_Xsize)
                xsize = dsm_Xsize - x * CutWeight;
            if (y * CutWeight + ysize > dsm_Ysize)
                ysize = dsm_Ysize - y * CutWeight;

            double[] buffer = new double[xsize * ysize];
            dsmDataset.GetRasterBand(1).ReadRaster(x * CutWeight, y * CutWeight, xsize, ysize, buffer, xsize, ysize, 0, 0);

            BasicUnitSlp bunitSlp = new BasicUnitSlp(x * CutWeight, y * CutWeight, xsize, ysize, imgNodata, imgNodata, buffer, pixel_x, pixel_y);

            double[] resArr = bunitSlp.Calculate();
            lock (slopeDataSet)
            {
                slopeDataSet.GetRasterBand(1).WriteRaster(x * CutWeight + 1, y * CutWeight + 1, xsize - 2, ysize - 2, resArr, xsize - 2, ysize - 2, 0, 0);
            }
        }
        #endregion

        #region 坡度线
        /// <summary>
        /// 获取原始坡度线
        /// </summary>
        /// <param name="Lev">提取级别，越高内容越丰富</param>
        static OSGeo.OGR.Layer getPDX(int Lev)
        {
            //创建原始坡度线 polygon
            shpDataSet.deleteLayerByName("slopeLine");
            OSGeo.OGR.Layer slopePolyLayer = shpDataSet.CreateLayer("slopeLine", null, OSGeo.OGR.wkbGeometryType.wkbPolygon, null);
            //获取分块数量
            int Maxcnt = GetCutNumberOfImg("");
            //开始计算
            StaticTools.msgLine("createSlopeLine...");
            for (int i = 0; i < Lev; i++)
            {
                //坡度阈值，每次变化得到的内容有所不同，最后合在一个polygon中
                ImprotLevel = 85 - i * 5;
                for (int ii = 0; ii < Maxcnt; ii++)
                {
                    TKDataF(slopePolyLayer, ii);
                    StaticTools.progress((ii + 1) * (i + 1) * 100 / (Lev * Maxcnt),
                        string.Format("{0}/{1}on{2}/{3}", ii, Maxcnt, i, Lev));
                }
            }
            slopeDataSet.Dispose();
            //提线完成，删掉坡度图
            if (IsDelete)
                gdalDriver.Delete(slopePath);
            return slopePolyLayer;
        }

        /// <summary>
        /// 获取图像分块儿数
        /// </summary>
        /// <param name="imgPath"></param>
        /// <returns></returns>
        private static int GetCutNumberOfImg(string img)
        {
            int xTimes = (dsm_Xsize - 300) / 500;
            int xRemainder = (dsm_Xsize - 300) % 500;
            int yTimes = (dsm_Ysize - 300) / 500;
            int yRemainder = (dsm_Ysize - 300) % 500;

            int ax = xTimes, by = yTimes;
            if (xTimes == 0)
                xRemainder = dsm_Xsize;
            if (yTimes == 0)
                yRemainder = dsm_Ysize;

            if (xRemainder > 10)
                ax++;
            if (yRemainder > 10)
                by++;

            return ax * by;
        }
        private static void TKDataF(OSGeo.OGR.Layer slopePolyLayer, int index)
        {
            double[] geoTansform = new double[6];
            dsmDataset.GetGeoTransform(geoTansform);

            int xTimes = (dsm_Xsize - 300) / 500;
            int xRemainder = (dsm_Xsize - 300) % 500;
            int yTimes = (dsm_Ysize - 300) / 500;
            int yRemainder = (dsm_Ysize - 300) % 500;
            int ax = xTimes, by = yTimes;
            if (xRemainder > 10)
                ax++;
            int x = index % ax;
            int y = index / ax;
            int xsize = 800, ysize = 800;
            if (x * 500 + 800 > dsm_Xsize)
                xsize = dsm_Xsize - x * 500;
            if (y * 500 + 800 > dsm_Ysize)
                ysize = dsm_Ysize - y * 500;

            double[] buffer = new double[xsize * ysize];
            double[] slopebuffer = new double[xsize * ysize];
            dsmDataset.GetRasterBand(1).ReadRaster(x * 500, y * 500, xsize, ysize, buffer, xsize, ysize, 0, 0);
            slopeDataSet.GetRasterBand(1).ReadRaster(x * 500, y * 500, xsize, ysize, slopebuffer, xsize, ysize, 0, 0);

            BasicUnit bUnit = new BasicUnit(x * 500, y * 500, xsize, ysize, ImprotLevel, buffer, slopebuffer, geoTansform, imgNodata);

            List<OSGeo.OGR.Geometry> geolist = bUnit.Identify2();

            lock (slopePolyLayer)
            {
                foreach (OSGeo.OGR.Geometry item in geolist)
                {
                    if (item.GetGeometryType() == OSGeo.OGR.wkbGeometryType.wkbPolygon25D ||
                        item.GetGeometryType() == OSGeo.OGR.wkbGeometryType.wkbPolygon)
                    {
                        OSGeo.OGR.Feature oFea = new OSGeo.OGR.Feature(slopePolyLayer.GetLayerDefn());
                        oFea.SetGeometry(item);
                        slopePolyLayer.CreateFeature(oFea);
                    }
                }
            }
        }
        /// <summary>
        /// 清理Layer中重复的，面积过小的featuer,返回一个新的Layer
        /// </summary>
        /// <param name="pdLayer">源layer</param>
        /// <param name="andArea">是否判断最小面积</param>
        /// <param name="minArea">最小面积</param>
        /// <returns></returns>
        public static OSGeo.OGR.Layer cleanLayer(OSGeo.OGR.Layer pdLayer, double minArea, double maxArea, double maxCha = 1)
        {
            shpDataSet.deleteLayerByName("pdClear");
            OSGeo.OGR.Layer outLayer = shpDataSet.CreateLayer("pdClear", pdLayer.GetSpatialRef(), pdLayer.GetGeomType(), null);
            OSGeo.OGR.Envelope oriEnve = new OSGeo.OGR.Envelope();
            OSGeo.OGR.Envelope nextEnve = new OSGeo.OGR.Envelope();
            StaticTools.msgLine("clean SlopeLine...");
            int featCount = pdLayer.GetFeatureCount(0);
            for (int i = 0; i < featCount - 1; i++)
            {
                bool isOnly = true;
                OSGeo.OGR.Feature ori = pdLayer.GetFeature(i);
                double oriArea = ori.GetGeometryRef().GetArea();

                if (oriArea < minArea || oriArea > maxArea)
                {
                    //判断当前要素的面积，过小或过大时，认为其有相同的图形，不加入新的Layer
                    isOnly = false;
                }
                else
                {
                    //判断当前要素与其它要素的包围盒的对应边，差值都小于阈值时，认为是相同的图形，需要跳过
                    //没有相同的图型时，isOnly为true ，塞到新的layer里
                    ori.GetGeometryRef().GetEnvelope(oriEnve);
                    for (int j = i + 1; j < featCount; j++)
                    {
                        pdLayer.GetFeature(j).GetGeometryRef().GetEnvelope(nextEnve);
                        if (Math.Abs(oriEnve.MaxX - nextEnve.MaxX) < maxCha &&
                            Math.Abs(oriEnve.MaxY - nextEnve.MaxY) < maxCha &&
                            Math.Abs(oriEnve.MinX - nextEnve.MinX) < maxCha &&
                            Math.Abs(oriEnve.MinY - nextEnve.MinY) < maxCha)
                        {
                            isOnly = false;
                            break;
                        }
                    }
                }
                if (isOnly)
                    outLayer.CreateFeature(ori);
                StaticTools.progress((i + 2) * 100 / featCount, $"{i + 1} / {featCount}");
            }
            if (IsDelete)
                shpDataSet.deleteLayerByName(pdLayer.GetName());

            return outLayer;
        }

        //分块清理，提高效率
        public static OSGeo.OGR.Layer cleanLayer_Cut(OSGeo.OGR.Layer pdLayer, double minArea, double maxArea, double maxCha = 1)
        {
            List<CutBox> Tree = buildTree(pdLayer);


            shpDataSet.deleteLayerByName("pdClear");
            OSGeo.OGR.Layer outLayer = shpDataSet.CreateLayer("pdClear", pdLayer.GetSpatialRef(), pdLayer.GetGeomType(), null);
            OSGeo.OGR.Envelope oriEnve = new OSGeo.OGR.Envelope();
            OSGeo.OGR.Envelope nextEnve = new OSGeo.OGR.Envelope();
            StaticTools.msgLine("clean SlopeLine...");
            for (int c = 0; c < Tree.Count; c++)
            {
                CutBox cb = Tree[c];
                int featCount = cb.pdxIDs.Count;

                for (int i = 0; i < featCount - 1; i++)
                {
                    bool isOnly = true;
                    OSGeo.OGR.Feature ori = pdLayer.GetFeature(cb.pdxIDs[i]);
                    double oriArea = ori.GetGeometryRef().GetArea();

                    if (oriArea < minArea || oriArea > maxArea)
                    {
                        //判断当前要素的面积，过小或过大时，认为其有相同的图形，不加入新的Layer
                        isOnly = false;
                    }
                    else
                    {
                        //判断当前要素与其它要素的包围盒的对应边，差值都小于阈值时，认为是相同的图形，需要跳过
                        //没有相同的图型时，isOnly为true ，塞到新的layer里
                        ori.GetGeometryRef().GetEnvelope(oriEnve);
                        for (int j = i + 1; j < featCount; j++)
                        {
                            pdLayer.GetFeature(cb.pdxIDs[j]).GetGeometryRef().GetEnvelope(nextEnve);
                            if (Math.Abs(oriEnve.MaxX - nextEnve.MaxX) < maxCha &&
                                Math.Abs(oriEnve.MaxY - nextEnve.MaxY) < maxCha &&
                                Math.Abs(oriEnve.MinX - nextEnve.MinX) < maxCha &&
                                Math.Abs(oriEnve.MinY - nextEnve.MinY) < maxCha)
                            {
                                isOnly = false;
                                break;
                            }
                        }
                    }
                    if (isOnly)
                        outLayer.CreateFeature(ori);
                    StaticTools.progress((i + 2) * 100 / featCount, $"{i + 1} / {featCount}");
                }
            }

            if (IsDelete)
                shpDataSet.deleteLayerByName(pdLayer.GetName());

            return outLayer;
        }
        #endregion

        #region 筛选
        /// <summary>
        /// 通过slope的位置和面积筛选dzxPoly
        /// </summary>
        /// <param name="slopePolyLayer"></param>
        /// <param name="dzxPolyLayer"></param>
        /// <returns></returns>
        static OSGeo.OGR.Layer selectionFeatuers(OSGeo.OGR.Layer slopePolyLayer, OSGeo.OGR.Layer dzxPolyLayer)
        {
            shpDataSet.deleteLayerByName("resLayer");
            OSGeo.OGR.Layer resLayer = shpDataSet.CreateLayer("resLayer", srs, dzxPolyLayer.GetGeomType(), null);

            List<CutBox> selectTree = buildTree(slopePolyLayer, dzxPolyLayer);

            StaticTools.msgLine("selection...");

            for (int i = 0; i < selectTree.Count; i++)
            {
                CutBox abox = selectTree[i];

                for (int pdi = 0; pdi < abox.pdxIDs.Count; pdi++)
                {
                    int ff = abox.pdxIDs[pdi];
                    OSGeo.OGR.Geometry pdGeom = slopePolyLayer.GetFeature(ff).GetGeometryRef();
                    double pdArea = pdGeom.GetArea();
                    double afterCha = double.MaxValue;
                    int yesID = -1;

                    for (int dzi = 0; dzi < abox.dzxIDs.Count; dzi++)
                    {
                        int tt = abox.dzxIDs[dzi];
                        OSGeo.OGR.Geometry dzGeom = dzxPolyLayer.GetFeature(tt).GetGeometryRef();
                        if (pdGeom.Intersect(dzGeom))
                        {
                            double cha = Math.Abs(dzGeom.GetArea() - pdArea);
                            if (cha < afterCha)
                            {
                                afterCha = cha;
                                yesID = tt;
                            }
                        }
                    }

                    if (yesID != -1)
                        resLayer.CreateFeature(dzxPolyLayer.GetFeature(yesID));
                    StaticTools.progress((pdi + 1) * 100 / abox.pdxIDs.Count, $"{pdi}/{abox.pdxIDs.Count} @ {i + 1} / {selectTree.Count}");
                }
            }
            if (IsDelete)
            {
                shpDataSet.deleteLayerByName(slopePolyLayer.GetName());
                shpDataSet.deleteLayerByName(dzxPolyLayer.GetName());
            }
            return resLayer;
        }
        /// <summary>
        /// 根据区块范围，获取对应的pdx,dzx
        /// </summary>
        /// <param name="pdLayer"></param>
        /// <param name="dzxlayer"></param>
        /// <returns></returns>
        static List<CutBox> buildTree(OSGeo.OGR.Layer pdLayer, OSGeo.OGR.Layer dzxlayer = null)
        {
            StaticTools.msgLine("buildTree...");
            OSGeo.OGR.Layer cutLayer = createCutLayer(3000);

            //用 cutLayer 中的 Featuers 创建一个容器,容器中是对应 Featuer 的 cutBox
            List<CutBox> allBoxes = new List<CutBox>();

            int cutFeatCount = cutLayer.GetFeatureCount(0);
            int pdFeatCount = pdLayer.GetFeatureCount(0);

            if (dzxlayer != null)
            {
                int dzFeatCount = dzxlayer.GetFeatureCount(0);
                for (int cuti = 0; cuti < cutFeatCount; cuti++)
                {
                    CutBox cb = new CutBox();
                    //把与盒子相交的等值线塞入盒子,向外Buffer一定距离，避免尴尬
                    OSGeo.OGR.Geometry cutGeom = cutLayer.GetFeature(cuti).GetGeometryRef().Buffer(5, 0);
                    for (int dzi = 0; dzi < dzFeatCount; dzi++)
                        if (cutGeom.Intersect(dzxlayer.GetFeature(dzi).GetGeometryRef()))
                            cb.dzxIDs.Add(dzi);
                    allBoxes.Add(cb);
                    StaticTools.progress((cuti + 1) * 100 / cutFeatCount, $"{cuti}/{cutFeatCount}");
                }
            }
            //遍历坡度线，放入对应的cutBox
            //不会重复，要素只会放入第一个匹配上的盒子里
            for (int pdi = 0; pdi < pdFeatCount; pdi++)
            {
                //拿出一个坡度要素
                OSGeo.OGR.Geometry aGeom = pdLayer.GetFeature(pdi).GetGeometryRef();
                for (int cuti = 0; cuti < cutFeatCount; cuti++)
                {
                    //拿出一个盒子，与要素求交
                    OSGeo.OGR.Geometry cutGeom = cutLayer.GetFeature(cuti).GetGeometryRef();
                    if (aGeom.Intersects(cutGeom))
                    {
                        //塞入盒子,盒子ID ，坡度线ID
                        allBoxes[cuti].pdxIDs.Add(pdi);
                        break;
                    }
                }
                StaticTools.progress((pdi + 1) * 100 / pdFeatCount, $"{pdi}/{pdFeatCount}");
            }
            if (IsDelete)
                shpDataSet.deleteLayerByName(cutLayer.GetName());

            return allBoxes;
        }
        /// <summary>
        /// 根据dsm的坐标划分区域
        /// </summary>
        /// <param name="tileSize">区块尺寸</param>
        /// <returns></returns>
        static OSGeo.OGR.Layer createCutLayer(double tileSize)
        {
            shpDataSet.deleteLayerByName("cutLayer");
            OSGeo.OGR.Layer cutLayer = shpDataSet.CreateLayer("cutLayer", srs, OSGeo.OGR.wkbGeometryType.wkbPolygon, null);
            double[] trans = new double[6];
            dsmDataset.GetGeoTransform(trans);
            //四边坐标
            double top = trans[3];
            double left = trans[0];
            double right = trans[0] + dsmDataset.RasterXSize * trans[1];
            double bottom = trans[3] + dsmDataset.RasterYSize * trans[5];
            //子块角点
            Point LB, LT, RT, RB;

            //横竖块数
            int hCount = (int)((top - bottom) / tileSize) + 1;
            int wCount = (int)((right - left) / tileSize) + 1;
            for (int h = 0; h < hCount; h++)
            {
                for (int w = 0; w < wCount; w++)
                {
                    OSGeo.OGR.Geometry subCutGeom = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbLinearRing);

                    LB.X = left + tileSize * w;
                    LB.Y = bottom + tileSize * h;
                    subCutGeom.AddPoint(LB.X, LB.Y, 0);

                    LT.X = LB.X;
                    LT.Y = LB.Y + tileSize;
                    subCutGeom.AddPoint(LT.X, LT.Y, 0);

                    RT.X = LB.X + tileSize;
                    RT.Y = LT.Y;
                    subCutGeom.AddPoint(RT.X, RT.Y, 0);

                    RB.X = RT.X;
                    RB.Y = LB.Y;
                    subCutGeom.AddPoint(RB.X, RB.Y, 0);
                    //封闭poly
                    subCutGeom.AddPoint(LB.X, LB.Y, 0);

                    OSGeo.OGR.Geometry cutGeom = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbPolygon);
                    cutGeom.AddGeometry(subCutGeom);
                    OSGeo.OGR.Feature cutFeat = new OSGeo.OGR.Feature(new OSGeo.OGR.FeatureDefn(""));
                    cutFeat.SetGeometry(cutGeom);
                    cutLayer.CreateFeature(cutFeat);
                }
            }
            return cutLayer;
        }
        /// <summary>
        /// 清理Layer中重复的，面积过小的featuer,返回一个新的Layer
        /// </summary>
        /// <param name="inLayer">源layer</param>
        /// <param name="andArea">是否判断最小面积</param>
        /// <param name="minArea">最小面积</param>
        /// <returns></returns>
        public static OSGeo.OGR.Layer cleanLayer_FF(OSGeo.OGR.Layer inLayer)
        {
            string oldLayerName = inLayer.GetName();
            string newLayerName = oldLayerName + "Clear";
            shpDataSet.deleteLayerByName(newLayerName);
            OSGeo.OGR.Layer outLayer = shpDataSet.CreateLayer(newLayerName, srs, inLayer.GetGeomType(), null);

            int featCount = inLayer.GetFeatureCount(0);
            StaticTools.msgLine($"clean resLine... before {featCount}");
            for (int i = 0; i < featCount - 1; i++)
            {
                bool isOnly = true;
                OSGeo.OGR.Feature ori = inLayer.GetFeature(i);
                OSGeo.OGR.Envelope oriEnve = new OSGeo.OGR.Envelope();
                ori.GetGeometryRef().GetEnvelope(oriEnve);
                double maxCha = 1;
                for (int j = i + 1; j < featCount; j++)
                {
                    OSGeo.OGR.Feature next = inLayer.GetFeature(j);
                    OSGeo.OGR.Envelope nextEnve = new OSGeo.OGR.Envelope();
                    next.GetGeometryRef().GetEnvelope(nextEnve);
                    if (Math.Abs(oriEnve.MaxX - nextEnve.MaxX) < maxCha &&
                        Math.Abs(oriEnve.MaxY - nextEnve.MaxY) < maxCha &&
                        Math.Abs(oriEnve.MinX - nextEnve.MinX) < maxCha &&
                        Math.Abs(oriEnve.MinY - nextEnve.MinY) < maxCha)
                    {
                        isOnly = false;
                        break;
                    }
                }
                if (isOnly)
                    outLayer.CreateFeature(ori);
                ori.Dispose();
                StaticTools.progress((i + 2) * 100 / featCount, $"{i + 1} / {featCount}");
            }
            if (IsDelete)
                shpDataSet.deleteLayerByName(inLayer.GetName());
            StaticTools.msgLine($"clean resLine... after {outLayer.GetFeatureCount(0)}");
            return outLayer;
        }

        #endregion

        #region 简化
        /// <summary>
        /// 清理原文件
        /// </summary>
        /// <param name="jiaodu"></param>
        /// <param name="cishu"></param>
        private static void jianhua(OSGeo.OGR.Layer resLayer, double jiaodu, int cishu)
        {
            StaticTools.msgLine("huajian...");
            StaticTools.progress(0);
            for (int i = cishu; i > 0; i--)
            {
                resLayer.claenPoint(jiaodu, i);
                StaticTools.progress((cishu - i + 1) * 100 / cishu);
            }
        }
        #endregion

        #region 获取高度值 未完成
        private static void getH(OSGeo.OGR.Layer oriLayer)
        {
            //创建一个BUFFER，BUFFER距离为1米
            OSGeo.OGR.Layer bufLayer = bufferFile(oriLayer, 1);

            //判断原文件中是否有以下字段，没有就创建

            if (oriLayer.FindFieldIndex("MIN", 1) == -1)
            {
                OSGeo.OGR.FieldDefn min = new OSGeo.OGR.FieldDefn("MIN", OSGeo.OGR.FieldType.OFTReal);
                oriLayer.CreateField(min, 1);
            }

            if (oriLayer.FindFieldIndex("MAX", 1) == -1)
            {
                OSGeo.OGR.FieldDefn max = new OSGeo.OGR.FieldDefn("MAX", OSGeo.OGR.FieldType.OFTReal);
                oriLayer.CreateField(max, 1);
            }

            if (oriLayer.FindFieldIndex("HIGHT", 1) == -1)
            {
                OSGeo.OGR.FieldDefn hight = new OSGeo.OGR.FieldDefn("HIGHT", OSGeo.OGR.FieldType.OFTReal);
                oriLayer.CreateField(hight, 1);
            }
            if (oriLayer.FindFieldIndex("TEMP", 1) == -1)
            {
                OSGeo.OGR.FieldDefn hight = new OSGeo.OGR.FieldDefn("TEMP", OSGeo.OGR.FieldType.OFTReal);
                oriLayer.CreateField(hight, 1);
            }
            double[] transfrom = new double[6];
            dsmDataset.GetGeoTransform(transfrom);
            int allX = dsmDataset.RasterXSize;
            int allY = dsmDataset.RasterYSize;
            StaticTools.msgLine("getH...");
            //开始计算每个Feature需要读取的栅格参数
            int featCount = oriLayer.GetFeatureCount(0);
            for (int i = 0; i < featCount; i++)
            {

                OSGeo.OGR.Feature oriFeat = oriLayer.GetFeature(i);
                OSGeo.OGR.Feature bufFeat = bufLayer.GetFeature(i);
                int[] subRasterOff_Size = subRasterInfo(transfrom, allX, allY, bufFeat);
                //  getMaxMinValue(dsmDataset, oriFeat, bufFeat, subRasterOff_Size);
                oriLayer.SetFeature(oriFeat);
                oriFeat.Dispose();
                bufFeat.Dispose();
                StaticTools.progress((i + 1) * 100 / featCount, $"{i} / {featCount}");
            }
            if (IsDelete)
                shpDataSet.deleteLayerByName(bufLayer.GetName());
        }
        /// <summary>
        /// 创建一个Buffer file,用于获取最小高度,juli是buffer的距离
        /// </summary>
        /// <param name="infile"></param>
        /// <param name="juli"></param>
        /// <returns></returns>
        private static OSGeo.OGR.Layer bufferFile(OSGeo.OGR.Layer inLayer, int juli)
        {
            string buf = inLayer.GetName() + "buf";
            shpDataSet.deleteLayerByName(buf);
            OSGeo.OGR.Layer bufferLayer = shpDataSet.CreateLayer(buf, inLayer.GetSpatialRef(), inLayer.GetGeomType(), null);

            int featCount = inLayer.GetFeatureCount(0);
            for (int i = 0; i < featCount; i++)
            {
                OSGeo.OGR.Feature inFeat = inLayer.GetFeature(i);
                OSGeo.OGR.Geometry inGeom = inFeat.GetGeometryRef();
                OSGeo.OGR.Geometry outGeom = inGeom.Buffer(juli, 0);
                OSGeo.OGR.Feature outFeat = new OSGeo.OGR.Feature(new OSGeo.OGR.FeatureDefn(""));
                outFeat.SetGeometry(outGeom);
                bufferLayer.CreateFeature(outFeat);
                inFeat.Dispose();
                outFeat.Dispose();
            }
            return bufferLayer;
        }

        /// <summary>
        /// 获取与一个Feature有关的Raster参数,[0]offX,[1]offY,[2]sizeX,[3]sizeY
        /// </summary>
        /// <param name="Trans"></param>
        /// <param name="aFeat"></param>
        /// <returns></returns>
        private static int[] subRasterInfo(double[] Trans, int xSize, int ySize, OSGeo.OGR.Feature aFeat)
        {
            //拿到Buffer Featuer的壳
            OSGeo.OGR.Geometry bufGeom = aFeat.GetGeometryRef();
            OSGeo.OGR.Envelope bufEnve = new OSGeo.OGR.Envelope();
            bufGeom.GetEnvelope(bufEnve);
            //判断壳是否超出全局范围，是则赋边界值
            double maxX, minY;
            StaticTools.imageToGeoSpace(Trans, xSize, ySize, out maxX, out minY);
            if (bufEnve.MinX < Trans[0])
            {
                bufEnve.MinX = Trans[0];
            }
            if (bufEnve.MaxY > Trans[3])
            {
                bufEnve.MaxY = Trans[3];
            }
            if (bufEnve.MaxX > maxX)
            {
                bufEnve.MaxX = maxX;
            }
            if (bufEnve.MinY < minY)
            {
                bufEnve.MinY = minY;
            }

            //通过壳坐标拿到SubRaster的起点及行列数
            var a = new int[4];
            int leftUpX, leftUpY, rightDownX, rightDownY;
            StaticTools.geoToImageSpace(Trans, bufEnve.MinX, bufEnve.MaxY, out leftUpX, out leftUpY);
            StaticTools.geoToImageSpace(Trans, bufEnve.MaxX, bufEnve.MinY, out rightDownX, out rightDownY);

            a[0] = leftUpX;
            a[1] = leftUpY;
            a[2] = Math.Abs(rightDownX - leftUpX);
            a[3] = Math.Abs(leftUpY - rightDownY);
            bufGeom.Dispose();
            bufEnve.Dispose();
            return a;
        }
        /// <summary>
        /// 获取Feature的最大值和最小值 范围不准
        /// </summary>
        /// <param name="dsmDs"></param>
        /// <param name="oriFeat"></param>
        /// <param name="bufFeat"></param>
        /// <param name="subRasterOff_Size"></param>
        private static void getMaxMinValue(OSGeo.GDAL.Dataset dsmDs, OSGeo.OGR.Feature oriFeat, OSGeo.OGR.Feature bufFeat, int[] subRasterOff_Size)
        {
            //拿到当前Feature对应的栅格值（数组）
            double[] rastValue = new double[subRasterOff_Size[2] * subRasterOff_Size[3]];

            //把SUBimg的值读进数组
            lock (dsmDs)
            {
                dsmDs.GetRasterBand(1).ReadRaster(
                subRasterOff_Size[0],
                subRasterOff_Size[1],
                subRasterOff_Size[2],
                subRasterOff_Size[3],
                rastValue,
                subRasterOff_Size[2],
                subRasterOff_Size[3],
                0, 0);
            }
            double max = rastValue.Max();
            double min = rastValue.Min();
            double cay = max - min;

            oriFeat.SetField("TEMP", max - min);
            oriFeat.SetField("MIN", 0);
            oriFeat.SetField("MAX", min);
            oriFeat.SetField("HIGHT", max);
        }
        /// <summary>
        /// 以前的代码
        /// </summary>
        /// <param name="dsmDs"></param>
        /// <param name="oriFeat"></param>
        /// <param name="bufFeat"></param>
        /// <param name="subRasterOff_Size"></param>
        private static void getMaxMinValue_(OSGeo.GDAL.Dataset dsmDs, OSGeo.OGR.Feature oriFeat, OSGeo.OGR.Feature bufFeat, int[] subRasterOff_Size)
        {
            //拿到当前Feature对应的栅格值（数组）
            double[] rastValue = new double[subRasterOff_Size[2] * subRasterOff_Size[3]];
            //拿到全局Transfrom
            double[] Transfrom = new double[6];
            dsmDs.GetGeoTransform(Transfrom);

            //把SUBimg的值读进数组
            dsmDs.GetRasterBand(1).ReadRaster(
                subRasterOff_Size[0],
                subRasterOff_Size[1],
                subRasterOff_Size[2],
                subRasterOff_Size[3],
                rastValue,
                subRasterOff_Size[2],
                subRasterOff_Size[3],
                0, 0);

            OSGeo.OGR.Geometry oriGeom = oriFeat.GetGeometryRef();
            string ddd = oriFeat.GetFieldAsString("MAX");
            while (oriFeat.GetFieldAsString("MAX") == "")
            {
                //拿到数组中最大值的索引
                int maxId = Array.IndexOf(rastValue, rastValue.Max());

                //通过索引拿到空间坐标
                double maxX, maxY;
                StaticTools.indexToGeoSpace(maxId, subRasterOff_Size, Transfrom, out maxX, out maxY);

                //把空间坐标写进Geomtry
                OSGeo.OGR.Geometry maxGeom = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbPoint);
                maxGeom.AddPoint_2D(maxX, maxY);

                //判断坐标是否在Feature中，在则把值写入对应字段，不在则把当前值改为平均值

                if (maxGeom.Within(oriGeom))
                {
                    oriFeat.SetField("MAX", rastValue.Max());
                }
                else
                {
                    rastValue[maxId] = rastValue.Average();
                }
            }
            string dd = oriFeat.GetFieldAsString("MIN");
            while (oriFeat.GetFieldAsString("MIN") == "")
            {
                //拿到数组中最小值的索引
                double ffdf = rastValue.Min();
                int minId = Array.IndexOf(rastValue, rastValue.Min());

                //通过索引拿到空间坐标
                double minX, minY;
                StaticTools.indexToGeoSpace(minId, subRasterOff_Size, Transfrom, out minX, out minY);

                //把空间坐标写进Geomtry
                OSGeo.OGR.Geometry minGeom = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbPoint);
                minGeom.AddPoint_2D(minX, minY);

                //判断坐标是否在Feature中，在则把值写入对应字段，不在则把当前值改为平均值
                OSGeo.OGR.Geometry bufGeom = bufFeat.GetGeometryRef();
                if (minGeom.Within(bufGeom))
                {
                    oriFeat.SetField("MIN", rastValue.Min());
                }
                else
                {
                    rastValue[minId] = rastValue.Average();
                }
            }
            double max = oriFeat.GetFieldAsDouble("MAX");
            double min = oriFeat.GetFieldAsDouble("MIN");
            oriFeat.SetField("HIGHT", max - min);
        }

        #endregion

        #region 最小外接矩形
        static void getMinOutLineFromLayerToLayer(OSGeo.OGR.Layer inLayer)
        {
            shpDataSet.deleteLayerByName("minOutLine");
            OSGeo.OGR.Layer minoutline = shpDataSet.CreateLayer("minOutLine", inLayer.GetSpatialRef(), inLayer.GetGeomType(), null);
            OSGeo.OGR.Feature aFeat = null;

            StaticTools.msgLine("getMinOutline...");
            int featCount = inLayer.GetFeatureCount(0);
            for (int i = 0; i < featCount; i++)
            {
                aFeat = inLayer.GetFeature(i);
                OSGeo.OGR.Geometry oGeometry = aFeat.GetGeometryRef();
                OSGeo.OGR.Geometry ogeo = oGeometry.GetGeometryRef(0);
                QTGeometry qtGeo = new QTGeometry(ogeo);
                minoutline.CreateFeature(qtGeo.GetSMBR());
                StaticTools.progress((i + 1) * 100 / featCount, $"{i} / {featCount}");
            }
            minoutline.Dispose();
            //inLayer.ResetReading();
            //while ((aFeat = inLayer.GetNextFeature()) != null)
            //{
            //    OSGeo.OGR.Geometry oGeometry = aFeat.GetGeometryRef();
            //    OSGeo.OGR.Geometry ogeo = oGeometry.GetGeometryRef(0);
            //    QTGeometry qtGeo = new QTGeometry(ogeo);
            //    minoutline.CreateFeature(qtGeo.GetSMBR());
            //}

        }
        #endregion
    }
    public partial class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool AllocConsole();
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool FreeConsole();

    }
}
