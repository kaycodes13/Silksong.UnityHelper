using BepInEx;

namespace UnityHelper;

#pragma warning disable CS1591
[BepInAutoPlugin(id: "org.silksong-modding.unityhelper")]
public partial class UnityHelperPlugin : BaseUnityPlugin
#pragma warning restore CS1591
{
    private void Awake()
    {
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }
}
