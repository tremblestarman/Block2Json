using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Data;

namespace Block2Json
{
    public class ModelInfo
    {
        public List<_Enum> Enums = new List<_Enum>(); //Enums of the Model Collection
        public List<_Enum.Element> GetElements(BlockInfo blockInfo, BlockCollection blockCollection, Vector3 pos, ResourceList res = null)
        {
            var e = Enums.FindAll((_Enum _enum) => _enum.ConditionsMet(blockInfo, blockCollection, pos, res));
            var el = new List<_Enum.Element>();
            foreach (var l in e)
            {
                el.AddRange(l.Elements);
            }
            return el;
        }
        public class _Enum
        {
            public List<Element> Elements = new List<Element>();
            public List<Condition> Conditions = new List<Condition>();
            public bool ConditionsMet(BlockInfo blockInfo, BlockCollection blockCollection, Vector3 pos, ResourceList res = null)
            {
                foreach (var condition in Conditions)
                {
                    if (!condition.ConditionMet(blockInfo, blockCollection, pos, res)) return false;
                }
                return true;
            }
            public class Element
            {
                public JsonModel.SimpleElement GetElement(BlockInfo _model, Vector3 _pos, string directoryPath)
                {
                    return new JsonModel.SimpleElement() { from = GetFrom(_model, _pos), to = GetTo(_model, _pos), texture = GetTexture(_model), uv = GetUV(_model, directoryPath) };
                }
                public List<string> from = new List<string>(); //From[3]
                public double[] GetFrom(BlockInfo _model, Vector3 Position)
                {
                    var e = Expression(from, _model);
                    return new double[] { e[0] + Position.X + 0.5, e[1] + Position.Y + 0.5, e[2] + Position.Z + 0.5 };
                }
                public List<string> to = new List<string>(); //To[3]
                public double[] GetTo(BlockInfo _model, Vector3 Position)
                {
                    var e = Expression(to, _model);
                    return new double[] { e[0] + Position.X + 0.5, e[1] + Position.Y + 0.5, e[2] + Position.Z + 0.5 };
                }
                public string Texture = ""; //Mark of a Texture
                public Texture GetTexture(BlockInfo _model)
                {
                    if (_model.Model.Textures.ContainsKey(Texture))
                        return _model.Model.Textures[Texture];
                    else
                        return null;
                }
                private double[] Expression(List<string> Vector3Express, BlockInfo _model)
                {
                    var _ = new double[] { 0, 0, 0 };
                    for (int i = 0; i < 3; i++)
                    {
                        var _e = Vector3Express[i].Replace("$sx", _model.Size.X.ToString()).Replace("$sy", _model.Size.Y.ToString()).Replace("$sz", _model.Size.Z.ToString());
                        _[i] = Expression(_e);
                    }
                    return _;
                }
                private double Expression(string Expression)
                {
                    try
                    {
                        object r = new DataTable().Compute(Expression, "");
                        return Convert.ToDouble(r);
                    }
                    catch
                    {
                        return 0.0;
                    }
                }
                private int[] GetUV(BlockInfo _info, string directoryPath)
                {
                    var uv = new int[] { 0, 0 };
                    var t = GetTexture(_info);
                    var path = "";
                    if (Program.version >= 1.13)
                    {
                        var m = t.Path.ToList();
                        m.RemoveAt(5);
                        path = String.Join(null, m.ToArray());
                    }
                    if (t.Params != null && t.Params.Contains("ColorMap")) //ColorMap
                    {
                        var pars = t.Params.Split(' ');
                        if (pars.Length != 5 && pars[0] != "ColorMap")
                            uv = GetColorMapUV(Program.temp, Program.rain, 0, false, 0);
                        else
                            uv = GetColorMapUV(Program.temp, Program.rain, int.Parse(pars[1]), bool.Parse(pars[2]), float.Parse(pars[3]), float.Parse(pars[4]));
                    }
                    else //Default
                    {
                        bool textureError = true;
                        if (Program.smooth)
                        {
                            try
                            {
                                Bitmap compare = new Bitmap(directoryPath + "\\textures\\" + path.Replace("/", "\\") + ".png");
                                for (int mh = 0; mh < compare.Size.Height; mh++)
                                {
                                    for (int mw = 0; mw < compare.Size.Width; mw++)
                                    {
                                        if (compare.GetPixel(mh, mw).A > 8) { uv[0] = mh; uv[1] = mw; break; }
                                    }
                                    if (uv[0] > 0 && uv[1] > 0) break;
                                }
                            }
                            catch
                            {
                                Current.Error("Texture: " + t.Path + ".png Not Found", false);
                            }
                        }
                        else
                        {
                            while (textureError)
                            {
                                uv[0] = (int)Math.Floor(StaticRandom.NextDouble() * 16);
                                uv[1] = (int)Math.Floor(StaticRandom.NextDouble() * 16);
                                try
                                {
                                    Bitmap compare = new Bitmap(System.Windows.Forms.Application.StartupPath + "\\textures\\" + path.Replace("/", "\\") + ".png");
                                    textureError = (compare.GetPixel((int)uv[0], (int)uv[1]).A <= 8);
                                }
                                catch
                                {
                                    Current.Error("Texture: " + t.Path + ".png Not Found", false);
                                }
                            }
                        }
                    }
                    return uv;
                }
                private static int[] GetColorMapUV(float temp, float rain, int pixel_range = 0, bool rich = false, float warmer = 0.0f, float wetter = 0.0f)
                {
                    int[] uv = new[] { 0, 0 };
                    double uv_x = 0.0f, uv_y = 0.0f;

                    if (temp > 1) temp = 1; if (temp < 0) temp = 0;
                    if (rain > 1) rain = 1; if (rain < 0) rain = 0;
                    bool rangeEroor = true;

                    while (rangeEroor)
                    {
                        if (!Program.smooth)
                        {
                            var theta = StaticRandom.NextDouble() * 360;
                            var d = StaticRandom.NextDouble() * pixel_range - pixel_range / 2;
                            uv_x = temp + (warmer / 256f) + (d / 256f) * Math.Cos(theta * Math.PI / 360); uv_y = temp * rain + (wetter / 256f) + (d / 256f) * Math.Sin(theta * Math.PI / 360);
                        }
                        else
                        {
                            uv_x = temp + (warmer / 256f); uv_y = temp * rain + (wetter / 256f);
                        }
                        if (uv_x >= 1) uv_x = 1;
                        if (uv_x <= 0) uv_x = 0;
                        if (uv_y >= 1) uv_y = 1;
                        if (uv_y <= 0) uv_y = 0;
                        if (uv_x >= uv_y) rangeEroor = false;
                    }

                    if (!rich)
                    {
                        uv_x = 1 - uv_x;
                        uv_y = 1 - uv_y;
                    }
                    uv[0] = (int)Math.Floor(uv_x * 16);
                    uv[1] = (int)Math.Floor(uv_y * 16);
                    return uv;
                }
            }
            public class Condition
            {
                private string _params = "Default";
                public string Params { get { return _params; } set { if (value != null) _params = value; } } //Params (Custome Params)
                private List<string> _paramslist = new List<string>(); //List of Params
                private void SetParamList()
                {
                    _paramslist = _params.Split(',').ToList();
                }
                public List<string> DataList()
                {
                    return _paramslist;
                }

