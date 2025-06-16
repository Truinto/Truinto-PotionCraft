using BepInEx;
using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.RecipeMap;
using UnityEngine;

namespace UnityCheats
{
    [BepInPlugin("Truinto.UnityCheats", "UnityCheats", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private Button HealKey = null!;
        private Button TeleportKey = null!;

        public void Awake()
        {
            HealKey = KeyboardKey.Get(KeyCode.H);
            TeleportKey = KeyboardKey.Get(KeyCode.Q);
        }

        public void Update()
        {
            if (HealKey.State == State.JustDowned)
            {
                Debug.Log("Hotkey HealPotion");
                HealPotion();
            }
            if (TeleportKey.State == State.JustDowned)
            {
                Debug.Log("Hotkey TeleportKey");
                TeleportPotionToMouse();
            }
        }

        public static void HealPotion()
        {
            if (Managers.RecipeMap == null || !Managers.RecipeMap.gameObject.activeInHierarchy)
                return;

            Managers.RecipeMap.indicator.AddHealthBySalt(1f);
        }

        public static void TeleportPotionToMouse()
        {
            if (Managers.RecipeMap == null || !Managers.RecipeMap.gameObject.activeInHierarchy)
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
    }
}
