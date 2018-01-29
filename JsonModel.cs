using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using Newtonsoft.Json;

namespace S2J
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

        public List<string> GetInners(Element e)
        {
            var facings = new List<string>();
            var task = Task.WhenAll(
                Task.Run(() =>
                {
                    if (IsInner(new double[] { e.from[0], e.from[1], e.from[2] }, new double[] { e.to[0], e.to[1], e.from[2] }, e)) facings.Add("North");//North
                }),
                Task.Run(() => {
                    if (IsInner(new double[] { e.from[0], e.from[1], e.to[2] }, new double[] { e.to[0], e.to[1], e.to[2] }, e)) facings.Add("South");//South
                }),
                Task.Run(() => {
                    if (IsInner(new double[] { e.to[0], e.from[1], e.from[2] }, new double[] { e.to[0], e.to[1], e.to[2] }, e)) facings.Add("East");//East
                }),
                Task.Run(() => {
                    if (IsInner(new double[] { e.from[0], e.from[1], e.from[2] }, new double[] { e.from[0], e.to[1], e.to[2] }, e)) facings.Add("West");//West
                }),
                Task.Run(() => {
                    if (IsInner(new double[] { e.from[0], e.from[1], e.from[2] }, new double[] { e.to[0], e.from[1], e.to[2] }, e)) facings.Add("Down");//Down
                }),
                Task.Run(() => {
                    if (IsInner(new double[] { e.from[0], e.to[1], e.from[2] }, new double[] { e.to[0], e.to[1], e.to[2] }, e)) facings.Add("Up"); //Up
                })
                );
            return facings;
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
            /*foreach (var e in _elements)
            {
                var _e = RegularFromTo(new List<Vector3>() { new Vector3() { X = e.from[0], Y = e.from[1], Z = e.from[2] }, new Vector3() { X = e.to[0], Y = e.to[1], Z = e.to[2] } });
                if (face[0].X >= _e[0].X && face[0].Y >= _e[0].Y && face[0].Z >= _e[0].Z && face[1].X <= _e[1].X && face[1].Y <= _e[1].Y && face[1].Z <= _e[1].Z)
                {
                    return true;
                }
            }
            */
        }
        private List<Vector3> RegularFromTo(List<Vector3> f_t)
        {
            return new List<Vector3>() { new Vector3() { X = f_t.Min(v => v.X), Y = f_t.Min(v => v.Y), Z = f_t.Min(v => v.Z) }, new Vector3() { X = f_t.Max(v => v.X), Y = f_t.Max(v => v.Y), Z = f_t.Max(v => v.Z) } };
        }
        private static void Swap(ref float a, ref float b)
        {
            var t = b;
            b = a;
            a = t;
        }
    }
}
