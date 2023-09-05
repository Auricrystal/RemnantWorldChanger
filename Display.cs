using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using static RemnantWorldChanger.DataPackage;

namespace RemnantWorldChanger
{
    public class Display
    {
        ObservableCollection<DataPackage> DataPackages;

        private readonly ComboBox cbSaveType;

        private readonly DataGrid SaveList;
        private readonly ListView DifficultyList;
        private readonly ListView ModifierList;

        private readonly TextBox tbSearchBar;

        public string SaveName
        {
            get
            {
                if (SaveList is null || SaveList.ItemsSource is null)
                    return "";
                if (SaveList.SelectedIndex == -1)
                    SaveList.SelectedIndex = 0;
                return ((KeyValuePair<string, string>)SaveList.SelectedItem).Key;
            }
        }

        public SaveType SaveType
        {
            get
            {
                return (SaveType)cbSaveType.SelectedItem;
            }
        }
        public SaveDifficulty SaveDifficulty
        {
            get
            {
                if (DifficultyList is null || DifficultyList.ItemsSource is null)
                    return SaveDifficulty.Unset;

                if (DifficultyList.SelectedIndex == -1)
                    DifficultyList.SelectedIndex = 0;
                return (SaveDifficulty)DifficultyList.SelectedItem;
            }
        }

        public void UpdateData(ObservableCollection<DataPackage> packages)
        {
            DataPackages = packages;
            Regenerate();
        }
        public Display(ObservableCollection<DataPackage> packages, DataGrid saveList, ListView difficultyList, ListView modifierList, TextBox tbSearchBar, ComboBox saveType)
        {
            DataPackages = packages;

            SaveList = saveList;
            DifficultyList = difficultyList;
            ModifierList = modifierList;

            saveList.SelectionChanged += SaveList_SelectionChanged;
            difficultyList.SelectionChanged += DifficultyList_SelectionChanged;
            modifierList.SelectionChanged += ModifierList_SelectionChanged;
            modifierList.DataContextChanged += ModifierList_DataContextChanged;

            this.tbSearchBar = tbSearchBar;

            tbSearchBar.TextChanged += TbSearchBar_TextChanged;

            cbSaveType = saveType;
            saveType.SelectionChanged += SaveType_SelectionChanged;

            Regenerate();
        }

        private void ModifierList_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine("Mod List Changed");

        }

        private void SaveType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Debug.WriteLine("Save Type Changed");

            Regenerate();
            SaveList.SelectedIndex = 0;
        }

        public void SaveInfo_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Debug.WriteLine("Collection Changed!");
            if (sender is null)
                return;
            DataPackages = ((ObservableCollection<DataPackage>)sender);
            Debug.WriteLine($"Collection Updated: {DataPackages.Count}");
            Regenerate();
        }
        private void TbSearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            Regenerate();
            SaveList.SelectedIndex = 0;
        }

        public void Regenerate()
        {
            SaveList.ItemsSource = SearchBar().GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First().World).OrderBy(x => x.Value).ThenBy(x => x.Key);
            DifficultyList.SelectedIndex = -1;
            ModifierList.ItemsSource = SearchBar().Select(x => x.Mods).Distinct().OrderBy(x => x);

        }
        private ObservableCollection<DataPackage> SearchBar()
        {
            string[] filterText = tbSearchBar.Text.Split(' ');

            return new ObservableCollection<DataPackage>(DataPackages.Where(x => x.Contains(filterText) && (x.Type == SaveType || SaveType == SaveType.All)));

        }
        private void SaveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            // Debug.WriteLine($"SaveList Changed{SaveList.SelectedIndex}");

            if (SaveList.SelectedIndex == -1)
            {
                //Debug.WriteLine("No Save Selected");
                if (DifficultyList.ItemsSource is not null)
                    CollectionViewSource.GetDefaultView(DifficultyList.ItemsSource).Filter = o => false;
                if (ModifierList.ItemsSource is not null)
                    CollectionViewSource.GetDefaultView(ModifierList.ItemsSource).Filter = o => false;
                return;
            }

            CollectionViewSource.GetDefaultView(DifficultyList.ItemsSource).Filter = o =>
            {
                return SearchBar().Where(x => x.Name == SaveName).Select(x => x.Difficulty.ToString()).Contains(o.ToString());
            };

            if (DifficultyList.SelectedIndex == -1)
                DifficultyList.SelectedIndex = 0;


            //Debug.WriteLine("Calling Difflist");
            DifficultyList_SelectionChanged(sender, e);
        }


        private void DifficultyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SaveList.SelectedIndex == -1)
                return;
            if (DifficultyList.SelectedIndex == -1)
            {
                if (ModifierList.ItemsSource is not null)
                    CollectionViewSource.GetDefaultView(ModifierList.ItemsSource).Filter = o => false;
                return;
            }



            var name = ((KeyValuePair<string, string>)SaveList.SelectedItem).Key;
            var diff = DifficultyList.SelectedItem.ToString();


            CollectionViewSource.GetDefaultView(ModifierList.ItemsSource).Filter = o =>
            {
                return SearchBar().Where(x => x.Name == name && x.Difficulty.ToString() == diff).Select(y => y.Mods).Contains(o.ToString());
            };


            if (ModifierList.SelectedIndex == -1)
                ModifierList.SelectedIndex = 0;

            ModifierList_SelectionChanged(sender, e);

        }
        private void ModifierList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            // Debug.WriteLine($"ModifierList Changed: {ModifierList.SelectedIndex}");


        }




    }
}
