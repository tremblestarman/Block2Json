using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace S2J
{
    public class BlockInfo
    {
        private double _versionFrom = 0;
        public double VersionFrom { get { return _versionFrom; } set { if (value != 0) _versionFrom = value; } } //VersionFrom
        private double _versionTo = 0;
        public double VersionTo { get { return _versionTo; } set { if (value != 0) _versionTo = value; } } //VersionTo

        private string _id = "";
        public string Id { get { return _id; } set { if (value != null) _id = value; } } //Id
        private string _data = "x";
        public string Data { get { return _data; } set { if (value != null) _data = value; } } //Data
        private List<string> _dataslist = new List<string>(); //List of Datas
        private void SetDataList()
        {
            _dataslist = _data.Split(',').ToList();
        }
        public List<string> DataList()
        {
            return _dataslist;
        }

        private Vector3 _size = new Vector3();
        public Vector3 Size { get { return _size; } set { if (value != null) _size = value; } } //Size
        public _Model _model = new _Model();
        public _Model Model { get { return _model; } set { if (value != null) _model = value; } } //Model

        public class _Model
        {
            private string _model = "Half.json";
            public string Name { get { return _model; } set { if (value != null) _model = value; } } //Name of Model
            private Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();
            public Dictionary<string, Texture> Textures { get { return _textures; } set { if (value != null) _textures = value; } } //Texture
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
        }

        private ModelInfo _modelInfo = new ModelInfo();
        public void SetModelInfo(Dictionary<string, ModelInfo> modelInfoList)
        {
            foreach (var key in modelInfoList.Keys)
            {
                if (key == Model.Name)
                    _modelInfo = modelInfoList[key];
            }
        }
        public ModelInfo GetModelInfo() { return _modelInfo; }

        private List<string> _tags = new List<string>();
        public void SetTag(Dictionary<string, List<SimpleBlockInfo>> blockTagList)
        {
            foreach (var tag in blockTagList.Keys)
            {
                foreach (var target in blockTagList[tag])
                {
                    if (Universal.MatchBlock(this, target)) TagAdd(tag);
                }
            }
        }
        public void TagAdd(string tagExpression)
        {
            if (!TagsContain(tagExpression))
            {
                var l = tagExpression.Split(';').ToList();
                foreach (var tag in l) _tags.Add(tag);
            }
        }
        public void TagRemove(string tagExpression)
        {
            if (TagsContain(tagExpression))
            {
                var l = tagExpression.Split(';').ToList();
                foreach(var tag in l) _tags.Remove(tag);
            }
        }
        public void TagClear()
        {
            _tags = new List<string>();
        }
        public bool TagsContain(string tagExpression)
        {
            return Universal.Match(_tags, tagExpression.Split(';').ToList());
        }
        public bool TagsContain(List<string> tags)
        {
            return Universal.Match(_tags, tags);
        }
        public bool TagsAllMet(string tagExpression)
        {
            return Universal.Match(tagExpression.Split(';').ToList(), _tags);
        }
        public bool TagsAllMet(List<string> tags)
        {
            return Universal.Match(tags, _tags);
        }
    }

    public class SimpleBlockInfo
    {
        public SimpleBlockInfo(string block)
        {
            //Id
            if (Regex.Match(block, @".*(?=\[.*\]$)").Success)
            {
                Id = Regex.Matches(block, @".*(?=\[.*\]$)")[0].Value;
            }
            else if (Regex.Match(block, @"\S+").Success)
            {
                Id = Regex.Matches(block, @"\S+")[0].Value;
            }
            if (!Regex.Match(block, @"\d+").Success && !Regex.Match(block, @"^minecraft:").Success) Id = "minecraft:" + Id;

            //Data
            if (Regex.Match(block, @"(?<=\[).*(?=\])").Success)
            {
                Data = Regex.Matches(block, @"(?<=\[).*(?=\])")[0].Value;
            }
        }
        private string _id = "?";
        public string Id { get { return _id; } set { _id = value; } }
        private string _data = "x";
        public string Data { get { return _data; } set { _data = value; } }
    }
}
