using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RemnantWorldChanger
{
    public class MultiKeyDictionary<TKey, TValue> where TKey : notnull
    {
        public Dictionary<TKey, Guid> Keys { get; set; }
        public Dictionary<Guid, TValue> Values { get; set; }

        [JsonConstructor]
        public MultiKeyDictionary()
        {
            Keys = new Dictionary<TKey, Guid>();
            Values = new Dictionary<Guid, TValue>();
        }

        public TValue this[TKey key] { get => Values[Keys[key]]; }

        public void Add(TKey key, TValue value)
        {
            if (!Values.Values.Contains(value))
            {
                Debug.WriteLine("Adding New Link");
                var _ = Guid.NewGuid();
                Keys.Add(key, _);
                Values.Add(_, value);
                return;
            }
            Debug.Indent();
            Debug.WriteLine("Existing Value Found");
            Assosciate(key, value);
            Debug.Unindent();
        }
        public bool Remove(TKey key)
        {
            Guid _ = Keys[key];
            if (Keys.Values.Count(x => x == _) == 1)
            {
                Debug.WriteLine("Deleting Last Value Link");
                Values.Remove(_);
            }
            Debug.WriteLine("Deleting Assosciation");
            return Keys.Remove(key);
        }

        public bool Assosciate(TKey key, TValue value)
        {
           
            Guid? _ = null;
            Debug.Indent();
            if (!TryGetLink(out _, value))
            {
                Debug.Unindent();
                return false;
            }
            Debug.Unindent();
            if (_ is null)
                return false;

            if (Keys.ContainsKey(key))
            {
                Debug.WriteLine("Updating Association");
                Keys[key] = _.Value;
            }
            else
            {
                Debug.WriteLine("Adding Association");
                Keys.Add(key, _.Value);
            }
            return true;

        }

        private bool TryGetLink(out Guid? key, TValue value)
        {
            key = null;
            if (!Values.Values.Contains(value))
            {
                Debug.WriteLine("No Link Found");
                return false;
            }
            Debug.WriteLine("Link Found");
            key = Values.First(x => x.Value!.Equals(value)).Key;
            return true;

        }



    }
}
