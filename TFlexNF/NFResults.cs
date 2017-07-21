using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TFlex.Drawing;
using TFlex.Model;
using TFlex.Model.Model2D;

namespace TFlexNF
{
    class NFPolyline
    {
        public int count => points.Count;
        protected int length = 0;
        private List<Point> points = new List<Point>();

        public void AddPoint(double x, double y) => points.Add(new Point(x, y));

        public void Draw(Document Doc, Page p) => new PolylineOutline(Doc, new PolylineGeometry(points)) { Page = p };
    }

    class NFResults
    {
        public static void Start()
        {
            OpenFileDialog OpenDialog = new OpenFileDialog();
            Document Doc = TFlex.Application.ActiveDocument;

            OpenDialog.FilterIndex = 1;
            OpenDialog.Filter = "Nesting Factory results (*.nres)|*.nres";
            OpenDialog.RestoreDirectory = true;
            if (OpenDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StreamReader myStream;
                    if ((myStream = new StreamReader(OpenDialog.FileName)) != null)
                    {
                        using (myStream)
                        {
                            Doc.BeginChanges("Вывод геометрии");
                            Process(myStream);
                            myStream.Close();
                            Doc.EndChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Невозможно прочитать файл, подробнее: " + ex.Message);
                }
            }
        }

        protected static void Process(StreamReader filestream)
        {
            Document Doc = TFlex.Application.ActiveDocument;
            NFUtils.Doc = Doc;

            Page p = new Page(Doc);
            p.Name = "NFResult";
            p.FontStyle.Italic = true;

            int contour_count = 0;
            string line = "";
            NFPolyline Poly = new NFPolyline();

            double y_offset = 0;

            do
            {
                line = filestream.ReadLine();
                var split = line.Split(' ').ToList();
                bool processing = false;
                double firstX = -1.337;
                double firstY = -1.337;

                if (line != null && split.Count == 2)
                {
                    processing = true;
                    contour_count++;
                    NFUtils.Msg("Contour: " + contour_count);

                    do
                    {
                        line = filestream.ReadLine();
                        split = line.Split(' ').ToList();

                        if (split.Count > 1)
                        {
                            if (split.Count == 3)
                            {
                                int bulge;
                                int.TryParse(split[2], out bulge);
                                NFUtils.Msg(line);

                                double x, y;
                                double.TryParse(split[0].Replace('.', ','), out x);
                                double.TryParse(split[1].Replace('.', ','), out y);

                                if (contour_count == 1)
                                {
                                    y_offset = Math.Max(y_offset, y);
                                }
                                else
                                {
                                    y = y_offset - y;
                                }

                                if (firstX == -1.337)
                                {
                                    firstX = x;
                                    firstY = y;
                                }
                                Poly.AddPoint(x, y);
                            }

                            if (split.Count == 5)
                            {

                                NFUtils.Msg($"ARC: {line}");
                                double x, y, radius, ang1, ang2;
                                double.TryParse(split[0].Replace('.', ','), out x);
                                double.TryParse(split[1].Replace('.', ','), out y);
                                double.TryParse(split[2].Replace('.', ','), out radius);
                                double.TryParse(split[3].Replace('.', ','), out ang1);
                                double.TryParse(split[4].Replace('.', ','), out ang2);
                                radius = radius / 2;

                                double x1 = Math.Cos(-ang1 / 180 * Math.PI) * radius + x + radius;
                                double y1 = Math.Sin(-ang1 / 180 * Math.PI) * radius + y + radius;

                                double x2 = Math.Cos((-ang1 - ang2 / 2) / 180 * Math.PI) * radius + x + radius;
                                double y2 = Math.Sin((-ang1 - ang2 / 2) / 180 * Math.PI) * radius + y + radius;

                                double x3 = Math.Cos((-ang1 - ang2) / 180 * Math.PI) * radius + x + radius;
                                double y3 = Math.Sin((-ang1 - ang2) / 180 * Math.PI) * radius + y + radius;

                                y1 = y_offset - y1;
                                y2 = y_offset - y2;
                                y3 = y_offset - y3;

                                FreeNode fn1 = new FreeNode(Doc, x1, y1);
                                FreeNode fn2 = new FreeNode(Doc, x2, y2);
                                FreeNode fn3 = new FreeNode(Doc, x3, y3);

                                if (firstX == -1.337)
                                {
                                    firstX = x1;
                                    firstY = y1;
                                }

                                if (Poly.count > 0)
                                {
                                    Poly.AddPoint(x1, y1);
                                    Poly.Draw(Doc, p);
                                    Poly = new NFPolyline();

                                }
                                Poly.AddPoint(x3, y3);
                                ThreePointArcOutline Arc = new ThreePointArcOutline(Doc, fn1, fn2, fn3);
                                Arc.Page = p;

                            }
                        }
                    } while (line != null & split.Count > 1);
                }

                if (processing & Poly.count > 0)
                {
                    NFUtils.Msg("INIT POLYLINE");
                    Poly.AddPoint(firstX, firstY);
                    Poly.Draw(Doc, p);
                    Poly = new NFPolyline();
                }
                if (filestream.EndOfStream) break;
            } while (true);
        }
    }
}