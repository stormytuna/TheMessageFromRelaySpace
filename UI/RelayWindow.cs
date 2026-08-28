using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using TRFDS.DataStructures;
using TRFDS.MonoBehaviours;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TRFDS.UI;

[HarmonyPatch]
public class RelayWindow
{
	private const float LinesPerPage = 12.315f;

	public static List<RelayMessage> receivedMessages = new List<RelayMessage>();

	private static GameObject relayRoot;
	private static GameObject recompileButton;
	private static TextMeshPro relayOutput;
	private static ScrollBar3D scrollbar;
	private static ScrollArea scrollArea;

	private static float lineHeight;
	private static float totalDisplayHeight;

	private static CalculatorWindow calculatorWindow;
	private static Oscilloscope relayOutputOscilloscope;

	[HarmonyPatch(typeof(ConsoleDisplay), "Awake")]
	[HarmonyPostfix]
	public static void Init(ConsoleDisplay __instance) {
		relayRoot = new GameObject("Relay Output");

		calculatorWindow = GameObject.Find("Calculator Window").GetComponent<CalculatorWindow>();

		var topBar = GameObject.Instantiate(__instance.readMessageGroup);
		topBar.name = "Relay Top Bar";
		topBar.transform.SetParent(relayRoot.transform);
		topBar.transform.position = new Vector3(-27.75f, 5f, -0.005f);
		topBar.GetComponentInChildren<TextMeshPro>().text = "Relay";
		GameObject.Destroy(topBar.transform.Find("Line Numbers").gameObject);

		var logoutButton = topBar.transform.Find("Respond Button");
		logoutButton.name = "Logout Button";
		logoutButton.transform.position = new Vector3(-27.05f, 5.675f, 0.257f);
		logoutButton.GetComponentInChildren<TextMeshPro>().text = "Logout";
		logoutButton.GetComponentInChildren<Button3D>().OnUseButton = new UnityEvent();
		logoutButton.GetComponentInChildren<Button3D>().OnUseButton.AddListener(LogOut);
		GameObject.Destroy(logoutButton.transform.Find("Icon").gameObject);
		UnityHelpers.ScaleMeshVertices(logoutButton.GetComponent<MeshFilter>().mesh, 0.7f);
		logoutButton.GetComponent<BoxCollider>().size -= Vector3.right * 0.3f;
		logoutButton.GetComponentInChildren<TextMeshPro>().transform.position += Vector3.right * 0.0365f;

		recompileButton = topBar.transform.GetChild(2).gameObject;
		recompileButton.name = "Recompile Button";
		recompileButton.transform.position = new Vector3(-27.95f, 5.675f, 0.257f);
		recompileButton.GetComponentInChildren<TextMeshPro>().text = "Recompile";
		recompileButton.GetComponentInChildren<Button3D>().OnUseButton = new UnityEvent();
		recompileButton.GetComponentInChildren<Button3D>().OnUseButton.AddListener(Recompile);
		GameObject.Destroy(recompileButton.transform.Find("Icon").gameObject);
		UnityHelpers.ScaleMeshVertices(recompileButton.GetComponent<MeshFilter>().mesh, 0.85f);
		recompileButton.GetComponent<BoxCollider>().size -= Vector3.right * 0.15f;
		recompileButton.GetComponentInChildren<TextMeshPro>().transform.position += Vector3.right * 0.0439f;
		recompileButton.gameObject.SetActive(false);

		var relayOutputObj = topBar.transform.Find("Output Display");
		relayOutputObj.name = "Relay Output Signals";
		relayOutput = relayOutputObj.GetComponent<TextMeshPro>();
		relayOutput.fontSize = 0.9f;
		relayOutput.maxVisibleLines = 0;
		relayOutput.maxVisibleCharacters = 0;

		scrollbar = topBar.GetComponentInChildren<ScrollBar3D>();
		scrollbar.name = "Scroll Handle Fucked";
		scrollbar.transform.position += Vector3.right * 0.5f; // Hiding scrollbar because I couldn't get it to function properly, sad!
		scrollbar.visuals = scrollbar.transform.Find("Scroll Visuals").gameObject;
		scrollbar.col = scrollbar.GetComponent<BoxCollider>();
		scrollbar.meshRenderer = scrollbar.GetComponentInChildren<MeshRenderer>();
		scrollArea = relayOutput.GetComponent<ScrollArea>();
		scrollbar.scrollArea = scrollArea.gameObject;

		totalDisplayHeight = relayOutput.rectTransform.sizeDelta.y;
		lineHeight = totalDisplayHeight / LinesPerPage;

		relayOutputOscilloscope = GameObject.Find("Output Oscilloscope").GetComponent<Oscilloscope>();

		RelaySocket.CallsignProcessed.AddListener((goodCallsign) => {
			if (goodCallsign) {
				recompileButton.SetActive(true);
			}
		});

		relayRoot.SetActive(false);
	}

	public static void TryShow() {
		if (relayRoot.activeSelf) {
			return;
		}

		relayRoot.SetActive(true);

		var console = ConsoleDisplay.Instance;
		console.display.gameObject.SetActive(false);
		console.readMessageGroup.gameObject.SetActive(false);
		console.monitorVisual.OnWipe();
	}

