using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace RemnantWorldChanger
{
    /// <summary>
    /// Interaction logic for SaveEditor.xaml
    /// </summary>
    public partial class SaveEditor : Window
    {
        private DataPackage? Original { get; set; }
        public DataPackage? Save { get; private set; }
        public SaveEditor(DataPackage dp)
        {
            InitializeComponent();
            this.Original = dp;
            EditorWindow.Text = SerializeDataPackage(dp);
        }

        private string SerializeDataPackage(DataPackage dp)
        {
           return JsonSerializer.Serialize<DataPackage>(dp, new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            });
        }

        private void ApplyEdit_Click(object sender, RoutedEventArgs e)
        {
            if (EditorWindow is null)
                return;
            if (EditorWindow.Text == "")
                return;

            Debug.WriteLine(EditorWindow.Text);
            var ser = new JsonSerializerOptions();
            ser.Converters.Add(new JsonStringEnumConverter());
            DataPackage temp = null;
            try
            {
                temp = JsonSerializer.Deserialize<DataPackage>(EditorWindow.Text, ser);
            }
            catch (JsonException je)
            {
                var _ = MessageBox.Show(je.Message + "\n\nRevert Data?", "Json Deserialization Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (_ == MessageBoxResult.Yes)
                    EditorWindow.Text=SerializeDataPackage(Original);

            }
            if (temp == null)
                return;
            Save = temp;
            Debug.WriteLine($"Updated: {Save}");
            DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
