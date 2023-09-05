using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Xml.Linq;
using static RemnantWorldChanger.DataPackage;

namespace RemnantWorldChanger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private FileSystemWatcher? _watcher;
        private string Packages
        {
            get
            {
                var _ = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\RemnantWorldChanger\\Packages\\";
                if (!Directory.Exists(_))
                    Directory.CreateDirectory(_);
                return _;
            }
        }

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
        private FileSystemWatcher PackageWatcher
        {
            get
            {
                if (_watcher is null)
                {
                    _watcher = new FileSystemWatcher()
                    {
                        Path = Packages,
                        NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.Security
                                     | NotifyFilters.Size,
                        Filter = "*.RIndex"
                    };
                    _watcher.Changed += Watcher_Changed;
                    _watcher.Created += Watcher_Changed;
                }
                return _watcher;
            }
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var result = MessageBox.Show("Reload Saves?", "New Data Detected", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result != MessageBoxResult.Yes)
                return;
            Saves = BulkSave.DeserializeData(Packages);
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    display?.UpdateData(Saves.SaveInfo);
                });

            }
            catch (Exception) { }

        }

        private KeyValuePair<string, byte[]>? savebackup;

        public void Backup(string path)
        {
            savebackup ??= new KeyValuePair<string, byte[]>(path, File.ReadAllBytes(path));
            btnRestoreBackup.IsEnabled = true;
        }
        public void Restore()
        {
            if (savebackup is null)
                return;

            File.WriteAllBytes(savebackup.Value.Key, savebackup.Value.Value);
            MessageBox.Show("Save Restored");
            savebackup = null;
            btnRestoreBackup.IsEnabled = false;
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
        private Display? display;
        public MainWindow()
        {
            InitializeComponent();


            EnableDebugOptions();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbSaveType.ItemsSource = Enum.GetValues(typeof(SaveType));
            DifficultyList.ItemsSource = Enum.GetValues(typeof(SaveDifficulty));

            cmbSaveType.SelectedIndex = 0;
            display = new Display(Saves.SaveInfo, SaveList, DifficultyList, ModifierList, tbSearchbar, cmbSaveType);

            Saves.SaveInfo.CollectionChanged += display.SaveInfo_CollectionChanged;

            SaveList.SelectedIndex = 0;

            PackageWatcher.EnableRaisingEvents = true;
            //display.Regenerate();

            //Debug.WriteLine($"Mods: {ModifierList.Items.Count}");

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
                {
                    Debug.WriteLine("New Package Failed!");
                    return;
                }
            else
            {
                Debug.WriteLine("Dialog False");
                return;
            }

            SaveList.SelectedIndex = 0;

            display?.Regenerate();
            Debug.WriteLine($"Count: {Saves.SaveInfo.Count}");
            Debug.WriteLine($"Count: {SaveList.Items.Count}");
        }



        private void SaveListContext_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Context Menu Click!");

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Restore();
            PackageWatcher.EnableRaisingEvents = false;
            Saves.SerializeData(Packages);
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
                    display.Regenerate();
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
                        Saves.AddSave(Array.Empty<byte>(), (SaveType)(j % 5 + 1), worlds[j % 3], $"Save{j}", diff, mod);
                }
            }
            skipupdate = false;
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
            {
                Backup(ofd.FileName);
                File.WriteAllBytes(ofd.FileName, Saves.GuidToBytes[_.ID]);
            }
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
            display.Regenerate();
        }

        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            Restore();
        }
    }
}
