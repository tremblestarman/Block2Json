using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using fNbt;

namespace Block2Json
{
    public class Program
    {
        #region Static
        static ResourceList res; //ResourceList
        static BlockCollection bcl; //BlockCollection
        public static double version = 1.122; //Current Version
        static string target = ".schematic"; //Target
        static public bool unlimit = false; static public bool smooth = false; static public bool nopause = false; static public bool log = false;//Essential Options
        static public float temp; static public float rain; //Biome
        #endregion
        static void Main(string[] args)
        {
            Current.Write("Reading Resources ...");
            res = new ResourceList(Application.StartupPath);
            Current.Write("");
            #region File & Options
            string file;
            //Current Version = 1.12.2
            version = 1.122;
            #region Get File
            if (args.Length > 0)
            {
                file = args[0];
            }
            else
            {
                Current.WriteLine("Input: <file> <mode & filter & biome>");
                args = Current.ReadLine().Split(' ');
                file = args[0];
            }

            if (file == null || !File.Exists(file))
            {
                Current.Error("File Not Found", true, true);
            }
            else if (new FileInfo(file).Extension == ".schematic" || new FileInfo(file).Extension == ".nbt")
            {
                target = new FileInfo(file).Extension;
            }
            else
            {
                Current.Error("File Not Supported", true, true);
            }
            #endregion
            #region GetOptions
            //Set Output Mode
            #region mode
            unlimit = args.Contains("unlimit");
            smooth = args.Contains("smooth");
            nopause = args.Contains("nopause");
            log = args.Contains("log");
            #endregion
            //Set Target Version
            #region version
            foreach (string reg in args)
            {
                if (Regex.Match(reg, "version=.*$").Success)
                {
                    if (Regex.Match(Regex.Matches(reg, "version=.*$")[0].Value, @"\d*\.\d*").Success)
                    {
                        List<double> v1st = new List<double>();
                        foreach (Match r in Regex.Matches(Regex.Matches(reg, "version=.*$")[0].Value, @"\d*\.\d*"))
                        {
                            v1st.Add(double.Parse(r.Value));
                        }
                        v1st.Sort(delegate (double x, double y)
                        {
                            return y.CompareTo(x);
                        });
                        version = v1st[0];
                    }
                }
            }
            #endregion
            //Set Target Biome;
            #region biome
            temp = 0.8f; rain = 0.4f; var biome = "TheVoid";
            foreach (string reg in args)
            {
                if (Regex.Match(reg, "(?<=biome=).*$").Success)
                {
                    var biomeName = Regex.Match(reg, "(?<=biome=).*$").Value;
                    BiomeList.UpdateList(Application.StartupPath + "\\BiomeList");
                    var cbiome = BiomeList.GetBiome(biomeName);
                    if (cbiome != null)
                    { temp = cbiome.Temperature; rain = cbiome.Rainfall; biome = biomeName; }
                }
            }
            #endregion
            //Set Process;
            #region Process
            var nocompress = false;
            var hollow = false;
            var surface = false;
            var fillflow = false;  var fillflow_xyz = new Vector3(){ X = 0.0, Y = 0.0, Z = 0.0 }; var fillflow_block = new SimpleBlockInfo(""); var reverse_fillflow = false;
            nocompress = args.Contains("nocompress");
            hollow = args.Contains("hollow");
            surface = args.Contains("surface");
            foreach (string reg in args)
            {
                if (Regex.Match(reg, "(?<=fillflow=).*$").Success)
                {
                    try
                    {
                        var o = Regex.Match(reg, "(?<=fillflow=).*$").Value;
                        reverse_fillflow = Regex.Match(o, "(reverse|r)(?=\\()").Success;
                        var os = Regex.Match(o, "(?<=\\().*(?=\\))").Value.Split(',');
                        var ob = Regex.Match(o, "(?<=\\)).*$").Value.Trim();
                        if (os != null && os.Length == 3)
                        {
                            fillflow_xyz.X = double.Parse(Regex.Match(os[0].Trim(), "\\d+(.\\d+)?").Value);
                            fillflow_xyz.Y = double.Parse(Regex.Match(os[1].Trim(), "\\d+(.\\d+)?").Value);
                            fillflow_xyz.Z = double.Parse(Regex.Match(os[2].Trim(), "\\d+(.\\d+)?").Value);
                        }
                        if (ob != null)
                        {
                            fillflow_block = new SimpleBlockInfo(ob);
                        }
                        fillflow = true;
                    }
                    catch
                    {
                        Current.Error("FillFlow Format Error.", true, true);
                    }
                }
            }
            #endregion
            #endregion
            #region Filter
            #region For Classes
            var filterClasses = new List<string>();
            //get filter_classes
            foreach (string reg in args)
            {
                if (Regex.Match(reg, "(?<=filter_classes=).*$").Success)
                {
                    filterClasses = new List<string>(Regex.Match(reg, "(?<=filter_classes=).*$").Value.Split(';'));
                }
            }
            //remove form list
            foreach (var c in filterClasses)
            {
                res.DelectBlockInfoClass(c);
            }
            #endregion
            #region For Blocks
            var filterBlocks = "";
            var fbs = new List<SimpleBlockInfo>();
            //Get filter_blocks
            foreach (string reg in args)
            {
                if (Regex.Match(reg, "(?<=filter_blocks=).*$").Success)
                {
                    filterBlocks = Regex.Match(reg, "(?<=filter_blocks=).*$").Value;
                    fbs = res.SimpleBlockInfoCollection(Regex.Match(reg, "(?<=filter_blocks=).*$").Value, Application.StartupPath);
                }
            }
            //Remove from List
            foreach (var b in fbs)
            {
                res.DelectBlockInfo(b.Id, b.Data);
            }
            #endregion
            #endregion
            #endregion
            #region Set BlockCollection
            if (target == ".schematic") Schematic2BlockCollection(file);
            if (target == ".nbt") NBT2BlockCollection(file);
            bcl.blocks.RemoveAll(block => block == null);
            #endregion
            #region Output Info
            Current.WriteLine("");
            Current.WriteLine("- Info:");
            //-output target
            Current.WriteLine("Target = " + target.Replace(".", "").ToUpper());
            //-output size
            Current.WriteLine("Width = " + bcl.GetWidth() + " ,Height = " + bcl.GetHeight() + " ,Length = " + bcl.GetLength() + ";");
            //-output filter
            var ig_b = String.Join(";", filterBlocks);
            Current.WriteLine(ig_b == "" ? "Filter Blocks = none;" : "Filter Blocks = " + ig_b + ";");
            var ig_m = String.Join(";", filterClasses);
            Current.WriteLine(ig_m == "" ? "Filter Models = none;" : "Filter Models = " + ig_m + ";");
            //-output biome
            Current.WriteLine(biome == "" ? "Biome = none" : "Biome = " + biome + ";");
            //-output mode
            Current.WriteLine("Unlimit = " + unlimit.ToString() + " ,Smooth = " + smooth.ToString() + ";");
            //-output compress
            Current.WriteLine("Nocompress = " + nocompress.ToString() + " ,Hollow = " + hollow.ToString() + " ,Surface = " + surface.ToString());
            #endregion
            if (hollow) bcl = Hollow(bcl);
            if (fillflow) bcl = FillFlow(bcl, fillflow_xyz, fillflow_block, reverse_fillflow);
            var jsonModel = JsonModel(Application.StartupPath);
            if (!nocompress) jsonModel = DuplicateOverlap(jsonModel);
            if (surface) jsonModel.KeepSurface();
            jsonModel = ScaleModel(RemoveEmptyElement(jsonModel));
            #region Generating File
            Current.WriteLine("");
            Current.WriteLine("- Generating File:");

            var extension = Path.GetExtension(file);
            var newFile = file.Replace(extension, ".json");
            File.WriteAllText(newFile, JsonConvert.SerializeObject(jsonModel, Formatting.Indented));

            Current.WriteLine(newFile + " has been Created");
            #endregion
            if (log) File.WriteAllText("Log -" + DateTime.Now.ToFileTime() + ".txt", Current.Log.ToString());
            if (nopause) return;
            Current.WriteLine("[ Press any key to continue... ]");
            Console.ReadKey(true);
        }

