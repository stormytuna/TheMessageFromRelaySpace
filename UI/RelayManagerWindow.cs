using HarmonyLib;
using System.Linq;
using TMFRS.MonoBehaviours;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TMFRS.UI;

[HarmonyPatch]
public class RelayManagerWindow : InfoWindow
{
    public static InfoDisplay infoDisplay;

    private static InfoWindow relayManagerWindow;
    private static SimpleWriter callsignInput;
    private static TextMeshPro relayInput;
    private static bool initRelayButton = false;
    private static bool initInfoDisplay = false;
    private static CalculatorWindow calculatorWindow;

    public static GameObject relayManagerViewport;
    public static GameObject relayInputViewport;

    [HarmonyPatch(typeof(TabsWindow), "Open")]
    [HarmonyPostfix]
    public static void MakeRelayButton(TabsWindow __instance) {
        if (initRelayButton) {
            return;
        }

        initRelayButton = true;

        var menuButton = __instance.GetComponentInChildren<WindowElement>();

        var relayButton = GameObject.Instantiate(menuButton, menuButton.transform.position with {y = 4.46f}, Quaternion.identity);
        relayButton.name = "DSCR"; // TODO: Rename, follow existing standard
        // TODO: What is this even doing? why are we modifying localPos?
        var y = relayButton.transform.localPosition.y;
        y = -0.49f;

        relayButton.transform.SetParent(menuButton.transform.parent);

        var text = relayButton.GetComponentInChildren<TextMeshPro>();
        text.text = "DSCR"; // TODO: Rename, probably

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

        // TODO: Abstract to helper
        var calendarWindow = Resources.FindObjectsOfTypeAll<Transform>()
            .Select(t => t.GetComponent<CalendarWindow>())
            .FirstOrDefault(c => c != null);
        
        var relayWindowObj = GameObject.Instantiate(calendarWindow).gameObject;
        relayWindowObj.name = "DSCR Manager Window"; // TODO: Rename, follow existing standard
        GameObject.DestroyImmediate(relayWindowObj.GetComponent<CalendarWindow>());

        relayWindowObj.SetActive(true);

        foreach (var child in relayWindowObj.GetComponentsInChildren<Transform>(true)) {
            if (child.name == "Mission Time" || child.name == "Running Time") {
                GameObject.DestroyImmediate(child.gameObject);
                continue;
            }
            
            child.gameObject.SetActive(true);
        }

        // Adding at end so Start runs after we destroy calendar stuff
        relayManagerWindow = relayWindowObj.AddComponent<RelayManagerWindow>();
    }

    private void Start() {
        relayManagerViewport = transform.GetChild(0).gameObject;
        relayManagerViewport.name = "DSCR Manager Viewport"; // TODO: Rename
        relayManagerViewport.SetActive(false);

        relayInputViewport = GameObject.Instantiate(relayManagerViewport.gameObject);
        relayInputViewport.transform.SetParent(transform);
        relayInputViewport.transform.position = relayManagerViewport.transform.position;
        relayInputViewport.name = "DSCR Input Viewport"; // TODO: Rename
        relayInputViewport.SetActive(false);

        // TODO: Move to own method
        /*
         * DSCR MANAGER
         */

        relayManagerViewport.GetComponentInChildren<TextMeshPro>().text = "DSCR Login"; // Rename

        calculatorWindow = GameObject.Find("Calculator Window").GetComponent<CalculatorWindow>();

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
        // TODO: Move to helper
        var vertices = new Vector3[inputFieldBackground.vertices.Length];
        for (int i = 0; i < inputFieldBackground.vertices.Length; i++) {
            var vertex = inputFieldBackground.vertices[i];
            vertex.x *= 0.2f;
            vertex.y *= 0.6f;
            vertices[i] = vertex;
        }
        inputFieldBackground.vertices = vertices;

        callsignInput = inputField.GetComponent<SimpleWriter>();
        callsignInput.label.transform.position = new Vector3(-29.1f, 5.46f, 0.2141f);
        callsignInput.label.text = "0000";
        callsignInput.charsLength = 4; // TODO: Setting charsLength does nothing, why?

        var calculatorViewport = calculatorWindow.viewport;
        var exampleButton = calculatorViewport.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "Round");
        var confirmButton = GameObject.Instantiate(exampleButton);
        confirmButton.name = "Confirm Callsign";
        confirmButton.transform.SetParent(relayManagerViewport.transform);
        confirmButton.transform.position = new Vector3(-29.3f, 5.5f, 0.216f);
        confirmButton.GetComponent<Button3D>().OnUseButton = new UnityEngine.Events.UnityEvent();
        confirmButton.GetComponent<Button3D>().OnUseButton.AddListener(SetCallsign);
        confirmButton.GetComponentInChildren<TextMeshPro>().text = "Set";

