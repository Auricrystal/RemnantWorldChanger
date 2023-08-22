using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using static RemnantWorldChanger.DataPackage;

namespace RemnantWorldChanger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string Packages { get => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\RemnantWorldChanger\\Packages\\"; }

        private BulkSave? saves;
        private BulkSave Saves
        {
            get
            {
                if (saves is not null)
                    return saves;

                if (!Directory.Exists(Packages))
                    return saves = new BulkSave();

                return saves = BulkSave.DeserializeData(Packages);
            }
            set { saves = value; }
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
        private FileSystemWatcher? savewatcher;
        private FileSystemWatcher SaveWatcher
        {
            get
            {
                if (savewatcher is null)
                {
                    Debug.WriteLine("Making new save watcher");
                    savewatcher = new FileSystemWatcher()
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
                    savewatcher.Changed += LockedSave_Changed;
                    savewatcher.Created += LockedSave_Changed;
                }
                return savewatcher;
            }
            set { savewatcher = value; }
        }
        private Dictionary<string, byte[]>? lockedsaves;
        private Dictionary<string, byte[]> LockedSaves
        {
            get
            {
                if (lockedsaves is null)
                    lockedsaves = new();
                return lockedsaves;
            }
            set { lockedsaves = value; }
        }
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
            ofd.Filter = "World Saves (.sav)|*.sav";
            ofd.DefaultExt = ".sav";
            ofd.Title = "Test Window";


            DataPackage? dp;
            if (ofd.ShowDialog() == true)
                if (Saves.NewPackage(out dp))
                    Saves.GuidToBytes.Add(dp.ID, File.ReadAllBytes(ofd.FileName));
                else
                    Debug.WriteLine("New Package Failed!");
            else
                Debug.WriteLine("Dialog False");

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
                    return Saves.SaveInfo.ToList().Find(x => x.Name == ((KeyValuePair<string, string>)o).Key)!.Type == st;
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

        private DataPackage? FindSelected()
        {
            string name = ((KeyValuePair<string, string>)SaveList.SelectedItem).Key;
            SaveDifficulty diff = (SaveDifficulty)(DifficultyList.SelectedItem ?? SaveDifficulty.Unset);
            string mod = ModifierList.SelectedItem?.ToString() ?? "Null";
            DataPackage? found;
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


        private void SaveEdit_Click(object sender, RoutedEventArgs e)
        {
            var dp = FindSelected();
            if (dp != null)
            {
                Debug.WriteLine($"FOUND: {dp} GUID:{dp.ID}");
                if (Saves.EditPackage(dp))
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
            DataPackage? _ = FindSelected();
            Debug.WriteLine($"Loading: {_}");
            if (_ is null)
                return;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "World Saves (.sav)|save_*.sav";
            ofd.DefaultExt = ".sav";
            ofd.Title = "Overwrite Save";

            if (ofd.ShowDialog() == true)
                File.WriteAllBytes(ofd.FileName, Saves.GuidToBytes[_.ID]);
            else
                Debug.WriteLine("Load Save Failed!");
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"Path: {GameSavePath}");

            Debug.WriteLine($"CheckBox: {cbKeepSave.IsChecked ?? false}");
            SaveWatcher.EnableRaisingEvents = cbKeepSave.IsChecked ?? false;
            if (cbKeepSave.IsChecked ?? false)
            {

                foreach (string s in Directory.EnumerateFiles(GameSavePath, "save_?.sav"))
                    LockedSaves.Add(Path.GetFileNameWithoutExtension(s), File.ReadAllBytes(s));

            }
            else
            {
                LockedSaves.Clear();
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
                    File.WriteAllBytes(GameSavePath + $@"\{e.Name}", LockedSaves[Path.GetFileNameWithoutExtension(e.FullPath)]);
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

        private void ViewDataFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", Packages);
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DeleteSave_Click(object sender, RoutedEventArgs e)
        {
            var _ = FindSelected();
            if (_ == null)
                return;
            var result = MessageBox.Show($"Are you sure you want to delete: \n\nDifficulty: {_.Difficulty}\nName: {_.Name}\nMods: {_.Mods} ", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
                if (Saves.GuidToBytes.Remove(_.ID))
                    Saves.SaveInfo.Remove(_);
            ViewUpdate();
        }
    }
}
