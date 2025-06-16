using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Shared.JsonNS;
using Newtonsoft.Json;

namespace RearrangeBookmarks
{
    public class Settings
    {
        [JsonIgnore] public string? FilePath;

        [JsonInclude] public int Version = 1;
        [JsonInclude] public KeyCode RearrangeKey = KeyCode.Space;
        [JsonInclude]
        public RailOption[] Rails =
        [
            new("LeftToRight1") { LimitTop = -0.45f },
            new("LeftToRight2") { LimitTop = -0.45f },
            new("TopToBottom1"),
            new("RightToLeft1") { LimitTop = -0.4f },
            new("RightToLeft2") { LimitTop = -0.4f },
            new("BottomToTop1"),
            new("BottomToTopSubRail") { Alignment = Alignment.Right, OffsetTop = 0.2f, LimitRight = 0.33f, StackEmpty = false },
            new("BottomToTopSubRail") { Alignment = Alignment.Evenly, OffsetTop = -0.2f, LimitRight = 0.33f, StackEmpty = false },
        ];

        public void Save()
        {
            JsonTool.SerializeFile(FilePath!, this);
        }

        public static Settings Load()
        {
            string filePath = Path.Combine(BepInEx.Paths.ConfigPath, "RearrangeBookmarks.json");
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
