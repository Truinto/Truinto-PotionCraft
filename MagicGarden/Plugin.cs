using BepInEx;
using DarkScreenSystem;
using HarmonyLib;
using PotionCraft.InputSystem;
using PotionCraft.LocalizationSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.BuildMode.Settings;
using PotionCraft.ManagersSystem.Day;
using PotionCraft.ManagersSystem.Ingredient;
using PotionCraft.ManagersSystem.Player;
using PotionCraft.NotificationSystem;
using PotionCraft.ObjectBased.Garden;
using PotionCraft.ObjectBased.UIElements.Books.GoalsBook;
using PotionCraft.ObjectBased.UIElements.ConfirmationWindowSystem;
using PotionCraft.ObjectBased.UIElements.FloatingText;
using PotionCraft.SceneLoader;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.BuildableInventoryItem;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.TMPAtlasGenerationSystem;
using System.Text;
using UnityEngine;

namespace MagicGarden
{
    [BepInPlugin("Truinto." + ModInfo.MOD_NAME, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private Button? F4Key;
        private Command? HarvestCommand;

        public void Awake()
        {
            F4Key = KeyboardKey.Get(KeyCode.F4);
            HarvestCommand = new Command("HarvestCommand", []);
            HarvestCommand.onDownedEvent.AddListener(AutoGarden);
            ReloadConfig();
            Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        public void ReloadConfig()
        {
            Settings.Load();
            HarvestCommand!.hotKeys = [new HotKey(Settings.State.HarvestKey.Select(s => KeyboardKey.Get(s)).ToArray())];
        }

        public void Update()
        {
            if (F4Key?.State == State.JustDowned)
            {
                ReloadConfig();
            }
        }

        [HarmonyPatch(typeof(DayManager), nameof(DayManager.StartNewDay))]
        [HarmonyPostfix]
        static void PatchStartNewDay()
        {
            if (Managers.Day.CurrentDayAbsoluteNum >= 4)
            {
                AutoGarden();
            }
        }

        public static void AutoGarden()
        {
            if (!ObjectsLoader.isLoaded || GameUnloader.IsUnloadingStarted())
                return;

            // speedup level up notifications
            Notification.settings.delayBetweenTexts = 0.1f;

            var itemCache = new List<(InventoryItem item, int amount)>();
            bool missingWildgrowth = false;
            bool missingStoneskin = false;

            // aggregate potions
            var inventory = Managers.Player.Inventory.items;
            var wildGrowthPotions = new List<(Potion Potion, int Num)>();
            var stoneSkinPotions = new List<(Potion Potion, int Num)>();
            foreach (var item in inventory)
            {
                if (item.Key is not Potion potion)
                    continue;
                var effects = potion.effects;
                int wildGrowth = 0;
                int stoneSkin = 0;
                for (int i = 0; i < effects.Length; i++)
                {
                    switch (effects[i].name)
                    {
                        case "WildGrowth":
                            wildGrowth++;
                            break;
                        case "StoneSkin":
                            stoneSkin++;
                            break;
                    }
                }
                if (wildGrowth >= 3)
                    wildGrowthPotions.Add((potion, item.Value));
                if (stoneSkin >= 3)
                    stoneSkinPotions.Add((potion, item.Value));
            }

            // iterate through all plants
            int harvestNum = 0;
            int gold = 0;
            foreach (var growingSpot in Managers.Growth.GrowingSpots) //Managers.Room.plants;
            {
                var watering = growingSpot.waterdropReceiver.wateringHandler;
                var growthHandler = growingSpot.GrowthHandler;
                var growingIngredientType = growingSpot.Ingredient.GetItemType();

                // watering
                if (!watering.IsFullyWatered && !watering.RequireNoWatering)
                {
                    watering.growth.WateringPercentage = 100f;
                    watering.UpdateValues(playDissolve: false);
                    Managers.Room.plants.AddExperience(growingIngredientType,
                        growthHandler.IsGrown ? ExperienceCategory.WateringGrownPlant : ExperienceCategory.WateringUngrownPlant);
                }

            repeat:
                // harvest; see growingSpot.plantGatherer.GatherIngredient();
                bool isFertilized = growthHandler.Growth.IsFertilized;
                if (growthHandler.TryHarvest())
                {
                    harvestNum++;
                    int num = growingSpot.plantGatherer.CalculateIngredientAmount();
                    if (PlantGatherer.SeedGatherData.TryGetValue(growingIngredientType, out int seedChance)
                        && growingSpot.plantGatherer.CanAddSeed(seedChance))
                    {
                        var seed = (Seed)growingSpot.buildableItem.InventoryItem.GetItem();
                        seed.Reset();
                        cacheItem(seed, 1);
                    }
                    cacheItem(growingSpot.plantGatherer.ingredient, num);
                    gold += Managers.Growth.GetGoldAmountOnGather(growingSpot, num);
                    Managers.Room.plants.AddExperience(growingIngredientType, isFertilized ? ExperienceCategory.GatherFertilizedIngredient : ExperienceCategory.GatherIngredient);
                    growthHandler.Growth.IsFertilized = false;

                    // pseudo watering (skipped call to ResetWatering)
                    Managers.Room.plants.AddExperience(growingIngredientType,
                        growthHandler.IsGrown ? ExperienceCategory.WateringGrownPlant : ExperienceCategory.WateringUngrownPlant);
                }

                // fertilize; see growingSpot.TryFertilize();
                if (growingSpot.PotionApplier.ReadyToApply() && tryRemovePotion(growingIngredientType == InventoryItemType.Crystal))
                {
                    growingSpot.PotionApplier.growth.HasAppliedPotion = true;
                    int previous_growth_value = growthHandler.Growth.Value;
                    growthHandler.AddGrowth(3, true, true);
                    growingSpot.shouldMature = growthHandler.IsGrown && previous_growth_value < growthHandler.PhasesCount - 1;
                    Managers.Room.plants.AddExperience(growingIngredientType, ExperienceCategory.GrowingPlant);

                    // can harvest again
                    goto repeat;
                }
            }

            if (harvestNum > 0)
            {
                GoalsLoader.GetGoalByName("GatherIngredient").ProgressIncrement(harvestNum);
                Managers.Player.Gold += gold;
                for (int i = 0; i < itemCache.Count; i++)
                {
                    var searchItem = itemCache[i].item;
                    var sameItem = inventory.Keys.FirstOrDefault(f => f.IsSame(searchItem));
                    if (sameItem != null)
                        inventory[sameItem] += itemCache[i].amount;
                    else
                        inventory[searchItem] = itemCache[i].amount;
                }
                Managers.Player.Inventory.onItemChanged.Invoke(true);

                ShowMessageBox(getMessageString(), "Auto Gardening");
            }
            else
                ShowFloatingMessage($"Jobs done", Vector2.zero);

            // restore notification delay
            Notification.settings.delayBetweenTexts = 3f;
            return;

            bool tryRemovePotion(bool isCrystal)
            {
                var list = isCrystal ? stoneSkinPotions : wildGrowthPotions;
                if (list.Count <= 0)
                {
                    if (isCrystal)
                        missingStoneskin = true;
                    else
                        missingWildgrowth = true;
                    return false;
                }
                var item = list[list.Count - 1];
                if (--item.Num <= 0)
                {
                    list.RemoveAt(list.Count - 1);
                    inventory.Remove(item);
                }
                return true;
            }

            void cacheItem(InventoryItem item, int amount)
            {
                int index = itemCache.FindIndex(f => f.item.IsSame(item));
                if (index < 0)
                    itemCache.Add((item, amount));
                else
                    itemCache[index] = (item, amount + itemCache[index].amount);
            }

            string getMessageString()
            {
                //Debug.Log($"TMPManagerSettings.Asset IconsAtlasName={TMPManagerSettings.Asset.IconsAtlasName} IngredientsAtlasName={TMPManagerSettings.Asset.IngredientsAtlasName}");
                var sb = new StringBuilder();
                sb.Append($"<line-height=90%><mspace=1.7>");
                itemCache.Sort(new SortInventoryItem());
                for (int i = 0; i < itemCache.Count; i++)
                {
                    if (i % 8 == 0 && i != 0)
                        sb.Append('\n');
                    if (itemCache[i].amount < 10)
                        sb.Append("\u2002"); // big space
                    sb.Append('+');
                    sb.Append(itemCache[i].amount);
                    sb.Append(" <size=130%><sprite=\"IngredientsAtlas\" name=\"");
                    sb.Append(ItemsGroupAtlasGenerator.GetAtlasSpriteName(itemCache[i].item));
                    sb.Append("\"></size>");
                }
                sb.Append($"</mspace>");
                if (missingWildgrowth)
                {
                    if (missingStoneskin)
                        sb.Append($"\nMissing fertilizer <size=130%><sprite=\"IconsAtlas\" name=\"WildGrowth\"> <sprite=\"IconsAtlas\" name=\"StoneSkin\"></size>");
                    else
                        sb.Append($"\nMissing fertilizer <size=130%><sprite=\"IconsAtlas\" name=\"WildGrowth\"></size>");
                }
                else if (missingStoneskin)
                    sb.Append($"\nMissing fertilizer <size=130%><sprite=\"IconsAtlas\" name=\"StoneSkin\"></size>");
                return sb.ToString();
            }
        }

        public static void ShowFloatingMessage(string msg, Vector3 position = default, float time = 2f)
        {
            if (position == Vector3.zero)
                position = (Vector2)Managers.Cursor.cursor.transform.position + PlantsSubManagerSettings.Asset.floatingTextCursorSpawnOffset;

            var floatingText = UnityEngine.Object.Instantiate(IngredientManagerSettings.Asset.CollectedFloatingText.gameObject, position, Quaternion.identity, Managers.Game.Cam.transform).GetComponent<CollectedFloatingText>();
            floatingText.lifeTime = time;
            floatingText.delayBeforeFadingOut = time - 0.5f;
            floatingText.velocity = Vector3.zero;
            floatingText.SpawnNewText(new CollectedFloatingText.FloatingTextContent(msg, CollectedFloatingText.FloatingTextContent.Type.Text));
            floatingText.UpdateLayout();
            floatingText.positionOnStart = floatingText.transform.position;
        }

        public static void ShowMessageBox(string msg, string title, Action? onClick = null)
        {
            var confirmationSettings = new ConfirmationWindowShowSettings(
                darkScreenLayer: DarkScreenLayer.Lower,
                titleKey: new Key("#parameters_1", [title], KeyParametersStyle.Normal, null),
                descriptionKey: new Key("#parameters_1", [msg], KeyParametersStyle.Normal, null),
                sprite: null,
                position: Vector2.zero,
                onOkClickAction: onClick,
                colorizeFirstCharacter: false
                );
            ConfirmationWindowsCollection.Asset.ShowWindow(confirmationSettings);
        }
    }
}
