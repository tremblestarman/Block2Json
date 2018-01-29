using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using fNbt;

namespace S2J
{
    public class Program
    {
        #region Static
        static ResourceList res; //ResourceList
        static BlockCollection bcl; //BlockCollection
        static double version = 1.122; //Current Version
        static string target = ".schematic"; //Target
        static public bool unlimit = false; static public bool smooth = false; static public bool nopause = false; static public bool log = false; static public bool nocompress = false;//Essential Options
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
            nocompress = args.Contains("nocompress");
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
                    fbs = res.SimpleBlockInfoCollection(Regex.Match(reg, "(?<=filter_blocks=).*$").Value, Application.StartupPath + "\\BlockTags");
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
            Current.WriteLine("Unlimit = " + unlimit.ToString() + " ,Smooth = " + smooth.ToString() + ";" + " ,Nocompress = " + nocompress.ToString() + ";");
            #endregion
            var jsonModel = JsonModel(Application.StartupPath);
            if (!nocompress) jsonModel = RemoveInteriorFaces(jsonModel);
            jsonModel = ScaleModel(RemoveEmplyElement(jsonModel));
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
            #endregion
            var obj = new Object();

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
                    if (id != "minecraft:air")
                    {
                        var blockInfo = res.GetBlockInfo(id, data, version);
                        if (blockInfo != null)
                        {
                            block.SetCoordinate((int)pos.X, (int)pos.Y, (int)pos.Z);
                            block.SetInfo(blockInfo);
                            bcl.blocks.Add(block);
                        }
                    }
                }

