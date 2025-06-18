using Shared.JsonNS;

namespace DefaultPotion
{
    public class Settings
    {
        [JsonIgnore] public string? FilePath;

        [JsonInclude] public int Version = 1;
        [JsonInclude] public bool ResetSkinOnLoad = true;
        [JsonInclude] public Dictionary<string, PotionProperties> Defaults = new();
        [JsonInclude] public PotionProperties Lowlander = new();

        public void Save()
        {
            JsonTool.SerializeFile(FilePath!, this);
        }

        public static Settings Load()
        {
            string filePath = Path.Combine(BepInEx.Paths.ConfigPath, "DefaultPotion.json");
            if (JsonTool.DeserializeFile(filePath, out _state))
            {
                _state.FilePath = filePath;
            }
            else
            {
                _state = new();
                _state.FilePath = filePath;
                _state.Save();
            }
            return _state;
        }

        private static Settings? _state;
        public static Settings State => _state ?? Load();
    }
}
