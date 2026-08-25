using System.Linq;
using HarmonyLib;
using TMFRS.MonoBehaviours;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TMFRS.UI;

[HarmonyPatch]
public class RelayManagerWindow : InfoWindow
{
	public static InfoDisplay infoDisplay;
	public static RelaySocket relaySocket;

	private static InfoWindow relayManagerWindow;
	private static SimpleWriter callsignInput;
	private static TextMeshPro relayInput;
	public static GameObject relayManagerViewport;
	public static GameObject relayInputViewport;
	private static bool initRelayButton = false;
	private static bool initInfoDisplay = false;

	private static CalculatorWindow calculatorWindow;
	private static PopupBox popupBox;
	private static GameObject switchToInputButton;


	[HarmonyPatch(typeof(TabsWindow), "Open")]
	[HarmonyPostfix]
	public static void MakeRelayButton(TabsWindow __instance) {
		if (initRelayButton) {
			return;
		}

		initRelayButton = true;

		var ideasButton = __instance.tabsList.transform.Find("Ideas Tab");

		var relayButton = GameObject.Instantiate(ideasButton, ideasButton.transform.position with { y = 4.46f }, Quaternion.identity);
		relayButton.name = "Relay Tab";

		// TODO: What is this even doing? why are we modifying localPos?
		var y = relayButton.transform.localPosition.y;
		y = -0.49f;

		relayButton.transform.SetParent(ideasButton.transform.parent);

		var text = relayButton.GetComponentInChildren<TextMeshPro>();
		text.text = "Relay";

		// TODO: Custom icon? Not sure how feasible this is...
		
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
		InitRightMonitorTopBar();
	}

	private void InitManagerViewport() {
		relayManagerViewport.GetComponentInChildren<TextMeshPro>().text = "Relay Manager"; // Rename

		calculatorWindow = GameObject.Find("Calculator Window").GetComponent<CalculatorWindow>();
		popupBox = UnityHelpers.FindSingleInstanceObject<PopupBox>("Idea Popup");

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

		// TODO: Callsign repurposes the float input field, which has maxLength = 36 and allows invalid chars like 8, 9, .
		// Need to update it somehow, patching would work but is clunky, not sure how to create a custom input field... requires thought
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

		var respondButton = ConsoleDisplay.instance.readMessageGroup.transform.Find("Respond Button");
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

		var respondButton = ConsoleDisplay.instance.readMessageGroup.transform.Find("Respond Button");
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

	// TODO: Should this even be here..?
	private void InitRightMonitorTopBar() {
		var topBar = GameObject.Instantiate(relayManagerViewport.transform.Find("Top Tab"));
		topBar.name = "Top Tab Right";
		topBar.GetComponentInChildren<TextMeshPro>().text = "Relay";
		GameObject.Destroy(topBar.GetComponentInChildren<Button3D>().gameObject); // TODO: Probably has many more objects attached that go unused, they need to be removed

		// TODO: Recompile button
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

	public override void Open() {
		// TODO: Opening relay from RESPOND window bugs out
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

	// TODO: Update callsign if asked
	private void SetCallsign() {
		var callsign = callsignInput.label.text
			.Where(c => (c - '0') <= 9 && (c - '0') >= 0)
			.Join(delimiter: "");

		if (callsign.Length != 4) {
			popupBox.PopupWithLabel("Callsign length must be 4");
			return;
		}
		
		if (callsign.Any(c => c == '8' || c == '9')) {
			popupBox.PopupWithLabel("Callsign must be in base 8");
			return;
		}

		var callsignBase8 = int.Parse(callsign);
		var callsignBase10 = calculatorWindow.EuclideanBaseChange(callsignBase8, 8, 10);

		if (relaySocket == null) {
			RelaySocket.Callsign = callsignBase10;
			var socketObj = new GameObject("Relay Socket");
			socketObj.AddComponent<RelaySocket>();
			relaySocket = socketObj.GetComponent<RelaySocket>();
			return;
		}

		RelaySocket.Callsign = callsignBase10;
		RelaySocket.UpdateCallsign = true;
	}

	// Called by RelaySocket
	// TODO: Perchance rewrite these to use events
	public static void GoodCallsign() {
		relayManagerViewport.SetActive(false);
		relayInputViewport.SetActive(true);
		switchToInputButton.SetActive(true);
		popupBox.PopupWithLabel("Callsign set to " + callsignInput.label.text);
	}

	public static void BadCallsign() {
		popupBox.PopupWithLabel("Callsign already in use");
	}

	public static void Disconnect() {
		RelayManagerWindow.infoDisplay.SwitchWindow(RelayManagerWindow.infoDisplay.tabsWindow);

		if (RelayManagerWindow.relaySocket == null) {
			return;
		}

		switchToInputButton.SetActive(false);
		RelayManagerWindow.relaySocket.Disconnect();
		GameObject.Destroy(RelayManagerWindow.relaySocket);
		RelayManagerWindow.relaySocket = null;
		RelaySocket.Callsign = null;
	}

	private static void SendMessage() {
		var message = TextDummyManager.Instance_PuzzleInput.currText.ToUpper();
		var compilerResult = new CompilerResult();
		var signalMessage = ConsoleDisplay.instance.compiler.CompileStringToSignal(message, ref compilerResult);
		if (compilerResult.compilerResultTag == CompilerResultTag.ERROR) {
			// TODO: Better error display
			popupBox.PopupWithLabel("Compilation errors: " + compilerResult.errorsCaught);
			return;
		}

		// TODO: Handle encrypted messages, somehow

		var signals = signalMessage.signals.Join(delimiter: ",");
		var compiledMessage = "M," + signals;

		RelaySocket.QueueSend(compiledMessage);

		TextDummyManager.Instance_PuzzleInput.Clear();
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
