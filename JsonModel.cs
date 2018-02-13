using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Block2Json
{
    public class JsonModel
    {
        public string __comment;
        public Dictionary<string, string> textures;
        public Element[] elements;
        public class Element
        {
            public float[] from;
            public float[] to;
            public Dictionary<string, Face> faces;

            public class Face
            {
                public string texture;
                public int[] uv;
            }
        }

        public class Face
        {
            public double[] from;
            public double[] to;
            public string facing;
            public Element parentElement;
            public bool tagged = false;
        }
        private List<Face> faces = new List<Face>();
        public void AddFace(Face face)
        {
            faces.Add(face);
        }
        public class SimpleElement : Element
        {
            new public double[] from; //Accurate From
            new public double[] to; //Accurate To
            public Texture texture; //Texture
            public int[] uv; //UV

            public Element ToJsonElment(string index)
            {
                var _from = new float[] { (float)from[0], (float)from[1], (float)from[2] };
                var _to = new float[] { (float)to[0], (float)to[1], (float)to[2] };
                if (_from[0] > _to[0]) { Swap(ref _from[0], ref _to[0]); }
                if (_from[1] > _to[1]) { Swap(ref _from[1], ref _to[1]); }
                if (_from[2] > _to[2]) { Swap(ref _from[2], ref _to[2]); }

                var face = new Face() { texture = "#" + index, uv = new int[] { uv[0], uv[1], uv[0], uv[1] } };
                var faces = new Dictionary<string, Face>()
                {
                    {"North", face},
                    {"East", face},
                    {"South", face},
                    {"West", face},
                    {"Up", face},
                    {"Down", face}
                };

                return new Element() { faces = faces, from = _from, to = _to };
            }
        }

        public void KeepSurface()
        {
            Current.WriteLine(Environment.NewLine + "- Delect Unseeable:");
            Current.SetProgressBar();

            faces.RemoveAll(face => face == null);
            var _element = elements.ToList();
            var _delect = new HashSet<Face>();
            var _taggedFaces = new HashSet<int>();

            var total = faces.Count;

            for(int i = 0; i < faces.Count; i++)
            {
                //Update Progress
                Current.DrawProgressBar((i + 1) * 100 / total);

                if (_taggedFaces.Contains(i)) continue;
                var connectFaces = Adjacency(i);

                var maxH = connectFaces.Max(index => faces[index].from[1]);
                var isIn = connectFaces.AsEnumerable().Where(index => faces[index].from[1] == maxH).Any(index => faces[index].facing == "Down");
                foreach (var faceI in connectFaces)
                {
                    if (isIn)
                    {
                        _delect.Add(faces[faceI]);
                    }
                    _taggedFaces.Add(faceI);
                }
            }
            foreach (var face in _delect)
            {
                face.parentElement.faces.Remove(face.facing);
                faces.Remove(face);
            }
            elements = _element.ToArray();
        }
        private HashSet<int> Adjacency(int Rootindex)
        {
            var faceCollection = new HashSet<int>();
            var end = false;
            var indexs = new HashSet<int>() { Rootindex };

            while (!end)
            {
                var success = false;
                var _indexs = new HashSet<int>();
                foreach (var i in indexs)
                {
                    _indexs.Add(i);
                }
                foreach (var index in _indexs)
                {
                    indexs.Remove(index);
                    faceCollection.Add(index);

                    var chose = new Dictionary<int, bool>();
                    for (int i = 0; i < faces.Count; i++)
                    {
                        var imp = opposite(faces[index].facing);
                        if (faces[i] != null && faces[i].facing != imp)
                        {
                            if (connected(faces[index], faces[i]))
                            {
                                if (faces[index].parentElement != faces[i].parentElement) //From the Same Element
                                {
                                    chose.Add(i, true);
                                }
                                else //From a Different Element
                                {
                                    chose.Add(i, false);
                                }
                            }
                        }
                    }
                    var ks = chose.Keys.ToList();
                    var conflicts = new HashSet<int>();
                    foreach (var k in ks)
                    {
                        foreach (var c in ks)
                        {
                            if (faces[k].facing == opposite(faces[c].facing) && chose[k] != chose[c])
                            {
                                conflicts.Add(k);
                            }
                        }
                    } //Get Facing Conflicts (From Different Element & Opposite)
                    foreach (var c in conflicts)
                    {
                        if (!chose[c]) chose.Remove(c);
                    } //Remove Facing Conflicts
                    foreach (var k in chose.Keys)
                    {
                        if (((conflicts.Contains(k) && chose[k]) || !conflicts.Contains(k)) && !faceCollection.Contains(k))
                        {
                            indexs.Add(k);
                            if (!success) success = true;
                        }
                    }
                }

                //var s = indexs.Count.ToString() + Environment.NewLine;
                //foreach (var g in indexs)
                //{
                //    s += faces[g].facing + " " + g + " " + faces[g].from[0] + "," + faces[g].from[1] + "," + faces[g].from[2] + " " + Environment.NewLine;
                //}
                //MessageBox.Show(s);
                if (!success) end = true;
            }

            return faceCollection;
        }
        private HashSet<int> TagAdjacency(int index, HashSet<int> faceCollection = null, int depth = 0)
        {
            if (faceCollection == null) faceCollection = new HashSet<int>();

            
            return faceCollection;
        }
        private bool connected(Face face1, Face face2)
        {
            var crx = cross1(face1.from[0], face1.to[0], face2.from[0], face2.to[0]);
            var cry = cross1(face1.from[1], face1.to[1], face2.from[1], face2.to[1]);
            var crz = cross1(face1.from[2], face1.to[2], face2.from[2], face2.to[2]);

            var s1 = 0;
            if (crx == 1) s1++;
            if (cry == 1) s1++;
            if (crz == 1) s1++;
            var s2 = 0;
            if (crx == 2) s2++;
            if (cry == 2) s2++;
            if (crz == 2) s2++;

            return s1 >= 2 && s2 >=1; //At Least 1 Public Line & 2 Public Points
        }
        private int cross1(double a1, double a2, double b1, double b2)
        {
            if (a1 > a2) Swap(ref a1, ref a2);
            if (b1 > b2) Swap(ref b1, ref b2);
            if (a2 > b2) { Swap(ref b1, ref a1); Swap(ref b2, ref a2); }
            if (a2 < b1) return 0; //No Public
            else if (a2 == b1 || a1 == a2 || b1 == b2) return 1; //Public Point
            else return 2; //Public Line
        }
        private string opposite(string facing)
        {
            var imp = "None";
            if (facing == "North") imp = "South";
            if (facing == "South") imp = "North";
            if (facing == "East") imp = "West";
            if (facing == "West") imp = "East";
            if (facing == "Down") imp = "Up";
            if (facing == "Up") imp = "Down";
            return imp;
        }
        public Element GetFaceElement(Element e)
        {
            var faceEnable = new bool[] { true, true, true, true, true, true };
            Parallel.ForEach(elements, _e =>
            {
                if (e.from[0] >= _e.from[0] && e.from[1] >= _e.from[1] && e.from[2] >= _e.from[2] && e.to[0] <= _e.to[0] && e.to[1] <= _e.to[1] && e.from[2] <= _e.to[2] && e != _e)
                {
                    e.faces.Remove("North");//North
                    faceEnable[0] = false;
                }
                if (e.from[0] >= _e.from[0] && e.from[1] >= _e.from[1] && e.to[2] >= _e.from[2] && e.to[0] <= _e.to[0] && e.to[1] <= _e.to[1] && e.to[2] <= _e.to[2] && e != _e)
                {
                    e.faces.Remove("South");//South
                    faceEnable[1] = false;
                }
                if (e.to[0] >= _e.from[0] && e.from[1] >= _e.from[1] && e.from[2] >= _e.from[2] && e.to[0] <= _e.to[0] && e.to[1] <= _e.to[1] && e.to[2] <= _e.to[2] && e != _e)
                {
                    e.faces.Remove("East");//East
                    faceEnable[2] = false;
                }
                if (e.from[0] >= _e.from[0] && e.from[1] >= _e.from[1] && e.from[2] >= _e.from[2] && e.from[0] <= _e.to[0] && e.to[1] <= _e.to[1] && e.to[2] <= _e.to[2] && e != _e)
                {
                    e.faces.Remove("West");//West
                    faceEnable[3] = false;
                }
                if (e.from[0] >= _e.from[0] && e.to[1] >= _e.from[1] && e.from[2] >= _e.from[2] && e.to[0] <= _e.to[0] && e.to[1] <= _e.to[1] && e.to[2] <= _e.to[2] && e != _e)
                {
                    e.faces.Remove("Up");//Up
                    faceEnable[4] = false;
                }
                if (e.from[0] >= _e.from[0] && e.from[1] >= _e.from[1] && e.from[2] >= _e.from[2] && e.to[0] <= _e.to[0] && e.from[1] <= _e.to[1] && e.to[2] <= _e.to[2] && e != _e)
                {
                    e.faces.Remove("Down");//Down
                    faceEnable[5] = false;
                }
            });
            if (faceEnable[0]) faces.Add(new Face() { facing = "North", from = new double[] { e.from[0], e.from[1], e.from[2] }, to = new double[] { e.to[0], e.to[1], e.from[2] }, parentElement = e });
            if (faceEnable[1]) faces.Add(new Face() { facing = "South", from = new double[] { e.from[0], e.from[1], e.from[2] }, to = new double[] { e.to[0], e.to[1], e.from[2] }, parentElement = e });
            if (faceEnable[2]) faces.Add(new Face() { facing = "East", from = new double[] { e.from[0], e.from[1], e.from[2] }, to = new double[] { e.to[0], e.to[1], e.from[2] }, parentElement = e });
            if (faceEnable[3]) faces.Add(new Face() { facing = "West", from = new double[] { e.from[0], e.from[1], e.from[2] }, to = new double[] { e.to[0], e.to[1], e.from[2] }, parentElement = e });
            if (faceEnable[4]) faces.Add(new Face() { facing = "Up", from = new double[] { e.from[0], e.from[1], e.from[2] }, to = new double[] { e.to[0], e.to[1], e.from[2] }, parentElement = e });
            if (faceEnable[5]) faces.Add(new Face() { facing = "Down", from = new double[] { e.from[0], e.from[1], e.from[2] }, to = new double[] { e.to[0], e.to[1], e.from[2] }, parentElement = e });
            return e;
        }
        public bool IsInner(double[] faceFrom, double[] faceTo, Element ignore)
        {
            var _elements = elements.ToList();
            _elements.Remove(ignore);
            var s = _elements.AsParallel().Any(
                e =>
                {
                    if (faceFrom[0] >= e.from[0] && faceFrom[1] >= e.from[1] && faceFrom[2] >= e.from[2] && faceTo[0] <= e.to[0] && faceTo[1] <= e.to[1] && faceTo[2] <= e.to[2])
                    {
                        return true;
                    }
                    return false;
                }
            );
            if (s) return true;
            else return false;
        }
        private static void Swap(ref float a, ref float b)
        {
            var t = b;
            b = a;
            a = t;
        }
        private static void Swap(ref double a, ref double b)
        {
            var t = b;
            b = a;
            a = t;
        }
    }
}