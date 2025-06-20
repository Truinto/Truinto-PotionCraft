using BepInEx;
using HarmonyLib;
using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Potion;
using PotionCraft.ObjectBased.Potion;
using PotionCraft.ObjectBased.UIElements.ElementChangerWindow.AlchemySubstanceCustomizationWindow;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.SceneLoader;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Ingredient;
using PotionCraft.ScriptableObjects.Potion;
using Shared.CollectionNS;
using UnityEngine;

// TODO: while recipe book is open, apply potion base of current bookmark

namespace DefaultPotion
{
    [BepInPlugin("Truinto.DefaultPotion", "DefaultPotion", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        private Button? F4Key;

        public void Awake()
        {
            ReloadConfig();
            ObjectsLoader.onLoadingEnd.AddListener(OnLoadingEnd);
            Harmony.CreateAndPatchAll(typeof(Plugin));
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
                ReloadConfig();
                if (ObjectsLoader.isLoaded && !GameUnloader.IsUnloadingStarted())
                    OnPotionBaseSelect();
            }
        }

        public static void OnLoadingEnd()
        {
            //Debug.Log($"Bottles: {Bottle.allPotionBottles.Join(s => s.name)}");
            //Debug.Log($"Stickers: {Sticker.allPotionStickers.Join(s => s.name)}");

            Managers.SaveLoad.onProgressLoad.AddListener(1000f, OnPotionBaseSelect);
            Managers.RecipeMap.onPotionBaseSelect.AddListener(OnPotionBaseSelect);
        }

        public static void OnPotionBaseSelect()
        {
            // overwrite default skins
            LoadPropertiesFromConfig(Managers.RecipeMap.currentMap.potionBase.name);

            // reset current potion skin
            if (!Settings.State.ResetSkinOnLoad)
                return;
            int currentTier = (Managers.Potion.potionCraftPanel.GetRecipeBookPageContent() as Potion)?.GetMaxTier() ?? 0;
            int currentCount = Managers.Potion.PotionUsedComponents.GetSummaryComponents().Count(c => c.Component is Ingredient);
            SetDefaultProps(currentTier);
            if (currentCount == 1)
                SetLowlanderProps(currentTier);
            AlchemySubstanceSkinChangerWindow.Instance.UpdateWindow();
        }

        #region Patches

        [HarmonyPatch(typeof(PotionVisualObject), nameof(PotionVisualObject.SetDefaultBottle))]
        [HarmonyPostfix]
        public static void Patch1()
        {
            SavePotionPropertiesToConfig();
        }

        [HarmonyPatch(typeof(PotionVisualObject), nameof(PotionVisualObject.SetDefaultSticker))]
        [HarmonyPostfix]
        public static void Patch2()
        {
            SavePotionPropertiesToConfig();
        }

        [HarmonyPatch(typeof(PotionVisualObject), nameof(PotionVisualObject.SetDefaultStickerAngle))]
        [HarmonyPostfix]
        public static void Patch3()
        {
            SavePotionPropertiesToConfig();
        }

        [HarmonyPatch(typeof(PotionManager), nameof(PotionManager.AddPotionUsedComponent))]
        [HarmonyPostfix]
        public static void Patch4(PotionManager __instance)
        {
            // get target bottle
            int tier = (__instance.potionCraftPanel.GetRecipeBookPageContent() as Potion)?.GetMaxTier() ?? 0;
            if (Settings.State.Lowlander.GetBottle(tier) == null && Settings.State.Lowlander.GetSticker(tier) == null && Settings.State.Lowlander.GetAngle(tier) < 0)
                return;

            // count unique ingredients
            int num_ingredients = __instance.PotionUsedComponents.GetSummaryComponents().Count(c => c.Component is Ingredient);
            if (num_ingredients <= 0)
                return;

            // apply logic
            if (num_ingredients == 1)
            {
                if (HasDefaultProps(tier))
                {
                    SetLowlanderProps(tier);
                    AlchemySubstanceSkinChangerWindow.Instance.UpdateWindow();
                }
            }
            else
            {
                if (HasLowlanderProps(tier))
                {
                    SetDefaultProps(tier);
                    AlchemySubstanceSkinChangerWindow.Instance.UpdateWindow();
                }
            }
        }

        [HarmonyPatch(typeof(PotionCraftPanel), nameof(PotionCraftPanel.UpdateCurrentVisualOnTierChange))]
        [HarmonyPrefix]
        public static bool Patch5(Potion? previousPotion, PotionCraftPanel __instance)
        {
            if (__instance.GetRecipeBookPageContent() is not Potion currentPotion)
                return true;

            int previousTier = previousPotion?.GetMaxTier() ?? 0;
            int currentTier = currentPotion.GetMaxTier();
            //int currentCount = currentPotion.GetUsedComponents().GetSummaryComponents().Count(c => c.Component is Ingredient);

            if (HasLowlanderProps(previousTier))
            {
                SetDefaultProps(currentTier);
                SetLowlanderProps(currentTier);
                AlchemySubstanceSkinChangerWindow.Instance.UpdateWindow();
                return false;
            }

            if (HasDefaultProps(previousTier))
            {
                SetDefaultProps(currentTier);
                AlchemySubstanceSkinChangerWindow.Instance.UpdateWindow();
                return false;
            }

            return false;
        }

        #endregion

        #region Helpers

        public static void LoadPropertiesFromConfig(string potionBase)
        {
            var game = Managers.Potion.potionCustomization;
            if (Settings.State.Defaults.TryGetValue(potionBase, out var mod))
            {
                if (mod.T1_Bottle != null) game.defaultBottleWeakPotion = mod.T1_Bottle;
                if (mod.T2_Bottle != null) game.defaultBottleNormalPotion = mod.T2_Bottle;
                if (mod.T3_Bottle != null) game.defaultBottleStrongPotion = mod.T3_Bottle;

                if (mod.T1_Sticker != null) game.defaultStickerWeakPotion = mod.T1_Sticker;
                if (mod.T2_Sticker != null) game.defaultStickerNormalPotion = mod.T2_Sticker;
                if (mod.T3_Sticker != null) game.defaultStickerStrongPotion = mod.T3_Sticker;

                if (mod.T1_Angle >= 0) game.defaultStickerAngleWeakPotion = mod.T1_Angle;
                if (mod.T2_Angle >= 0) game.defaultStickerAngleNormalPotion = mod.T2_Angle;
                if (mod.T3_Angle >= 0) game.defaultStickerAngleStrongPotion = mod.T3_Angle;
            }
        }

        public static void SavePotionPropertiesToConfig()
        {
            var game = Managers.Potion.potionCustomization;
            string potionBase = Managers.RecipeMap.currentMap.potionBase.name;
            Settings.State.Defaults.Ensure(potionBase, out var mod);

            mod.T1_Bottle = game.defaultBottleWeakPotion;
            mod.T2_Bottle = game.defaultBottleNormalPotion;
            mod.T3_Bottle = game.defaultBottleStrongPotion;

            mod.T1_Sticker = game.defaultStickerWeakPotion;
            mod.T2_Sticker = game.defaultStickerNormalPotion;
            mod.T3_Sticker = game.defaultStickerStrongPotion;

            mod.T1_Angle = game.defaultStickerAngleWeakPotion;
            mod.T2_Angle = game.defaultStickerAngleNormalPotion;
            mod.T3_Angle = game.defaultStickerAngleStrongPotion;

            Settings.State.Save();
        }

        private static void SetLowlanderProps(int tier)
        {
            var potion = Managers.Potion.potionCraftPanel.customizationPanel;
            if (Settings.State.Lowlander.GetBottle(tier) != null) potion.UpdateCurrentBottle(Settings.State.Lowlander.GetBottle(tier));
            if (Settings.State.Lowlander.GetSticker(tier) != null) potion.UpdateCurrentSticker(Settings.State.Lowlander.GetSticker(tier));
            if (Settings.State.Lowlander.GetAngle(tier) >= 0) potion.UpdateCurrentStickerAngle(Settings.State.Lowlander.GetAngle(tier));
        }

        private static void SetDefaultProps(int tier)
        {
            var customize = Managers.Potion.potionCustomization;
            var potion = Managers.Potion.potionCraftPanel.customizationPanel;
            potion.UpdateCurrentBottle(customize.GetDefaultBottle(tier));
            potion.UpdateCurrentSticker(customize.GetDefaultSticker(tier));
            potion.UpdateCurrentStickerAngle(customize.GetDefaultStickerAngle(tier));
        }

        private static bool HasLowlanderProps(int tier)
        {
            if (Settings.State.Lowlander.IsEmpty)
                return false;

            var potion = Managers.Potion.potionCraftPanel.customizationPanel;
            return (Settings.State.Lowlander.GetBottle(tier) == null || potion.currentBottle == Settings.State.Lowlander.GetBottle(tier))
                && (Settings.State.Lowlander.GetSticker(tier) == null || potion.currentSticker == Settings.State.Lowlander.GetSticker(tier))
                && (Settings.State.Lowlander.GetAngle(tier) < 0 || potion.currentStickerAngle == Settings.State.Lowlander.GetAngle(tier));
        }

        private static bool HasDefaultProps(int tier)
        {
            var customize = Managers.Potion.potionCustomization;
            var potion = Managers.Potion.potionCraftPanel.customizationPanel;
            return potion.currentBottle == customize.GetDefaultBottle(tier)
                && potion.currentSticker == customize.GetDefaultSticker(tier)
                && potion.currentStickerAngle == customize.GetDefaultStickerAngle(tier);
        }

        #endregion
    }
}
