using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static RemnantWorldChanger.DataPackage;

namespace RemnantWorldChanger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private BulkSave saves;
        private string Packages { get => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\RemnantWorldChanger\\Packages\\"; }
        private BulkSave Saves
        {
            get
            {
                if (saves != null)
                    return saves;

                if (Directory.Exists(Packages))
                {
                    Debug.WriteLine($"Bulk folder exists! {Packages}");
                    saves = BulkSave.DeserializeData(Packages);
                    return saves;
                }
                else
                {

                    saves = new BulkSave();
                    return saves;
                }

            }
        }
        private static string GameSavePath
        {
            get
            {
                string s;
                var dirs = Directory.GetDirectories(s = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Saved Games\\Remnant2");
                foreach (var dir in dirs)
                {
                    if (Directory.GetDirectories(dir).Length > 0)
                    {
                        var saveDir = Directory.GetDirectories(dir)[0];
                        return saveDir;
                    }
                }
                return s;
            }
        }
        private FileSystemWatcher SaveWatcher;
        private Dictionary<string, byte[]> LockedSaves;
        public MainWindow()
        {
            InitializeComponent();


            EnableDebugOptions();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SaveList.ItemsSource = Saves.SaveInfo.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First().World);
            DifficultyList.ItemsSource = Saves.SaveInfo.Select(x => x.Difficulty).Distinct();
            ModifierList.ItemsSource = Saves.SaveInfo.Select(x => x.Mods).Distinct();


            cmbSaveType.ItemsSource = Enum.GetValues(typeof(SaveType));

            cmbSaveType.SelectedIndex = 0;
        }

        [Conditional("DEBUG")]
        private void EnableDebugOptions()
        {
            btnGenExample.Visibility = Visibility.Visible;
            btnGenExample.IsEnabled = true;
        }

        private void SaveCheckpoint_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Saving Checkpoint.....");
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "World Saves (.sav)|save_*.sav";
            ofd.DefaultExt = ".sav";
            ofd.Title = "Test Window";


            DataPackage? dp;
            if (ofd.ShowDialog() == true)
                if ((dp = NewPackage()) is not null)
                    Saves.AddSave(File.ReadAllBytes(ofd.FileName), dp);

            SaveList.SelectedIndex = 0;
            ViewUpdate();
            Debug.WriteLine($"Count: {Saves.SaveInfo.Count}");
            Debug.WriteLine($"Count: {SaveList.Items.Count}");
        }



        private void SaveListContext_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Context Menu Click!");

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Saves.SerializeData(Packages);
        }


        private void ViewUpdate()
        {
            SaveList.ItemsSource = Saves.SaveInfo.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First().World);
            DifficultyList.ItemsSource = Saves.SaveInfo.Select(x => x.Difficulty).Distinct().OrderBy(x => ((int)x));
            ModifierList.ItemsSource = Saves.SaveInfo.Select(x => x.Mods).Distinct().OrderBy(x => x);

            var st = ((SaveType)cmbSaveType.SelectedItem);

            if (st == SaveType.All)
                CollectionViewSource.GetDefaultView(SaveList.ItemsSource).Filter = o => true;
            else
                CollectionViewSource.GetDefaultView(SaveList.ItemsSource).Filter = o =>
                {
                    return Saves.SaveInfo.ToList().Find(x => x.Name == ((KeyValuePair<string, string>)o).Key).Type == st;
                };
            if (SaveList.SelectedIndex == -1)
            {
                CollectionViewSource.GetDefaultView(DifficultyList.ItemsSource).Filter = o => false;
                CollectionViewSource.GetDefaultView(ModifierList.ItemsSource).Filter = o => false;
                return;
            }

            var name = ((KeyValuePair<string, string>)SaveList.SelectedItem).Key;

            CollectionViewSource.GetDefaultView(DifficultyList.ItemsSource).Filter = o =>
            {
                return Saves.SaveInfo.Where(x => x.Name == name).Select(x => x.Difficulty.ToString()).Contains(o.ToString());
            };

            if (DifficultyList.SelectedIndex == -1)
                DifficultyList.SelectedIndex = 0;

            var diff = DifficultyList.SelectedItem.ToString();
            CollectionViewSource.GetDefaultView(ModifierList.ItemsSource).Filter = o =>
            {

                return Saves.SaveInfo.Where(x => x.Name == name && x.Difficulty.ToString() == diff).Select(y => y.Mods).Contains(o.ToString());
            };
            if (ModifierList.SelectedIndex == -1)
                ModifierList.SelectedIndex = 0;
        }




        private void PrintSave()
        {
            Debug.WriteLine(FindSelected());
        }
        private DataPackage? FindSelected()
        {
            string name = ((KeyValuePair<string, string>)SaveList.SelectedItem).Key;
            string world = ((KeyValuePair<string, string>)SaveList.SelectedItem).Value;
            SaveDifficulty diff = (SaveDifficulty)(DifficultyList.SelectedItem ?? SaveDifficulty.Unset);
            string mod = ModifierList.SelectedItem?.ToString() ?? "Null";
            DataPackage found;
            if (Exists(name, diff, mod, out found))
                return found;
            return null;
        }
        public bool Exists(string name, SaveDifficulty diff, string mod)
        {
            DataPackage? _;
            return Exists(name, diff, mod, out _);
        }
        public bool Exists(string name, SaveDifficulty diff, string mod, out DataPackage? found)
        {
            found = null;
            var test = Saves.SaveInfo.Where(i => i.Name == name && i.Difficulty == diff && i.Mods == mod);
            if (!test.Any())
                return false;
            found = test.First();
            return true;
        }

        private DataPackage? NewPackage()
        {
            Guid guid = Guid.NewGuid();
            return EditPackage(new DataPackage(guid));
        }
        private DataPackage? EditPackage(DataPackage dp)
        {
            SaveEditor editor = new SaveEditor() { };
            var ser = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            ser.Converters.Add(new JsonStringEnumConverter());
            editor.EditorWindow.Text = JsonSerializer.Serialize<DataPackage>(dp, ser);


            if (editor.ShowDialog() == false)
                return null;

            if (editor.Save is null)
                return null;

            return editor.Save;


        }
        private void SaveEdit_Click(object sender, RoutedEventArgs e)
        {
            var dp = FindSelected();
            if (dp != null)
            {
                Debug.WriteLine($"FOUND: {dp} GUID:{dp.ID}");
                DataPackage? dpo;
                if ((dpo = EditPackage(dp)) is not null)
                    if (Saves.SaveInfo.Remove(dp))
                        Saves.SaveInfo.Add(dpo);
                SaveList.ItemsSource = Saves.SaveInfo.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First().World);
                DifficultyList.ItemsSource = Saves.SaveInfo.Select(x => x.Difficulty).Distinct();
                ModifierList.ItemsSource = Saves.SaveInfo.Select(x => x.Mods).Distinct();

                ViewUpdate();

            }
            else { Debug.WriteLine($"Editor: False"); }
        }


        bool skipupdate = false;
        private void GenerateExamples_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Clicked Generate!");
            skipupdate = true;
            var worlds = new string[] { "Yaesha", "N'Erud", "Losomn" };
            var mods = new string[] { "Hearty", "Vicious", "Spiteful", "Drain", "Skullcracker", "Vortex", "Waller", "RatSwarm", "Displacer", "Teleporter" };
            for (int k = 1; k < 5; k++)
            {
                SaveDifficulty diff = (SaveDifficulty)k;
                for (int i = 0, j; i < 500; i++)
                {
                    string mod = string.Join(", ", mods.OrderBy(x => BulkSave.R.Next()).Take((int)diff).OrderBy(x => x));
                    if (!Exists($"Save{j = i % 20}", diff, mod))
                        Saves.AddSave(new byte[] { }, (SaveType)(j % 5 + 1), worlds[j % 3], $"Save{j}", diff, mod);
                }
            }
            ViewUpdate();
            skipupdate = false;
        }

        private void Save_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!skipupdate)
                ViewUpdate();
        }

        private void LoadSave_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Loading: {FindSelected()}");

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Path: {GameSavePath}");
            if (SaveWatcher is null)
            {
                Debug.WriteLine("Making new save watcher");
                SaveWatcher = new FileSystemWatcher()
                {
                    Path = GameSavePath,
                    Filter = "save_*.sav",
                    NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size
                };
                SaveWatcher.Changed += LockedSave_Changed;
                SaveWatcher.Created += LockedSave_Changed;


            }
            Debug.WriteLine($"CheckBox: {cbKeepSave.IsChecked ?? false}");
            SaveWatcher.EnableRaisingEvents = cbKeepSave.IsChecked ?? false;
            if (cbKeepSave.IsChecked ?? false)
            {
                if (LockedSaves is null)
                    LockedSaves = new();
                foreach (string s in Directory.EnumerateFiles(GameSavePath, "save_?.sav"))
                    LockedSaves.Add(s.Split(@"\").Last(), File.ReadAllBytes(s));
            }
            else
            {
                LockedSaves = null;
            }
        }
        private void LockedSave_Changed(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"Changed:{e.Name}");
            SaveWatcher.EnableRaisingEvents = false;
            this.Dispatcher.Invoke(() =>
            {

                try
                {
                    Debug.WriteLine("Overwriting!");
                    File.WriteAllBytes(GameSavePath + $@"\{e.Name}", LockedSaves[e.Name]);
                }
                catch (IOException ex)
                {
                    if (ex.Message.Contains("being used by another process"))
                    {
                        Console.WriteLine("WorldSave file in use; waiting 0.5 seconds and retrying.");

                        Thread.Sleep(500);
                        LockedSave_Changed(sender, e);
                    }
                }
            });
            SaveWatcher.EnableRaisingEvents = true;
        }
    }
}
