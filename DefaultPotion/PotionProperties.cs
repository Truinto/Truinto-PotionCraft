using PotionCraft.ScriptableObjects;

namespace DefaultPotion
{
    public class PotionProperties
    {
        [JsonInclude] public string? T1_BottleName;
        [JsonInclude] public string? T2_BottleName;
        [JsonInclude] public string? T3_BottleName;

        [JsonInclude] public string? T1_StickerName;
        [JsonInclude] public string? T2_StickerName;
        [JsonInclude] public string? T3_StickerName;

        [JsonInclude] public int T1_Angle = -1;
        [JsonInclude] public int T2_Angle = -1;
        [JsonInclude] public int T3_Angle = -1;

        [JsonIgnore] private Bottle? _T1_Bottle;
        [JsonIgnore] private Bottle? _T2_Bottle;
        [JsonIgnore] private Bottle? _T3_Bottle;
        [JsonIgnore] private Sticker? _T1_Sticker;
        [JsonIgnore] private Sticker? _T2_Sticker;
        [JsonIgnore] private Sticker? _T3_Sticker;

        [JsonIgnore]
        public Bottle? T1_Bottle
        {
            get => _T1_Bottle ??= (T1_BottleName == null ? null : Bottle.GetByName(T1_BottleName));
            set => T1_BottleName = (_T1_Bottle = value)?.name;
        }

        [JsonIgnore]
        public Bottle? T2_Bottle
        {
            get => _T2_Bottle ??= (T2_BottleName == null ? null : Bottle.GetByName(T2_BottleName));
            set => T2_BottleName = (_T2_Bottle = value)?.name;
        }
        [JsonIgnore]
        public Bottle? T3_Bottle
        {
            get => _T3_Bottle ??= (T3_BottleName == null ? null : Bottle.GetByName(T3_BottleName));
            set => T3_BottleName = (_T3_Bottle = value)?.name;
        }
        [JsonIgnore]
        public Sticker? T1_Sticker
        {
            get => _T1_Sticker ??= (T1_StickerName == null ? null : Sticker.GetByName(T1_StickerName));
            set => T1_StickerName = (_T1_Sticker = value)?.name;
        }
        [JsonIgnore]
        public Sticker? T2_Sticker
        {
            get => _T2_Sticker ??= (T2_StickerName == null ? null : Sticker.GetByName(T2_StickerName));
            set => T2_StickerName = (_T2_Sticker = value)?.name;
        }
        [JsonIgnore]
        public Sticker? T3_Sticker
        {
            get => _T3_Sticker ??= (T3_StickerName == null ? null : Sticker.GetByName(T3_StickerName));
            set => T3_StickerName = (_T3_Sticker = value)?.name;
        }
        [JsonIgnore]
        public bool IsEmpty => T1_BottleName == null && T2_BottleName == null && T3_BottleName == null 
            && T1_StickerName == null && T2_StickerName == null && T3_StickerName == null
            && T1_Angle < 0 && T2_Angle < 0 && T3_Angle < 0;

        public Bottle? GetBottle(int tier) => tier switch { 2 => T2_Bottle, 3 => T3_Bottle, _ => T1_Bottle };
        public Sticker? GetSticker(int tier) => tier switch { 2 => T2_Sticker, 3 => T3_Sticker, _ => T1_Sticker };
        public int GetAngle(int tier) => tier switch { 2 => T2_Angle, 3 => T3_Angle, _ => T1_Angle };

        public void ClearBuffer()
        {
            _T1_Bottle = null;
            _T2_Bottle = null;
            _T3_Bottle = null;
            _T1_Sticker = null;
            _T2_Sticker = null;
            _T3_Sticker = null;
        }
    }
}
