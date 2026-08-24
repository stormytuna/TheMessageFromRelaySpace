using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using TMFRS.MonoBehaviours;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TMFRS.UI;

[HarmonyPatch]
public class RelayWindow
{
	private const int LinesPerPage = 12;

	private static GameObject relayRoot = null;
	private static TextMeshPro relayOutput = null;
	private static RelaySocket relaySocket = null;
	private static ScrollBar3D scrollbar = null;
	private static ScrollArea scrollArea = null;
	private static CalculatorWindow calculatorWindow = null;

	private static float lineHeight;
	private static float totalDisplayHeight;

	[HarmonyPatch(typeof(ConsoleDisplay), "Awake")]
	[HarmonyPostfix]
	public static void Init(ConsoleDisplay __instance) {
		relayRoot = new GameObject("Relay Output");
		relayRoot.SetActive(false);

		calculatorWindow = GameObject.Find("Calculator Window").GetComponent<CalculatorWindow>();

		var topBar = GameObject.Instantiate(__instance.readMessageGroup);
		topBar.transform.SetParent(relayRoot.transform);
		topBar.transform.position = new Vector3(-27.75f, 5f, -0.005f);
		topBar.name = "Relay Top Bar";
		topBar.GetComponentInChildren<TextMeshPro>().text = "Relay";

		var logoutButton = topBar.transform.Find("Respond Button");
		logoutButton.GetComponentInChildren<TextMeshPro>().text = "Logout";
		logoutButton.GetComponentInChildren<Button3D>().OnUseButton = new UnityEvent();
		logoutButton.GetComponentInChildren<Button3D>().OnUseButton.AddListener(LogOut);

		var dscrOutputObj = GameObject.Instantiate(GameObject.Find("Output Display"));
		dscrOutputObj.transform.SetParent(relayRoot.transform);
		dscrOutputObj.transform.position = new Vector3(-27.9f, 5.55f, 0.22f);
		dscrOutputObj.name = "Relay Output Signals";
		relayOutput = dscrOutputObj.GetComponent<TextMeshPro>();
		relayOutput.fontSize = 0.9f;

		scrollbar = topBar.GetComponentInChildren<ScrollBar3D>();
		scrollbar.visuals = scrollbar.transform.Find("Scroll Visuals").gameObject;
		scrollbar.col = scrollbar.GetComponent<BoxCollider>();
		scrollbar.meshRenderer = scrollbar.GetComponentInChildren<MeshRenderer>();
		scrollArea = relayOutput.GetComponent<ScrollArea>();
		scrollArea.initialConfigure = false;
		scrollbar.scrollArea = scrollArea.gameObject;

		totalDisplayHeight = relayOutput.rectTransform.sizeDelta.y;
		lineHeight = totalDisplayHeight / LinesPerPage;
	}

	// TODO: Do we really need to make the socket in this object specifically?
	public static void SetCallsign(string callsign) {
		if (relaySocket == null) {
			RelaySocket.Callsign = callsign;
			var socketObj = new GameObject("Relay Socket");
			socketObj.AddComponent<RelaySocket>();
			relaySocket = socketObj.GetComponent<RelaySocket>();
			return;
		}

		// TODO: Update callsign if asked
	}

	public static void TryShow() {
		if (relayRoot.activeSelf) {
			return;
		}

		relayRoot.SetActive(true);

		var console = ConsoleDisplay.instance;
		console.display.gameObject.SetActive(false);
		console.readMessageGroup.gameObject.SetActive(false);
		console.monitorVisual.OnWipe();
	}

	public static void LogOut() {
		relayRoot.SetActive(false);

		if (relaySocket != null) {
			relaySocket.Disconnect();
			GameObject.Destroy(relaySocket);
			relaySocket = null;
		}

		relayOutput.text = "";
		RelaySocket.Callsign = null;

		RelayManagerWindow.infoDisplay.SwitchWindow(RelayManagerWindow.infoDisplay.tabsWindow);

		var console = ConsoleDisplay.instance;
		console.display.gameObject.SetActive(true);
		console.readMessageGroup.gameObject.SetActive(true);
		console.monitorVisual.OnWipe();
	}

	public static void PrintText(string text) {
		char messageType = text[0];

		if (messageType == 'C') {
			// Syncing active clients, don't care about this
			return;
		}

		if (messageType != 'R') {
			return;
		}

		var messageRegex = new Regex(@"^R,([\d]+),([\d]+),(.*)$");
		var matches = messageRegex.Match(text);
		string sender = matches.Groups[1].Value;
		string messageNumber = matches.Groups[2].Value;

		int[] messageSignals = matches.Groups[3].Value.Split(',').Select(x => int.Parse(x)).ToArray();
		var signalMessage = new SignalMessage() with { signals = messageSignals };

		// TODO: Make configurable, also only do confetti if -702 is defined
		if (messageSignals.Contains(-702)) {
			GameObject.Find("Confetti L (Controller)").GetComponent<ConfettiCannon>().Fire();
		}

		var compileResult = new CompilerResult();
		var compiler = ConsoleDisplay.Instance.compiler;
		var compiledMessage = compiler.CompileSignalToString(signalMessage, ref compileResult);
		string compiledOutput = "";
		for (int i = 0; i < compiledMessage.Count; i++) {
			if (i == compiledMessage.Count - 1) {
				compiledOutput += compiledMessage[i];
			}
			else {
				compiledOutput += compiledMessage[i] + "\n";
			}
		}

		var senderBase10 = int.Parse(sender);
		var senderBase8 = calculatorWindow.EuclideanBaseChange(senderBase10, 10, 8);
		relayOutput.text += $"{senderBase8}:{messageNumber}\n{compiledOutput}\n\n";

		// TODO: Scroll bar doesn't work until another message is sent, odd
		relayOutput.StartCoroutine(SetupScrollbar(compiledMessage.Count));
	}

	private static IEnumerator SetupScrollbar(int count) {
		yield return null;

		float num = (relayOutput.textInfo.lineCount + 1) * lineHeight;
		float relativeMenuHeight = num / totalDisplayHeight;
		bool scrollToBottom = scrollbar.NormalizedScroll <= 0.01f;
		scrollbar.ConfigureHeight(relativeMenuHeight, true);
		if (scrollToBottom) {
			scrollbar.ForceScrollTo(0f);
		}
		scrollArea.Configure(scrollbar, lineHeight + num, totalDisplayHeight);
	}
}
