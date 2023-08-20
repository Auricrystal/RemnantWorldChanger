using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static RemnantWorldChanger.DataPackage;

namespace RemnantWorldChanger
{
    public class BulkSave
    {
        static Random _R = new Random();
        public static Random R { get { return _R; } }
        public static T RandomEnumValue<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(_R.Next(v.Length - 1) + 1);
        }
        public ObservableCollection<DataPackage> SaveInfo { get; set; }

        private Dictionary<Guid, byte[]> GuidToBytes { get; set; }

        public BulkSave(ObservableCollection<DataPackage> header, Dictionary<Guid, byte[]> data)
        {
            SaveInfo = header;
            GuidToBytes = data;
        }
        public BulkSave()
        {
            SaveInfo = new ObservableCollection<DataPackage>();
            GuidToBytes = new Dictionary<Guid, byte[]>();
        }

        public void AddSave(byte[] savedata, SaveType type = SaveType.All, string world = "Earth", string name = "Unknown", SaveDifficulty diff = SaveDifficulty.Unset, string mods = "")
        {
            Guid guid = Guid.NewGuid();

            GuidToBytes.Add(guid, savedata);
            var dp = new DataPackage(guid) { Difficulty = diff, World = world, Name = name, Mods = mods, Type = type };
            SaveInfo.Add(dp);
            //Debug.WriteLine("Created: "+dp);
        }
        public void AddSave(byte[] savedata, DataPackage dp)
        {
            GuidToBytes.Add(dp.ID, savedata);
            SaveInfo.Add(dp);
        }

        public static DirectoryInfo TryGetSolutionDirectoryInfo(string currentPath = null)
        {
            var directory = new DirectoryInfo(
                currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory;
        }

        [Conditional("DEBUG")]
        public void SerializeData(string name)
        {
            if (!Directory.Exists(name))
                Directory.CreateDirectory(name);
            File.WriteAllText($@"{name}\Data.RIndex",
            JsonSerializer.Serialize(SaveInfo, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            File.WriteAllText($@"{name}\Data.RData",
            JsonSerializer.Serialize(GuidToBytes, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        public static BulkSave DeserializeData(string path)
        {
            var header = new ObservableCollection<DataPackage>(Directory.EnumerateFiles(path, "*.RIndex").Select(s => JsonSerializer.Deserialize<ObservableCollection<DataPackage>>(File.ReadAllText(s))).SelectMany(x => x));

            var data = Directory.EnumerateFiles(path, "*.RData").Select(s => JsonSerializer.Deserialize<Dictionary<Guid, byte[]>>(File.ReadAllText(s))).SelectMany(x => x).ToDictionary(x => x.Key, y => y.Value);

            return new BulkSave(header, data);
        }

    }

    public class DataPackage
    {
        public enum SaveDifficulty { Unset, Survivor, Veteran, Nightmare, Apocalypse }
        public enum SaveType { All, MiniBoss, WorldBoss, SideD, OverworldPOI, Vendor }

        [JsonConstructor]
        public DataPackage() { }
        public DataPackage(Guid guid, SaveType type = SaveType.All, SaveDifficulty diff = SaveDifficulty.Unset, string world = "", string name = "", string modifiers = "")
        {
            ID = guid;
            Difficulty = diff;
            World = world;
            Type = type;
            Name = name;
            Mods = modifiers;

        }
        public Guid ID { get; set; }
        public SaveDifficulty Difficulty { get; set; }
        public string World { get; set; }
        public SaveType Type { get; set; }
        public string Name { get; set; }
        public string Mods { get; set; }

        public override string? ToString()
        {
            return $"Type: {Type.ToString()} World: {World} Name: {Name} Difficulty: {Difficulty.ToString()} Mods: {Mods} GUID: {ID}";
        }
    }

}
