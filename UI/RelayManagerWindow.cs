using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TRFDS.MonoBehaviours;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using System.IO;
using System.Reflection;
using TRFDS.Helpers;

namespace TRFDS.UI;

[HarmonyPatch]
public class RelayManagerWindow : InfoWindow
{
	public static InfoDisplay infoDisplay;
	public static RelaySocket relaySocket;
	public static SongPlayer songPlayer;

	private static bool initRelayButton = false;
	private static bool initInfoDisplay = false;
	private static bool loadEnabledChannels = false;
	private static bool reinitDict = false;
	private static int currCallsignBase8;
	private static int currChannelIndex = 0;
	private static List<(int id, string name)> enabledChannels = new List<(int, string)>() { (0, "Default channel") };

	private static GameObject relayManagerViewport;
	private static GameObject relayInputViewport;
	private static InfoWindow relayManagerWindow;
	private static SimpleWriter callsignInput;
	private static TextMeshPro relayInput;
	private static GameObject switchToInputButton;
	private static TextMeshPro newSignalIdInput;
	private static TextMeshPro newSignalNameInput;
	private static TextMeshPro messageSelectorInput;
	private static GameObject playMessageSongButton;
	private static TextMeshPro playMessageSongText;
	private static TextMeshPro messageTimeText;
	private static TextMeshPro currentChannelLabel;

	private static CalculatorWindow calculatorWindow;
	private static PopupBox popupBox;
	private static Autosaver autosaver;
	private static DictionaryWindow dictionaryWindow;
	private static PuzzleCounter puzzleCounter;
	private static Oscilloscope playerInputOscilloscope;
	private static VisualWindow visualWindow;

	[HarmonyPatch(typeof(TabsWindow), "Open")]
	[HarmonyPostfix]
	public static void MakeRelayButton(TabsWindow __instance) {
		if (initRelayButton) {
			return;
		}

		var songPlayerObj = new GameObject("Song Player");
		songPlayerObj.AddComponent<SongPlayer>();
		songPlayer = songPlayerObj.GetComponent<SongPlayer>();

		initRelayButton = true;

		var ideasButton = __instance.tabsList.transform.Find("Ideas Tab");

		var relayButton = GameObject.Instantiate(ideasButton, ideasButton.transform.position with { y = 4.46f }, Quaternion.identity);
		relayButton.name = "Relay Tab";

		relayButton.transform.SetParent(ideasButton.transform.parent);

		var text = relayButton.GetComponentInChildren<TextMeshPro>();
		text.text = "Relay";

		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TRFDS.Assets.relay.png");
		byte[] textureData = new byte[stream.Length];
		stream.Read(textureData, 0, textureData.Length);
		Texture2D texture = new Texture2D(2, 2);
		texture.LoadImage(textureData);
		relayButton.transform.Find("Icon").GetComponent<MeshRenderer>().material.mainTexture = texture;
		
		var button = relayButton.GetComponent<Button3D>();
		button.OnUseButton = new UnityEngine.Events.UnityEvent();
		button.OnUseButton.AddListener(() => infoDisplay.SwitchWindow(relayManagerWindow));
	}

	[HarmonyPatch(typeof(InfoDisplay), "Start")]
	[HarmonyPostfix]
	public static void MakeRelayManagerScreen(InfoDisplay __instance) {
		if (initInfoDisplay) {
			return;
		}

		initInfoDisplay = true;

		infoDisplay = __instance;

		var calendarWindow = UnityHelpers.FindSingleInstanceObject<CalendarWindow>();
		var relayWindowObj = GameObject.Instantiate(calendarWindow).gameObject;
		relayWindowObj.name = "Relay Manager Window";
		GameObject.DestroyImmediate(relayWindowObj.GetComponent<CalendarWindow>());

		relayWindowObj.SetActive(true);

		foreach (var child in relayWindowObj.GetComponentsInChildren<Transform>(true)) {
			if (child.name == "Mission Time" || child.name == "Running Time") {
				GameObject.DestroyImmediate(child.gameObject);
				continue;
			}

			child.gameObject.SetActive(true);
		}

		// Adding at end so Start runs after we destroy Calendar stuff, otherwise it crashes on startup
		relayManagerWindow = relayWindowObj.AddComponent<RelayManagerWindow>();
	}

