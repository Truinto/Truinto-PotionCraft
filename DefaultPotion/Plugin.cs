using BepInEx;
using HarmonyLib;
using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Potion;
using PotionCraft.SceneLoader;
using PotionCraft.ScriptableObjects;
using Shared.CollectionNS;
using UnityEngine;

namespace DefaultPotion
{
    [BepInPlugin("Truinto.DefaultPotion", "DefaultPotion", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private Button? F4Key;

        public void Awake()
        {
            ReloadConfig();
            ObjectsLoader.onLoadingEnd.AddListener(OnLoadingEnd);
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Instance = this;
        }

        public void ReloadConfig()
        {
            Settings.Load();
            F4Key ??= KeyboardKey.Get(KeyCode.F4);
        }

        public void Update()
        {
            if (F4Key?.State == State.JustDowned)
            {
                Debug.Log("Hotkey F4");
                ReloadConfig();
            }
        }

        public void OnLoadingEnd()
        {
            //Debug.Log($"Bottles: {Bottle.allPotionBottles.Join(s => s.name)}");
            //Debug.Log($"Stickers: {Sticker.allPotionStickers.Join(s => s.name)}");

            Managers.RecipeMap.onPotionBaseSelect.AddListener(OnPotionBaseSelect);
        }

        public void OnPotionBaseSelect()
        {
            var game = Managers.Potion.potionCustomization;
            string potionBase = Managers.RecipeMap.currentMap.potionBase.name;

            if (Settings.State.Defaults.TryGetValue(potionBase, out var mod))
            {
                if (Bottle.GetByName(mod.T1_Bottle) is Bottle bottle1) game.defaultBottleWeakPotion = bottle1;
                if (Sticker.GetByName(mod.T1_Bottle) is Sticker sticker1) game.defaultStickerWeakPotion = sticker1;
                game.defaultStickerAngleWeakPotion = mod.T1_Angle;

                if (Bottle.GetByName(mod.T2_Bottle) is Bottle bottle2) game.defaultBottleNormalPotion = bottle2;
                if (Sticker.GetByName(mod.T2_Bottle) is Sticker sticker2) game.defaultStickerNormalPotion = sticker2;
                game.defaultStickerAngleNormalPotion = mod.T2_Angle;

                if (Bottle.GetByName(mod.T3_Bottle) is Bottle bottle3) game.defaultBottleStrongPotion = bottle3;
                if (Sticker.GetByName(mod.T3_Bottle) is Sticker sticker3) game.defaultStickerStrongPotion = sticker3;
                game.defaultStickerAngleStrongPotion = mod.T3_Angle;
            }
        }

        public void SavePotionProperties()
        {
            var game = Managers.Potion.potionCustomization;
            string potionBase = Managers.RecipeMap.currentMap.potionBase.name;
            Settings.State.Defaults.Ensure(potionBase, out var mod);

            mod.T1_Bottle = game.defaultBottleWeakPotion.name;
            mod.T1_Sticker = game.defaultStickerWeakPotion.name;
            mod.T1_Angle = game.defaultStickerAngleWeakPotion;

            mod.T2_Bottle = game.defaultBottleNormalPotion.name;
            mod.T2_Sticker = game.defaultStickerNormalPotion.name;
            mod.T2_Angle = game.defaultStickerAngleNormalPotion;

            mod.T3_Bottle = game.defaultBottleStrongPotion.name;
            mod.T3_Sticker = game.defaultStickerStrongPotion.name;
            mod.T3_Angle = game.defaultStickerAngleStrongPotion;
        }

        public static Plugin? Instance;

        [HarmonyPatch(typeof(PotionVisualObject), nameof(PotionVisualObject.SetDefaultBottle))]
        [HarmonyPostfix]
        public static void Patch1()
        {
            Instance?.SavePotionProperties();
        }

        [HarmonyPatch(typeof(PotionVisualObject), nameof(PotionVisualObject.SetDefaultSticker))]
        [HarmonyPostfix]
        public static void Patch2()
        {
            Instance?.SavePotionProperties();
        }

        [HarmonyPatch(typeof(PotionVisualObject), nameof(PotionVisualObject.SetDefaultStickerAngle))]
        [HarmonyPostfix]
        public static void Patch3()
        {
            Instance?.SavePotionProperties();
        }
    }
}