        private static void Schematic2BlockCollection(string file)
        {
            #region Initialization
            var nbt = new NbtFile();
            nbt.LoadFromFile(file);
            var root = nbt.RootTag; //Read File

            bcl = new BlockCollection();
            bcl.SetWidth(root.Get<NbtShort>("Width").ShortValue); //Get Width
            bcl.SetHeight(root.Get<NbtShort>("Height").ShortValue);
            bcl.SetLength(root.Get<NbtShort>("Length").ShortValue);

            bcl.Comments = "Created by S2J"; //Set Comment
            var blocks = root.Get<NbtByteArray>("Blocks").ByteArrayValue; //Initialize Blocks
            var datas = root.Get<NbtByteArray>("Data").ByteArrayValue; //Initialize Datas

            bcl.level = new BlockCollection.Block[bcl.GetWidth(), bcl.GetHeight(), bcl.GetLength()];
            #endregion
            var obj = new object();

            Current.WriteLine(Environment.NewLine + "- Reading Schematic:");
            Current.SetProgressBar();
            int current = 0;
            int total = bcl.GetWidth();

            #region Read Schematic
            Parallel.For(0, bcl.GetWidth(), x =>
            {
                for (int y = 0; y < bcl.GetHeight(); y++)
                {
                    for (int z = 0; z < bcl.GetLength(); z++)
                    {
                        var block = new BlockCollection.Block();
                        var index = y * bcl.GetWidth() * bcl.GetLength() + z * bcl.GetWidth() + x;
                        if (blocks[index] != 0)
                        {
                            var blockInfo = res.GetBlockInfo(blocks[index].ToString(), datas[index].ToString(), version);
                            if (blockInfo != null)
                            {
                                block.SetCoordinate(x, y, z);
                                block.SetInfo(blockInfo);
                                bcl.blocks.Add(block);
                                bcl.level[x, y, z] = block;
                            }
                        }
                    }
                }
                lock (obj) //Update Progress
                {
                    current++;
                    Current.DrawProgressBar(current * 100 / total);
                }
            });
            #endregion
        }
        private static void NBT2BlockCollection(string file)
        {
            #region Initialization
            var nbt = new NbtFile();
            nbt.LoadFromFile(file);
            var root = nbt.RootTag; //Read File

            bcl = new BlockCollection();
            bcl.SetWidth(root.Get<NbtList>("size")[0].IntValue); //Get Width
            bcl.SetHeight(root.Get<NbtList>("size")[1].IntValue);
            bcl.SetLength(root.Get<NbtList>("size")[2].IntValue);

            bcl.Comments = "Created by S2J"; //Set Comment

            var p = root.Get<NbtList>("palette");
            var palettes = new List<NbtCompound>();
            foreach (NbtCompound _p in p) { palettes.Add(_p); }
            var blocks = root.Get<NbtList>("blocks");

            bcl.level = new BlockCollection.Block[bcl.GetWidth(), bcl.GetHeight(), bcl.GetLength()];
            #endregion

            Current.WriteLine(Environment.NewLine + "- Reading NBT:");
            Current.SetProgressBar();
            int current = 0;
            int total = bcl.GetWidth() * bcl.GetHeight() * bcl.GetLength();

            #region Read NBT
            foreach (NbtCompound b in blocks)
            {
                var index = b.Get<NbtInt>("state").IntValue;
                var _p = b.Get<NbtList>("pos");
                var pos = new Vector3() { X = _p[0].IntValue, Y = _p[1].IntValue, Z = _p[2].IntValue };
                var state = palettes[index];
                if (state != null && state.Get<NbtString>("Name") != null)
                {
                    var block = new BlockCollection.Block();
                    #region Id & Data
                    var id = state.Get<NbtString>("Name").StringValue;
                    var datas = new List<string>();
                    if (state.Get<NbtCompound>("Properties") != null)
                        foreach (var _s in state.Get<NbtCompound>("Properties"))
                        {
                            datas.Add(_s.Name + ":" + _s.StringValue);
                        }
                    var data = string.Join(",", datas.ToArray());
                    #endregion
                    //MessageBox.Show(id + " " + data);
                    if (id != "minecraft:air" && id != "minecraft:void_air" && id != "minecraft:cave_air")
                    {
                        var blockInfo = res.GetBlockInfo(id, data, version);
                        if (blockInfo != null)
                        {
                            block.SetCoordinate((int)pos.X, (int)pos.Y, (int)pos.Z);
                            block.SetInfo(blockInfo);
                            bcl.blocks.Add(block);
                            bcl.level[(int)pos.X, (int)pos.Y, (int)pos.Z] = block;
                        }
                    }
                }

                //Update Progress
                current++;
                Current.DrawProgressBar(current * 100 / total);
            }
            #endregion
        }
        private static BlockCollection Hollow(BlockCollection blocks)
        {
            Current.WriteLine(Environment.NewLine + "- Filling Hollow:");
            Current.SetProgressBar();
            int current = 0;
            int total = bcl.GetWidth() * bcl.GetHeight() * bcl.GetLength();

            var close = new int[blocks.GetWidth(), blocks.GetHeight(), blocks.GetLength()]; //Get Inside Blocks
            for (int x = 0; x < blocks.GetWidth(); x++)
            {
                for (int y = 0; y < blocks.GetHeight(); y++)
                {
                    for (int z = 0; z < blocks.GetLength(); z++)
                    {
                        var curb = blocks.level[x, y, z];
                        if (curb != null)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    for (int dz = -1; dz <= 1; dz++)
                                    {
                                        if (dx != 0 || dy != 0 || dz != 0)
                                        {
                                            int cx = x + dx; int cy = y + dy; int cz = z + dz;
                                            if (cx > 0 && cx < (blocks.GetWidth() - 1) && cy > 0 && cy < (blocks.GetHeight() - 1) && cz > 0 && cz < (blocks.GetLength() - 1))
                                                close[cx, cy, cz]++;
                                        }
                                    }
                                }
                            }
                        //Update Progress
                        current++;
                        Current.DrawProgressBar(current * 100 / total);
                        Current.Write(" " + current + " blocks have been scanned.(total : " + total + " )");
                    }
                }
            }
            current = 0;
            Current.WriteLine("");
            for (int x = 1; x < blocks.GetWidth() - 1; x++) //Remove Inside Blocks
            {
                for (int y = 1; y < blocks.GetHeight() - 1; y++)
                {
                    for (int z = 1; z < blocks.GetLength() - 1; z++)
                    {
                        if (close[x,y,z] >= 26)
                        {
                            var block = blocks.level[x, y, z];
                            if (block != null)
                            {
                                blocks.blocks.Remove(block);
                                blocks.level[x, y, z] = null;
                            }
                        }
                        //Update Progress
                        current++;
                        Current.DrawProgressBar(current * 100 / total);
                        Current.Write(" " + current + " blocks have been disposed of.(total : " + total + " )");
                    }
                }
            }
            return blocks;
        }
        private static BlockCollection FillFlow(BlockCollection blocks, Vector3 origin, SimpleBlockInfo fill_block, bool reverse = false)
        {
            Current.WriteLine(Environment.NewLine + "- Filling Flow...");
            var m = blocks.level[blocks.GetWidth() / 2, blocks.GetHeight() / 2, blocks.GetLength() / 2];
            var close = new List<int[]>();
            var newb = res.GetBlockInfo(fill_block.Id, fill_block.Data, version);
            if (newb == null) { newb = new BlockInfo(); newb.Id = "$empty"; } //Get Fill Material

            if (origin.X < 0 || origin.X >= blocks.GetWidth() || origin.Y < 0 || origin.Y >= blocks.GetHeight() || origin.Z < 0 || origin.Z >= blocks.GetLength())
            { origin.X = blocks.GetWidth() / 2; origin.Y = blocks.GetHeight() / 2; origin.Z = blocks.GetLength() / 2; }
            close.Add(new int[] { (int)origin.X, (int)origin.Y, (int)origin.Z }); //Set Finding Origin

            var flowed = new bool[blocks.GetWidth(), blocks.GetHeight(), blocks.GetLength()]; //Tag: flowed
            var _blocks = new List<BlockCollection.Block>();
            foreach (var b in blocks.blocks)
            {
                _blocks.Add(b);
            }

            while (close.Count() > 0) //Flowing
            {
                var c = new { x = close[close.Count - 1][0], y = close[close.Count - 1][1], z = close[close.Count - 1][2] };
                close.RemoveAt(close.Count - 1);
                if (blocks.level[c.x, c.y, c.z] == m)
                {
                    var block = new BlockCollection.Block();
                    block.SetCoordinate(c.x, c.y, c.z);
                    block.SetInfo(newb);
                    blocks.blocks.Add(block);
                    blocks.level[c.x, c.y, c.z] = block;
                    flowed[c.x, c.y, c.z] = true;

                    if (c.x - 1 >= 0) close.Add(new int[] { c.x - 1, c.y, c.z });
                    if (c.x + 1 < blocks.GetWidth()) close.Add(new int[] { c.x + 1, c.y, c.z });
                    if (c.y - 1 >= 0) close.Add(new int[] { c.x, c.y - 1, c.z });
                    if (c.y + 1 < blocks.GetHeight()) close.Add(new int[] { c.x, c.y + 1, c.z });
                    if (c.z - 1 >= 0) close.Add(new int[] { c.x, c.y, c.z - 1 });
                    if (c.z + 1 < blocks.GetLength()) close.Add(new int[] { c.x, c.y, c.z + 1 });
                }
            }
            if (reverse)
            {
                Current.WriteLine(Environment.NewLine + "- Reversing Filling Flow...");
                Current.SetProgressBar();
                int current = 0;
                int total = bcl.GetWidth() * bcl.GetHeight() * bcl.GetLength(); //Fill Empty Regions
                for (int x = 0; x < blocks.GetWidth(); x++)
                {
                    for (int y = 0; y < blocks.GetHeight(); y++)
                    {
                        for (int z = 0; z < blocks.GetLength(); z++)
                        {
                            if (!flowed[x, y, z] && blocks.level[x, y, z] == null)
                            {
                                var block = new BlockCollection.Block();
                                block.SetCoordinate(x, y, z);
                                block.SetInfo(newb);
                                _blocks.Add(block);
                                blocks.level[x, y, z] = block;
                            }
                            //Update Progress
                            current++;
                            Current.DrawProgressBar(current * 100 / total);
                            Current.Write(" " + current + " blocks have been scanned.(total : " + total + " )");
                        }
                    }
                }
                blocks.blocks = _blocks;
            }
            return blocks;
        }
        private static JsonModel JsonModel(string directoryPath)
        {
            var obj = new object();

            Current.WriteLine(Environment.NewLine + "- Generating Model:");
            Current.SetProgressBar();
            int current = 0;
            int total = bcl.blocks.Count;
            var jsonModel = new JsonModel();
            jsonModel.__comment = bcl.Comments;
            var elements = new List<JsonModel.Element>();
            jsonModel.textures = new Dictionary<string, string>();

            foreach (var block in bcl.blocks)
            {
                if (block.GetBlockInfo().Id == "$empty")
                {
                    var e = new JsonModel.Element()
                    {
                        faces = new Dictionary<string, JsonModel.Element.Face>()
                        {
                            {"North", null},
                            {"East", null},
                            {"South", null},
                            {"West", null},
                            {"Up", null},
                            {"Down", null}
                        },
                        from = new float[] { (float)block.GetCoordinate().X, (float)block.GetCoordinate().Y, (float)block.GetCoordinate().Z },
                        to = new float[] { (float)block.GetCoordinate().X + 1, (float)block.GetCoordinate().Y + 1, (float)block.GetCoordinate().Z + 1 }
                    };
                    elements.Add(e);
                }
                else
                {
                    var l = block.GetSimpleElements(directoryPath, bcl, res);
                    foreach (var e in l)
                    {
                        var index = block.GetBlockInfo().Id + "_" + block.GetBlockInfo().Data;
                        elements.Add(e.ToJsonElment(index));
                        if (!jsonModel.textures.Keys.Contains(index)) jsonModel.textures.Add(index, PathConverter(e.texture.Path));
                    }
                }
                current++;
                Current.DrawProgressBar(current * 100 / total);
            }
            jsonModel.elements = elements.ToArray();
            return jsonModel;
        }
        private static JsonModel DuplicateOverlap(JsonModel jsonModel)
        {
            var elements = jsonModel.elements;
            var obj = new object();

            Current.WriteLine(Environment.NewLine + "- Compressing Model:");
            Current.SetProgressBar();
            int current = 0;
            int total = elements.Length;

            #region Delect Overlap
            for (int i = 0; i < total; i++)
            {
                elements[i] = jsonModel.GetFaceElement(elements[i]);
                lock (obj)
                {
                    current++;
                    Current.DrawProgressBar(current * 100 / total);
                    Current.Write(" " + current + " elements have been disposed of.(total : " + total + " )");
                }
            }
            #endregion
            Current.Write("");
            return jsonModel;
        }
        private static JsonModel RemoveEmptyElement(JsonModel jsonModel)
        {
            #region Delect Empty Elements
            var _el = jsonModel.elements.ToList();
            _el.RemoveAll(e => e == null);
            _el.RemoveAll(e => e.faces == null || !((e.faces.ContainsKey("West") && e.faces["West"] != null) || (e.faces.ContainsKey("East") && e.faces["East"] != null) || (e.faces.ContainsKey("North") && e.faces["North"] != null) || (e.faces.ContainsKey("South") && e.faces["South"] != null) || (e.faces.ContainsKey("Down") && e.faces["Down"] != null) || (e.faces.ContainsKey("Up") && e.faces["Up"] != null)));
            foreach(var e in _el)
            {
                if (!((e.faces.ContainsKey("West") && e.faces["West"] != null) || (e.faces.ContainsKey("East") && e.faces["East"] != null) || (e.faces.ContainsKey("North") && e.faces["North"] != null) || (e.faces.ContainsKey("South") && e.faces["South"] != null) || (e.faces.ContainsKey("Down") && e.faces["Down"] != null) || (e.faces.ContainsKey("Up") && e.faces["Up"] != null))) Current.Error("Delect failed.");
            }
            jsonModel.elements = _el.ToArray();
            return jsonModel;
            #endregion
        }
        private static JsonModel ScaleModel(JsonModel jsonModel)
        {
            var elements = jsonModel.elements;
            if (!unlimit)
            {
                var max = elements.Select(element => element.to.Max()).Concat(new float[] { 0 }).Max();
                var changedAmount = (max - 32.0f) / max;
                if (changedAmount > 0)
                {
                    var obj = new Object();

                    Current.WriteLine("");
                    Current.WriteLine("- Scaling:");
                    Current.SetProgressBar();
                    int current = 0;
                    int total = elements.Length;

                    Parallel.ForEach(elements, element =>
                    {
                        for (var i = 0; i < 3; i++)
                        {
                            element.from[i] = Math.Max(element.from[i] - changedAmount * element.from[i], 0);
                            if (element.from[i] > 32) element.from[i] = 32;
                        }
                        for (var i = 0; i < 3; i++)
                        {
                            element.to[i] = Math.Max(element.to[i] - changedAmount * element.to[i], 0);
                            if (element.to[i] > 32) element.to[i] = 32;
                        }

                        lock (obj) //Update Progress
                        {
                            current++;
                            Current.DrawProgressBar(current * 100 / total);
                        }
                    });
                }
            }
            return jsonModel;
        }

        private static string PathConverter(string oldpath)
        {
            if (Program.version >= 1.13)
            {
                var m = oldpath.ToList();
                m.RemoveAt(5);
                return String.Join(null, m.ToArray());
            }
            else return oldpath;
        }
    }
}