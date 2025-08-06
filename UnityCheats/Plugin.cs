using BepInEx;
using DarkScreenSystem;
using HarmonyLib;
using JetBrains.Annotations;
using PotionCraft.Core.Extensions;
using PotionCraft.FactionSystem;
using PotionCraft.InputSystem;
using PotionCraft.InventorySystem;
using PotionCraft.LocalizationSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.BuildMode.Settings;
using PotionCraft.ManagersSystem.Environment;
using PotionCraft.ManagersSystem.Ingredient;
using PotionCraft.ManagersSystem.Potion;
using PotionCraft.ManagersSystem.RecipeMap;
using PotionCraft.ObjectBased.PhysicalParticle;
using PotionCraft.ObjectBased.RecipeMap;
using PotionCraft.ObjectBased.RecipeMap.RecipeMapItem.IndicatorMapItem;
using PotionCraft.ObjectBased.Salt;
using PotionCraft.ObjectBased.Stack;
using PotionCraft.ObjectBased.Stack.StackItem;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.ConfirmationWindowSystem;
using PotionCraft.ObjectBased.UIElements.ElementChangerWindow.AlchemySubstanceCustomizationWindow;
using PotionCraft.ObjectBased.UIElements.FloatingText;
using PotionCraft.ObjectBased.UIElements.PotionDescriptionWindow;
using PotionCraft.QuestSystem;
using PotionCraft.SceneLoader;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Ingredient;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.ScriptableObjects.Salts;
using PotionCraft.Settings;
using Shared;
using Shared.CollectionNS;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityCheats
{
    [BepInPlugin("Truinto." + ModInfo.MOD_NAME, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private Button? F4Key;
        private Button? RotateKey;
        private Button? ShiftKey;
        private Button? EnterKey;
        private bool Rotating = false;

        public void Awake()
        {
            F4Key = KeyboardKey.Get(KeyCode.F4);
            RotateKey = KeyboardKey.Get(KeyCode.O);
            ShiftKey = KeyboardKey.Get(KeyCode.LeftShift);
            EnterKey = KeyboardKey.Get(KeyCode.KeypadEnter);

            var cmd = new Command("TruintoTeleportPotionToMouse", [new HotKey([KeyboardKey.Get(KeyCode.LeftControl), KeyboardKey.Get(KeyCode.Q)])]);
            cmd.onDownedEvent.AddListener(TeleportPotionToMouse);

            cmd = new Command("TruintoHealPotion", [new HotKey([KeyboardKey.Get(KeyCode.LeftControl), KeyboardKey.Get(KeyCode.H)])]);
            cmd.onDownedEvent.AddListener(HealPotion);

            cmd = new Command("TruintoRotatePotion", [new HotKey([KeyboardKey.Get(KeyCode.O)])]);
            cmd.onDownedEvent.AddListener(() => Rotating = true);

            cmd = new Command("TruintoScripting", [new HotKey([KeyboardKey.Get(KeyCode.LeftControl), KeyboardKey.Get(KeyCode.X)])]);
            cmd.onDownedEvent.AddListener(StartScripting);

            Harmony.CreateAndPatchAll(typeof(Plugin));

            ObjectsLoader.onLoadingEnd.AddListener(OnLoadingEnd);
            ObjectsLoader.onLoadingEnd.AddListener(PrintQuests);
        }

        public void OnLoadingEnd()
        {
        }

        public void Update()
        {
            if (RotateKey?.State == State.Upped) // CommandInvokeRepeater?
                Rotating = false;
            else if (Rotating)
                RotatePotion();
            if (F4Key?.State == State.JustDowned)
            {
            }
            //if (EnterKey?.State == State.JustDowned)
            //{
            //    if (Managers.Potion.descriptionWindow.isShowing)
            //    {
            //        Managers.Potion.descriptionWindow.descriptionInputField.DeactivateInputField();
            //        Managers.Potion.descriptionWindow.Show(false, default);
            //    }
            //}

            if (ScriptStirLeft <= 0f)
                ScriptStirLeft = 0f;
            else if (ScriptStirLeft > 0.01f)
            {
                Managers.RecipeMap.indicator.lengthToDeleteFromPath += 0.01f;
                ScriptStirLeft -= 0.01f;
            }
            else
            {
                Managers.RecipeMap.indicator.lengthToDeleteFromPath += ScriptStirLeft;
                ScriptStirLeft = 0;
            }

            if (ScriptLadleLeft <= 0f)
                ScriptLadleLeft = 0f;
            else if (ScriptLadleLeft > 0.1f)
            {
                Managers.RecipeMap.MoveIndicatorByLadle(1f);
                ScriptLadleLeft -= 0.1f;
            }
            else
            {
                ScriptLadleLeft = 0;
            }

            if (ScriptHeatLeft <= 0f)
                ScriptHeatLeft = 0f;
            else if (ScriptHeatLeft > 0.01f)
            {
                Managers.Ingredient.coals.Heat += 0.01f;
                ScriptHeatLeft -= 0.01f;
            }
            else
            {
                Managers.Ingredient.coals.Heat += ScriptHeatLeft;
                ScriptHeatLeft = 0;
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

            if (ShiftKey?.State == State.Downed)
                Managers.RecipeMap.indicatorRotation.RotateTo(Managers.RecipeMap.indicatorRotation.Value - 1f);
            else
                Managers.RecipeMap.indicatorRotation.RotateTo(Managers.RecipeMap.indicatorRotation.Value + 1f);
        }

        #region Scripting

        /*
         * NonTeleportationFixedHint.RunAlongPath
         * TeleportationFixedHint.RunAlongPath
         * 
         * 
         * Record teleport from crystals
         * Stop recording when potion resets; not when ctrl+X
         * 
         * ladle
         * RecipeMapManager.MoveIndicatorTowardsObject
         */

        public static void StartScripting()
        {
            ShowInputBox("Script", null, default, ProcessScript);
        }

        public static StringBuilder PotionLogger = new();
        public static float PotionFloat;

        public static void ScriptFlush(RecipeBookRecipeMarkType nextType, string? nextString = null, float nextFloat = 0f)
        {
            // look for last action; if next action is something new, record the result, otherwise accumulate
            var mark = Managers.Potion.recipeMarks.GetMarksList().LastOrDefault();
            if (mark != null)
            {
                switch (mark.type)
                {
                    case RecipeBookRecipeMarkType.Spoon:
                        if (nextType != RecipeBookRecipeMarkType.Spoon)
                        {
                            if (PotionFloat > 0f)
                            {
                                PotionLogger.AppendLine($"Stir: {PotionFloat.ToString("G9", CultureInfo.InvariantCulture)}");
                                ShowFloatingMessage($"stir ={PotionFloat.ToString(CultureInfo.InvariantCulture)}", time: 1f);
                            }
                            PotionFloat = 0f;
                        }
                        else
                        {
                            // TODO: if nextFloat is large, apply multiplier? (0.99f)
                            PotionFloat += nextFloat;
                            ShowFloatingMessage($"stir +{nextFloat.ToString("G9", CultureInfo.InvariantCulture)} ={PotionFloat.ToString(CultureInfo.InvariantCulture)}", time: 1f);
                        }
                        break;

                    case RecipeBookRecipeMarkType.Ladle:
                        if (nextType != RecipeBookRecipeMarkType.Ladle)
                        {
                            if (PotionFloat > 0f)
                            {
                                PotionLogger.AppendLine($"Ladle: {PotionFloat.ToString("G9", CultureInfo.InvariantCulture)}");
                                ShowFloatingMessage($"ladle ={PotionFloat.ToString(CultureInfo.InvariantCulture)}", time: 1f);
                            }
                            PotionFloat = 0f;
                        }
                        else
                        {
                            PotionFloat += nextFloat;
                            ShowFloatingMessage($"ladle +{nextFloat.ToString("G9", CultureInfo.InvariantCulture)} ={PotionFloat.ToString(CultureInfo.InvariantCulture)}", time: 1f);
                        }
                        break;

                    case RecipeBookRecipeMarkType.Bellows:
                        if (nextType != RecipeBookRecipeMarkType.Bellows)
                        {
                            if (PotionFloat > 0f)
                            {
                                PotionLogger.AppendLine($"Heat: {PotionFloat.ToString("G9", CultureInfo.InvariantCulture)}");
                                ShowFloatingMessage($"heat ={PotionFloat.ToString(CultureInfo.InvariantCulture)}", time: 1f);
                            }
                            PotionFloat = 0f;
                        }
                        else
                        {
                            PotionFloat += nextFloat;
                            ShowFloatingMessage($"heat +{nextFloat.ToString("G9", CultureInfo.InvariantCulture)} ={PotionFloat.ToString(CultureInfo.InvariantCulture)}", time: 1f);
                        }
                        break;
                }
            }

            // these can be recorded immedately
            switch (nextType)
            {
                case RecipeBookRecipeMarkType.Salt:
                    PotionLogger.AppendLine($"Salt: {nextString}:{Mathf.RoundToInt(nextFloat)}");
                    break;
                case RecipeBookRecipeMarkType.Ingredient:
                    PotionLogger.AppendLine($"Ingredient: {nextString}:{nextFloat.ToString("G9", CultureInfo.InvariantCulture)}");
                    break;
            }
        }

        [HarmonyPatch(typeof(PotionManager.RecipeMarksSubManager), nameof(PotionManager.RecipeMarksSubManager.ResetCurrentRecipeMarks))]
        [HarmonyPrefix]
        public static void Patch1()
        {
            ScriptFlush(default);
            PotionLogger.Clear();
        }

        [HarmonyPatch(typeof(PotionManager.RecipeMarksSubManager), nameof(PotionManager.RecipeMarksSubManager.AddSaltMark))]
        [HarmonyPrefix]
        public static void Patch2(Salt salt)
        {
            ScriptFlush(RecipeBookRecipeMarkType.Salt, salt.name);
        }

        [HarmonyPatch(typeof(PotionManager.RecipeMarksSubManager), nameof(PotionManager.RecipeMarksSubManager.AddIngredientMark))]
        [HarmonyPrefix]
        public static void Patch3(Ingredient ingredient, float grindStatus)
        {
            ScriptFlush(RecipeBookRecipeMarkType.Ingredient, ingredient.name, grindStatus);
        }

        [HarmonyPatch(typeof(PotionManager.RecipeMarksSubManager), nameof(PotionManager.RecipeMarksSubManager.AddSpoonMark))]
        [HarmonyPrefix]
        public static void Patch4(float spoonValue)
        {
            ScriptFlush(RecipeBookRecipeMarkType.Spoon, nextFloat: spoonValue);
        }

        [HarmonyPatch(typeof(RecipeMapManager), nameof(RecipeMapManager.MoveIndicatorByLadle))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Patch5(IEnumerable<CodeInstruction> code, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(code, generator, original);
            tool.Seek(typeof(RecipeMapManagerIndicatorSettings), nameof(RecipeMapManagerIndicatorSettings.ladleIndicatorRotationTime));
            tool.Offset(1);
            tool.NameLocal("movedDistance");
            tool.Seek(typeof(RecipeMapManager), nameof(RecipeMapManager.MoveIndicatorTowardsObject));
            tool.InsertAfter(patch);
            return tool;
            static void patch([LocalParameter("movedDistance")] float movedDistance)
            {
                if (movedDistance > 0f)
                    ScriptFlush(RecipeBookRecipeMarkType.Ladle, nextFloat: movedDistance);
            }
        }

        [HarmonyPatch(typeof(RecipeMapManager), nameof(RecipeMapManager.MoveIndicatorTowardsVortex))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Patch6(IEnumerable<CodeInstruction> code, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(code, generator, original);
            tool.Seek(tool.GetLocal(typeof(float), "distance"), false);
            tool.InsertAfter(patch);
            return tool;
            static void patch([LocalParameter("distance")] float distance)
            {
                const float mult = 17.714f / 21.0617428f;
                if (distance > 0f)
                    ScriptFlush(RecipeBookRecipeMarkType.Bellows, nextFloat: distance * mult);
            }
        }

        public static void ProcessScript(string value)
        {
            if (value.Length <= 0)
                return;

            if (value[0] == 'p')
            {
                ScriptFlush(default);
                ShowInputBox("Output", PotionLogger.ToString(), default, null);
                return;
            }

            Match match; int count; float amount;
            if ((match = Regex.Match(value, @"Ingredient: (\w+):([\d\.]+)")).Success)
            {
                float.TryParse(match.Groups[2].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
                ScriptAddIngredient(match.Groups[1].Value, amount);
                return;
            }
            if ((match = Regex.Match(value, @"Salt: (\w+):(\d+)")).Success)
            {
                int.TryParse(match.Groups[2].Value, out count);
                ScriptAddSalt(match.Groups[1].Value, count);
                return;
            }
            if ((match = Regex.Match(value, @"Stir: ([\d\.]+)")).Success)
            {
                float.TryParse(match.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
                ScriptAddStir(amount);
                return;
            }
            if ((match = Regex.Match(value, @"Ladle: ([\d\.]+)")).Success)
            {
                float.TryParse(match.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
                ScriptAddLadle(amount);
                return;
            }
            if ((match = Regex.Match(value, @"Heat: ([\d\.]+)")).Success)
            {
                float.TryParse(match.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
                ScriptAddHeat(amount);
                return;
            }
        }

        public static void ScriptAddIngredient(string name, float grindStatus)
        {
            var positionInventory = new Vector2(6f, 0f);
            var positioncauldron = (Vector2)Managers.Ingredient.cauldron.transform.position + Managers.Ingredient.cauldron.spawnAboveCauldronFromScenePosition;

            var ingredient = Managers.Player.Inventory.items.FirstOrDefault(f => f.Key is Ingredient && f.Key.name == name);
            if (ingredient.Value >= 1)
            {
                Managers.Player.Inventory.RemoveItem(ingredient.Key, 1, false);

                // see StackThrower.TryThrowToCauldron()
                var stack = Stack.SpawnNewItemStack(positionInventory, (Ingredient)ingredient.Key, Managers.Player.InventoryPanel);
                stack.state.TryToSet(StateMachine.State.InHand);
                stack.transform.Rotate(Vector3.forward * UnityEngine.Random.Range(0, 360));
                stack.isFallingFromPanel = true;
                stack.thisRigidbody.ThrowTowardsTheTarget(positioncauldron, EnvironmentManagerSettings.Asset.stackThrowingHeightRangeInRoom);

                foreach (var item in stack.itemsFromThisStack)
                {
                    if (item is not IngredientFromStack leave)
                        continue;
                    for (int i = 0; i < 3 && leave.currentGrindState < 3 && !leave.IsDestroyed; i++)
                        leave.NextGrindState();
                }
                stack.leavesGrindStatus = grindStatus;
                stack.substanceGrinding.grindTicksPerformed = stack.substanceGrinding.GrindTicksToFullGrind * grindStatus;
                stack.substanceGrinding._currentGrindStatus = grindStatus;
                stack.UpdateOverallGrindStatus();
                stack.UpdateGrindedSubstance();
                ShowFloatingMessage($"set grind to {stack.overallGrindStatus:P2}", time: 5f);
            }
        }

        public static void ScriptAddSalt(string name, int count)
        {
            var positioncauldron = (Vector2)Managers.Ingredient.cauldron.transform.position + Managers.Ingredient.cauldron.spawnAboveCauldronFromScenePosition;

            var salt = Managers.Player.Inventory.items.FirstOrDefault(f => f.Key is Salt && f.Key.name == name);
            if (salt.Value >= count)
            {
                Managers.Player.Inventory.RemoveItem(salt.Key, count, false);
                for (int i = 0; i < count; i++)
                    PhysicalParticle.SpawnSaltParticle((Salt)salt.Key, positioncauldron + new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0f, 3f)), Vector2.zero);
                ShowFloatingMessage($"add salt {count}", time: 5f);
            }
        }

        public static float ScriptStirLeft;
        public static void ScriptAddStir(float amount)
        {
            //Managers.RecipeMap.indicator.lengthToDeleteFromPath += amount;
            ScriptStirLeft = amount;
            ShowFloatingMessage($"add stir {amount:G9}", time: 5f);
        }

        public static float ScriptLadleLeft;
        public static void ScriptAddLadle(float amount)
        {
            //Managers.RecipeMap.MoveIndicatorByLadle(amount);
            ScriptLadleLeft = amount;
            ShowFloatingMessage($"add ladle {amount:G9}", time: 5f);
        }

        public static float ScriptHeatLeft;
        public static void ScriptAddHeat(float amount)
        {
            //Managers.Ingredient.coals.Heat = amount;
            ScriptHeatLeft = amount;
            ShowFloatingMessage($"add heat {amount:G9}", time: 5f);
        }

        #endregion

        #region Printout

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

        public static void PrintAllLocalizedText()
        {
            foreach (var d1 in LocalizationManager.localizationData.data)
            {
                Debug.Log($"{d1}:");
                foreach (var d2 in d1.Value.text)
                {
                    Debug.Log($"\t{d2.Key}: {d2.Value}");
                }
            }
        }

        #endregion

        #region Patches

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

        #endregion

        #region Messageboxes

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

        public static void ShowInputBox(string title, string? body, Vector2 position, Action<string>? onClose)
        {
            var window = Managers.Potion.descriptionWindow;
            if (window.isShowing)
                return;
            if (position == Vector2.zero)
                position = Managers.Potion.potionCraftPanel.customizationPanel.customizeDescriptionButton.descriptionWindowPosition;

            window.descriptionInputField.lineLimit = 0;
            window.Show(true, position, new Key("#parameters_1", [title], KeyParametersStyle.Normal, null), body);
            window.descriptionInputField.ActivateInputField();
            window.onActiveStateChanged.AddListener(inputBoxActiveState);

            void inputBoxActiveState(bool active)
            {
                if (!active)
                {
                    window.onActiveStateChanged.RemoveListener(inputBoxActiveState);
                    onClose?.Invoke(window.descriptionInputField.text ?? "");
                }
            }
        }

        #endregion

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

        #region collection dump

        public static string[] CustomIconOrder = ["QuestionMark", "Healing", "Cross2", "TwoCrosses", "ThreeCrosses", "Poison", "DissolvingSword", "Fire", "Flame", "FireSpiral", "Frost", "Snowflake", "Snowflakes", "Explosion", "Explosion2", "Explosion3", "Lightning", "Lightning2", "Lightning3", "Acid", "Drop1", "Sprout", "Sprout2", "BicepArm", "MuscleMan", "CatPaw", "JumpingPuma", "WingedBoots", "HighBoot", "MagicalVision", "Eye", "Mana", "MagicWand", "Light", "Light2", "Light3", "Sleep", "Moon", "Snail", "Bull'sHead2", "BullHead", "FullBull", "Enchantment", "Hearts", "Fig", "Sex", "Transparency", "Teleportation2", "Teleportation1", "Wings2", "Wings1", "Necromancy", "ExplodingSkull", "StoneSkin", "StoneShield", "PoisonProtection", "AcidProtection", "LightningProtection", "FireProtection", "FrostProtection", "MagicProtection", "MagicShield", "FireballShield", "Glue", "StickyBoot", "Oilcan", "SickCloud", "SkullCloud", "Incense", "Perfume", "Shrinking", "Enlargement", "Apple", "Harp", "Lyre", "Scream", "PsychedelicSpiral", "HorseshoeUp", "HorseshoeDown", "Shamrock", "VoodooDoll", "Nigredo", "Albedo", "Citrinitas", "Rubedo", "Philosopher'sStone", "VoidSalt", "MoonSalt", "SunSalt", "LifeSalt", "Philosopher'sSalt", "OliveOil", "BerserkerAxe1", "Wheat", "Spring", "SlowDown", "StoneFigure", "Cross1", "Sword", "Swords", "BerserkerAxe2", "BowAndArrow", "Staff", "WizardHat", "Cloak", "Ring", "Goblet", "Juggler", "CurvedHorn", "WarHorn", "Trumpets", "Cymbals", "Bell", "Drum", "Magnet", "Anvil", "Hammer", "Sickle", "Weight", "ElfEar", "Torso", "BubbleLungs", "VikingHead", "MedusaHead", "Illithid", "BrainWaves", "AstralFace", "ThirdEye", "MasonEye", "BlindEye", "PsychedelicEye", "CrossedEye", "Ouroboros", "Astral2", "MagicStar", "Astral1", "Telekinesis", "Brain", "RadiatingBrain", "InkblotBrain", "RadiatingHeart", "Bones", "SkullAndBones1", "SkullAndBones2", "BurningBones", "BoneCloud", "Tombstone", "BaphometPentagram", "DissolvingPentagram", "DissolvingSpiral", "Bird", "Eagle", "Feather1", "Feather2", "Wing", "Cloud", "MistCloud", "SmokeCloud1", "SmokeCloud2", "Poo1", "Poo2", "UnknownSubstance", "Fir", "Tree", "Leaf3", "Leaf5", "Leaf4", "Leaf1", "Leaf2", "Flower1", "Flower2", "Lotus", "Stump", "Ginseng", "Banana", "Fruits", "PsychedelicMushrooms", "BerserkerMushroom", "Hypnotoad", "Weasel", "Kangaroo", "Spring2", "Virus", "Inkblot1", "Inkblot2", "BarbedCone", "Drop2", "MagicDrop", "Bubble", "Bubbles", "Ship", "Flag1", "Flag2", "4PointStarMedium", "4PointStarLarge", "5PointStarMedium", "5PointStarLarge", "6PointStarMedium", "6PointStarLarge", "7PointStarLarge", "8PointStarLarge", "8PointDoubleStarLarge", "Award", "OvalSmall", "OvalLarge", "CircleSmall", "CircleMedium", "CircleGem", "CircleDouble", "CircleTriple", "Rhombus", "RhombusSmall", "RhombusGem", "RhombusDouble", "PentagonMedium", "PentagonGem", "PentagonGemLarge", "HexagonMedium", "HexagonGem", "ExclamationMark", "Herbalist", "Mushroomer", "Dwarf", "Chest", "DoorWooden", "DoorMetal", "DoorOpen", "IronBars", "Key1", "Key2", "Lock", "LockBroken", "Lockpicking",];
        public static string[] AllIngredients = ["Windbloom", "Featherbloom", "FoggyParasol", "Fluffbloom", "Whirlweed", "PhantomSkirt", "CloudCrystal", "WitchMushroom", "ThunderThistle", "DreamBeet", "ShadowChanterelle", "Mageberry", "ArcaneCrystal", "Waterbloom", "Icefruit", "Tangleweed", "Coldleaf", "KrakenMushroom", "Watercap", "FrostSapphire", "Lifeleaf", "Goodberry", "DruidsRosemary", "MossShroom", "HealersHeather", "EvergreenFern", "LifeCrystal", "Terraria", "DryadsSaddle", "Poopshroom", "Weirdshroom", "Goldthorn", "Mudshroom", "EarthPyrite", "StinkMushroom", "GoblinMushroom", "Marshroom", "HairyBanana", "Thornstick", "GraveTruffle", "PlagueStibnite", "Firebell", "SulphurShelf", "Lavaroot", "Flameweed", "MagmaMorel", "DragonPepper", "FireCitrine", "MadMushroom", "Bloodthorn", "TerrorBud", "GraspingRoot", "Boombloom", "LustMushroom", "BloodRuby", "RainbowCap", "FableBismuth",];

        #endregion
    }
}
