using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using static RemnantWorldChanger.DataPackage;
using static System.Net.Mime.MediaTypeNames;

namespace RemnantWorldChanger
{
    public class BulkSave
    {
        static Random _R = new Random();
        public static Random R { get { return _R; } }
        public static T RandomEnumValue<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(_R.Next(v.Length - 1) + 1)!;
        }
        public ObservableCollection<DataPackage> SaveInfo { get; set; }


        public Dictionary<Guid, byte[]> GuidToBytes { get; set; }
        public BulkSave()
        {
            SaveInfo = new ObservableCollection<DataPackage>();
            GuidToBytes = new Dictionary<Guid, byte[]>();
        }

        public BulkSave(ObservableCollection<DataPackage>? header, Dictionary<Guid, byte[]>? data) : this()
        {
            if (header is null || data is null)
                return;
            SaveInfo = header;
            GuidToBytes = data;
        }




        public void AddSave(byte[] savedata, SaveType type = SaveType.All, string world = "Earth", string name = "Unknown", SaveDifficulty diff = SaveDifficulty.Unset, string mods = "")
        {
            Guid guid = Guid.NewGuid();

            GuidToBytes.Add(guid, savedata);
            var dp = new DataPackage(guid) { Difficulty = diff, World = world, Name = name, Mods = mods, Type = type };
            SaveInfo.Add(dp);
        }
        public void AddSave(byte[] savedata, DataPackage dp)
        {
            GuidToBytes.Add(dp.ID, savedata);
            SaveInfo.Add(dp);
        }


        public static DirectoryInfo TryGetSolutionDirectoryInfo(string currentPath = null)
        {
            var directory = new DirectoryInfo(currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory!;
        }

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
            var indexes = Directory.EnumerateFiles(path, "*.RIndex");
            MessageBoxResult result = MessageBoxResult.None;
            if (indexes.Count() > 1)
                result = MessageBox.Show("Merge Packages?", "Multiple Data Files", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.No)
                return new BulkSave(
                    JsonSerializer.Deserialize<ObservableCollection<DataPackage>>(File.ReadAllText(path + "\\Data.RIndex")),
                    JsonSerializer.Deserialize<Dictionary<Guid, byte[]>>(File.ReadAllText(path + "\\Data.RData")));
            result = MessageBoxResult.None;
            ObservableCollection<DataPackage> header = new();
            Dictionary<Guid, byte[]> bulk = new();

            foreach (string index in indexes)
            {
                Debug.WriteLine("Start Deserialize");

                var data = Path.ChangeExtension(index, ".RData");
                if (!File.Exists(data))
                {
                    MessageBox.Show($"{Path.GetFileName(data)} does not exist!!");
                    continue;
                }
                var _ = JsonSerializer.Deserialize<ObservableCollection<DataPackage>>(File.ReadAllText(index));
                if (_ is null)
                    continue;

                List<DataPackage> dupelist, newlist;
                if (header.Count == 0)
                {
                    Debug.WriteLine("Initial Deserialization");
                    header = new ObservableCollection<DataPackage>(_);
                    Debug.WriteLine("Data 1 Deserialization");

                    JsonSerializer.Deserialize<Dictionary<Guid, byte[]>>(File.ReadAllText(data))!.ToList().ForEach(pair => bulk[pair.Key] = pair.Value);

                    Debug.WriteLine($"Data: {bulk.Count}");
                    continue;
                }


                dupelist = _.ToList().FindAll(x => header.Any(y => y.ID == x.ID));
                newlist = _.ToList().FindAll(x => !header.Any(y => y.ID == x.ID));

                Debug.WriteLine($"New: {newlist.Count}\nDupes: {dupelist.Count}");


                foreach (var item in newlist)
                    header.Add(item);
                int i = dupelist.Count;
            
                foreach (var dp in dupelist)
                {
                    var id = header.ToList().FindIndex(x => x.ID == dp.ID);
                    result = MessageBox.Show($"Duplicate GUID Entries ({i--}):\n\nExisting:\n{header[id]}\n\nNew:\n{dp}\n\nReplace existing with new?", "Duplicate GUID", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                        header[id] = dp;
                }




                Debug.WriteLine("Data 2 Deserialization");

                JsonSerializer.Deserialize<Dictionary<Guid, byte[]>>(File.ReadAllText(data))!.ToList().ForEach(pair => bulk[pair.Key] = pair.Value);

                Debug.WriteLine($"Data: {bulk.Count}");

            }
            var bs = new BulkSave(header, bulk);

            Directory.Delete(path, true);
            bs.SerializeData(path);
            return bs;
        }

    }

    public class DataPackage : IEquatable<DataPackage>
    {
        public enum SaveDifficulty { Unset, Survivor, Veteran, Nightmare, Apocalypse }
        public enum SaveType { All, MiniBoss, WorldBoss, SideD, OverworldPOI, Vendor, ItemDrop }

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

        public bool Equals(DataPackage? other)
        {
            if (other is null)
            {
                //Debug.WriteLine($"Comparing:{this}\nTO\nNULL ");
                return false;
            }

            //if (this.ID == other.ID)
            //{
            //    Debug.WriteLine($"Comparing:{this}\nTO\n{other}\nID MATCH ");
            //    return true;
            //}

            bool test = this.Difficulty == other.Difficulty && this.Name == other.Name && this.Mods == other.Mods;
            //Debug.WriteLine($"Comparing:\n{this}\nTO\n{other}\nResult: {test}");

            return test;
        }
        public override bool Equals(object obj)
        {
            //Debug.WriteLine($"Override Comparing:\n{this}\nTO\n{obj}");
            return Equals(obj as DataPackage);
        }

        public bool Contains(string s)
        {

            var _ = new string[] { Name, World, Mods, Difficulty.ToString() };

            return _.ToList().Select(x => x.ToLower()).Any(x => x.Contains(s.ToLower()));

        }

        public bool Contains(params string[] st)
        {
            return st.ToList().All(x => Contains(x));
        }
        public override int GetHashCode() => (Difficulty, Name, Mods).GetHashCode();
    }

}
