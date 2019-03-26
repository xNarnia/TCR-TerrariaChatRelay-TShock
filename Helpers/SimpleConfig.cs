using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace TerrariaChatRelay.Helpers
{
    public abstract class SimpleConfig : Dictionary<string, string>
    {
        public string ConfigId { get; }
        public string FilePath { get; }
        public bool IsNewConfig { get; }

        /// <summary>
        /// <para>Base configuration class. </para>
        /// Inherit this class, provide a constructor with an id, and override the DefaultData method. 
        /// All you have to do from that point is instatiate your new config class to load it.
        /// </summary>
        /// <param name="configId"></param>
        public SimpleConfig(string configId)
        {
            ConfigId = configId;
            FilePath = Path.Combine(Main.SavePath, "Mod Configs", ConfigId + ".json");
            IsNewConfig = false;
            LoadConfig(true);
        }

        /// <summary>
        /// Returns the Default Dictionary to use when creating a new Configuration.
        /// </summary>
        /// <returns>Dictionary to use for new Configuration.</returns>
        public abstract Dictionary<string, string> DefaultData();

        /// <summary>
        /// Loads the config if found in Mod Config. 
        /// If the config isn't found, creates a new config in "Mod Config" folder if CreateNewIfNotFound is set to true.
        /// </summary>
        /// <param name="CreateNewIfNotFound">Whether to create a new config or not if the config isn't found.</param>
        public void LoadConfig(bool CreateNewIfNotFound)
        {
            if (ConfigExists())
            {
                using (StreamReader reader = new StreamReader(FilePath))
                {
                    string json = reader.ReadToEnd();

                    var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    Clear();

                    foreach (var key in data)
                    {
                        Add(key.Key, key.Value);
                    }
                }
                Console.WriteLine($"Config loaded - {ConfigId}");
            }
            else if (CreateNewIfNotFound)
            {
                CreateNewConfig();

                foreach (var key in DefaultData())
                {
                    Add(key.Key, key.Value);
                }
                Console.WriteLine($"Config created - {ConfigId}");
            }
        }

        /// <summary>
        /// Creates a new JSON config file in Mod Config.
        /// </summary>
        public void CreateNewConfig()
            => CreateConfigFromData(DefaultData());

        /// <summary>
        /// Saves the config in the Mod Config folder.
        /// </summary>
        public void SaveConfig()
            => CreateConfigFromData(this);

        /// <summary>
        /// Creates a configuration using the specified Dictionary
        /// </summary>
        /// <param name="data">Data to make config with.</param>
        private void CreateConfigFromData(Dictionary<string, string> data)
        {
            using (StreamWriter file = File.CreateText(FilePath))
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                file.Write(json);
            }
        }

        /// <summary>
        /// Checks if config is present in Mod Config folder.
        /// </summary>
        /// <returns>Whether config is present.</returns>
        private bool ConfigExists()
            => File.Exists(FilePath);
    }
}