	private void Start() {
		relayManagerViewport = transform.GetChild(0).gameObject;
		relayManagerViewport.name = "Relay Manager Viewport";
		relayManagerViewport.SetActive(false);

		relayInputViewport = GameObject.Instantiate(relayManagerViewport.gameObject);
		relayInputViewport.transform.SetParent(transform);
		relayInputViewport.transform.position = relayManagerViewport.transform.position;
		relayInputViewport.name = "Relay Input Viewport";
		relayInputViewport.SetActive(false);

		InitManagerViewport();
		InitInputViewport();

		autosaver = UnityHelpers.FindSingleInstanceObject<Autosaver>();
		dictionaryWindow = UnityHelpers.FindSingleInstanceObject<DictionaryWindow>();
		puzzleCounter = UnityHelpers.FindSingleInstanceObject<PuzzleCounter>();
		playerInputOscilloscope = GameObject.Find("Input Oscilloscope").GetComponent<Oscilloscope>();

		RelaySocket.CallsignProcessed.AddListener((goodCallsign) => {
			if (goodCallsign) {
				GoodCallsign();
			} else {
				BadCallsign();
			}
		});

		SongPlayer.OnSongFinished.AddListener(() => {
			playMessageSongText.text = "Play";
			messageTimeText.text = "";
		});
	}

	private void InitManagerViewport() {
		relayManagerViewport.GetComponentInChildren<TextMeshPro>().text = "Relay Manager"; // Rename

		calculatorWindow = GameObject.Find("Calculator Window").GetComponent<CalculatorWindow>();
		popupBox = UnityHelpers.FindSingleInstanceObject<PopupBox>("Idea Popup");
		visualWindow = UnityHelpers.FindSingleInstanceObject<VisualWindow>();

		var callsignLabel = GameObject.Instantiate(calculatorWindow.outputLabel.gameObject);
		callsignLabel.name = "Callsign Label";
		callsignLabel.transform.SetParent(relayManagerViewport.transform);
		var rectTransform = callsignLabel.GetComponent<RectTransform>();
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.position = new Vector3(-30.3f, 5.45f, 0f);
		var text = callsignLabel.GetComponent<TextMeshPro>();
		text.text = "Callsign (base 8):";
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.overflowMode = TextOverflowModes.Overflow;

		var inputField = GameObject.Instantiate(calculatorWindow.operand1.gameObject);
		inputField.name = "Callsign Input";
		inputField.transform.SetParent(relayManagerViewport.transform);
		inputField.transform.position = new Vector3(-29.7f, 5.5f, 0.22f);

		var inputFieldBackground = inputField.GetComponent<MeshFilter>().mesh;
		UnityHelpers.ScaleMeshVertices(inputFieldBackground, 0.2f, 0.6f);

		callsignInput = inputField.GetComponent<SimpleWriter>();
		callsignInput.label.transform.position = new Vector3(-29.1f, 5.46f, 0.2141f);

		string randomCallsign = "";
		for (int i = 0; i < 4; i++) {
			randomCallsign += Random.RandomRangeInt(0, 8);
		}
		callsignInput.label.text = randomCallsign;

		var calculatorViewport = calculatorWindow.viewport;
		var exampleButton = calculatorViewport.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "Round");
		var confirmButton = GameObject.Instantiate(exampleButton);
		confirmButton.name = "Confirm Callsign Button";
		confirmButton.transform.SetParent(relayManagerViewport.transform);
		confirmButton.transform.position = new Vector3(-29.3f, 5.5f, 0.216f);
		confirmButton.GetComponent<Button3D>().OnUseButton = new UnityEngine.Events.UnityEvent();
		confirmButton.GetComponent<Button3D>().OnUseButton.AddListener(SetCallsign);
		confirmButton.GetComponentInChildren<TextMeshPro>().text = "Set";

		var respondButton = ConsoleDisplay.Instance.readMessageGroup.transform.Find("Respond Button");
		switchToInputButton = GameObject.Instantiate(respondButton).gameObject;
		switchToInputButton.name = "Relay Input Button";
		switchToInputButton.transform.SetParent(relayManagerViewport.transform);
		switchToInputButton.transform.position = new Vector3(-29.38f, 5.675f, 0.25f);
		switchToInputButton.GetComponent<Button3D>().OnUseButton = new UnityEngine.Events.UnityEvent();
		switchToInputButton.GetComponent<Button3D>().OnUseButton.AddListener(SwitchToInput);
		switchToInputButton.GetComponentInChildren<TextMeshPro>().text = "Input";
		GameObject.Destroy(switchToInputButton.transform.Find("Icon").gameObject);
		UnityHelpers.ScaleMeshVertices(switchToInputButton.GetComponent<MeshFilter>().mesh, 0.6f);
		switchToInputButton.GetComponent<BoxCollider>().size -= Vector3.right * 0.4f;
		switchToInputButton.gameObject.SetActive(false);

		var switchToInputText = switchToInputButton.transform.Find("Text");
		switchToInputText.position += Vector3.right * 0.0325f;

