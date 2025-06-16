using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Ingredient;
using PotionCraft.ManagersSystem.Player;
using PotionCraft.ManagersSystem.TMP;
using PotionCraft.ObjectBased.Garden;
using PotionCraft.ObjectBased.UIElements.FloatingText;
using PotionCraft.ScriptableObjects.BuildableInventoryItem;
using UnityEngine;
using static PotionCraft.ObjectBased.UIElements.FloatingText.CollectedFloatingText;

namespace RearrangeBookmarks
{
    public static class Helpers
    {
        // Resources.FindObjectsOfTypeAll<WateringPotItem>().ToList();
        // _ = Managers.Room.CurrentRoomIndex;
        // Managers.Room.InstantiatedRooms.TryGetValue(RoomIndex.Garden, out var room);
        // Managers.Room.plants.GetPlantsStatus(RoomIndex.Garden);

        public static List<BuildableInventoryItem> GetSeeds()
        {
            var allBuildableItems = BuildableInventoryItem.allBuildableItems;
            return allBuildableItems[BuildableInventoryItemType.Seed];
        }

        public static GameObject[] GetSeeds2()
        {
            var gameObject = GameObject.Find("ItemContainer");
            if (gameObject == null)
                return [];
            var list = new List<GameObject>();
            foreach (Transform item in gameObject.transform)
            {
                if (item.GetComponent<GrowingSpotController>() != null && item.name.EndsWith("Seed"))
                    list.Add(item.gameObject);
            }
            return list.ToArray();
        }
    }
}