        /*
         * DSCR INPUT
         */
        relayInputViewport.GetComponentInChildren<TextMeshPro>().text = "DSCR Input"; // TODO: Change name

        // TODO: Helper
        var inputDisplay = Resources.FindObjectsOfTypeAll<Transform>()
            .Select(t => t.GetComponent<InputDisplaySelector>())
            .FirstOrDefault(c => c != null);

        var dscrInputObj = GameObject.Instantiate(inputDisplay);
        dscrInputObj.transform.SetParent(relayInputViewport.transform);
        dscrInputObj.transform.position = new Vector3(-30.1f, 5.55f, 0.22f);
        dscrInputObj.name = "DSCR Input";
        relayInput = dscrInputObj.GetComponent<TextMeshPro>();
        relayInput.text = ""; // TODO: Do we even need to do this?
        relayInput.fontSize = 0.9f;
        dscrInputObj.GetComponent<InputDisplaySelector>().tInput = relayInput;

        // TODO: ConsoleDisplay.Instance
        var console = Resources.FindObjectsOfTypeAll<Transform>()
            .Select(t => t.GetComponent<ConsoleDisplay>())
            .FirstOrDefault(c => c != null);
        var sendMessageButton = GameObject.Instantiate(console.readMessageGroup.transform.GetChild(1)); // TODO: Perchance use .Find
        sendMessageButton.name = "DSCR Send Message"; // TODO: Rename
        sendMessageButton.transform.SetParent(relayInputViewport.transform);
        sendMessageButton.transform.position = new Vector3(-29.5f, 5.68f, 0.25f);
        sendMessageButton.GetComponentInChildren<TextMeshPro>().text = "Send";
        sendMessageButton.GetComponentInChildren<Button3D>().OnUseButton = new UnityEvent();
        sendMessageButton.GetComponentInChildren<Button3D>().OnUseButton.AddListener(SendMessage);

        /*
         * Right hand side monitor
         */
        var topBar = GameObject.Instantiate(relayManagerViewport.transform.Find("Top Tab"));
        topBar.name = "Top Tab Right"; // TODO: Rename
        topBar.GetComponentInChildren<TextMeshPro>().text = "DSCR"; // TODO: Rename
        GameObject.Destroy(topBar.GetComponentInChildren<Button3D>().gameObject); // TODO: Probably has many more objects attached that go unused
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
        if (RelaySocket.Callsign == null) {
            relayManagerViewport.SetActive(true);
        } else {
            relayInputViewport.SetActive(true);
        }
        RelayWindow.TryShow();
    }

    public override void Close() {
        relayManagerViewport.SetActive(false);
        relayInputViewport.SetActive(false);
    }

    private void SetCallsign() {
        var callsign = callsignInput.label.text
            .Where(c => (c - '0') <= 7 && (c - '0') >= 0)
            .Join(delimiter: "");

        // TODO: What if callsign isn't 4 chars after clamping to [0..8]?
        
        if (!int.TryParse(callsign, out var callsignBase8)) {
            // TODO: Player facing error message
            TMFRSPlugin.Logger.LogInfo("Bad callsign: " + callsign);
            return;
        }

        var callsignBase10 = calculatorWindow.EuclideanBaseChange(callsignBase8, 8, 10);
        RelayWindow.SetCallsign(callsignBase10);
        relayManagerViewport.SetActive(false);
        relayInputViewport.SetActive(true);
    }

    private static void SendMessage() {
        var message = TextDummyManager.Instance_PuzzleInput.currText.ToUpper();
        var compilerResult = new CompilerResult();
        var signalMessage = ConsoleDisplay.instance.compiler.CompileStringToSignal(message, ref compilerResult);
        if (compilerResult.compilerResultTag == CompilerResultTag.ERROR) {
            TMFRSPlugin.Logger.LogError("Compilation Error: " + compilerResult.ErrorMsg);
            // TODO: Actually catch compilation errors, display using the little popup at the bottom that new ideas use
        }

        // TODO: Handle encrypted messages, somehow

        var signals = signalMessage.signals.Join(delimiter: ",");
        var compiledMessage = "M," + signals;

        RelaySocket.QueueSend(compiledMessage);

        TextDummyManager.Instance_PuzzleInput.Clear();
    }
}
