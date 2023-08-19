using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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


        private BulkSave Saves;
        private int index = 0;
        public MainWindow()
        {
            InitializeComponent();
            this.Saves = new BulkSave();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SaveList.ItemsSource = Saves.SaveInfo.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First().World);
            DifficultyList.ItemsSource = Saves.SaveInfo.Select(x => x.Difficulty).Distinct();
            ModifierList.ItemsSource = Saves.SaveInfo.Select(x => x.Mods).Distinct();


            cmbSaveType.ItemsSource = Enum.GetValues(typeof(SaveType));





            cmbSaveType.SelectedIndex = 0;
        }

        private void SaveCheckpoint_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Saving Checkpoint.....");
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "World Saves (.sav)|save_*.sav";
            ofd.DefaultExt = ".sav";
            ofd.Title = "Test Window";

            //Nullable<bool> test = ofd.ShowDialog();

            //if (test == true)
            //    Saves.AddSave(File.ReadAllBytes(ofd.FileName), "Earth", ofd.SafeFileName);


            Random random = new Random();
            int test = index++;
            var list = new string[] { "Earth", "Yaesha", "N'Erud", "Losomn" };
            int world = random.Next(list.Length);
            for (int i = 0; i < 6; i++)
                Saves.AddSave(new byte[] { }, list[world], $"Save{test}");


            SaveList.ItemsSource = Saves.SaveInfo.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First().World);
            DifficultyList.ItemsSource = Saves.SaveInfo.Select(x => x.Difficulty).Distinct();
            ModifierList.ItemsSource = Saves.SaveInfo.Select(x => x.Mods).Distinct();

            SaveList.SelectedIndex = 0;
            Debug.WriteLine($"Count: {Saves.SaveInfo.Count}");
            Debug.WriteLine($"Count: {SaveList.Items.Count}");

        }

        private void SaveListContext_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Context Menu Click!");

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Saves.Serialize();
        }

        private void SaveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((DataGrid)sender)?.SelectedItem == null)
                return;
            ViewUpdate();
            if (DifficultyList.ItemsSource == null)
                return;
            DifficultyList.SelectedIndex = 0;

            if (ModifierList.ItemsSource == null)
                return;
            ModifierList.SelectedIndex = 0;
            PrintSave();
        }
        private void ViewUpdate()
        {
            var st = ((SaveType)cmbSaveType.SelectedItem).ToString();

            CollectionViewSource.GetDefaultView(SaveList.ItemsSource).Filter = o =>
            {

                return Saves.SaveInfo.ToList().Find(x => x.Name == ((KeyValuePair<string, string>)o).Key).Type.ToString() == st;
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
                return;

            var diff = DifficultyList.SelectedItem.ToString();
            CollectionViewSource.GetDefaultView(ModifierList.ItemsSource).Filter = o =>
            {

                return Saves.SaveInfo.Where(x => x.Name == name && x.Difficulty.ToString() == diff).Select(y => y.Mods).Contains(o.ToString());
            };
        }

        private void DifficultyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ListView)sender)?.SelectedItem == null)
                return;
            var s = ((ListView)sender)?.SelectedItem.ToString();
            ViewUpdate();

            if (ModifierList.ItemsSource == null)
                return;
            ModifierList.SelectedIndex = 0;
            PrintSave();
        }

        private void ModifierList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ListView)sender)?.SelectedItem == null)
                return;
            PrintSave();


        }

        private void PrintSave()
        {
            Debug.WriteLine(FindSelected());
        }
        private DataPackage FindSelected() {
            string name = ((KeyValuePair<string, string>)SaveList.SelectedItem).Key;
            string world = ((KeyValuePair<string, string>)SaveList.SelectedItem).Value;
            SaveDifficulty diff = (SaveDifficulty)(DifficultyList.SelectedItem ?? SaveDifficulty.Unset);
            string mod = ModifierList.SelectedItem?.ToString() ?? "Null";

            return Saves.SaveInfo.Single(i => i.Name == name && i.World == world && i.Difficulty == diff && i.Mods == mod);
        }
        private void SaveEdit_Click(object sender, RoutedEventArgs e)
        {
            var dp = FindSelected();
            if (dp != null)
            {
                Debug.WriteLine($"FOUND: {dp} GUID:{dp.ID}");
                SaveEditor editor = new SaveEditor() { };
                var ser = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                ser.Converters.Add(new JsonStringEnumConverter());
                editor.EditorWindow.Text = JsonSerializer.Serialize<DataPackage>(dp,ser);

                Debug.WriteLine(editor.EditorWindow.Text);

                if (editor.ShowDialog() == true)
                {
                    Debug.WriteLine($"Returned: {editor.Save}");
                    if (editor.Save is null)
                        return;
                    Debug.WriteLine($"Size: {Saves.SaveInfo.Count}");
                    if (Saves.SaveInfo.Remove(dp))
                    {
                        Debug.WriteLine($"Size: {Saves.SaveInfo.Count}");
                        Debug.WriteLine("Removed!");
                        Saves.SaveInfo.Add(editor.Save);
                        Debug.WriteLine($"Size: {Saves.SaveInfo.Count}");

                    }


                    SaveList.ItemsSource = Saves.SaveInfo.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First().World);
                    DifficultyList.ItemsSource = Saves.SaveInfo.Select(x => x.Difficulty).Distinct();
                    ModifierList.ItemsSource = Saves.SaveInfo.Select(x => x.Mods).Distinct();

                    ViewUpdate();

                }
                else { Debug.WriteLine($"Editor: False"); }
            }

        }
    }
}
