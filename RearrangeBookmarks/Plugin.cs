using BepInEx;
using HarmonyLib;
using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.NotificationSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.SceneLoader;
using System.Reflection;
using System.Security.Principal;
using UnityEngine;

namespace RearrangeBookmarks
{
    [BepInPlugin("Truinto." + ModInfo.MOD_NAME, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
    public partial class Plugin : BaseUnityPlugin
    {
        private Button? F4Key;
        private Command? RearrangeCommand;

        public void Awake()
        {
            F4Key = KeyboardKey.Get(KeyCode.F4);
            RearrangeCommand = new Command("RearrangeBookmarks", []);
            RearrangeCommand.onDownedEvent.AddListener(RearrangeBookmarks);
            ReloadConfig();
            Harmony.CreateAndPatchAll(typeof(Plugin));
            ObjectsLoader.onLoadingEnd.AddListener(() => BookmarkOrganizerUpdate = Type.GetType("PotionCraftBookmarkOrganizer.Scripts.Services.RecipeBookService, PotionCraftBookmarkOrganizer", false)?.GetMethod("UpdateBookmarkGroupsForCurrentRecipe"));
        }

        public void ReloadConfig()
        {
            Settings.Load();
            RearrangeCommand!.hotKeys = [new HotKey(KeyboardKey.Get(Settings.State.RearrangeKey))];
        }

        public void Update()
        {
            if (F4Key?.State == State.JustDowned)
            {
                ReloadConfig();
            }
        }

        public void RearrangeBookmarks()
        {
            if (RecipeBook.Instance == null || !RecipeBook.Instance.gameObject.activeInHierarchy)
                return;

            // notes:
            // - mark position is normalized; the position is from 0 to 1 in both directions
            // - marks have a real length of mr = 0.66
            // - for marks y = 0 is lowest point; y = 1 is highest point
            // - for marks x = mr/2 is first position; x = 1-mr/2 is last position
            // controllers=1:MainController
            // rail=LeftToRight1 length=7,4 marks=11 mark_sizes=7,26
            // rail=LeftToRight2 length=7,4 marks=11 mark_sizes=7,26
            // rail=TopToBottom1 length=10 marks=9 mark_sizes=5,94
            // rail=RightToLeft1 length=7,4 marks=11 mark_sizes=7,26
            // rail=RightToLeft2 length=7,4 marks=10 mark_sizes=6,6
            // rail=BottomToTop1 length=10 marks=3 mark_sizes=1,98
            // rail=BottomToTopInvisiRail length=100 marks=6 mark_sizes=3,96
            // rail=BottomToTopSubRail length=2,28 marks=0 mark_sizes=0

            var options = Settings.State.Rails;
            var group_controller = RecipeBook.Instance.bookmarkControllersGroupController;
            var bookmark_presort = group_controller.GetAllBookmarksList();
            var bookmark_controller = group_controller.controllers[0].bookmarkController;
            var rails = bookmark_controller.rails;
            Debug.Log($"controllers={group_controller.controllers.Count}:{group_controller.controllers.Join(s => s?.name)}");

            //marks[0].GetBookMarkSize();
            //rails[0].Connect(marks[0], marks[0].GetNormalizedPosition());

            int index = 0;
            foreach (var rail in rails)
            {
                if (rail.railBookmarks.Count == 0)
                    continue;

                // check if any options apply to this rail
                var option = options.FirstOrDefault(f => f.Name == rail.name);
                if (option == null)
                    goto next;

                // aggregate bookmark types
                var bookmarks = rail.railBookmarks.ToArray();
                var bookmark_type = new BookmarkType[bookmarks.Length];
                int empty = 0, top = 0, bottom = 0;
                for (int i = 0; i < bookmarks.Length; i++)
                {
                    var page = RecipeBook.Instance.GetPageContent(index + i);
                    if (page == null && option.StackEmpty)
                    {
                        bookmark_type[i] = BookmarkType.Empty;
                        empty++;
                    }
                    else if (page is IRecipeBookPageContent recipe && recipe.CustomDescription?.StartsWith(":") == true)
                    {
                        bookmark_type[i] = BookmarkType.Ignore;
                    }
                    else if (bookmarks[i].GetNormalizedPosition().y > 0.5f)
                    {
                        bookmark_type[i] = BookmarkType.Top;
                        top++;
                    }
                    else
                    {
                        bookmark_type[i] = BookmarkType.Bottom;
                        bottom++;
                    }
                }
                bool rail_plus1 = rail.name is "BottomToTopSubRail";
                if (rail_plus1) // +1 since modded rail has an extra bookmark
                    bottom++;

                // calculate how much space between each bookmark
                float bookmark_size = 0.66f / rail.size.x; // marks[0].GetBookMarkSize().x == 0.66f
                float rail_min = bookmark_size * option.LimitLeft;
                float rail_max = 1f - (bookmark_size * option.LimitRight);
                float bottom_step = 0f, top_step = 0f;
                bool too_large = bookmark_size * (bottom - 1) > rail_max - rail_min || bookmark_size * (top - 1) > rail_max - rail_min;
                if (option.Alignment is Alignment.Evenly || too_large)
                {
                    bottom_step = (rail_max - rail_min) / (bottom + (empty > 0 ? 0 : -1));
                    top_step = (rail_max - rail_min) / (top - 1);
                }
                else if (option.Alignment is Alignment.Left or Alignment.Right)
                {
                    bottom_step = top_step = bookmark_size;
                }

                // reposition bookmarks
                if (option.Alignment is Alignment.Right) // right alignment iterates in reverse
                {
                    float bottom_current = bookmark_size * (option.OffsetBottom + option.LimitRight);
                    float top_current = bookmark_size * (option.OffsetTop + option.LimitRight);
                    bottom_current = 1f - bottom_current;
                    top_current = 1f - top_current;
                    if (rail_plus1) // this starts at +1
                        bottom_current = Mathf.Clamp(bottom_current - bottom_step, rail_min, rail_max);
                    for (int i = bookmarks.Length - 1; i >= 0; i--)
                    {
                        switch (bookmark_type[i])
                        {
                            case BookmarkType.Empty:
                                bookmarks[i].SetPosition(new(rail_min, 0f));
                                break;
                            case BookmarkType.Bottom:
                                bookmarks[i].SetPosition(new(bottom_current, 0f));
                                bottom_current = Mathf.Clamp(bottom_current - bottom_step, rail_min, rail_max);
                                break;
                            case BookmarkType.Top:
                                bookmarks[i].SetPosition(new(top_current, 1f - option.LimitTop));
                                top_current = Mathf.Clamp(top_current - top_step, rail_min, rail_max);
                                break;
                        }
                    }
                }
                else
                {
                    float bottom_current = bookmark_size * (option.OffsetBottom + option.LimitLeft);
                    float top_current = bookmark_size * (option.OffsetTop + option.LimitLeft);
                    if (rail_plus1 && top == 1)
                        top_current = Mathf.Clamp(rail_max + bookmark_size * option.OffsetTop, rail_min, rail_max);
                    for (int i = 0; i < bookmarks.Length; i++)
                    {
                        switch (bookmark_type[i])
                        {
                            case BookmarkType.Empty:
                                bookmarks[i].SetPosition(new(rail_max, 0f));
                                break;
                            case BookmarkType.Bottom:
                                bookmarks[i].SetPosition(new(bottom_current, 0f));
                                bottom_current = Mathf.Clamp(bottom_current + bottom_step, rail_min, rail_max);
                                break;
                            case BookmarkType.Top:
                                bookmarks[i].SetPosition(new(top_current, 1f - option.LimitTop));
                                top_current = Mathf.Clamp(top_current + top_step, rail_min, rail_max);
                                break;
                        }
                    }
                }

                // change order to reflect position change
                //rail.SortBookmarksInClockwiseOrder();
                rail.railBookmarks.Sort((b1, b2) =>
                {
                    int pos = b1.transform.localPosition.x.CompareTo(b2.transform.localPosition.x);
                    if (pos == 0)
                        return b1.transform.localPosition.y.CompareTo(b2.transform.localPosition.y);
                    return pos;
                });

            // each bookmark's recipe data is indexed by the bookmark's index across all rails
            // so we need to sum all previous rails for use in RecipeBook.Instance.GetPageContent
            next:
                index += rail.railBookmarks.Count;
            }

            // trigger the bookmarks rearranged method to reorganize the savedRecipes list to match the new bookmark order
            bookmark_controller.CallOnBookmarksRearrangeIfNecessary(bookmark_presort);

            // if BookmarkOrganizer is installed, trigger an update call
            BookmarkOrganizerUpdate?.Invoke(null, null);
        }

        public static MethodInfo? BookmarkOrganizerUpdate;

        #region Patches

        [HarmonyPatch(typeof(BookmarkController), nameof(BookmarkController.LoadFrom))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        public static void Postfix(BookmarkController __instance)
        {
            if (ReferenceEquals(__instance, RecipeBook.Instance.bookmarkControllersGroupController.controllers[0].bookmarkController))
            {
                var type = Type.GetType("PotionCraftBookmarkOrganizer.Scripts.Storage.StaticStorage, PotionCraftBookmarkOrganizer", false);
                if (type == null)
                    return;
                var list = (List<int>)type.GetField("SavedRecipePositions").GetValue(null);
                if (list == null)
                    return;
                var savedRecipes = Managers.SaveLoad.SelectedProgressState.savedRecipes;
                int bookmarkCount = __instance.rails.Sum(s => s.railBookmarks.Count);
                Debug.Log($"[RearrangeBookmarks] savedRecipes={savedRecipes.Count} bookmarks={bookmarkCount} SavedRecipePositions count={list.Count}");

                if (savedRecipes.Count != list.Count)
                    Notification.ShowText("ERROR", "BookmarkOrganizer count mismatch", Notification.TextType.LevelUpText);

                //var duplicate = new List<int>();
                //for (int i = 0; i < list.Count; i++)
                //{
                //    if (duplicate.Contains(list[i]))
                //        Debug.Log($" {i} -> {list[i]}  duplicate!!");
                //    else
                //        Debug.Log($" {i} -> {list[i]}");
                //    duplicate.Add(list[i]);
                //}

                //while (savedRecipes.Count < list.Count)
                //    savedRecipes.Add(default);

                //while (list.Count < savedRecipes.Count)
                //    list.Add(list.Count - 1);
            }
        }

        #endregion
    }
}
