using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test01
{
    public class BasicUnit
    {
        int xOff, yOff;
        int xSize, ySize;
        double importLevel, nodataValue;
        double[] bufferArr, slopeBuffer;
        double[] OrGeoTransform;

        bool[,] Bevisited;
        List<OrderPoint> _resPntList;
        List<OrderPoint> _resPntList_unClock;

        public BasicUnit(int xoff, int yoff, int xsize, int ysize, double importlvl, double[] buffer, double[] slopeBuffer, double[] orGeoTransform, double nodataValue)
        {
            this.xOff = xoff;
            this.yOff = yoff;
            this.xSize = xsize;
            this.ySize = ysize;
            this.importLevel = importlvl;
            this.bufferArr = buffer;
            this.slopeBuffer = slopeBuffer;
            this.OrGeoTransform = goeTransformOff(orGeoTransform, xOff, yOff);
            this.nodataValue = nodataValue;
            Bevisited = new bool[xsize, ysize];
        }
        /*
        public void Identify(string polygonShpPath, string lineShpPath)
        {
            for (int _j = 0; _j < ySize; _j++)
            {
                for (int _i = 0; _i < xSize; _i++)
                {
                    if (IsImportPnt(slopeBuffer, _i, _j, importLevel) && !Bevisited[_i, _j])
                    {
                        _resPntList = new List<OrderPoint>();
                        _resPntList_unClock = new List<OrderPoint>();

                        OrderPoint _orStartPnt = GetStartPnt(xSize, ySize, _i, _j);
                        OrderPoint _orSecondPnt = GetSecondPnt2(bufferArr, slopeBuffer, ref _orStartPnt);
                        if (_orStartPnt != null)
                        {
                            Bevisited[_orStartPnt.X, _orStartPnt.Y] = true;
                        }
                        if (_orSecondPnt != null)
                        {
                            Bevisited[_orSecondPnt.X, _orSecondPnt.Y] = true;
                        }
                        if (_orStartPnt != null && _orSecondPnt != null)
                        {
                            int _relaType = ((int)_orStartPnt.Direct + 4) & 7;
                            _resPntList = GetOnePolygon(_orStartPnt, _orSecondPnt);
                            _orSecondPnt.Direct = (Toward)_relaType;
                            _resPntList_unClock = GetOnePolygon_UnClock(_orSecondPnt, _orStartPnt);
                        }
                        if (_resPntList.Count > 1 && _resPntList_unClock.Count > 0)
                        {
                            WriteShp(polygonShpPath, lineShpPath, _resPntList, _resPntList_unClock, _orStartPnt, _orSecondPnt);
                        }
                    }
                    Bevisited[_i, _j] = true;
                }
            }
        }
        */
        public List<Geometry> Identify2()
        {
            List<Geometry> li = new List<Geometry>();
            for (int _j = 0; _j < ySize; _j++)
            {
                for (int _i = 0; _i < xSize; _i++)
                {

                    if (IsImportPnt(slopeBuffer, _i, _j, importLevel) && !Bevisited[_i, _j])
                    {
                        _resPntList = new List<OrderPoint>();
                        _resPntList_unClock = new List<OrderPoint>();

                        OrderPoint _orStartPnt = GetStartPnt(xSize, ySize, _i, _j);
                        OrderPoint _orSecondPnt = GetSecondPnt2(bufferArr, slopeBuffer, ref _orStartPnt);

                        if (_orStartPnt == null)
                        {
                            continue;
                        }
                        else if (_orStartPnt != null && _orSecondPnt == null)
                        {
                            Bevisited[_orStartPnt.X, _orStartPnt.Y] = true;
                            continue;
                        }
                        else if (_orStartPnt != null && _orSecondPnt != null)
                        {

                            Bevisited[_orStartPnt.X, _orStartPnt.Y] = true;
                            Bevisited[_orSecondPnt.X, _orSecondPnt.Y] = true;
                            int _relaType = ((int)_orStartPnt.Direct + 4) & 7;
                            _resPntList = GetOnePolygon(_orStartPnt, _orSecondPnt);
                            _orSecondPnt.Direct = (Toward)_relaType;
                            _resPntList_unClock = GetOnePolygon_UnClock(_orSecondPnt, _orStartPnt);
                        }
                        if (_resPntList.Count > 0 && _resPntList_unClock.Count > 0)
                        {
                            Geometry preGeo = GetGeometry_SelfPolygon(_resPntList);
                            if (preGeo != null)
                            {
                                li.Add(preGeo);
                                continue;
                            }
                            preGeo = GetGeometry_SelfPolygon(_resPntList_unClock);
                            if (preGeo != null)
                            {
                                li.Add(preGeo);
                                continue;
                            }
                            Geometry geo = GetGeometry(_resPntList, _resPntList_unClock, _orStartPnt, _orSecondPnt);
                            if (geo != null)
                            {
                                li.Add(geo);
                            }
                        }
                    }
                    Bevisited[_i, _j] = true;
                }
            }
            return li;
        }
        /*
        private void WriteShp(string polygonShpPath, string lineShpPath, List<OrderPoint> list, List<OrderPoint> list_unClock, OrderPoint _orStartPnt, OrderPoint _orSecondPnt)
        {
            DataSource polygonDs = Ogr.Open(polygonShpPath, 1);
            DataSource polylineDs = Ogr.Open(lineShpPath, 1);
            Layer polygonLyr = polygonDs.GetLayerByIndex(0);
            Layer polylineLyr = polylineDs.GetLayerByIndex(0);

            double _disOf2Pnt = DistanceBetweenPnts(_resPntList[_resPntList.Count - 1], _resPntList_unClock[_resPntList_unClock.Count - 1]);
            if (_disOf2Pnt < Math.Sqrt(51) && (_resPntList.Count + _resPntList_unClock.Count) > 50)
            {
                //新建一个polygon对象
                Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
                foreach (OrderPoint _myPoint in _resPntList_unClock)
                {
                    double _x = OrGeoTransform[0] + (_myPoint.X + 0.5) * OrGeoTransform[1] + (_myPoint.Y + 0.5) * OrGeoTransform[2];
                    double _y = OrGeoTransform[3] + (_myPoint.X + 0.5) * OrGeoTransform[4] + (_myPoint.Y + 0.5) * OrGeoTransform[5];
                    ring.AddPoint(_x, _y, 0);
                }
                for (int i = (_resPntList.Count - 1); i > 0; i--)
                {
                    double _x = OrGeoTransform[0] + (_resPntList[i].X + 0.5) * OrGeoTransform[1] + (_resPntList[i].Y + 0.5) * OrGeoTransform[2];
                    double _y = OrGeoTransform[3] + (_resPntList[i].X + 0.5) * OrGeoTransform[4] + (_resPntList[i].Y + 0.5) * OrGeoTransform[5];
                    ring.AddPoint(_x, _y, 0);
                }
                double xt = OrGeoTransform[0] + (_orSecondPnt.X + 0.5) * OrGeoTransform[1] + (_orSecondPnt.Y + 0.5) * OrGeoTransform[2];
                double yt = OrGeoTransform[3] + (_orSecondPnt.X + 0.5) * OrGeoTransform[4] + (_orSecondPnt.Y + 0.5) * OrGeoTransform[5];
                ring.AddPoint(xt, yt, 0);
                xt = OrGeoTransform[0] + (_orStartPnt.X + 0.5) * OrGeoTransform[1] + (_orStartPnt.Y + 0.5) * OrGeoTransform[2];
                yt = OrGeoTransform[3] + (_orStartPnt.X + 0.5) * OrGeoTransform[4] + (_orStartPnt.Y + 0.5) * OrGeoTransform[5];
                ring.AddPoint(xt, yt, 0);

                Geometry Geo_poly = new Geometry(polygonLyr.GetGeomType());
                Geo_poly.AddGeometryDirectly(ring);
                Feature oFea = new Feature(polygonLyr.GetLayerDefn());
                oFea.SetGeometry(Geo_poly);
                polygonLyr.CreateFeature(oFea);
            }
            else if ((_resPntList.Count + _resPntList_unClock.Count) > 40)
            {
                //新建一个polyline对象
                Geometry line = new Geometry(wkbGeometryType.wkbLineString);
                for (int i = _resPntList.Count - 1; i > 0; i--)
                {
                    double _x = OrGeoTransform[0] + (_resPntList[i].X + 0.5) * OrGeoTransform[1] + (_resPntList[i].Y + 0.5) * OrGeoTransform[2];
                    double _y = OrGeoTransform[3] + (_resPntList[i].X + 0.5) * OrGeoTransform[4] + (_resPntList[i].Y + 0.5) * OrGeoTransform[5];
                    line.AddPoint(_x, _y, 0);
                }
                foreach (OrderPoint _myPoint in _resPntList_unClock)
                {
                    double _x = OrGeoTransform[0] + (_myPoint.X + 0.5) * OrGeoTransform[1] + (_myPoint.Y + 0.5) * OrGeoTransform[2];
                    double _y = OrGeoTransform[3] + (_myPoint.X + 0.5) * OrGeoTransform[4] + (_myPoint.Y + 0.5) * OrGeoTransform[5];
                    line.AddPoint(_x, _y, 0);
                }

                Feature oFeature = new Feature(polylineLyr.GetLayerDefn());
                oFeature.SetGeometry(line);
                polylineLyr.CreateFeature(oFeature);
            }
            polygonLyr.Dispose();
            polylineLyr.Dispose();
            polygonDs.Dispose();
            polylineDs.Dispose();
        }
        */
        public Geometry GetGeometry(List<OrderPoint> list, List<OrderPoint> list_unClock, OrderPoint _orStartPnt, OrderPoint _orSecondPnt)
        {
            double _disOf2Pnt = DistanceBetweenPnts(list[list.Count - 1], list_unClock[list_unClock.Count - 1]);
            if (_disOf2Pnt < Math.Sqrt(51) && (list.Count + list_unClock.Count) > 50)
            {
                //新建一个polygon对象
                Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
                foreach (OrderPoint _myPoint in list_unClock)
                {
                    double _x = OrGeoTransform[0] + (_myPoint.X + 0.5) * OrGeoTransform[1] + (_myPoint.Y + 0.5) * OrGeoTransform[2];
                    double _y = OrGeoTransform[3] + (_myPoint.X + 0.5) * OrGeoTransform[4] + (_myPoint.Y + 0.5) * OrGeoTransform[5];
                    ring.AddPoint(_x, _y, 0);
                }
                for (int i = (list.Count - 1); i > 0; i--)
                {
                    double _x = OrGeoTransform[0] + (list[i].X + 0.5) * OrGeoTransform[1] + (list[i].Y + 0.5) * OrGeoTransform[2];
                    double _y = OrGeoTransform[3] + (list[i].X + 0.5) * OrGeoTransform[4] + (list[i].Y + 0.5) * OrGeoTransform[5];
                    ring.AddPoint(_x, _y, 0);
                }
                double xt = OrGeoTransform[0] + (_orSecondPnt.X + 0.5) * OrGeoTransform[1] + (_orSecondPnt.Y + 0.5) * OrGeoTransform[2];
                double yt = OrGeoTransform[3] + (_orSecondPnt.X + 0.5) * OrGeoTransform[4] + (_orSecondPnt.Y + 0.5) * OrGeoTransform[5];
                ring.AddPoint(xt, yt, 0);
                xt = OrGeoTransform[0] + (_orStartPnt.X + 0.5) * OrGeoTransform[1] + (_orStartPnt.Y + 0.5) * OrGeoTransform[2];
                yt = OrGeoTransform[3] + (_orStartPnt.X + 0.5) * OrGeoTransform[4] + (_orStartPnt.Y + 0.5) * OrGeoTransform[5];
                ring.AddPoint(xt, yt, 0);
                Geometry polygon = new Geometry(wkbGeometryType.wkbPolygon);
                polygon.AddGeometry(ring);
                return polygon;
            }
            else if ((list.Count + list_unClock.Count) > 40)
            {
                //新建一个polyline对象
                Geometry line = new Geometry(wkbGeometryType.wkbLineString);
                for (int i = list.Count - 1; i > 0; i--)
                {
                    double _x = OrGeoTransform[0] + (list[i].X + 0.5) * OrGeoTransform[1] + (list[i].Y + 0.5) * OrGeoTransform[2];
                    double _y = OrGeoTransform[3] + (list[i].X + 0.5) * OrGeoTransform[4] + (list[i].Y + 0.5) * OrGeoTransform[5];
                    line.AddPoint(_x, _y, 0);
                }
                foreach (OrderPoint _myPoint in list_unClock)
                {
                    double _x = OrGeoTransform[0] + (_myPoint.X + 0.5) * OrGeoTransform[1] + (_myPoint.Y + 0.5) * OrGeoTransform[2];
                    double _y = OrGeoTransform[3] + (_myPoint.X + 0.5) * OrGeoTransform[4] + (_myPoint.Y + 0.5) * OrGeoTransform[5];
                    line.AddPoint(_x, _y, 0);
                }

                return line;
            }
            return null;
        }
        public Geometry GetGeometry_SelfPolygon(List<OrderPoint> list)
        {
            List<OrderPoint> li = LineSelfSect.Instance.RingedList(list);
            if (li == null)
                return null;
            else
            {
                //新建一个polygon对象
                Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
                foreach (OrderPoint _myPoint in li)
                {
                    double _x = OrGeoTransform[0] + (_myPoint.X + 0.5) * OrGeoTransform[1] + (_myPoint.Y + 0.5) * OrGeoTransform[2];
                    double _y = OrGeoTransform[3] + (_myPoint.X + 0.5) * OrGeoTransform[4] + (_myPoint.Y + 0.5) * OrGeoTransform[5];
                    ring.AddPoint(_x, _y, 0);
                }
                double _xx = OrGeoTransform[0] + (li[0].X + 0.5) * OrGeoTransform[1] + (li[0].Y + 0.5) * OrGeoTransform[2];
                double _yy = OrGeoTransform[3] + (li[0].X + 0.5) * OrGeoTransform[4] + (li[0].Y + 0.5) * OrGeoTransform[5];
                ring.AddPoint(_xx, _yy, 0);
                Geometry polygon = new Geometry(wkbGeometryType.wkbPolygon);
                polygon.AddGeometry(ring);
                return polygon;
            }
        }

        private bool IsImportPnt(double[] slopeBuf, int X, int Y, double ImportLevel)
        {
            if (slopeBuf[Y * xSize + X] >= ImportLevel)
            {
                return true;
            }
            else
                return false;
        }

        private OrderPoint GetStartPnt(int xSize, int ySize, int X, int Y)
        {
            double _tempReadVal = nodataValue;
            BlockType _blockType;
            double[] _nbhVal;//GrayDs邻域的值 
            double[] _nbhVal_Slp;//SlopeDs邻域的值
            OrderPoint _resPnt = new OrderPoint(X, Y);

            _blockType = BasicStruct.Instance.GetBlockType(xSize, ySize, X, Y);
            _nbhVal = BasicStruct.Instance.GetNBHval(bufferArr, X, Y, xSize, ySize, _blockType);
            _nbhVal_Slp = BasicStruct.Instance.GetNBHval(slopeBuffer, X, Y, xSize, ySize, _blockType);

            #region
            for (int i = 0; i < _nbhVal_Slp.Length; i++)
            {
                if (_blockType == BlockType.Angular_LeftTop)
                {
                    if (_nbhVal_Slp[i] >= importLevel && _nbhVal[i] > _tempReadVal)
                    {
                        _resPnt.X = X + i % 2;
                        _resPnt.Y = Y + i / 2;
                    }
                }
                else if (_blockType == BlockType.Angular_LeftBot)
                {
                    if (_nbhVal_Slp[i] >= importLevel && _nbhVal[i] > _tempReadVal)
                    {
                        _resPnt.X = X + i % 2;
                        _resPnt.Y = Y - 1 + i / 2;
                    }
                }
                else if (_blockType == BlockType.Angular_RightTop)
                {
                    if (_nbhVal_Slp[i] >= importLevel && _nbhVal[i] > _tempReadVal)
                    {
                        _resPnt.X = X - 1 + i % 2;
                        _resPnt.Y = Y + i / 2;
                    }
                }
                else if (_blockType == BlockType.Angular_RightBot)
                {
                    if (_nbhVal_Slp[i] >= importLevel && _nbhVal[i] > _tempReadVal)
                    {
                        _resPnt.X = X - 1 + i % 2;
                        _resPnt.Y = Y - 1 + i / 2;
                    }
                }

                else if (_blockType == BlockType.Edge_Left)
                {
                    if (_nbhVal_Slp[i] >= importLevel && _nbhVal[i] > _tempReadVal)
                    {
                        _resPnt.X = X + i % 2;
                        _resPnt.Y = Y - 1 + i / 2;
                    }
                }
                else if (_blockType == BlockType.Edge_Right)
                {
                    if (_nbhVal_Slp[i] >= importLevel && _nbhVal[i] > _tempReadVal)
                    {
                        _resPnt.X = X - 1 + i % 2;
                        _resPnt.Y = Y - 1 + i / 2;
                    }
                }
                else if (_blockType == BlockType.Edge_Top)
                {
                    if (_nbhVal_Slp[i] >= importLevel && _nbhVal[i] > _tempReadVal)
                    {
                        _resPnt.X = X - 1 + i % 3;
                        _resPnt.Y = Y + i / 3;
                    }
                }
                else if (_blockType == BlockType.Edge_Bot)
                {
                    if (_nbhVal_Slp[i] >= importLevel && _nbhVal[i] > _tempReadVal)
                    {
                        _resPnt.X = X - 1 + i % 3;
                        _resPnt.Y = Y - 1 + i / 3;
                    }
                }
                else
                {
                    if (_nbhVal_Slp[i] >= importLevel && _nbhVal[i] > _tempReadVal)
                    {
                        _resPnt.X = X - 1 + i % 3;
                        _resPnt.Y = Y - 1 + i / 3;
                    }
                }
            }
            #endregion
            return _resPnt;
        }
        private OrderPoint GetSecondPnt2(double[] arr, double[] slopeArr, ref OrderPoint FirstPnt)
        {
            if (FirstPnt == null)
                return null;

            double tempValue = -100000;
            OrderPoint _resPnt = null;
            BlockType _blkType;
            double[] _nbhVal;//GrayDs邻域的值 
            double[] _nbhVal_Slp;//SlopeDs邻域的值

            _blkType = BasicStruct.Instance.GetBlockType(xSize, ySize, FirstPnt.X, FirstPnt.Y);

            if (_blkType == BlockType.Center)
            {
                _nbhVal = BasicStruct.Instance.GetNBHval(arr, FirstPnt.X, FirstPnt.Y, xSize, ySize, _blkType);
                _nbhVal_Slp = BasicStruct.Instance.GetNBHval(slopeArr, FirstPnt.X, FirstPnt.Y, xSize, ySize, _blkType);

                _nbhVal = BasicStruct.Instance.Clockwise_Nbhd(_nbhVal, _blkType);
                _nbhVal_Slp = BasicStruct.Instance.Clockwise_Nbhd(_nbhVal_Slp, _blkType);

                for (int i = 0; i < 8; i++)
                {
                    if (_nbhVal[i] > tempValue && _nbhVal_Slp[i] >= importLevel
                        && _nbhVal[(i - 1) & 7] > _nbhVal[(i + 1) & 7])
                    {
                        _resPnt = BasicStruct.Instance.ReflectMyPoint(i, FirstPnt);
                        FirstPnt.Direct = (Toward)i;
                    }
                }
            }
            return _resPnt;
        }

        private List<OrderPoint> GetOnePolygon(OrderPoint _stPnt, OrderPoint _ndPnt)
        {
            List<OrderPoint> _resPntList = new List<OrderPoint>();
            _resPntList.Add(_ndPnt);

            if ((int)BasicStruct.Instance.GetBlockType(xSize, ySize, _ndPnt.X, _ndPnt.Y) != 9)
                return _resPntList;

            OrderPoint _rdPnt = GetNextPnt(_stPnt, _ndPnt);

            if (_rdPnt == null || Bevisited[_rdPnt.X, _rdPnt.Y])
            {
                return _resPntList;
            }
            else if (_rdPnt != null)
            {
                Bevisited[_rdPnt.X, _rdPnt.Y] = true;
                _resPntList.AddRange(GetOnePolygon(_ndPnt, _rdPnt));
            }
            return _resPntList;
        }
        private OrderPoint GetNextPnt(OrderPoint AheadPnt, OrderPoint MidPnt)
        {
            double[] eight_Nbhd_Val = new double[8];
            double[] eight_Nbhd_Slp = new double[8];
            double[] readValBuffer = new double[9];//以MidPnt为中心点读取（栅格像素图）九邻域
            double[] readSlpBuffer = new double[9];//以MidPnt为中心点读取（slope图）九邻域
            OrderPoint nextPnt = new OrderPoint();
            readValBuffer = BasicStruct.Instance.ReadArr(bufferArr, xSize, MidPnt.X - 1, MidPnt.Y - 1, 3, 3);
            readSlpBuffer = BasicStruct.Instance.ReadArr(slopeBuffer, xSize, MidPnt.X - 1, MidPnt.Y - 1, 3, 3);

            eight_Nbhd_Slp = BasicStruct.Instance.Clockwise_Nbhd(readSlpBuffer, (BlockType)9);
            eight_Nbhd_Val = BasicStruct.Instance.Clockwise_Nbhd(readValBuffer, (BlockType)9);

            int startVal;
            int time;
            if ((int)(AheadPnt.Direct) % 2 == 1)
            {
                startVal = (int)AheadPnt.Direct - 1;
                time = 3;
            }
            else
            {
                startVal = ((int)(AheadPnt.Direct) - 2) & 7;
                time = 5;
            }
            double tempSlope = -1;
            for (int i = 0; i < time; i++)
            {
                int _index = (startVal + i) & 7;
                if (eight_Nbhd_Slp[_index] > importLevel && eight_Nbhd_Slp[_index] > tempSlope && eight_Nbhd_Val[(_index - 1) & 7] > eight_Nbhd_Val[(_index + 1) & 7])
                {
                    nextPnt = BasicStruct.Instance.ReflectMyPoint(_index, MidPnt);
                    MidPnt.Direct = (Toward)_index;
                    tempSlope = eight_Nbhd_Slp[_index];
                }
            }
            if (Bevisited[nextPnt.X, nextPnt.Y] || tempSlope < 0)
            {
                //Bevisited[nextPnt.X, nextPnt.Y] = true;
                return null;
            }
            //更新Visited表
            //Bevisited[nextPnt.X, nextPnt.Y] = true;
            return nextPnt;
        }

        private List<OrderPoint> GetOnePolygon_UnClock(OrderPoint _stPnt, OrderPoint _ndPnt)
        {
            List<OrderPoint> _resPntList = new List<OrderPoint>();
            _resPntList.Add(_ndPnt);

            if ((int)BasicStruct.Instance.GetBlockType(xSize, ySize, _ndPnt.X, _ndPnt.Y) != 9)
                return _resPntList;

            OrderPoint _rdPnt = GetNextPnt_UnClock(_stPnt, _ndPnt);

            if (_rdPnt == null || Bevisited[_rdPnt.X, _rdPnt.Y])
            {
                return _resPntList;
            }
            else if (_rdPnt != null)
            {
                Bevisited[_rdPnt.X, _rdPnt.Y] = true;
                _resPntList.AddRange(GetOnePolygon_UnClock(_ndPnt, _rdPnt));
            }
            return _resPntList;
        }
        private OrderPoint GetNextPnt_UnClock(OrderPoint AheadPnt, OrderPoint MidPnt)
        {
            double[] eight_Nbhd_Val = new double[8];
            double[] eight_Nbhd_Slp = new double[8];
            double[] readValBuffer = new double[9];//以MidPnt为中心点读取（栅格像素图）九邻域
            double[] readSlpBuffer = new double[9];//以MidPnt为中心点读取（slope图）九邻域
            OrderPoint nextPnt = new OrderPoint();
            readValBuffer = BasicStruct.Instance.ReadArr(bufferArr, xSize, MidPnt.X - 1, MidPnt.Y - 1, 3, 3);
            readSlpBuffer = BasicStruct.Instance.ReadArr(slopeBuffer, xSize, MidPnt.X - 1, MidPnt.Y - 1, 3, 3);

            eight_Nbhd_Slp = BasicStruct.Instance.Clockwise_Nbhd(readSlpBuffer, (BlockType)9);
            eight_Nbhd_Val = BasicStruct.Instance.Clockwise_Nbhd(readValBuffer, (BlockType)9);

            int startVal;
            int time;
            if ((int)(AheadPnt.Direct) % 2 == 1)
            {
                startVal = ((int)AheadPnt.Direct + 1) & 7;
                time = 3;
            }
            else
            {
                startVal = ((int)(AheadPnt.Direct) + 2) & 7;
                time = 5;
            }
            double tempSlope = 0;
            for (int i = 0; i < time; i++)
            {
                int _index = (startVal - i) & 7;
                if (eight_Nbhd_Slp[_index] > tempSlope && eight_Nbhd_Val[(_index - 1) & 7] < eight_Nbhd_Val[(_index + 1) & 7])
                {
                    nextPnt = BasicStruct.Instance.ReflectMyPoint(_index, MidPnt);
                    MidPnt.Direct = (Toward)_index;
                    tempSlope = eight_Nbhd_Slp[_index];
                }
            }

            if (Bevisited[nextPnt.X, nextPnt.Y])
            {
                //Bevisited[nextPnt.X, nextPnt.Y] = true;
                return null;
            }
            //更新Visited表
            //Bevisited[nextPnt.X, nextPnt.Y] = true;
            return nextPnt;
        }

        private double[] goeTransformOff(double[] geoTransform, int xoff, int yoff)
        {
            double[] arr = new double[6];
            arr = geoTransform;
            arr[0] = geoTransform[0] + xoff * geoTransform[1];
            arr[3] = geoTransform[3] + yoff * geoTransform[5];
            return arr;
        }

        private double DistanceBetweenPnts(OrderPoint Point1, OrderPoint Point2)
        {
            double _dis = 0;
            _dis = Math.Sqrt((Point1.X - Point2.X) * (Point1.X - Point2.X) + (Point1.Y - Point2.Y) * (Point1.Y - Point2.Y));
            return _dis;
        }
    }
}
