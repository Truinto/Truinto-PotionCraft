using BepInEx;
using PotionCraft.SceneLoader;
using PotionCraft.ScriptableObjects.BuildZone;

namespace StackableGarden
{
    [BepInPlugin("Truinto.StackableGarden", "StackableGarden", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public void Awake()
        {
            ObjectsLoader.onLoadingEnd.AddListener(MakeSeedsStackable);
        }

        public static void MakeSeedsStackable()
        {
            foreach (var zone in BuildZone.allBuildZones)
                zone.itemsCanBeLayered = true;
        }
    }
}
