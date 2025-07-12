using BepInEx;
using HarmonyLib;
using PotionCraft.InputSystem;
using PotionCraft.InventorySystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.RecipeMap;
using PotionCraft.ObjectBased.RecipeMap;
using PotionCraft.ObjectBased.UIElements.ElementChangerWindow.AlchemySubstanceCustomizationWindow;
using PotionCraft.QuestSystem;
using PotionCraft.SceneLoader;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Ingredient;
using PotionCraft.ScriptableObjects.Potion;
using System.Runtime.InteropServices;
using UnityEngine;
using Shared.CollectionNS;
using PotionCraft.FactionSystem;
using PotionCraft.ObjectBased.ElementSystem;
using JetBrains.Annotations;

namespace UnityCheats
{
    [BepInPlugin("Truinto." + ModInfo.MOD_NAME, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
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

            ObjectsLoader.onLoadingEnd.AddListener(OnLoadingEnd);
            ObjectsLoader.onLoadingEnd.AddListener(PrintQuests);
        }

        public void OnLoadingEnd()
        {
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

        public void PrintQuests()
        {
            // print quests
            var allQuests = new List<Quest>();
            foreach (var faction in Faction.allFactions)
            {
                foreach (var factionClassInFaction in faction.factionClasses)
                {
                    var factionClass = factionClassInFaction.factionClass;
                    foreach (var quest in factionClass.quests)
                    {
                        if (!allQuests.ContainsReference(quest))
                            allQuests.Add(quest);
                    }
                }
            }
            Debug.Log($"[Cheats] Quests ({allQuests.Count})");

            // print requirements
            Debug.Log($"[Cheats] Requirements ({QuestRequirementInQuest.allRequirements.Count}):");
            foreach (var inQuest in QuestRequirementInQuest.allRequirements)
            {
                if (inQuest.requirement is QuestRequirementCertainIngredient requireIngredient)
                    Debug.Log($" type={requireIngredient.GetType().Name} checkPotential={requireIngredient.checkPotential} shortDistancePotential={requireIngredient.shortDistancePotential} threshold={requireIngredient.potentialThreshold}");
            }

            // print icons
            Debug.Log($"[Cheats] Icons ({OrderedIcons.Count}): {OrderedIcons.Join(f => f.name)}");

            //// print effects
            //Debug.Log($"[Cheats] Effects:");
            //foreach (var effect in PotionEffect.allPotionEffects)
            //{
            //    var elementType = effect.elementalPotential.GetDominantElementType();
            //    var mainIngredients = new List<Ingredient>();
            //    var sideIngredients = new List<Ingredient>();
            //    foreach (var ingredient in Ingredient.allIngredients)
            //    {
            //        var potentialLong = ingredient.longDistanceElementalPotential.GetPotential(elementType);
            //        var potentialShort = ingredient.shortDistanceElementalPotential.GetPotential(elementType);
            //        if (potentialLong >= 0.2f)
            //            mainIngredients.Add(ingredient);
            //        else if (potentialShort >= 0.07f)
            //            sideIngredients.Add(ingredient);
            //    }
            //    mainIngredients.Sort(s => s.price);
            //    sideIngredients.Sort(s => s.price);
            //    Debug.Log($" {effect.name,-20} {elementType,-10} {mainIngredients.Join(f => f.name)}");
            //    Debug.Log($"                                 {sideIngredients.Join(f => f.name)}");
            //}

            // print effects
            Debug.Log($"[Cheats] Effects:");
            Debug.Log($"Effect\t{AllIngredients.Join(null, "\t")}");
            foreach (var effect in PotionEffect.allPotionEffects)
            {
                var elementType = effect.elementalPotential.GetDominantElementType();
                var mainIngredients = new List<string>();
                var sideIngredients = new List<string>();
                foreach (var ingredient in Ingredient.allIngredients)
                {
                    var potentialLong = ingredient.longDistanceElementalPotential.GetPotential(elementType);
                    var potentialShort = ingredient.shortDistanceElementalPotential.GetPotential(elementType);
                    if (potentialLong >= 0.2f)
                        mainIngredients.Add(ingredient.name);
                    else if (potentialShort >= 0.07f)
                        sideIngredients.Add(ingredient.name);
                }
                Debug.Log($"{effect.name}\t{AllIngredients.Join(f =>
                {
                    if (mainIngredients.Contains(f))
                        return "P";
                    if (sideIngredients.Contains(f))
                        return "s";
                    return " ";
                }, "\t")}");
            }
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
            __result = __result.Where(Managers.RecipeMap.potionBaseSubManager.IsBaseUnlocked);
        }

        [HarmonyPatch(typeof(ItemsPanel), nameof(ItemsPanel.GetSortedItems))]
        [HarmonyPostfix]
        public static void PatchSorting(InventoryItemIntDictionary itemsList, ItemsPanel __instance, ref List<InventoryItem> __result)
        {
            if (__instance.SortType is not SortType.ByType)
                return;

            if (__instance.ReversedSort)
                __result.Sort(compare);
            else
                __result.Sort((s1, s2) => compare(s2, s1));

            return;

            int compare(InventoryItem item1, InventoryItem item2)
            {
                // check null
                var potion1 = item1 as Potion;
                var potion2 = item2 as Potion;
                if (potion1 == null && potion2 == null)
                    return 0;
                if (potion1 == null || potion2 == null)
                    return potion1 == null ? -1 : 1;

                // check by icon
                var icon1 = OrderedIcons.GetIndex(potion1.coloredIcon.icon);
                var icon2 = OrderedIcons.GetIndex(potion2.coloredIcon.icon);
                if (icon1 != icon2)
                    return icon1.CompareTo(icon2);

                // check by icon tier
                var icon = potion1.coloredIcon.icon;
                icon1 = potion1.effects.Count(f => f.icon == icon);
                icon2 = potion2.effects.Count(f => f.icon == icon);
                if (icon1 != icon2)
                    return icon1.CompareTo(icon2);

                // check by price
                return potion1.effects.Sum(f => f.price).CompareTo(potion2.effects.Sum(f => f.price));
            }
        }

        private static List<Icon>? _orderedIcons;
        public static List<Icon> OrderedIcons
        {
            get
            {
                if (_orderedIcons == null)
                {
                    _orderedIcons = new();
                    foreach (string name in CustomIconOrder)
                    {
                        var icon = Icon.allIcons.FirstOrDefault(f => f.name == name);
                        if (icon != null)
                            _orderedIcons.Add(icon);
                    }
                    foreach (var icon in AlchemySubstanceSkinChangerWindow.Instance.iconSkinChangerPanelGroup.combinedPanels.SelectMany(s => s.GetElements().SelectNotNull(s2 => s2 as Icon)))
                    {
                        if (!_orderedIcons.Contains(icon))
                            _orderedIcons.Add(icon);
                    }
                }
                return _orderedIcons;
            }
        }
        public static string[] CustomIconOrder = ["QuestionMark", "Healing", "Cross2", "TwoCrosses", "ThreeCrosses", "Poison", "DissolvingSword", "Fire", "Flame", "FireSpiral", "Frost", "Snowflake", "Snowflakes", "Explosion", "Explosion2", "Explosion3", "Lightning", "Lightning2", "Lightning3", "Acid", "Drop1", "Sprout", "Sprout2", "BicepArm", "MuscleMan", "CatPaw", "JumpingPuma", "WingedBoots", "HighBoot", "MagicalVision", "Eye", "Mana", "MagicWand", "Light", "Light2", "Light3", "Sleep", "Moon", "Snail", "Bull'sHead2", "BullHead", "FullBull", "Enchantment", "Hearts", "Fig", "Sex", "Transparency", "Teleportation2", "Teleportation1", "Wings2", "Wings1", "Necromancy", "ExplodingSkull", "StoneSkin", "StoneShield", "PoisonProtection", "AcidProtection", "LightningProtection", "FireProtection", "FrostProtection", "MagicProtection", "MagicShield", "FireballShield", "Glue", "StickyBoot", "Oilcan", "SickCloud", "SkullCloud", "Incense", "Perfume", "Shrinking", "Enlargement", "Apple", "Harp", "Lyre", "Scream", "PsychedelicSpiral", "HorseshoeUp", "HorseshoeDown", "Shamrock", "VoodooDoll", "Nigredo", "Albedo", "Citrinitas", "Rubedo", "Philosopher'sStone", "VoidSalt", "MoonSalt", "SunSalt", "LifeSalt", "Philosopher'sSalt", "OliveOil", "BerserkerAxe1", "Wheat", "Spring", "SlowDown", "StoneFigure", "Cross1", "Sword", "Swords", "BerserkerAxe2", "BowAndArrow", "Staff", "WizardHat", "Cloak", "Ring", "Goblet", "Juggler", "CurvedHorn", "WarHorn", "Trumpets", "Cymbals", "Bell", "Drum", "Magnet", "Anvil", "Hammer", "Sickle", "Weight", "ElfEar", "Torso", "BubbleLungs", "VikingHead", "MedusaHead", "Illithid", "BrainWaves", "AstralFace", "ThirdEye", "MasonEye", "BlindEye", "PsychedelicEye", "CrossedEye", "Ouroboros", "Astral2", "MagicStar", "Astral1", "Telekinesis", "Brain", "RadiatingBrain", "InkblotBrain", "RadiatingHeart", "Bones", "SkullAndBones1", "SkullAndBones2", "BurningBones", "BoneCloud", "Tombstone", "BaphometPentagram", "DissolvingPentagram", "DissolvingSpiral", "Bird", "Eagle", "Feather1", "Feather2", "Wing", "Cloud", "MistCloud", "SmokeCloud1", "SmokeCloud2", "Poo1", "Poo2", "UnknownSubstance", "Fir", "Tree", "Leaf3", "Leaf5", "Leaf4", "Leaf1", "Leaf2", "Flower1", "Flower2", "Lotus", "Stump", "Ginseng", "Banana", "Fruits", "PsychedelicMushrooms", "BerserkerMushroom", "Hypnotoad", "Weasel", "Kangaroo", "Spring2", "Virus", "Inkblot1", "Inkblot2", "BarbedCone", "Drop2", "MagicDrop", "Bubble", "Bubbles", "Ship", "Flag1", "Flag2", "4PointStarMedium", "4PointStarLarge", "5PointStarMedium", "5PointStarLarge", "6PointStarMedium", "6PointStarLarge", "7PointStarLarge", "8PointStarLarge", "8PointDoubleStarLarge", "Award", "OvalSmall", "OvalLarge", "CircleSmall", "CircleMedium", "CircleGem", "CircleDouble", "CircleTriple", "Rhombus", "RhombusSmall", "RhombusGem", "RhombusDouble", "PentagonMedium", "PentagonGem", "PentagonGemLarge", "HexagonMedium", "HexagonGem", "ExclamationMark", "Herbalist", "Mushroomer", "Dwarf", "Chest", "DoorWooden", "DoorMetal", "DoorOpen", "IronBars", "Key1", "Key2", "Lock", "LockBroken", "Lockpicking",];
        public static string[] AllIngredients = ["Windbloom", "Featherbloom", "FoggyParasol", "Fluffbloom", "Whirlweed", "PhantomSkirt", "CloudCrystal", "WitchMushroom", "ThunderThistle", "DreamBeet", "ShadowChanterelle", "Mageberry", "ArcaneCrystal", "Waterbloom", "Icefruit", "Tangleweed", "Coldleaf", "KrakenMushroom", "Watercap", "FrostSapphire", "Lifeleaf", "Goodberry", "DruidsRosemary", "MossShroom", "HealersHeather", "EvergreenFern", "LifeCrystal", "Terraria", "DryadsSaddle", "Poopshroom", "Weirdshroom", "Goldthorn", "Mudshroom", "EarthPyrite", "StinkMushroom", "GoblinMushroom", "Marshroom", "HairyBanana", "Thornstick", "GraveTruffle", "PlagueStibnite", "Firebell", "SulphurShelf", "Lavaroot", "Flameweed", "MagmaMorel", "DragonPepper", "FireCitrine", "MadMushroom", "Bloodthorn", "TerrorBud", "GraspingRoot", "Boombloom", "LustMushroom", "BloodRuby", "RainbowCap", "FableBismuth",];
    }
}
