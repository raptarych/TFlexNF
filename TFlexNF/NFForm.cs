using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using TFlex;
using TFlex.Model;
using TFlex.Model.Model2D;

namespace TFlexNF
{
    public partial class NFForm : Form
    {
        NFTask task;
        private Area SelectedArea;
        private NFItem SelectedItem;
        private int SelectedIndex;

        bool DefaultReflection = false;
        int DefaultRotation = 0;

        public void selectComboNum(ref ComboBox cmb, int id)
        {
            cmb.Text = cmb.Items[id].ToString();
        }

        public void initList()
        {
            listView1.Items.Clear();
            for (int i = 0; i < task.Count(); i++)
            {
                NFItem item = task.GetItem(i);

                ListViewItem l_item = new ListViewItem();
                l_item.Text = item.Name;
                l_item.ImageIndex = 0;
                listView1.Items.Add(l_item);
            }
        }
        public NFForm(NFTask iTask)
        {
            InitializeComponent();
            task = iTask;
            initList();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            task.DefaultReflection = (checkBox1.Checked ? 1 : 0);
            int ic; int.TryParse(textBox3.Text, out ic); task.DefaultItemCount = Math.Max(1, ic);
            task.DefaultRotation = comboBox3.SelectedIndex;
            int dc; int.TryParse(textBox5.Text, out dc); task.DomainCount = Math.Max(1, dc);


            int p2p; int.TryParse(textBox6.Text, out p2p); task.p2p = Math.Max(0, p2p);
            int p2l; int.TryParse(textBox7.Text, out p2l); task.p2l = Math.Max(0, p2l);
            int lx; int.TryParse(textBox1.Text, out lx); task.ListX = Math.Max(0, lx);
            int ly; int.TryParse(textBox2.Text, out ly); task.ListY = Math.Max(0, ly);

            if (lx <= 0 || ly <= 0)
            {
                MessageBox.Show("Задайте размер листа перед экспортом раскроя!");
                return;
            }

            TFlex.Model.Model2D.FolderBrowserDialog browse = new TFlex.Model.Model2D.FolderBrowserDialog();
            browse.Description = "Выберите папку для сохранения задания на раскрой:";
            if (browse.ShowDialog() == DialogResult.OK)
            {
                task.SaveToItems(browse.SelectedPath,checkBox2.Checked);
            }
        }

        private void listView1_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {

                string areaName = listView1.SelectedItems[0].Text;
                SelectedIndex = listView1.SelectedItems[0].Index;
                SelectedItem = task.GetItem(SelectedIndex);

                //Отрисовка превьюшки детали
                Document Doc = TFlex.Application.ActiveDocument;
                SelectedArea = Doc.GetObjectByName(areaName) as Area;
                TFlex.Drawing.Rectangle bound = SelectedArea.BoundRect;
                double Scale = 159 / Math.Max(bound.Width, bound.Height);

                Bitmap img = new Bitmap(160, 160);
                Graphics graph = Graphics.FromImage(img);
                Pen pen = new Pen(Brushes.White);
                graph.DrawRectangle(pen, new Rectangle(0, 0, 159, 159));
                pen = new Pen(Brushes.Black);
                pen.Width = 1;
                for (int cc = 0; cc < SelectedArea.ContourCount; cc++)
                {
                    Contour cont = SelectedArea.GetContour(cc);
                    foreach (var segm in cont)
                    {
                        switch (segm.GeometryType)
                        {
                            case ObjectGeometryType.Line:
                                LineGeometry line = segm.Geometry as LineGeometry;
                                if (line != null)
                                    graph.DrawLine(pen, (float)((line.X1 - bound.Left) * Scale), (float)((bound.Top - line.Y1) * Scale), (float)((line.X2 - bound.Left) * Scale), (float)((bound.Top - line.Y2) * Scale));
                                break;
                            case ObjectGeometryType.Circle:
                                CircleGeometry circle = segm.Geometry as CircleGeometry;
                                if (circle == null) break;
                                double radius = (circle.Radius * Scale);
                                int xc = (int)((circle.CenterX - bound.Left) * Scale);
                                int yc = (int)((bound.Top - circle.CenterY) * Scale);

                                graph.DrawEllipse(pen, new Rectangle((int)(xc - radius), (int)(yc - radius), (int)radius * 2, (int)radius * 2));
                                break;
                            case ObjectGeometryType.CircleArc:
                                CircleArcGeometry cgeom = segm.Geometry as CircleArcGeometry;
                                if (cgeom == null) break;
                                int xc1 = (int)((cgeom.CenterX - bound.Left) * Scale);
                                int yc1 = (int)((bound.Top - cgeom.CenterY) * Scale);
                                radius = (cgeom.Radius * Scale);
                                double[] angles = NFGetGeom.GetArcAngle(cgeom, segm.IsCounterclockwise);
                                double ang = angles[0] * 180 / Math.PI;
                                double ang1 = angles[1] * 180 / Math.PI - 90;
                                graph.DrawArc(pen, (float)(xc1 - radius), (float)(yc1 - radius), (float)(radius * 2), (float)(radius * 2), (float)ang1, (float)ang);
                                break;
                            default:

                                PolylineGeometry geom = segm.Geometry as PolylineGeometry;

                                if (geom == null) break;
                                for (int i = 1; i < geom.Count; i++)
                                {
                                    int x1 = (int) ((geom.GetX(i) - bound.Left) * Scale);
                                    int y1 = (int) ((bound.Top - geom.GetY(i)) * Scale);
                                    int x2 = (int) ((geom.GetX(i - 1) - bound.Left) * Scale);
                                    int y2 = (int) ((bound.Top - geom.GetY(i - 1)) * Scale);
                                    graph.DrawLine(pen, (float) x1, (float) y1, (float) x2, (float) y2);
                                }
                                break;
                        }
                    }
                }

                pictureBox1.Image = img;

                //Задание параметров форме

                label10.Text = $"Размеры детали = {(int) bound.Width} x {(int) bound.Height}";
                label9.Text = $"Количество контуров: {SelectedArea.ContourCount}";

                selectComboNum(ref comboBox2, SelectedItem.Rotation);

                selectComboNum(ref comboBox1, SelectedItem.Reflection);
                selectComboNum(ref comboBox1, SelectedItem.Reflection);

                textBox4.Text = SelectedItem.Count.ToString();


            }
        }

        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (SelectedArea != null)
            {
                SelectedItem.Reflection = comboBox1.SelectedIndex;
                task.SetItem(SelectedIndex, SelectedItem);
            }
        }

        private void comboBox2_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (SelectedArea != null)
            {
                SelectedItem.Rotation = comboBox2.SelectedIndex;
                task.SetItem(SelectedIndex, SelectedItem);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void comboBox3_SelectionChangeCommitted(object sender, EventArgs e)
        {
            task.DefaultRotation = comboBox3.SelectedIndex;
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (SelectedArea != null)
            {
                int c;
                if (int.TryParse(textBox4.Text, out c))
                {
                    SelectedItem.Count = c;
                    task.SetItem(SelectedIndex, SelectedItem);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (SelectedArea != null)
            {
                task.RemoveItem(SelectedIndex);
                initList();
            }
        } 
    }
}
