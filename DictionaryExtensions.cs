using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.Intrinsics.Arm;
using System.Diagnostics;

namespace RemnantWorldChanger
{
    public static class DictionaryExtensions
    {
        public static bool ChangeKey<TKey, TValue>(this IDictionary<TKey, TValue> dict,
                                          TKey oldKey, TKey newKey)
        {
            if (!dict.Remove(oldKey, out var value))
                return false;

            dict[newKey] = value;  // or dict.Add(newKey, value) depending on ur comfort
            return true;
        }
        public static bool NewPackage(this BulkSave Saves, out DataPackage dp)
        {
            Guid guid = Guid.NewGuid();
            return EditPackage(Saves, dp = new DataPackage(guid), false);
        }
        public static bool EditPackage(this BulkSave Saves, DataPackage dp, bool editmode = true)
        {
            SaveEditor editor = new SaveEditor(Saves, dp);

            if (editor.ShowDialog() == false)
            {
                Debug.WriteLine("Dialog Result False");
                return false;
            }

            if (editor.Save is null)
            {
                Debug.WriteLine("Editor Save Null");
                return false;
            }


            if (editmode && dp.ID != editor.Save.ID)
            {
                var _ = MessageBox.Show($"Warning GUID Mismatch:\n\nOld: {dp.ID}\nNew: {editor.Save.ID}\n\nChange GUID?", "GUID Mismatch", MessageBoxButton.YesNoCancel);
                if (_ == MessageBoxResult.Cancel)
                    return false;

                if (_ == MessageBoxResult.No)
                    editor.Save.ID = dp.ID;
                else
                {
                    Saves.GuidToBytes.ChangeKey(dp.ID, editor.Save.ID);
                }
            }

            if ((editmode == Saves.SaveInfo.Remove(dp)))
                Saves.SaveInfo.Add(editor.Save);
            else
                return false;
            return true;


        }
    }
}