		var newSignalsLabel = GameObject.Instantiate(calculatorWindow.outputLabel.gameObject);
		newSignalsLabel.name = "New Signals Label";
		newSignalsLabel.transform.SetParent(relayManagerViewport.transform);
		rectTransform = newSignalsLabel.GetComponent<RectTransform>();
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.position = new Vector3(-30.3f, 5.25f, 0f);
		text = newSignalsLabel.GetComponent<TextMeshPro>();
		text.text = "Add new signals:";
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.overflowMode = TextOverflowModes.Overflow;

		var signalIdLabel = GameObject.Instantiate(calculatorWindow.outputLabel.gameObject);
		signalIdLabel.name = "Signal ID Label";
		signalIdLabel.transform.SetParent(relayManagerViewport.transform);
		rectTransform = signalIdLabel.GetComponent<RectTransform>();
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.position = new Vector3(-30.3f, 5.15f, 0f);
		text = signalIdLabel.GetComponent<TextMeshPro>();
		text.text = "ID";
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.overflowMode = TextOverflowModes.Overflow;

		var signalNameLabel = GameObject.Instantiate(calculatorWindow.outputLabel.gameObject);
		signalNameLabel.name = "Signal Name Label";
		signalNameLabel.transform.SetParent(relayManagerViewport.transform);
		rectTransform = signalNameLabel.GetComponent<RectTransform>();
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.position = new Vector3(-29.63f, 5.15f, 0f);
		text = signalNameLabel.GetComponent<TextMeshPro>();
		text.text = "Name";
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.overflowMode = TextOverflowModes.Overflow;

		var exampleDictEntry = calculatorWindow.viewport.GetComponentsInChildren<SimpleWriter>(true)
			.First(x => x.name == "Name Input");
		
		var newSignalId = GameObject.Instantiate(calculatorWindow.operand1.gameObject);
		newSignalId.name = "New Signal ID Input";
		newSignalId.transform.SetParent(relayManagerViewport.transform);
		newSignalId.transform.position = new Vector3(-30.755f, 5.1f, 0.22f);
		UnityHelpers.ScaleMeshVertices(newSignalId.GetComponent<MeshFilter>().mesh, 0.3f, 0.6f);
		newSignalId.GetComponent<BoxCollider>().size -= Vector3.right * 0.4f;

		newSignalIdInput = newSignalId.GetComponentInChildren<TextMeshPro>();
		newSignalIdInput.text = "-";
		newSignalIdInput.transform.localPosition = new Vector3(0.3503f, -0.2237f, -0.59f);

		var newSignalName = GameObject.Instantiate(calculatorWindow.operand1.gameObject);
		newSignalName.name = "New Signal Name Input";
		newSignalName.transform.SetParent(relayManagerViewport.transform);
		newSignalName.transform.position = new Vector3(-29.95f, 5.1f, 0.22f);
		UnityHelpers.ScaleMeshVertices(newSignalName.GetComponent<MeshFilter>().mesh, 0.5f, 0.6f);
		newSignalName.GetComponent<BoxCollider>().size -= Vector3.right * 0.4f;
		newSignalName.transform.GetComponent<SimpleWriter>().dummyType = InputDummyType.TermName;

		newSignalNameInput = newSignalName.GetComponentInChildren<TextMeshPro>();
		newSignalNameInput.text = "";
		newSignalNameInput.transform.localPosition = new Vector3(0.25f, -0.2237f, -0.59f);

		var makeNewSignalButton = GameObject.Instantiate(exampleButton);
		makeNewSignalButton.name = "Make New Dict Entry Button";
		makeNewSignalButton.transform.SetParent(relayManagerViewport.transform);
		makeNewSignalButton.transform.position = new Vector3(-29.3f, 5.1f, 0.216f);
		makeNewSignalButton.GetComponent<Button3D>().OnUseButton = new UnityEngine.Events.UnityEvent();
		makeNewSignalButton.GetComponent<Button3D>().OnUseButton.AddListener(CreateNewDictEntry);
		makeNewSignalButton.GetComponentInChildren<TextMeshPro>().text = "Make";

		var messageSelectorLabel = GameObject.Instantiate(calculatorWindow.outputLabel.gameObject);
		messageSelectorLabel.name = "Message Select Label";
		messageSelectorLabel.transform.SetParent(relayManagerViewport.transform);
		rectTransform = messageSelectorLabel.GetComponent<RectTransform>();
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.position = new Vector3(-30.3f, 4.83f, 0f);
		text = messageSelectorLabel.GetComponent<TextMeshPro>();
		text.text = "Message Actions:";
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.overflowMode = TextOverflowModes.Overflow;