	public static void LogOut() {
		relayRoot.SetActive(false);
		recompileButton.SetActive(false);
		relayOutput.text = "";
		relayOutput.maxVisibleLines = 0;
		relayOutput.maxVisibleCharacters = 0;
		receivedMessages = new List<RelayMessage>();

		RelayManagerWindow.Disconnect();

		var console = ConsoleDisplay.Instance;
		console.display.gameObject.SetActive(true);
		console.readMessageGroup.gameObject.SetActive(true);
		console.monitorVisual.OnWipe();
	}

	private static string CompileSignalsToString(SignalMessage signalMessage, out CompilerResult compileResult) {
		compileResult = new CompilerResult();
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

		return compiledOutput;
	}
	
	private static void Recompile() {
		relayOutput.text = "";
		relayOutput.maxVisibleLines = 0;
		relayOutput.maxVisibleCharacters = 0;

		string text = "";
		foreach (var message in receivedMessages) {
			string compiledOutput = CompileSignalsToString(message.Signals, out _);
			text += $"{message.Sender}:{message.TransmissionId}\n{compiledOutput}\n\n";
		}

		relayOutput.StartCoroutine(PrintTextRoutine(text, false, true));
	}

	public static void PrintText(string text, bool byChar, bool instant) {
		if (text[0] != 'R') {
			return;
		}

		if (TRFDSPlugin.RelayTypingDelay.Value <= 0f) {
			instant = true;
		}

		// Relay sends the last 10 messages before you connected, so these should print quickly
		bool initialMessages = receivedMessages.Count < 10;
		if (initialMessages) {
			byChar = false;
		}

		var messageRegex = new Regex(@"^R,([\d]+),([\d]+),(.*)$");
		var matches = messageRegex.Match(text);
		string sender = matches.Groups[1].Value;
		string messageNumber = matches.Groups[2].Value;

		int[] messageSignals = matches.Groups[3].Value.Split(',').Select(x => int.Parse(x)).ToArray();
		var signalMessage = new SignalMessage() with { signals = messageSignals };

		if (!initialMessages && TRFDSPlugin.PlayConfetti.Value && messageSignals.Contains(-702) && UserDictionary.Instance.terms.ContainsKey(-702)) {
			GameObject.Find("Confetti L (Controller)").GetComponent<ConfettiCannon>().Fire();
		}

		var compiledOutput = CompileSignalsToString(signalMessage, out var compileResult);

		var senderBase10 = int.Parse(sender);
		var senderBase8 = calculatorWindow.EuclideanBaseChange(senderBase10, 10, 8);
		string message =  $"{senderBase8}:{messageNumber}\n{compiledOutput}\n\n";
		receivedMessages.Add(new RelayMessage() { 
			Sender = short.Parse(senderBase8),
			TransmissionId = short.Parse(messageNumber),
			Signals = signalMessage,
		});

		relayOutput.StartCoroutine(PrintTextRoutine(message, byChar, instant));

		if (TRFDSPlugin.Oscilloscopes.Value) {
			relayOutputOscilloscope.PlaySignal(signalMessage, true);
		}
	}

	private static IEnumerator PrintTextRoutine(string text, bool byChar, bool instant) {
		relayOutput.text += text;

		yield return relayOutput.StartCoroutine(SetupScrollbar());

		int fullLineCount = relayOutput.textInfo.lineCount;
		int fullCharCount = relayOutput.textInfo.characterCount;

		if (byChar) {
			relayOutput.maxVisibleLines = fullLineCount;
			for (int i = relayOutput.maxVisibleCharacters; i < fullCharCount; i++) {
				relayOutput.maxVisibleCharacters = i;
				SFXPlayer.instance.PlayMonitorTypeBlip();
				if (!instant) {
					yield return new WaitForSeconds(TRFDSPlugin.RelayTypingDelay.Value);
				}
			}
		} else {
			relayOutput.maxVisibleCharacters = fullCharCount;
			for (int i = relayOutput.maxVisibleLines; i < fullLineCount; i++) {
				relayOutput.maxVisibleLines = i;
				SFXPlayer.instance.PlayMonitorTypeBlip();
				if (!instant) {
					yield return new WaitForSeconds(TRFDSPlugin.RelayTypingDelay.Value);
				}
			}
		}

		yield return relayOutput.StartCoroutine(SetupScrollbar());
	}

	private static IEnumerator SetupScrollbar() {
		yield return null;

		float totalLineHeight = (relayOutput.textInfo.lineCount + 1) * lineHeight;
		float relativeMenuHeight = totalLineHeight / totalDisplayHeight;
		bool scrollToBottom = scrollbar.NormalizedScroll <= 0.01f;

		scrollbar.ConfigureHeight(relativeMenuHeight, false);
		scrollArea.Configure(scrollbar, lineHeight + totalLineHeight, totalDisplayHeight);

		if (scrollToBottom) {
			scrollbar.ForceScrollTo(0f);
		}
	}
}