                private Dictionary<string, Vector3> _relativeBlocks = new Dictionary<string, Vector3>();
                public Dictionary<string, Vector3> RelativeBlocks { get { return _relativeBlocks; } set { if (value != null) _relativeBlocks = value; } } //RelativeBlocks (Tags | Ids, Relative Coordinate)

                public bool ConditionMet(BlockInfo blockInfo, BlockCollection blockCollection, Vector3 pos, ResourceList res = null)
                {
                    SetParamList();
                    if (_paramslist != null && blockInfo.Model.Params != null) //Match Params
                    {
                        if (!Universal.Match(blockInfo.Model.Params.Split(',').ToList(), _paramslist)) return false; //When Params of BlockInfo Not Include Params of ModelInfoCondition
                    }
                    if (_relativeBlocks != null) //Match RelativeBlocks
                    {
                        foreach (var info in _relativeBlocks.Keys)
                        {
                            if (res != null)
                            {
                                var _b = res.SimpleBlockInfoCollection(info);
                                var b = blockCollection.GetBlock((int)(_relativeBlocks[info].X + pos.X), (int)(_relativeBlocks[info].Y + pos.Y), (int)(_relativeBlocks[info].Z + pos.Z));
                                if (b != null && _b != null)
                                {
                                    
                                    var success = _b.Find((SimpleBlockInfo __b) => Universal.MatchBlock(b.GetBlockInfo(), __b));
                                    if (success == null) return false;//When Blocks Not Match
                                }
                                else return false;
                            }
                        }
                    }
                    return true;
                }
            }
        }
    }

    public class Texture
    {
        public string Path { get; set; }
        public string Params { get; set; }
    }
}
