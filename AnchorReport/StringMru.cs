using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Windows.Storage;

namespace AnchorReport
{
    /// <summary>
    /// Helper for an mru of strings that can be persisted to a string in settings
    /// </summary>
    class StringMru
    {
        private string settingsKey;
        private List<string> strings = new List<string>();

        public IEnumerable<string> Strings { get { return this.strings; } }

        public StringMru(string settingsKey)
        {
            this.settingsKey = settingsKey;
            this.ReadFromSettings();
        }

        public void StringUsed(string s)
        {
            if(this.strings.Contains(s))
            {
                strings.Remove(s);
            }
            strings.Insert(0, s);

            this.SaveToSettings();
        }

        private void ReadFromSettings()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            object jsonObject;
            if(localSettings.Values.TryGetValue(this.settingsKey, out jsonObject))
            {
                string jsonString = jsonObject as string;

                if(jsonString != null)
                {
                    var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));

                    var serializer = new DataContractJsonSerializer(typeof(string[]));
                    try
                    {
                        var stringArray = (string[])serializer.ReadObject(stream);
                        this.strings = new List<string>(stringArray);
                    }
                    catch(Exception)
                    {
                        // Eat parse exception
                    }
                }
            }
        }

        private void SaveToSettings()
        {
            var memoryStream = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(string[]));
            serializer.WriteObject(memoryStream, this.strings.ToArray());
            memoryStream.Position = 0;
            string jsonString;
            using (StreamReader reader = new StreamReader(memoryStream, Encoding.UTF8))
            {
                jsonString = reader.ReadToEnd();
            }

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[this.settingsKey] = jsonString;
        }
    }
}
