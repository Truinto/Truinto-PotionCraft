using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;

namespace ShareRecipe
{
    public partial class Plugin
    {
        public class SerializedRecipebook
        {
            [JsonInclude] public List<SerializedRecipe?> SavedRecipes = new();
            [JsonInclude] public Dictionary<RecipeBookPageContentType, int> RecipeTypes = new();
            [JsonInclude] public List<BookmarkController.SerializedBookmarkController> Controllers = new();
            [JsonInclude] public string? BookmarkOrganizer;
        }
    }
}
