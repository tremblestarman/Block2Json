using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Block2Json
{
    //Origin: https://github.com/erich666/Mineways/blob/master/Win/biomes.cpp
    static public class BiomeList
    {
        static public Dictionary<string, Biome> _biome = new Dictionary<string, Biome>();
        public class Biome
        {
            public float Temperature;
            public float Rainfall;
        }
        static public void UpdateList(string directoryPath)
        {
            FileInfo[] files = new DirectoryInfo(directoryPath).GetFiles();
            DirectoryInfo[] directories = new DirectoryInfo(directoryPath).GetDirectories();
            foreach (var file in files)
            {
                var newbiome = new Dictionary<string, Biome>();
                if (file.Extension == ".json")
                    _biome = _biome.Concat(JsonConvert.DeserializeObject<Dictionary<string, Biome>>(File.ReadAllText(file.FullName)))
                                   .ToDictionary(item => item.Key, item => item.Value);
            }
            foreach (var directory in directories)
            {
                UpdateList(directory.FullName);
            }
        }
        static public Biome GetBiome(string biomeName)
        {
            if (_biome.Keys.Contains(biomeName))
                return _biome[biomeName];
            else
            {
                Current.Error("Biome: Biome " + biomeName + " Not Found.");
                return null;
            }
        }
    }
}
