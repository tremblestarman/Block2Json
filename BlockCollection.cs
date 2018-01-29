using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Forms;

namespace S2J
{
    public class BlockCollection
    {
        public List<Block> blocks = new List<Block>();
        public string Comments = "?";
        public class Block
        {
            #region Coordinate
            private Vector3 Coordinate = new Vector3();
            public void SetCoordinate(int X, int Y, int Z) { Coordinate.X = X; Coordinate.Y = Y; Coordinate.Z = Z; }
            public Vector3 GetCoordinate() { return Coordinate; }
            #endregion
            #region BlockInfo
            private BlockInfo BlockInfo { get; set; }
            public void SetInfo(BlockInfo blockInfo) { BlockInfo = blockInfo; }
            public BlockInfo GetBlockInfo() { return BlockInfo; }
            #endregion
            public List<JsonModel.SimpleElement> GetSimpleElements(string directoryPath, BlockCollection blockCollection, ResourceList res = null)
            {
                var infoElements = GetElement(blockCollection, res);
                var elements = new List<JsonModel.SimpleElement>();
                foreach(var e in infoElements)
                {
                    var g = e.GetElement(BlockInfo, GetCoordinate(), directoryPath);
                    elements.Add(e.GetElement(BlockInfo, GetCoordinate(), directoryPath));
                }
                return elements;
            }
            public List<ModelInfo._Enum.Element> GetElement(BlockCollection blockCollection, ResourceList res = null)
            { return BlockInfo.GetModelInfo().GetElements(BlockInfo, blockCollection, GetCoordinate(), res); }
        }
        public Block GetBlock(int X, int Y, int Z)
        {
            foreach (var e in blocks)
            {
                if (e.GetCoordinate().X == X && e.GetCoordinate().Y == Y && e.GetCoordinate().Z == Z) return e;
            }
            return null;
        }
        #region Size
        private int Width { get; set; }
        public void SetWidth(int width) { Width = width; }
        public int GetWidth() { return Width; }
        private int Height { get; set; }
        public void SetHeight(int height) { Height = height; }
        public int GetHeight() { return Height; }
        private int Length { get; set; }
        public void SetLength(int length) { Length = length; }
        public int GetLength() { return Length; }
        #endregion
    }
}
