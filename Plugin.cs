using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TMFRS.UI;

namespace TMFRS;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class TMFRSPlugin : BaseUnityPlugin
{
	public static ConfigEntry<string> RelaySource;
	public static ConfigEntry<ColorSetType> ConfigTheme;
	public static ConfigEntry<bool> PlayConfetti;
	public static ConfigEntry<float> RelayTypingDelay;
	public static ConfigEntry<int> RelayTypeCharByCharCutoff;
	public static ConfigEntry<int> RelayTypeLineByLineCutoff;

	internal static new ManualLogSource Logger;

	private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

	private void Awake() {
		Logger = base.Logger;
		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

		harmony.PatchAll();

		LoadConfig();
	}

	private void LoadConfig() {
		string section = "General";
		string key = "Colors";
		string description = "The theme the monitor uses.\nVisual = Pink, Brown, Red\nAtoms = Blue\nSpace = Purple\nLife = Yellow, Green\nAbstractDust = Yellow, Red\nVitality = Pink, Red\nPlanet = Cyan\nComplexCulture = Orange, Purple\nKnowledge = White\nRetroGreen = Green\nRandom = Picks a random theme each time you load the game";
		ConfigTheme = Config.Bind(section, key, ColorSetType.None, description);

		key = "Confetti";
		description = "Whether the confetti cannon should be allowed to play when a specific frequency is sent. The frequency must also be defined in your dictionary.";
		PlayConfetti = Config.Bind(section, key, true, description);

		section = "Typing";
		key = "RelayTypingDelay";
		description = "The time to wait between printing new lines or characters, in human seconds. Set to 0 to remove line-by-line and character-by-character printing";
		RelayTypingDelay = Config.Bind(section, key, 0.04f, description);

		key = "RelayTypeCharByCharCutoff";
		description = "The amount of characters a message must be under to be typed character-by-character. Messages with a length longer than this are types either line-by-line or instantly. Does nothing if you set RelayTypingDelay to 0";
		RelayTypeCharByCharCutoff = Config.Bind(section, key, 128, description);

		key = "RelayTypeLineByLineCutoff";
		description = "The amount of characters a message must be under to be typed line-by-line. Messages with a length longer than this are types instantly. Does nothing if you set RelayTypingDelay to 0";
		RelayTypeLineByLineCutoff = Config.Bind(section, key, 512, description);

		section = "Relay";
		key = "RelaySource";
		description = "The web socket source to pull Relay messages from.";
		RelaySource = Config.Bind(section, key, "wss://dscr-relay.dixonary.co.uk", description);
	}
}

