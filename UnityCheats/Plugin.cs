using BepInEx;
using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.RecipeMap;
using PotionCraft.ObjectBased.RecipeMap;
using PotionCraft.SceneLoader;
using UnityEngine;

namespace UnityCheats
{
    [BepInPlugin("Truinto.UnityCheats", "UnityCheats", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private Button RotateKey = null!;
        private Button ShiftKey = null!;
        private bool Rotating = false;

        public void Awake()
        {
            RotateKey = KeyboardKey.Get(KeyCode.O);
            ShiftKey = KeyboardKey.Get(KeyCode.LeftShift);

            var cmd = new Command("TeleportPotionToMouse", [new HotKey([KeyboardKey.Get(KeyCode.Q)])]);
            cmd.onDownedEvent.AddListener(TeleportPotionToMouse);

            cmd = new Command("HealPotion", [new HotKey([KeyboardKey.Get(KeyCode.H)])]);
            cmd.onDownedEvent.AddListener(HealPotion);

            cmd = new Command("RotatePotion", [new HotKey([KeyboardKey.Get(KeyCode.O)])]);
            cmd.onDownedEvent.AddListener(() => Rotating = true);
        }

        public void Update()
        {
            if (RotateKey.State is State.Upped) // CommandInvokeRepeater?
                Rotating = false;
            else if (Rotating)
                RotatePotion();
        }

        public void HealPotion()
        {
            if (!ObjectsLoader.isLoaded || GameUnloader.IsUnloadingStarted())
                return;

            Managers.RecipeMap.indicator.AddHealthBySalt(1f);
        }

        public void TeleportPotionToMouse()
        {
            if (!ObjectsLoader.isLoaded || GameUnloader.IsUnloadingStarted())
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
            if (!ObjectsLoader.isLoaded || GameUnloader.IsUnloadingStarted())
                return;

            Managers.RecipeMap.indicatorRotation.SetRotatorType(IndicatorRotatorType.Other);

            if (ShiftKey.State == State.Downed)
                Managers.RecipeMap.indicatorRotation.RotateTo(Managers.RecipeMap.indicatorRotation.Value - 1f);
            else
                Managers.RecipeMap.indicatorRotation.RotateTo(Managers.RecipeMap.indicatorRotation.Value + 1f);
        }
    }
}
