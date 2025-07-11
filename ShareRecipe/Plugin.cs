using BepInEx;
using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.NotificationSystem;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.SceneLoader;
using Shared.JsonNS;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace ShareRecipe
{
    [BepInPlugin("Truinto.ShareRecipe", "ShareRecipe", "1.0.2")]
    public partial class Plugin : BaseUnityPlugin
    {
        private static Button? F4Key;
        private static Button? ShiftKey;

        public void Awake()
        {
            ReloadConfig();

            var cmd = new Command("Export Bookmark", [new HotKey([KeyboardKey.Get(KeyCode.LeftControl), KeyboardKey.Get(KeyCode.C)])]);
            cmd.onDownedEvent.AddListener(ExportRecipe);

            cmd = new Command("Import Bookmark", [new HotKey([KeyboardKey.Get(KeyCode.LeftControl), KeyboardKey.Get(KeyCode.V)])]);
            cmd.onDownedEvent.AddListener(ImportRecipe);

            cmd = new Command("Export Recipebook", [new HotKey([KeyboardKey.Get(KeyCode.LeftControl), KeyboardKey.Get(KeyCode.D)])]);
            cmd.onDownedEvent.AddListener(ExportRecipebook);

            cmd = new Command("Import Recipebook", [new HotKey([KeyboardKey.Get(KeyCode.LeftControl), KeyboardKey.Get(KeyCode.F)])]);
            cmd.onDownedEvent.AddListener(ImportRecipebook);
        }

        public void ReloadConfig()
        {
            Settings.Load();
            F4Key ??= KeyboardKey.Get(KeyCode.F4);
            ShiftKey ??= KeyboardKey.Get(KeyCode.LeftShift);
        }

        public void Update()
        {
            if (F4Key?.State == State.JustDowned)
            {
                ReloadConfig();
            }
        }

        public static void ExportRecipe()
        {
            try
            {
                if (!ObjectsLoader.isLoaded || GameUnloader.IsUnloadingStarted() || !RecipeBook.Instance.gameObject.activeInHierarchy)
                    return;
                IRecipeBookPageContent recipe = RecipeBook.Instance.savedRecipes[RecipeBook.Instance.currentPageIndex];
                if (recipe is null)
                    return;

                var serializedRecipe = recipe.GetSerializedRecipe();
                string json;
                if (ShiftKey?.State == State.Downed)
                {
                    json = JsonTool.Serialize(serializedRecipe, JsonTool.JsonOptions);
                }
                else
                {
                    json = JsonTool.Serialize(serializedRecipe, JsonTool.JsonOptionsCompact);
                    json = Compress(json, "0");
                }
                GUIUtility.systemCopyBuffer = json;
                Notification.ShowText("ShareRecipe", "Recipe copied", Notification.TextType.LevelUpText);
            } catch (Exception e) { Debug.Log(e.ToString()); }
        }

        public static void ImportRecipe()
        {
            try
            {
                if (!ObjectsLoader.isLoaded || GameUnloader.IsUnloadingStarted() || !RecipeBook.Instance.gameObject.activeInHierarchy)
                    return;
                if (RecipeBook.Instance.savedRecipes[RecipeBook.Instance.currentPageIndex] != null)
                    return;
                string json = GUIUtility.systemCopyBuffer;
                if (json is null || json.Length < 2)
                    return;

                json = Decompress(json);
                var serializedRecipe = JsonTool.Deserialize<SerializedRecipe>(json, JsonTool.JsonOptions);
                var recipe = SerializedRecipe.DeserializeRecipe(serializedRecipe);

                RecipeBook.Instance.savedRecipes[RecipeBook.Instance.currentPageIndex] = recipe;
                RecipeBook.Instance.UpdateBookmarkIcon(RecipeBook.Instance.currentPageIndex);
                RecipeBook.Instance.UpdateCurrentPage();
                RecipeBook.Instance.onRecipeAdded?.Invoke();
                Notification.ShowText("ShareRecipe", "Recipe pasted", Notification.TextType.LevelUpText);
            } catch (Exception e) { Debug.Log(e.ToString()); }
        }

        public static void ExportRecipebook()
        {
            try
            {
                if (!ObjectsLoader.isLoaded || GameUnloader.IsUnloadingStarted() || !RecipeBook.Instance.gameObject.activeInHierarchy)
                    return;
                var recipebook = new SerializedRecipebook();

                // get data
                foreach (var recipe in RecipeBook.Instance.savedRecipes)
                    recipebook.SavedRecipes.Add(recipe?.GetSerializedRecipe());
                recipebook.RecipeTypes = RecipeBook.Instance.currentRecipeIndexes;
                recipebook.Controllers = RecipeBook.Instance.bookmarkControllersGroupController.GetSerialized();

                // get mod data
                var type = Type.GetType("PotionCraftBookmarkOrganizer.Scripts.Services.SaveLoadService, PotionCraftBookmarkOrganizer", false);
                if (type != null)
                {
                    object[] parameters = ["{\"version\":1}"];
                    type.GetMethod("StoreBookmarkGroups").Invoke(null, parameters);
                    recipebook.BookmarkOrganizer = (string)parameters[0];
                }

                string json;
                if (ShiftKey?.State == State.Downed)
                {
                    json = JsonTool.Serialize(recipebook, JsonTool.JsonOptions);
                }
                else
                {
                    json = JsonTool.Serialize(recipebook, JsonTool.JsonOptionsCompact);
                    json = Compress(json, "1");
                }
                GUIUtility.systemCopyBuffer = json;

                Notification.ShowText("ShareRecipe", "Book copied", Notification.TextType.LevelUpText);
            } catch (Exception e) { Debug.Log(e.ToString()); }
        }

        public static void ImportRecipebook()
        {
            try
            {
                if (!ObjectsLoader.isLoaded || GameUnloader.IsUnloadingStarted() || !RecipeBook.Instance.gameObject.activeInHierarchy)
                    return;
                string json = GUIUtility.systemCopyBuffer;
                if (json is null || json.Length < 2)
                    return;
                json = Decompress(json);
                var recipebook = JsonTool.Deserialize<SerializedRecipebook>(json, JsonTool.JsonOptions) ?? throw new Exception("invalid json data");

                // override save state
                Managers.SaveLoad.SelectedProgressState.savedRecipes = recipebook.SavedRecipes;

                // set mod data
                var type = Type.GetType("PotionCraftBookmarkOrganizer.Scripts.Services.SaveLoadService, PotionCraftBookmarkOrganizer", false);
                var type2 = Type.GetType("PotionCraftBookmarkOrganizer.Scripts.Storage.StaticStorage, PotionCraftBookmarkOrganizer", false);
                if (type != null && type2 != null)
                {
                    type2.GetField("StateJsonString").SetValue(null, recipebook.BookmarkOrganizer);
                    type.GetMethod("RetreiveStoredBookmarkGroups").Invoke(null, null);
                }

                // set data
                RecipeBook.Instance.savedRecipes.Clear();
                for (int i = 0; i < recipebook.SavedRecipes.Count; i++)
                    RecipeBook.Instance.savedRecipes.Add(SerializedRecipe.DeserializeRecipe(recipebook.SavedRecipes[i]));
                RecipeBook.Instance.bookmarkControllersGroupController.LoadFrom(recipebook.Controllers);
                int count = RecipeBook.Instance.bookmarkControllersGroupController.GetAllBookmarksList().Count;
                for (int i = RecipeBook.Instance.savedRecipes.Count; i < count; i++)
                    RecipeBook.Instance.savedRecipes.Add(null);
                RecipeBook.Instance.currentPageIndex = 0;
                RecipeBook.Instance.currentRecipeIndexes = recipebook.RecipeTypes;

                // refresh icons
                for (int i = 0; i < recipebook.SavedRecipes.Count; i++)
                    RecipeBook.Instance.UpdateBookmarkIcon(i);

                Notification.ShowText("ShareRecipe", "Book pasted", Notification.TextType.LevelUpText);
                Debug.Log($"[ShareRecipe] Import Bookmarks controller={RecipeBook.Instance.bookmarkControllersGroupController.GetAllBookmarksList().Count} recipes={RecipeBook.Instance.savedRecipes.Count}");
            } catch (Exception e) { Debug.Log(e.ToString()); }
        }

        public static string Compress(string json, string prefix)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                msi.CopyTo(gs);
            }
            return prefix + Convert.ToBase64String(mso.ToArray());
        }

        public static string Decompress(string base64)
        {
            if (base64[0] is '0' or '1')
            {
                var bytes = Convert.FromBase64String(base64.Substring(1));
                using var msi = new MemoryStream(bytes);
                using var mso = new MemoryStream();
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }
                return Encoding.UTF8.GetString(mso.ToArray());
            }
            return base64;
        }
    }
}
