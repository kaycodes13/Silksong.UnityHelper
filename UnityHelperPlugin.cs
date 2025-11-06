using BepInEx;

namespace UnityHelper;

// Dummy class for showing that this exists in the output log
[BepInAutoPlugin(id: "io.github.flibber-hk.unityhelper")]
public partial class UnityHelperPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }
}
