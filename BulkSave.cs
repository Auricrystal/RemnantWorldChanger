using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        static T RandomEnumValue<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(_R.Next(v.Length-1)+1);
        }
        public ObservableCollection<DataPackage> SaveInfo { get; set; }

        private Dictionary<Guid, byte[]> GuidToBytes { get; set; }

        public BulkSave()
        {
            SaveInfo = new ObservableCollection<DataPackage>();
            GuidToBytes = new Dictionary<Guid, byte[]>();
        }

        public void AddSave(byte[] savedata, string world = "",  string name = "")
        {
            Guid guid = Guid.NewGuid();
            GuidToBytes.Add(guid, savedata);

            var list = new string[] {"Hearty", "Vicious", "Spiteful", "Drain", "Skullcracker", "Vortex", "Waller", "RatSwarm", "Displacer", "Teleporter" };
            
            Random random = new Random();
            SaveDifficulty diff = RandomEnumValue<SaveDifficulty>();

            string mods = string.Join(", ",list.OrderBy(x=>random.Next()).Take((int)diff));


            SaveInfo.Add(new DataPackage(guid) {Difficulty=diff, World = world, Name = name, Mods = mods });

            
        }

        public void Serialize()
        {
            File.WriteAllText(@"C:\Users\AuriCrystal\Documents\VisualProjects\RemnantWorldChanger\Output\DataA.json",
            JsonSerializer.Serialize(SaveInfo, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            File.WriteAllText(@"C:\Users\AuriCrystal\Documents\VisualProjects\RemnantWorldChanger\Output\DataB.json",
            JsonSerializer.Serialize(GuidToBytes, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
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