                //Update Progress
                current++;
                Current.DrawProgressBar(current * 100 / total);               
            }
            #endregion
        }
        private static JsonModel JsonModel(string directoryPath)
        {
            var obj = new Object();

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
                lock (obj)
                {
                    var l = block.GetSimpleElements(directoryPath, bcl, res);
                    foreach (var e in l)
                    {
                        var index = block.GetBlockInfo().Id + "_" + block.GetBlockInfo().Data;
                        elements.Add(e.ToJsonElment(index));
                        if (!jsonModel.textures.Keys.Contains(index)) jsonModel.textures.Add(index, e.texture.Path);
                    }
                    current++;
                    Current.DrawProgressBar(current * 100 / total);
                }
            }
            jsonModel.elements = elements.ToArray();
            return jsonModel;
        }
        private static JsonModel RemoveInteriorFaces(JsonModel jsonModel)
        {
            var elements = jsonModel.elements;
            var obj = new object();

            Current.WriteLine(Environment.NewLine + "- Compressing Model:");
            Current.SetProgressBar();
            int current = 0;
            int total = elements.Length;

            #region Delect Interior Faces
            foreach (var e in elements)
            {
                var facings = jsonModel.GetInners(e);
                if (facings != null)
                    lock (obj)
                    {
                        foreach (var facing in facings)
                        {
                            if (facing != null)
                            e.faces.Remove(facing);
                        }
                        current++;
                        Current.DrawProgressBar(current * 100 / total);
                    }
            }
            #endregion
            return jsonModel;
        }
        private static JsonModel RemoveEmplyElement(JsonModel jsonModel)
        {
            #region Delect Empty Elements
            var _el = jsonModel.elements.ToList();
            _el.RemoveAll(e => e == null);
            _el.RemoveAll(e => e.faces == null || e.faces.Count == 0);
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
                        }
                        for (var i = 0; i < 3; i++)
                        {
                            element.to[i] = Math.Max(element.to[i] - changedAmount * element.to[i], 0);
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

        /*static void Main(string[] args)
        {
            
            Current.Write("Reading Model Classes ...");
            modelList.UpdateList(Application.StartupPath + "\\ModelClasses"); //Update Model Classes

            Current.Write("Reading Block Tags ...");
            modelList.List2BlockTags(Application.StartupPath + "\\BlockTags");
            blockTags.UpdateList(Application.StartupPath + "\\BlockTags"); //Update Block Tags
            Current.Write("");
            #region File & Options
            string file;
            //Current Version = 1.12.2
            version = 1.122;
            
            # region Get File
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
                        foreach (Match res in Regex.Matches(Regex.Matches(reg, "version=.*$")[0].Value, @"\d*\.\d*"))
                        {
                            v1st.Add(double.Parse(res.Value));
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
                modelList.DelectClass(c);
            }
            #endregion
            #region For Blocks
            var filterBlocks = "";
            var fbs = new List<BlockTags.BlockPair>();
            //Get filter_blocks
            foreach (string reg in args)
            {
                if (Regex.Match(reg, "(?<=filter_blocks=).*$").Success)
                {
                    filterBlocks = Regex.Match(reg, "(?<=filter_blocks=).*$").Value;
                    fbs = blockTags.NewBlockPairs(Regex.Match(reg, "(?<=filter_blocks=).*$").Value, Application.StartupPath + "\\BlockTags");
                }
            }
            //Remove from List
            foreach (var b in fbs)
            {
                modelList.DelectBlock(b.Id, b.Data);
            }


            #endregion
            #endregion

            #region InputInfo
            Current.WriteLine("");
            Current.WriteLine("- Info:");
            //-output target
            Current.WriteLine("Target = " + target.Replace(".","").ToUpper());
            //-output size
            Current.WriteLine("Width = " + model.GetWidth() + " ,Height = " + model.GetHeight() + " ,Length = " + model.GetLength() + ";");
            //-output filter
            var ig_b = String.Join(";", filterBlocks);
            Current.WriteLine(ig_b == "" ? "Filter Blocks = none;" : "Filter Blocks = " + ig_b + ";");
            var ig_m = String.Join(";", filterClasses);
            Current.WriteLine(ig_m == "" ? "Filter Models = none;" : "Filter Models = " + ig_m + ";");
            //-output biome
            Current.WriteLine(biome == "" ? "Biome = none" : "Biome = " + biome + ";");
            //-output mode
            Current.WriteLine("Unlimit = " + unlimit.ToString() + " ,Smooth = " + smooth.ToString() + ";");
            #endregion
            #endregion

            if (new FileInfo(file).Extension == ".schematic") //Read Schematic
            {
                Schematic2Model(file);
            }

            elements.RemoveAll(element => element == null);
            ModelGenerating(elements); //Generating

            ScaleModel(elements); //Scale Model
            
            model.elements = elements.ToArray();

            #region Generating File
            Current.WriteLine("");
            Current.WriteLine("- Generating File:");

            var extension = Path.GetExtension(file);
            var newFile = file.Replace(extension, ".json");
            File.WriteAllText(newFile, JsonConvert.SerializeObject(model, Formatting.Indented));

            Current.WriteLine(newFile + " has been Created");
            #endregion
            if (log) File.WriteAllText("Log -" + DateTime.Now.ToFileTime() + ".txt", Current.Log.ToString());
            if (nopause) return;
            Current.WriteLine("[ Press any key to continue... ]");
            Console.ReadKey(true);
        }
        private static void Schematic2Model(string file)
        {
            #region Initialization
            var nbt = new NbtFile();
            nbt.LoadFromFile(file);
            var root = nbt.RootTag; //Read File

            model.SetWidth(root.Get<NbtShort>("Width").ShortValue); //Get Width
            model.SetHeight(root.Get<NbtShort>("Height").ShortValue);
            model.SetLength(root.Get<NbtShort>("Length").ShortValue);

            model.__comment = "Created by S2J"; //Set Comment
            var blocks = root.Get<NbtByteArray>("Blocks").ByteArrayValue; //Initialize Blocks
            var datas = root.Get<NbtByteArray>("Data").ByteArrayValue; //Initialize Datas
            #endregion
            var obj = new Object();

            Current.WriteLine(Environment.NewLine + "- Reading Schematic:");
            Current.SetProgressBar();
            int current = 0;
            int total = model.GetWidth();

            #region Read Schematic
            Parallel.For(0, model.GetWidth(), x =>
            {
                for (int y = 0; y < model.GetHeight(); y++)
                {
                    for (int z = 0; z < model.GetLength(); z++)
                    {
                        var element = new Model.Element();
                        var index = y * model.GetWidth() * model.GetLength() + z * model.GetWidth() + x;
                        if (blocks[index] != 0)
                        {
                            var blockInfo = modelList.GetBlockModel(blocks[index].ToString(), datas[index].ToString(), version);

                            if (blockInfo != null)
                            {
                                element.SetCoordinate(x, y, z);
                                element.SetInfo(blockInfo);
                                elements.Add(element);
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
        private static void ModelGenerating(List<Model.Element> elements)
        {
            var textures = model.textures; var obj = new Object();

            Current.WriteLine(Environment.NewLine + "- Generating Model:");
            Current.SetProgressBar();
            int current = 0;
            int total = elements.Count;

            Parallel.ForEach(elements, element =>
            {
                try
                {
                    if (element != null)
                    {
                        var _Size = GetSize(element.GetCoordinate(), element.GetInfo());
                        var _Texture = GetTexture(temp, rain, element.GetInfo());
                        lock (obj)
                        {
                            #region Size
                            if (_Size != null)
                            {
                                element.from = new float[] { (float)_Size.from[0], (float)_Size.from[1], (float)_Size.from[2] };
                                element.to = new float[] { (float)_Size.to[0], (float)_Size.to[1], (float)_Size.to[2] };
                            }
                            #endregion
                            #region Texture
                            if (_Texture != null)
                            {
                                var id = element.GetInfo().Id; var data = element.GetInfo().Data;
                                //if (id == "2") MessageBox.Show(_Texture.uv[0] + " " + _Texture.uv[1]);
                                var key = id + "_" + data;
                                if (!textures.Keys.Contains(key)) textures.Add(key, _Texture.texture);
                                var face = new Model.Element.Face { texture = "#" + id + "_" + data, uv = new[] { _Texture.uv[0], _Texture.uv[1], _Texture.uv[0], _Texture.uv[1] } };
                                element.faces = new Dictionary<string, Model.Element.Face>
                                {
                                    {"North", face},
                                    {"East", face},
                                    {"South", face},
                                    {"West", face},
                                    {"Up", face},
                                    {"Down", face}
                                };
                            }
                            #endregion

                            //Update Progress
                            current++;
                            Current.DrawProgressBar(current * 100 / total);
                        }
                    }
                }
                catch(Exception e) { Current.Error("Model: Encountered some Undetermined Problems", false, false, e); }
            });
        }
        private static void ScaleModel(List<Model.Element> elements)
        {
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
                    int total = elements.Count;

                    Parallel.ForEach(elements, element =>
                    {
                        for (var i = 0; i < 3; i++)
                        {
                            element.from[i] = Math.Max(element.from[i] - changedAmount * element.from[i], 0);
                        }
                        for (var i = 0; i < 3; i++)
                        {
                            element.to[i] = Math.Max(element.to[i] - changedAmount * element.to[i], 0);
                        }

                        lock (obj) //Update Progress
                        {
                            current++;
                            Current.DrawProgressBar(current * 100 / total);
                        }
                    });
                }
            }
        }

        private static Size GetSize(Model.Element.Coord Coordinate, ModelClass.BlockInfo blockInfo)
        {
            var dx = blockInfo.Size.x < 0 ? 0 : blockInfo.Size.x;
            var dy = blockInfo.Size.y < 0 ? 0 : blockInfo.Size.y;
            var dz = blockInfo.Size.z < 0 ? 0 : blockInfo.Size.z;
            var mode = blockInfo.Mode == null ? new Dictionary<string, string>() { { "Half", "Bottom" } } : blockInfo.Mode;
            var Size = new Size();
            var center = new double[] { Coordinate.X + 0.5, Coordinate.Y + 0.5, Coordinate.Z + 0.5 };

            if (mode.Keys.Contains("Half")) //Half
            {
                switch (mode["Half"]){
                    case "Top":
                        center[1] += 0.5;
                        Size.from = new double[] { center[0] - dx, center[1], center[2] - dz };
                        Size.to = new double[] { center[0] + dx, center[1] - dy, center[2] + dz };
                        break;
                    case "Bottom":
                        center[1] -= 0.5;
                        Size.from = new double[] { center[0] - dx, center[1], center[2] - dz };
                        Size.to = new double[] { center[0] + dx, center[1] + dy, center[2] + dz };
                        break;
                    default:
                        center[1] -= 0.5;
                        Size.from = new double[] { center[0] - dx, center[1], center[2] - dz };
                        Size.to = new double[] { center[0] + dx, center[1] + dy, center[2] + dz };
                        break;
                }
            }
            else if (mode.Keys.Contains("Wall")) //Wall
            {
                switch (mode["Wall"])
                {
                    case "North":
                        center[2] += 0.5;
                        Size.from = new double[] { center[0] - dx, center[1] - dz, center[2] - dy };
                        Size.to = new double[] { center[0] + dx, center[1] + dz, center[2] + dy };
                        break;
                    case "South":
                        center[2] -= 0.5;
                        Size.from = new double[] { center[0] - dx, center[1] - dz, center[2] };
                        Size.to = new double[] { center[0] + dx, center[1] + dz, center[2] + dy };
                        break;
                    case "West":
                        center[0] += 0.5;
                        Size.from = new double[] { center[0] - dy, center[1] - dx, center[2] - dz };
                        Size.to = new double[] { center[0] + dy, center[1] + dx, center[2] + dz };
                        break;
                    case "East":
                        center[0] -= 0.5;
                        Size.from = new double[] { center[0], center[1] - dx, center[2] - dz };
                        Size.to = new double[] { center[0] + dy, center[1] + dx, center[2] + dz };
                        break;
                    default:
                        center[0] += 0.5;
                        Size.from = new double[] { center[0], center[1] - dx, center[2] - dz };
                        Size.to = new double[] { center[0] - dy, center[1] + dx, center[2] + dz };
                        break;
                }
            }
            else //Default
            {
                center[1] -= 0.5;
                Size.from = new double[] { center[0] - dx, center[1], center[2] - dz };
                Size.to = new double[] { center[0] + dx, center[1] + dy, center[2] + dz };
            }

            if (Size.from != null && Size.to != null)
            {
                return Size;
            }
            else return null;
        }
        private static Texture GetTexture(float temp, float rain, ModelClass.BlockInfo blockInfo)
        {
            var texture = blockInfo.Texture;
            var mode = blockInfo.Mode == null ? new Dictionary<string, string>() { { "Default", "Default" } } : blockInfo.Mode;
            var Texture = new Texture();
            Texture.texture = texture;

            if (mode.Keys.Contains("ColorMap")) //ColorMap
            {
                var pars = mode["ColorMap"].Split(' ');
                if (pars.Length != 4)
                    Texture.uv = getColorMapUV(temp, rain, 0, false, 0);
                else
                    Texture.uv = getColorMapUV(temp, rain, int.Parse(pars[0]), bool.Parse(pars[1]), float.Parse(pars[2]), float.Parse(pars[3]));
                    
            }
            else //Default
            {
                bool textureError = true;
                if (smooth)
                {
                    try
                    {
                        Texture.uv[0] = 0.0f; Texture.uv[1] = 0.0f;
                        Bitmap compare = new Bitmap(Application.StartupPath + "\\textures\\" + texture.Replace("/", "\\") + ".png");
                        for (int mh = 0; mh < compare.Size.Height; mh++)
                        {
                            for (int mw = 0; mw < compare.Size.Width; mw++)
                            {
                                if (compare.GetPixel(mh, mw).A > 8) { Texture.uv[0] = mh; Texture.uv[1] = mw; break; }
                            }
                            if (Texture.uv[0] > 0 && Texture.uv[1] > 0) break;
                        }
                    }
                    catch
                    {
                        Current.Error("Texture: " + texture + ".png Not Found", false);
                    }
                }
                else
                {
                    while (textureError)
                    {
                        Texture.uv[0] = (float)StaticRandom.NextDouble() * 16;
                        Texture.uv[1] = (float)StaticRandom.NextDouble() * 16;
                        try
                        {
                            Bitmap compare = new Bitmap(System.Windows.Forms.Application.StartupPath + "\\textures\\" + texture.Replace("/", "\\") + ".png");
                            textureError = (compare.GetPixel((int)Texture.uv[0], (int)Texture.uv[1]).A <= 8);
                        }
                        catch
                        {
                            Current.Error("Texture: " + texture + ".png Not Found", false);
                        }
                    }
                }
            }
            return Texture;
        }
        private static float[] getColorMapUV(float temp, float rain, int pixel_range = 0, bool rich = false, float warmer = 0.0f, float wetter = 0.0f)
        {
            float[] uv = new[] { 0.0f, 0.0f };
            double uv_x = 0.0f, uv_y = 0.0f;

            bool rangeEroor = true;

            while (rangeEroor)
            {
                if (!smooth)
                {
                    var theta = StaticRandom.NextDouble() * 360;
                    var d = StaticRandom.NextDouble() * pixel_range - pixel_range / 2;
                    uv_x = temp + (warmer / 256f) + (d / 256f) * Math.Cos(theta * Math.PI / 360); uv_y = temp * rain + (wetter / 256f) + (d / 256f) * Math.Sin(theta * Math.PI / 360);
                }
                else
                {
                    uv_x = temp + (warmer / 256f); uv_y = temp * rain + (wetter / 256f);
                }
                if (uv_x <= 1 && uv_x >= 0 && uv_y <= 1 && uv_y >= 0 && uv_x >= uv_y) rangeEroor = false;
            }
            
            if (!rich)
            {
                uv_x = 1 - uv_x;
                uv_y = 1 - uv_y;
            }
            uv[0] = (float)Math.Round(uv_x * 16);
            uv[1] = (float)Math.Round(uv_y * 16);
            return uv;

        }
        public class Size
        {
            public double[] from;
            public double[] to;
        }
        public class Texture
        {
            public string texture;
            public float[] uv = new float[] { 0f, 0f };
        }
*/
    }
}