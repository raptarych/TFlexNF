using System;
using System.Collections.Generic;
using TFlex.Model;
using TFlex.Model.Model2D;
using TFlex.Drawing;


namespace TFlexNF
{
    class NFUtils
    {
        
        public static Document Doc;
        public static void Msg(string M)
        {
            if (Doc != null)
                Doc.Diagnostics.Add(new DiagnosticsMessage(DiagnosticsMessageType.Information, M));
        }

        protected static void cArcToDoubles(CircleArcGeometry cgeom, ref NFContour cont,Rectangle b,bool ccw)
        {
            double sx = cgeom.StartX;
            double sy = cgeom.StartY;

            double ex = cgeom.EndX;
            double ey = cgeom.EndY;

            var angs = GetArcAngle(cgeom);
            double bulge = Math.Tan(angs.Item1 / 4);

            if (ccw)
            {
                bulge = -bulge;
                cont.AddPoint(new NFPoint(sx - b.Left, b.Top - sy, bulge));
                cont.AddPoint(new NFPoint(ex - b.Left, b.Top - ey, bulge));
            }
            else
            {
                cont.AddPoint(new NFPoint(ex - b.Left, b.Top - ey, bulge));
                cont.AddPoint(new NFPoint(sx - b.Left, b.Top - sy, bulge));
            }

        }

        public static Tuple<double, double> GetArcAngle(CircleArcGeometry arc)
        {
            double xb = 0, yb = 0, xm = 0, ym = 0, xe = 0, ye = 0;
            arc.GetThreePoints(ref xe, ref ye, ref xm, ref ym, ref xb, ref yb);

            double xc = arc.CenterX;
            double yc = arc.CenterY;

            double dx1 = xb - xc;
            double dy1 = yb - yc;
            double ang1 = Math.Atan2(dx1, dy1);

            double ang2 = Math.Atan2(xe - xc, ye - yc);
            double sweep = ang2 - ang1;

            if (sweep < 0)
            {
                sweep = 2*Math.PI + sweep;
            }
            return new Tuple<double, double>(sweep, ang1);
        }

        public static NFTask GetGeometry()
        {
            Msg("[Nesting Factory] Starting collect geometry...");

            ICollection<Area> Areas = Doc.GetAreas();

            NFTask task = new NFTask();

            foreach (var area in Areas) {
                Rectangle BoundBox = area.BoundRect;
                double boundX = BoundBox.Left;
                double boundY = BoundBox.Top;


                NFItem item = new NFItem(area.ObjectId.ToString());

                for (int num_contour = 0; num_contour < area.ContourCount; num_contour++)
                {
                    Contour contour = area.GetContour(num_contour);
                    NFContour cont = new NFContour();

                    foreach (var csegment in contour) {

                        switch (csegment.GeometryType)
                        {
                            case ObjectGeometryType.Line:
                                LineGeometry linegeom = csegment.Geometry as LineGeometry;
                                cont.AddPoint(new NFPoint(linegeom.X1 - boundX, boundY - linegeom.Y1, 0));
                                cont.AddPoint(new NFPoint(linegeom.X2 - boundX, boundY - linegeom.Y2, 0));
                                break;
                            case ObjectGeometryType.CircleArc:
                                CircleArcGeometry cgeom = csegment.Geometry as CircleArcGeometry;
                                cArcToDoubles(cgeom, ref cont, BoundBox, csegment.IsCounterclockwise);
                                break;
                            case ObjectGeometryType.Circle:
                                CircleGeometry cirgeom = csegment.Geometry as CircleGeometry;
                                cont.AddPoint(new NFPoint(cirgeom.CenterX + cirgeom.Radius - boundX, boundY - cirgeom.CenterY, 1));
                                cont.AddPoint(new NFPoint(cirgeom.CenterX - cirgeom.Radius - boundX, boundY - cirgeom.CenterY, 1));
                                break;
                            default:
                                PolylineGeometry polygeom = csegment.Geometry as PolylineGeometry;
                                int v_count = polygeom.Count;
                                for (int i = 0; i < v_count; i++)
                                {
                                    if (v_count < 50 || i % (csegment.GeometryType == ObjectGeometryType.Ellipse ? 5 : 1) == 0 || i == v_count)
                                    {
                                        cont.AddPoint(new NFPoint(polygeom.GetX(i) - boundX, boundY - polygeom.GetY(i), 0));
                                    }
                                }
                                break;
                        }
                    }
                    item.AddContour(cont);
                }
                task.AddItem(item);
            }
            Msg("[Nesting Factory] Geometry collected");
            return task;
        }
    }
}
