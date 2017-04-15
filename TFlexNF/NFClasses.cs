using System;
using System.IO;
using System.Collections.Generic;

namespace TFlexNF
{
    public enum NFRotation
    {
        None = 0,
        Pi = 1,
        HalfPi = 2,
        Free = 3,
    }

    public class NFPoint
    {
        public double X, Y, B;
        public NFPoint(double x, double y, double b)
        {
            X = Math.Round(x, 1);
            Y = Math.Round(y, 1);
            B = Math.Round(b, 3);
        }
    }

    public class NFContour
    {
        public readonly List<NFPoint> Points = new List<NFPoint>();

        public void AddPoint(NFPoint Point)
        {
            Points.Add(Point);
        }

        public int VertexCount()
        {
            return Points.Count;
        }
    }

    public class NFItem
    {
        public int Rotation = 0;
        public int Reflection = 0;
        public readonly List<NFContour> Contours = new List<NFContour>();
        public string Name = "";
        public int Count = 0;

        public NFItem(string name)
        {
            Name = name;
        }

        public void AddContour(NFContour contour)
        {
            Contours.Add(contour);
        }
    }

    public class NFTask
    {

        public readonly List<NFItem> Items = new List<NFItem>();
        public int DomainCount = 1;
        public int DefaultRotation;
        public int DefaultReflection;
        public int DefaultItemCount;
        public int p2p = 5;
        public int p2l = 5;

        public int ListX;
        public int ListY;

        public void AddItem(NFItem item)
        {
            Items.Add(item);
        }

        public NFItem GetItem(int id)
        {
            return Items[id];
        }

        public void SetItem(int id, NFItem item)
        {
            Items[id] = item;
        }

        public void RemoveItem(int id)
        {
            for (int i = 0; i < Items.Count-id-1; i++)
            {
                Items[id + i] = Items[id + i + 1];
            }
            Items.RemoveAt(Items.Count - 1);
        }

        public int Count()
        {
            return Items.Count;
        }

        public void SaveToItems(string filePath, bool toCatAgent)
        {
            string taskfile = "TASKNAME:\tnest\nTIMELIMIT:\t3600000\nTASKTYPE:\tSheet\n";
            if (toCatAgent)
            {
                taskfile += $"DOMAINFILE:\t{Items.Count}.item\n";
            } else
            {
                taskfile += $"WIDTH:\t{ListY}\nLENGTH:\t{ListX}\n";
            }
            
            taskfile += $"SHEETQUANT:\t{DomainCount}\n";
            taskfile += $"ITEM2DOMAINDIST:\t{p2l}\n";
            taskfile += $"ITEM2ITEMDIST:\t{p2p}\n";


            var itemId = 0;
            foreach (var item in Items)
            {
                string fileData = $"ITEMNAME:\t{item.Name}\n";

                var rot = (NFRotation) (item.Rotation == 0 ? DefaultRotation : item.Rotation - 1);
                string rotstep = "";
                switch (rot)
                {
                    case NFRotation.None:
                        rotstep = "NO";
                        break;
                    case NFRotation.Pi:
                        rotstep = "PI";
                        break;
                    case NFRotation.HalfPi:
                        rotstep = "PI/2";
                        break;
                    case NFRotation.Free:
                        rotstep = "FREE";
                        break;

                }
                int refl = (item.Reflection == 0 ? DefaultReflection : item.Reflection - 1);
                int count = (item.Count == 0 ? DefaultItemCount : item.Count);

                taskfile += $"ITEMFILE:\t{itemId}.item\n";
                taskfile += $"ITEMQUANT:\t{count}\n";
                taskfile += $"ROTATE:\t{((int) rot > 1 ? 1 : (int) rot)}\n";
                taskfile += $"ROTSTEP:\t{rotstep}\n";
                taskfile += $"REFLECT:\t{refl}\n";


                foreach (var contour in item.Contours)
                {
                    int VC = contour.VertexCount();
                    fileData += $"VERTQUANT:\t{VC}\n";

                    foreach (var point in contour.Points)
                    {
                        fileData += $"VERTEX:\t{point.X}\t{point.Y}\t{point.B}\n";
                    }
                }
                using (StreamWriter sw = File.CreateText($"{filePath}{itemId}.item"))
                {
                    fileData = fileData.Replace(",", ".");
                    sw.WriteLine(fileData);
                    sw.Close();
                }
                itemId++;
            }

            if (toCatAgent)
            {

                string DomainData = "ITEMNAME:\tdomain\nVERTQUANT:\t4\nVERTEX:\t0\t0\t0\n";
                DomainData += $"VERTEX:\t{ListX}\t0\t0\n";
                DomainData += $"VERTEX:\t{ListX}\t{ListY}\t0\n";
                DomainData += $"VERTEX:\t0\t{ListY}\t0\n";

                using (StreamWriter sw = File.CreateText($"{filePath}{Count()}.item"))
                {
                    DomainData = DomainData.Replace(",", ".");
                    sw.WriteLine(DomainData);
                    sw.Close();
                }
            }

            using (StreamWriter sw = File.CreateText(filePath + "nest.task"))
            {
                sw.WriteLine(taskfile);
                sw.Close();
            }
        }
    }
}
