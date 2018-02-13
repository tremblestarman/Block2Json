using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Block2Json
{
    public class ResourceList
    {
        public ResourceList(string StartupPath)
        {
            if (Directory.Exists(StartupPath + "\\ModelInfos"))
                SetModelInfoList(StartupPath + "\\ModelInfos");
            if (Directory.Exists(StartupPath + "\\BlockInfos"))
                SetBlockInfoList(StartupPath + "\\BlockInfos");
            if (Directory.Exists(StartupPath + "\\BlockTags"))
                SetBlockTagList(StartupPath + "\\BlockTags");
            
            foreach (var key in BlockInfoList.Keys)
            {
                foreach (var list in BlockInfoList[key])
                {
                    foreach (var block in list)
                    {
                        block.SetTag(BlockTagList);
                        block.SetModelInfo(ModelInfoList);
                    }
                }
            }
            BlockInfos2BlockTags(StartupPath + "\\BlockTags");
        }
        public ResourceList(string modelInfoDirectoryPath = null, string blockInfoDirectoryPath = null, string blockTagDirectoryPath = null)
        {
            if (modelInfoDirectoryPath != null && Directory.Exists(modelInfoDirectoryPath))
                SetModelInfoList(modelInfoDirectoryPath);
            if (blockInfoDirectoryPath != null && Directory.Exists(blockInfoDirectoryPath))
                SetBlockInfoList(blockInfoDirectoryPath);
            if (blockTagDirectoryPath != null && Directory.Exists(blockTagDirectoryPath))
                SetBlockTagList(blockTagDirectoryPath);

            foreach (var key in BlockInfoList.Keys)
            {
                foreach (var list in BlockInfoList[key])
                {
                    foreach (var block in list)
                    {
                        block.SetTag(BlockTagList);
                        block.SetModelInfo(ModelInfoList);
                    }
                }
            }
        }
        //Block Info List
        private Dictionary<string, List<List<BlockInfo>>> BlockInfoList = new Dictionary<string, List<List<BlockInfo>>>();
        private void SetBlockInfoList(string directoryPath, string upper = "")
        {
            FileInfo[] files = new DirectoryInfo(directoryPath).GetFiles();
            DirectoryInfo[] directories = new DirectoryInfo(directoryPath).GetDirectories();
            foreach (var file in files)
            {
                if (file.Extension == ".json")
                    BlockInfoList.Add(
                        ((upper == "") ? "" : upper + "/") + file.Name,
                        JsonConvert.DeserializeObject<List<List<BlockInfo>>>(File.ReadAllText(file.FullName))
                        );
            }
            foreach (var directory in directories)
            {
                SetBlockInfoList(directory.FullName, directory.Name);
            }
        }
        //Block Tag List
        public Dictionary<string, List<SimpleBlockInfo>> BlockTagList = new Dictionary<string, List<SimpleBlockInfo>>();
        private void SetBlockTagList(string directoryPath, string upper = "")
        {
            FileInfo[] files = new DirectoryInfo(directoryPath).GetFiles();
            DirectoryInfo[] directories = new DirectoryInfo(directoryPath).GetDirectories();
            foreach (var file in files)
            {
                if (file.Extension == ".json")
                    BlockTagList.Add(((upper == "") ? "" : upper + "/") + file.Name, GetSelectedBlocks(directoryPath, file.Name));
            }
            foreach (var directory in directories)
            {
                SetBlockTagList(directory.FullName, directory.Name);
            }
        }
        public List<SimpleBlockInfo> SimpleBlockInfoCollection(string expression, string directoryPath = null)
        {
            var items = expression.Split(';');
            var simpleBlockCollection = new List<SimpleBlockInfo>();

            foreach (var item in items)
            {
                if (new Regex(@".*(?=.json)").Match(item).Success)
                {
                    var i = item.Replace("/", "\\");
                    i = "\\BlockTags\\" + i;
                    if (directoryPath != null && File.Exists(directoryPath + i))
                    {
                        var newPairs = GetSelectedBlocks(directoryPath, i);
                        simpleBlockCollection.AddRange(newPairs);
                    }
                    else if (BlockTagList.Keys.Contains(i)) simpleBlockCollection.AddRange(BlockTagList[i]);
                }
                else
                    simpleBlockCollection.Add(new SimpleBlockInfo(item));
            }
            return simpleBlockCollection;
        }
        private List<SimpleBlockInfo> GetSelectedBlocks(string directoryPath, string contents)
        {
            List<SimpleBlockInfo> simpleBlockInfos = new List<SimpleBlockInfo>();
            foreach (var block in ReadBlockExpressions(directoryPath, File.ReadAllText(directoryPath + "\\" + contents)))
            {
                simpleBlockInfos.Add(new SimpleBlockInfo(block));
            }
            return simpleBlockInfos;
        }
        private List<string> ReadBlockExpressions(string directoryPath, string content)
        {
            var Values = new List<string>();
            content = content.Replace(Environment.NewLine, "");

            #region Get Elements
            var plus = JsonConvert.DeserializeObject<BlockTag>(content).Plus;
            var minus = JsonConvert.DeserializeObject<BlockTag>(content).Minus;

            if (plus != null)
                foreach (var element in plus)
                {
                    var _element = element.Replace("/", "\\");
                    if (_element == null) continue;
                    else if (new Regex(@".*(?=.json)").Match(element).Success)
                    {
                        if (File.Exists(directoryPath + "\\" + _element))
                        {
                            var file = new FileInfo(directoryPath + "\\" + _element);
                            var _content = File.ReadAllText(file.FullName);
                            Values.AddRange(ReadBlockExpressions(directoryPath, _content));
                        }
                    }
                    else
                    {
                        if (!Values.Contains(element))
                            Values.Add(element);
                    }
                }
            if (minus != null)
                foreach (var element in minus)
                {
                    var _element = element.Replace("/", "\\");
                    if (_element == null) continue;
                    else if (new Regex(@".*(?=.json)").Match(element).Success)
                    {
                        if (File.Exists(directoryPath + "\\" + _element))
                        {
                            var file = new FileInfo(directoryPath + "\\" + _element);
                            var _content = File.ReadAllText(file.FullName);
                            var _new = ReadBlockExpressions(directoryPath, _content);
                            Values.RemoveAll(item => _new.Contains(item));
                        }
                    }
                    else
                    {
                        if (Values.Contains(element))
                            Values.Remove(element);
                    }
                }
            #endregion
            return Values;
        }
        private void BlockInfos2BlockTags(string directoryPath = null)
        {
            var blockTag = new BlockTag();
            blockTag.Plus = new List<string>();
            if (directoryPath != null)
                foreach (var key in BlockInfoList.Keys)
                    foreach (var blocks in BlockInfoList[key])
                        foreach (var b in blocks)
                            blockTag.Plus.Add(b.Id + "[" + b.Data + "]");
            File.WriteAllText(directoryPath + "\\All.json", JsonConvert.SerializeObject(blockTag));
        }
        //Model Info List
        public Dictionary<string, ModelInfo> ModelInfoList = new Dictionary<string, ModelInfo>();
        private void SetModelInfoList(string directoryPath, string upper = "")
        {
            FileInfo[] files = new DirectoryInfo(directoryPath).GetFiles();
            DirectoryInfo[] directories = new DirectoryInfo(directoryPath).GetDirectories();
            foreach (var file in files)
            {
                if (file.Extension == ".json")
                    ModelInfoList.Add(
                        ((upper == "") ? "" : upper + "/") + file.Name,
                        JsonConvert.DeserializeObject<ModelInfo>(File.ReadAllText(file.FullName))
                        );
            }
            foreach (var directory in directories)
            {
                SetBlockInfoList(directory.FullName, directory.Name);
            }
        }

        //Get
        public BlockInfo GetBlockInfo(string id, string datas, double version)
        {
            var blockList = GetBlockList(id, datas);
            if (blockList == null)
            {
                Current.Error("Block Model: " + id + "[" + datas + "] Not Found", false);
                return null;
            }
            foreach (var model in blockList)
            {
                if ( (model.VersionFrom <= version && model.VersionFrom > 0) || model.VersionFrom == 0)
                {
                    if (model.VersionTo > version || model.VersionTo == 0)
                    {
                        return model;
                    }
                }
            }
            Current.Error("Block Version: " + id + "[" + datas + "] Not Correct", false);
            return null;
        }
        public List<BlockInfo> GetBlockList(string id, string datas)
        {
            var t = new List<BlockInfo>();
            if (!Regex.Match(id, @"\d+").Success && !Regex.Match(id, @"^minecraft:").Success) id = "minecraft:" + id;

            foreach (var key in BlockInfoList.Keys)
                {
                t = BlockInfoList[key].Find
                (
                    blockList
                    => blockList.Any
                    (
                            blockInfo
                            => blockInfo.Id == id && Universal.Match(datas, blockInfo.Data)
                    )
                );
                if (t != null) return t;
            }
            return null;
        }

        //Delect
        public void DelectBlockInfoClass(string blockInfoClass)
        {
            if (BlockInfoList.Keys.Contains(blockInfoClass))
                BlockInfoList.Remove(blockInfoClass);
            else
                Current.Error("Filter Class: \"" + blockInfoClass + "\" Not Found", false);
        }
        public void DelectBlockInfo(string id, string datas)
        {
            var Success = false;
            foreach (var key in BlockInfoList.Keys)
            {
                var s = BlockInfoList[key].RemoveAll
                (
                    (List<BlockInfo> blockList)
                    => blockList.Exists
                    (
                            (BlockInfo blockInfo)
                            => blockInfo.Id == id && Universal.Match(blockInfo.Data, datas)
                    )
                );
                if (s > 0) Success = true;
            }
            if (!Success)
            {
                Current.Error("Filter Block: \"" + id + "[" + datas + "]\" Not Found", false);
            }
            return;
        }
    }

    public class BlockTag
    {
        public List<string> Plus { get; set; }
        public List<string> Minus { get; set; }
    }
}