		var messageSelectorInputObj = GameObject.Instantiate(calculatorWindow.operand1.gameObject);
		messageSelectorInputObj.name = "Relay Message Selector Input";
		messageSelectorInputObj.transform.SetParent(relayManagerViewport.transform);
		messageSelectorInputObj.transform.position = new Vector3(-30.83f, 4.77f, 0.22f);
		UnityHelpers.ScaleMeshVertices(messageSelectorInputObj.GetComponent<MeshFilter>().mesh, 0.2f, 0.6f);
		messageSelectorInputObj.GetComponent<BoxCollider>().size -= Vector3.right * 0.8f;

		messageSelectorInput = messageSelectorInputObj.GetComponentInChildren<TextMeshPro>();
		messageSelectorInput.text = "";
		messageSelectorInput.transform.position = new Vector3(-30.2f, 4.73f, 0.2141f);
		
		var viewMessageVisualButton = GameObject.Instantiate(exampleButton);
		viewMessageVisualButton.name = "View Relay Message Visual Button";
		viewMessageVisualButton.transform.SetParent(relayManagerViewport.transform);
		viewMessageVisualButton.transform.position = new Vector3(-30.4f, 4.77f, 0.216f);
		viewMessageVisualButton.GetComponent<Button3D>().OnUseButton = new UnityEngine.Events.UnityEvent();
		viewMessageVisualButton.GetComponent<Button3D>().OnUseButton.AddListener(ViewVisual);
		viewMessageVisualButton.GetComponentInChildren<TextMeshPro>().text = "View";

		playMessageSongButton = GameObject.Instantiate(exampleButton).gameObject;
		playMessageSongButton.name = "Play Relay Message Song Button";
		playMessageSongButton.transform.SetParent(relayManagerViewport.transform);
		playMessageSongButton.transform.position = new Vector3(-30f, 4.77f, 0.216f);
		playMessageSongButton.GetComponent<Button3D>().OnUseButton = new UnityEngine.Events.UnityEvent();
		playMessageSongButton.GetComponent<Button3D>().OnUseButton.AddListener(PlayStopSong);

		playMessageSongText = playMessageSongButton.GetComponentInChildren<TextMeshPro>();
		playMessageSongText.text = "Play";

		var songDurationLabel = GameObject.Instantiate(calculatorWindow.outputLabel.gameObject);
		songDurationLabel.name = "Song Duration Label";
		songDurationLabel.transform.SetParent(relayManagerViewport.transform);
		rectTransform = songDurationLabel.GetComponent<RectTransform>();
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.position = new Vector3(-29.1f, 4.71f, 0f);
		messageTimeText = songDurationLabel.GetComponent<TextMeshPro>();
		messageTimeText.text = "";
		messageTimeText.textWrappingMode = TextWrappingModes.NoWrap;
		messageTimeText.overflowMode = TextOverflowModes.Overflow;

		var changeChannelLabel = GameObject.Instantiate(calculatorWindow.outputLabel.gameObject);
		changeChannelLabel.name = "Change Channel Label";
		changeChannelLabel.transform.SetParent(relayManagerViewport.transform);
		rectTransform = changeChannelLabel.GetComponent<RectTransform>();
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.position = new Vector3(-30.3f, 4.52f, 0f);
		text = changeChannelLabel.GetComponent<TextMeshPro>();
		text.text = "Select channel:";
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.overflowMode = TextOverflowModes.Overflow;

		var channelLeftButton = GameObject.Instantiate(exampleButton);
		channelLeftButton.name = "Cycle Channel Left Button";
		channelLeftButton.transform.SetParent(relayManagerViewport.transform);
		channelLeftButton.transform.position = new Vector3(-30.89f, 4.45f, 0.216f);
		channelLeftButton.GetComponent<Button3D>().OnUseButton = new UnityEngine.Events.UnityEvent();
		channelLeftButton.GetComponent<Button3D>().OnUseButton.AddListener(() => CycleChannel(-1));
		channelLeftButton.GetComponentInChildren<TextMeshPro>().text = "<";
		UnityHelpers.ScaleMeshVertices(channelLeftButton.GetComponent<MeshFilter>().mesh, 0.5f);
		channelLeftButton.GetComponent<BoxCollider>().size -= Vector3.right * 0.5f;

