using BepInEx;
using PotionCraft.SceneLoader;
using PotionCraft.ScriptableObjects.BuildZone;

namespace StackableGarden
{
    [BepInPlugin("Truinto." + ModInfo.MOD_NAME, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
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
