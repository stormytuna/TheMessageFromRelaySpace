using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace TMFRS;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class TMFRSPlugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;

	private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

	private void Awake() {
		Logger = base.Logger;
		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

		harmony.PatchAll();
	}
}