		var channelRightButton = GameObject.Instantiate(exampleButton);
		channelRightButton.name = "Cycle Channel Right Button";
		channelRightButton.transform.SetParent(relayManagerViewport.transform);
		channelRightButton.transform.position = new Vector3(-29.58f, 4.45f, 0.216f);
		channelRightButton.GetComponent<Button3D>().OnUseButton = new UnityEngine.Events.UnityEvent();
		channelRightButton.GetComponent<Button3D>().OnUseButton.AddListener(() => CycleChannel(1));
		channelRightButton.GetComponentInChildren<TextMeshPro>().text = ">";
		UnityHelpers.ScaleMeshVertices(channelRightButton.GetComponent<MeshFilter>().mesh, 0.5f);
		channelRightButton.GetComponent<BoxCollider>().size -= Vector3.right * 0.5f;

		var currentChannelLabelObj = GameObject.Instantiate(calculatorWindow.outputLabel.gameObject);
		currentChannelLabelObj.name = "Current Channel Label";
		currentChannelLabelObj.transform.SetParent(relayManagerViewport.transform);
		rectTransform = currentChannelLabelObj.GetComponent<RectTransform>();
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.position = new Vector3(-30.235f, 4.445f, 0f);
		currentChannelLabel = currentChannelLabelObj.GetComponent<TextMeshPro>();
		currentChannelLabel.text = enabledChannels[currChannelIndex].name;
		currentChannelLabel.textWrappingMode = TextWrappingModes.NoWrap;
		currentChannelLabel.overflowMode = TextOverflowModes.Overflow;
		currentChannelLabel.alignment = TextAlignmentOptions.Center;
	}

	private void InitInputViewport() {
		relayInputViewport.GetComponentInChildren<TextMeshPro>().text = "Relay Input";

		var inputDisplay = UnityHelpers.FindSingleInstanceObject<InputDisplaySelector>();

		var relayInputObj = GameObject.Instantiate(inputDisplay);
		relayInputObj.transform.SetParent(relayInputViewport.transform);
		relayInputObj.transform.position = new Vector3(-30.1f, 5.55f, 0.22f);
		relayInputObj.name = "Relay Input";
		relayInput = relayInputObj.GetComponent<TextMeshPro>();
		relayInput.fontSize = 0.9f;
		relayInputObj.GetComponent<InputDisplaySelector>().tInput = relayInput;

		var respondButton = ConsoleDisplay.Instance.readMessageGroup.transform.Find("Respond Button");
		var sendMessageButton = GameObject.Instantiate(respondButton);
		sendMessageButton.name = "Relay Send Message Button";
		sendMessageButton.transform.SetParent(relayInputViewport.transform);
		sendMessageButton.transform.position = new Vector3(-29.34f, 5.675f, 0.25f);
		sendMessageButton.GetComponentInChildren<TextMeshPro>().text = "Send";
		sendMessageButton.GetComponentInChildren<Button3D>().OnUseButton = new UnityEvent();
		sendMessageButton.GetComponentInChildren<Button3D>().OnUseButton.AddListener(SendMessage);
		GameObject.Destroy(sendMessageButton.transform.Find("Icon").gameObject);
		UnityHelpers.ScaleMeshVertices(sendMessageButton.GetComponent<MeshFilter>().mesh, 0.5f);
		sendMessageButton.GetComponent<BoxCollider>().size -= Vector3.right * 0.5f;

		var sendMessageText = sendMessageButton.transform.Find("Text");
		sendMessageText.position += Vector3.right * 0.0325f;

		var managerButton = GameObject.Instantiate(respondButton);
		managerButton.name = "Relay Manager Button";
		managerButton.transform.SetParent(relayInputViewport.transform);
		managerButton.transform.position = new Vector3(-29.73f, 5.675f, 0.25f);
		managerButton.GetComponentInChildren<TextMeshPro>().text = "Manager";
		managerButton.GetComponentInChildren<Button3D>().OnUseButton = new UnityEvent();
		managerButton.GetComponentInChildren<Button3D>().OnUseButton.AddListener(SwitchToManager);
		GameObject.Destroy(managerButton.transform.Find("Icon").gameObject);
		UnityHelpers.ScaleMeshVertices(managerButton.GetComponent<MeshFilter>().mesh, 0.8f);
		managerButton.GetComponent<BoxCollider>().size -= Vector3.right * 0.2f;

		var managerText = managerButton.transform.Find("Text");
		managerText.position += Vector3.right * 0.0355f;
	}


	[HarmonyPatch(typeof(WorldSpaceClicker), "ScanForHitbox")]
	[HarmonyPostfix]
	public static void HandleInput(WorldSpaceClicker __instance) {
		// Clicking the text box wasn't working and I couldn't figure out why, so we just manually enable it
		// Horrendous approach, please do not copy this
		if (__instance.mainClickDown && relayInputViewport.gameObject.activeSelf && !__instance.InRightSide) {
			relayInput.GetComponent<InputDisplaySelector>().OnRelease(__instance);
		}
	}

	[HarmonyPatch(typeof(SimpleWriter), nameof(SimpleWriter.BecomeWritingTarget))]
	[HarmonyPrefix]
	public static void FuckUnfuckFloatTextDummy(SimpleWriter __instance) {
		if (__instance == callsignInput) {
			TextDummyManager.Instance_Float.InputField.inputValidator = ScriptableObject.CreateInstance<CallsignInputValidator>();
			return;
		} else if (TextDummyManager.Instance_Float.InputField.inputValidator is not FloatInputValidator) {
			TextDummyManager.Instance_Float.InputField.inputValidator = ScriptableObject.CreateInstance<FloatInputValidator>();
		}
	}

	public override void Open() {
		if (!loadEnabledChannels) {
			LoadEnabledChannels();
			loadEnabledChannels = true;
		}

		if (UserDictionary.Instance.terms.ContainsKey(-577)) {
			ShowSongStuff();
		} else {
			HideSongStuff();
		}

		if (RelaySocket.Callsign == null) {
			relayManagerViewport.SetActive(true);
		}
		else {
			relayInputViewport.SetActive(true);
		}
		RelayWindow.TryShow();
	}

	public override void Close() {
		relayManagerViewport.SetActive(false);
		relayInputViewport.SetActive(false);
	}

	private void SetCallsign() {
		var callsign = callsignInput.label.text.Where(c => (c - '0') <= 9 && (c - '0') >= 0).Join(delimiter: "");

		if (callsign.Length != 4) {
			popupBox.PopupWithLabel("Callsign length must be 4");
			return;
		}

		var callsignBase8 = int.Parse(callsign);
		var callsignBase10 = calculatorWindow.EuclideanBaseChange(callsignBase8, 8, 10);

		if (relaySocket == null) {
			RelaySocket.Callsign = callsignBase10;
			currCallsignBase8 = callsignBase8;

			var socketObj = new GameObject("Relay Socket");
			socketObj.AddComponent<RelaySocket>();
			relaySocket = socketObj.GetComponent<RelaySocket>();

			return;
		}

		RelaySocket.Callsign = callsignBase10;
		RelaySocket.UpdateCallsign = true;
	}

	public static void GoodCallsign() {
		SwitchToInput();
		switchToInputButton.SetActive(true);
		popupBox.PopupWithLabel("Callsign set to " + callsignInput.label.text);
		puzzleCounter.StartCoroutine(puzzleCounter.UpdateCounterRoutine(currCallsignBase8, 0f));
	}

	public static void BadCallsign() {
		popupBox.PopupWithLabel("Callsign already in use");
	}

	public static void Disconnect() {
		RelayManagerWindow.infoDisplay.SwitchWindow(RelayManagerWindow.infoDisplay.tabsWindow);

		if (RelayManagerWindow.relaySocket == null) {
			return;
		}

		puzzleCounter.StartCoroutine(puzzleCounter.UpdateCounterRoutine(PuzzleManager.Instance.TotalPuzzleID + 1, 0f));

		switchToInputButton.SetActive(false);
		RelayManagerWindow.relaySocket.Disconnect();
		GameObject.Destroy(RelayManagerWindow.relaySocket);
		RelayManagerWindow.relaySocket = null;
		RelaySocket.Callsign = null;
	}

	private static void SendMessage() {
		var message = TextDummyManager.Instance_PuzzleInput.currText.ToUpper();
		var compilerResult = new CompilerResult();
		var signalMessage = ConsoleDisplay.Instance.compiler.CompileStringToSignal(message, ref compilerResult);
		if (compilerResult.compilerResultTag == CompilerResultTag.ERROR) {
			var errorStrings = compilerResult.errorMsg.Split(Environment.NewLine);
			var errorType = errorStrings[1];

			if (errorType == "Null Input") {
				popupBox.PopupWithLabel("Err: null input");
			} else if (errorType.Contains("Entry not found")) {
				string unknownEntry = errorStrings[2];
				popupBox.PopupWithLabel($"Err: {unknownEntry} not found");
			} else {
				popupBox.PopupWithLabel("Err: Unknown error type");
			}

			return;
		}

		if (signalMessage.signals[0] == -65534) {
			if (signalMessage.signals.Length > 2 || signalMessage.signals.ElementAtOrDefault(1) >= 0) {
				popupBox.PopupWithLabel($"Usage: {UserDictionary.Instance.GetWordFromInt(-65534)} <signal>");
				return;
			}

			if (enabledChannels.Any(x => x.id == signalMessage.signals[1])) {
				popupBox.PopupWithLabel("Already in channel: " + UserDictionary.Instance.GetWordFromInt(signalMessage.signals[1]));
				return;
			}

			enabledChannels.Add((signalMessage.signals[1], UserDictionary.Instance.GetWordFromInt(signalMessage.signals[1])));
			currChannelIndex = enabledChannels.Count - 1;
			currentChannelLabel.text = enabledChannels[currChannelIndex].name;
			RelayWindow.SetActiveChannel(enabledChannels[currChannelIndex].id);
			popupBox.PopupWithLabel("Joined channel: " + enabledChannels[currChannelIndex].name);
			SaveEnabledChannels();
			TextDummyManager.Instance_PuzzleInput.Clear();
			return;
		}

		if (signalMessage.signals[0] == -65533) {
			if (signalMessage.signals.Length > 2 || signalMessage.signals.ElementAtOrDefault(1) >= 0) {
				popupBox.PopupWithLabel($"Usage: {UserDictionary.Instance.GetWordFromInt(-65533)} <signal>");
				return;
			}

			int enabledChannelIndex = enabledChannels.FindIndex(0, x => x.id == signalMessage.signals[1]);
			if (enabledChannelIndex < 0) {
				popupBox.PopupWithLabel("Not in channel: " + UserDictionary.Instance.GetWordFromInt(signalMessage.signals[1]));
				return;
			}

			string name = enabledChannels[enabledChannelIndex].name;
			enabledChannels.RemoveAt(enabledChannelIndex);
			currChannelIndex = 0;
			currentChannelLabel.text = enabledChannels[currChannelIndex].name;
			RelayWindow.SetActiveChannel(enabledChannels[currChannelIndex].id);
			popupBox.PopupWithLabel("Left channel: " + name);
			SaveEnabledChannels();
			TextDummyManager.Instance_PuzzleInput.Clear();
			return;
		}

		int activeChannelId = enabledChannels[currChannelIndex].id;
		if (activeChannelId != 0 && activeChannelId != -65536) {
			signalMessage.signals = new int[]{-65535, activeChannelId}.Concat(signalMessage.signals).ToArray();
		}

		var signals = signalMessage.signals.Join(delimiter: ",");
		var compiledMessage = "M," + signals;

		RelaySocket.QueueSend(compiledMessage);

		TextDummyManager.Instance_PuzzleInput.Clear();

		if (TRFDSPlugin.Oscilloscopes.Value) {
			playerInputOscilloscope.PlaySignal(signalMessage, false);
		}
	}

	private static void CreateNewDictEntry() {
		if (newSignalIdInput.text.Contains('.')) {
			popupBox.PopupWithLabel("Signal must be an integer");
			return;
		}

		if (newSignalIdInput.text[0] != '-') {
			popupBox.PopupWithLabel("Signal must be negative");
			return;
		}

		string signalIdText = newSignalIdInput.text
			.Where(c => Char.IsDigit(c))
			.Join(delimiter: "");

		int signalId = Convert.ToInt32(signalIdText) * -1; // Char.IsDigit strips the '-', so we just add it back in, duh!
		if (UserDictionary.Instance.terms.ContainsKey(signalId)) {
			popupBox.PopupWithLabel("Duplicate signal: " + signalId);
			return;
		}

		string signalName = newSignalNameInput.text.Trim(' ').Trim('\u200b');
		if (UserDictionary.Instance.keys.ContainsKey(signalName)) {
			popupBox.PopupWithLabel("Duplicate name: " + signalName);
			return;
		}

		bool success = UserDictionary.Instance.AddEntry(signalName, signalId);
		if (success) {
			dictionaryWindow.UpdateToAddEntry(signalId, signalName, "?");
			popupBox.PopupWithLabel("Added signal");
			newSignalIdInput.text = "-";
			newSignalNameInput.text = "";
			autosaver.Autosave(PuzzleManager.Instance);

			if (signalId == -577) {
				ShowSongStuff();
			}
		} else {
			// Should never happen, but just in case, let the user know it didn't work
			popupBox.PopupWithLabel("Failed to add signal");
		}
	}

	private void ViewVisual() {
		if (RelayWindow.receivedMessages.Count <= 0) {
			popupBox.PopupWithLabel("No messages to try select :(");
			return;
		}

		string selectedMessageText = messageSelectorInput.text.Trim(' ').Trim('\u200b');
		if (!short.TryParse(selectedMessageText, out var selectedMessageId)) {
			popupBox.PopupWithLabel("Message ID must be an integer");
			return;
		}

		var index = RelayWindow.activeChannel.FindIndex(0, x => x.TransmissionId == selectedMessageId);
		if (index < 0) {
			popupBox.PopupWithLabel("Message " + selectedMessageId + " not in active channel");
			return;
		}

		var selectedMessage = RelayWindow.activeChannel[index];
		var dummyPuzzle = ScriptableObject.CreateInstance<Puzzle>();
		dummyPuzzle.rockOutput = selectedMessage.Signals;
		
		// Visual Window expects validVisuals to be set to the index of the 'Visual'/'Graph'/'Image' signal
		bool foundVisual = false;
		var signals = dummyPuzzle.rockOutput.signals;
		for (int i = 0; i < signals.Length; i++) {
			if (signals[i] == -53 && i < signals.Length -1 && signals[i + 1] == -14) {
				foundVisual = true;
				visualWindow.validVisuals = new List<int>();
				visualWindow.validVisuals.Add(i);
				break;
			}
		}

		if (!foundVisual) {
			popupBox.PopupWithLabel("Message " + selectedMessageId + " has no visual data");
			return;
		}

		visualWindow.QueueDraw(dummyPuzzle);
		visualWindow.currDrawPuzzle = dummyPuzzle;
		visualWindow.visualDetectedPopup.Popup();
		RelayManagerWindow.infoDisplay.SwitchWindow(RelayManagerWindow.infoDisplay.visualWindow);
	}

	private void PlayStopSong() {
		if (SongPlayer.IsPlaying) {
			songPlayer.StopSong();
			playMessageSongText.text = "Play";
			messageTimeText.text = "";
			return;
		}

		if (RelayWindow.receivedMessages.Count <= 0) {
			popupBox.PopupWithLabel("No messages to try select :(");
			return;
		}

		string selectedMessageText = messageSelectorInput.text.Trim(' ').Trim('\u200b');
		if (!short.TryParse(selectedMessageText, out var selectedMessageId)) {
			popupBox.PopupWithLabel("Message ID must be an integer");
			return;
		}

		var index = RelayWindow.activeChannel.FindIndex(0, x => x.TransmissionId == selectedMessageId);
		if (index < 0) {
			popupBox.PopupWithLabel("Message " + selectedMessageId + " not in active channel");
			return;
		}

		var selectedMessage = RelayWindow.activeChannel[index];
		if (!songPlayer.TryPlaySong(selectedMessage.Signals)) {
			popupBox.PopupWithLabel("Failed to parse song");
			return;
		}
		
		playMessageSongText.text = "Stop";
	}

	public static void SetSongDurationLabel(string durationLabel) {
		messageTimeText.text = durationLabel;
	}

	private static void HideSongStuff() {
		playMessageSongButton.SetActive(false);
	}

	private static void ShowSongStuff() {
		playMessageSongButton.SetActive(true);
	}

	private static void CycleChannel(int dir) {
		currChannelIndex += dir;
		if (currChannelIndex >= enabledChannels.Count) {
			currChannelIndex = 0;
		}
		if (currChannelIndex < 0) {
			currChannelIndex = enabledChannels.Count - 1;
		}

		currentChannelLabel.text = enabledChannels[currChannelIndex].name;
		RelayWindow.SetActiveChannel(enabledChannels[currChannelIndex].id);
	}

	private static void SaveEnabledChannels() {
		var tmfrsDataDir = Path.Join(Application.persistentDataPath, $"/{nameof(TRFDS)}");
		if (!Directory.Exists(tmfrsDataDir)) {
			Directory.CreateDirectory(tmfrsDataDir);
		}

		var enabledChannelsFile = Path.Join(tmfrsDataDir, "enabledChannels.save");
		var enabledChannelsText = enabledChannels.Select(x => x.id).Where(x => x != 0).Join(delimiter: ",");
		File.WriteAllText(enabledChannelsFile, enabledChannelsText);
	}

	private static void LoadEnabledChannels() {
		var savedChannelsFile = Path.Join(Application.persistentDataPath, $"/{nameof(TRFDS)}/enabledChannels.save");
		if (!File.Exists(savedChannelsFile)) {
			return;
		}

		var savedChannelsText = File.ReadAllText(savedChannelsFile);
		var savedChannels = savedChannelsText.Split(',').Select(x => int.Parse(x)).ToArray();
		foreach (var savedChannel in savedChannels) {
			enabledChannels.Add((savedChannel, UserDictionary.Instance.GetWordFromInt(savedChannel)));
		}
	}

	private static void SwitchToManager() {
		relayManagerViewport.SetActive(true);
		relayInputViewport.SetActive(false);
	}

	private static void SwitchToInput() {
		relayManagerViewport.SetActive(false);
		relayInputViewport.SetActive(true);
	}
}
