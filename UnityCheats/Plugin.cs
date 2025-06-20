using BepInEx;
using HarmonyLib;
using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.RecipeMap;
using PotionCraft.ObjectBased.RecipeMap;
using PotionCraft.QuestSystem;
using PotionCraft.SceneLoader;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Ingredient;
using UnityEngine;

namespace UnityCheats
{
    [BepInPlugin("Truinto.UnityCheats", "UnityCheats", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private Button F4Key = null!;
        private Button RotateKey = null!;
        private Button ShiftKey = null!;
        private bool Rotating = false;

        public void Awake()
        {
            F4Key = KeyboardKey.Get(KeyCode.F4);
            RotateKey = KeyboardKey.Get(KeyCode.O);
            ShiftKey = KeyboardKey.Get(KeyCode.LeftShift);

            var cmd = new Command("TeleportPotionToMouse", [new HotKey([KeyboardKey.Get(KeyCode.Q)])]);
            cmd.onDownedEvent.AddListener(TeleportPotionToMouse);

            cmd = new Command("HealPotion", [new HotKey([KeyboardKey.Get(KeyCode.H)])]);
            cmd.onDownedEvent.AddListener(HealPotion);

            cmd = new Command("RotatePotion", [new HotKey([KeyboardKey.Get(KeyCode.O)])]);
            cmd.onDownedEvent.AddListener(() => Rotating = true);

            Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        public void Update()
        {
            if (RotateKey.State is State.Upped) // CommandInvokeRepeater?
                Rotating = false;
            else if (Rotating)
                RotatePotion();
            if (F4Key.State is State.JustDowned)
            {
            }
        }

        public void HealPotion()
        {
            if (!ObjectsLoader.isLoaded || GameUnloader.IsUnloadingStarted() || !Managers.RecipeMap.gameObject.activeInHierarchy)
                return;

            Managers.RecipeMap.indicator.AddHealthBySalt(1f);
        }

        public void TeleportPotionToMouse()
        {
            if (!ObjectsLoader.isLoaded || GameUnloader.IsUnloadingStarted() || !Managers.RecipeMap.gameObject.activeInHierarchy)
                return;

            Vector3 cursorWorldPosition = Managers.Cursor.cursor.transform.position;
            if (!Managers.RecipeMap.recipeMapObject.visibilityZoneCollider.OverlapPoint(cursorWorldPosition))
                return;

            Vector2 mouseInWorld = Managers.RecipeMap.recipeMapObject.transmitterWindow.ViewToCamera(cursorWorldPosition);
            Vector2 mouseOnMap = Managers.RecipeMap.currentMap.referencesContainer.transform.InverseTransformPoint(mouseInWorld);
            Managers.RecipeMap.indicator.SetPositionOnMap(mouseOnMap);
            Managers.RecipeMap.indicator.wasTeleportedByDeveloperTeleportInThisFrame = true;
            MapStatesManager.MapChangeLock = true;
            Managers.Potion.potionCraftPanel.onPotionUpdated.Invoke(arg0: true);
        }

        public void RotatePotion()
        {
            if (!ObjectsLoader.isLoaded || GameUnloader.IsUnloadingStarted() || !Managers.RecipeMap.gameObject.activeInHierarchy)
                return;

            Managers.RecipeMap.indicatorRotation.SetRotatorType(IndicatorRotatorType.Other);

            if (ShiftKey.State == State.Downed)
                Managers.RecipeMap.indicatorRotation.RotateTo(Managers.RecipeMap.indicatorRotation.Value - 1f);
            else
                Managers.RecipeMap.indicatorRotation.RotateTo(Managers.RecipeMap.indicatorRotation.Value + 1f);
        }

        [HarmonyPatch(typeof(QuestRequirementCertainIngredient), nameof(QuestRequirementCertainIngredient.GetIngredient))]
        [HarmonyPrefix]
        public static void PatchQuests1(Quest quest, List<GeneratedQuestRequirement> generatedRequirements, HashSet<string> usedIngredients, QuestRequirementCertainIngredient __instance)
        {
            if (__instance is QuestRequirementNoParticularIngredient)
                return;

            var inventory = Managers.Player.Inventory;
            foreach (var ingredient in Ingredient.allIngredients)
            {
                if (!inventory.items.TryGetValue(ingredient, out _)) // if ingredient is not in inventory, put in blacklist
                    usedIngredients.Add(ingredient.name);
            }
        }

        [HarmonyPatch(typeof(QuestRequirementCertainBase), nameof(QuestRequirementCertainBase.GetAvailableBases))]
        [HarmonyPostfix]
        public static void PatchQuests2(QuestRequirementCertainBase __instance, ref IEnumerable<PotionBase> __result)
        {
            if (__instance is QuestRequirementNoParticularBase)
                return;

            __result = __result.Where(Managers.RecipeMap.potionBaseSubManager.IsBaseUnlocked);
        }
    }
}
