using HarmonyLib;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[HarmonyPatch]
public class DSCRManagerWindow : InfoWindow
{
    public static InfoDisplay infoDisplay;

    private static InfoWindow dscrManagerWindow;
    private static SimpleWriter callsignInput;
    private static TextMeshPro dscrInput;
    private static bool initDscrButton = false;
    private static bool initInfoDisplay = false;
    private static CalculatorWindow calculatorWindow;

    public static GameObject dscrManagerViewport;
    public static GameObject dscrInputViewport;

    [HarmonyPatch(typeof(TabsWindow), "Open")]
    [HarmonyPostfix]
    public static void MakeDSCRButton(TabsWindow __instance) {
        if (initDscrButton) {
            return;
        }

        initDscrButton = true;

        var menuButton = __instance.GetComponentInChildren<WindowElement>();

        var dscrButton = GameObject.Instantiate(menuButton, menuButton.transform.position with {y = 4.46f}, Quaternion.identity);
        dscrButton.name = "DSCR";
        var y = dscrButton.transform.localPosition.y;
        y = -0.49f;

        dscrButton.transform.SetParent(menuButton.transform.parent);

        var text = dscrButton.GetComponentInChildren<TextMeshPro>();
        text.text = "DSCR";

        var button = dscrButton.GetComponent<Button3D>();
        button.OnUseButton = new UnityEngine.Events.UnityEvent();
        button.OnUseButton.AddListener(() => infoDisplay.SwitchWindow(dscrManagerWindow));
    }

    [HarmonyPatch(typeof(InfoDisplay), "Start")]
    [HarmonyPostfix]
    public static void MakeDSCRManagerScreen(InfoDisplay __instance) {
        if (initInfoDisplay) {
            return;
        }

        initInfoDisplay = true;

        infoDisplay = __instance;

        var calendarWindow = Resources.FindObjectsOfTypeAll<Transform>()
            .Select(t => t.GetComponent<CalendarWindow>())
            .FirstOrDefault(c => c != null);
        
        var dscrWindowObj = GameObject.Instantiate(calendarWindow).gameObject;
        dscrWindowObj.name = "DSCR Manager Window";
        GameObject.DestroyImmediate(dscrWindowObj.GetComponent<CalendarWindow>());

        dscrWindowObj.SetActive(true);

        foreach (var child in dscrWindowObj.GetComponentsInChildren<Transform>(true)) {
            if (child.name == "Mission Time" || child.name == "Running Time") {
                GameObject.DestroyImmediate(child.gameObject);
                continue;
            }
            
            child.gameObject.SetActive(true);
        }

        // Adding at end so Start runs after we destroy calendar stuff
        dscrManagerWindow = dscrWindowObj.AddComponent<DSCRManagerWindow>();
    }

    private void Start() {
        dscrManagerViewport = transform.GetChild(0).gameObject;
        dscrManagerViewport.name = "DSCR Manager Viewport";
        dscrManagerViewport.SetActive(false);

        dscrInputViewport = GameObject.Instantiate(dscrManagerViewport.gameObject);
        dscrInputViewport.transform.SetParent(transform);
        dscrInputViewport.transform.position = dscrManagerViewport.transform.position;
        dscrInputViewport.name = "DSCR Input Viewport";
        dscrInputViewport.SetActive(false);

        /*
         * DSCR MANAGER
         */

        dscrManagerViewport.GetComponentInChildren<TextMeshPro>().text = "DSCR Login";

        calculatorWindow = GameObject.Find("Calculator Window").GetComponent<CalculatorWindow>();

        var callsignLabel = GameObject.Instantiate(calculatorWindow.outputLabel.gameObject);
        callsignLabel.name = "Callsign Label";
        callsignLabel.transform.SetParent(dscrManagerViewport.transform);

        var rectTransform = callsignLabel.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.position = new Vector3(-30.3f, 5.45f, 0f);

        var text = callsignLabel.GetComponent<TextMeshPro>();
        text.text = "Callsign (base 8):";
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;

        var inputField = GameObject.Instantiate(calculatorWindow.operand1.gameObject);
        inputField.name = "Callsign Input";
        inputField.transform.SetParent(dscrManagerViewport.transform);
        inputField.transform.position = new Vector3(-29.7f, 5.5f, 0.22f);

        var inputFieldBackground = inputField.GetComponent<MeshFilter>().mesh;
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
        callsignInput.charsLength = 4;

        var calculatorViewport = calculatorWindow.viewport;
        var exampleButton = calculatorViewport.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "Round");
        var confirmButton = GameObject.Instantiate(exampleButton);
        confirmButton.name = "Confirm Callsign";
        confirmButton.transform.SetParent(dscrManagerViewport.transform);
        confirmButton.transform.position = new Vector3(-29.3f, 5.5f, 0.216f);
        confirmButton.GetComponent<Button3D>().OnUseButton = new UnityEngine.Events.UnityEvent();
        confirmButton.GetComponent<Button3D>().OnUseButton.AddListener(SetCallsign);
        confirmButton.GetComponentInChildren<TextMeshPro>().text = "Set";

        /*
         * DSCR INPUT
         */
        dscrInputViewport.GetComponentInChildren<TextMeshPro>().text = "DSCR Input";

        var inputDisplay = Resources.FindObjectsOfTypeAll<Transform>()
            .Select(t => t.GetComponent<InputDisplaySelector>())
            .FirstOrDefault(c => c != null);
        var dscrInputObj = GameObject.Instantiate(inputDisplay);
        dscrInputObj.transform.SetParent(dscrInputViewport.transform);
        dscrInputObj.transform.position = new Vector3(-30.1f, 5.55f, 0.22f);
        dscrInputObj.name = "DSCR Input";
        dscrInput = dscrInputObj.GetComponent<TextMeshPro>();
        dscrInput.text = "This is some testing text so you can see the thing\n\nfoo bar\nbaz \ncumshot";
        dscrInput.fontSize = 0.9f;
        dscrInputObj.GetComponent<InputDisplaySelector>().tInput = dscrInput;

        var console = Resources.FindObjectsOfTypeAll<Transform>()
            .Select(t => t.GetComponent<ConsoleDisplay>())
            .FirstOrDefault(c => c != null);
        var sendMessageButton = GameObject.Instantiate(console.readMessageGroup.transform.GetChild(1));
        sendMessageButton.name = "DSCR Send Message";
        sendMessageButton.transform.SetParent(dscrInputViewport.transform);
        sendMessageButton.transform.position = new Vector3(-29.5f, 5.68f, 0.25f);
        sendMessageButton.GetComponentInChildren<TextMeshPro>().text = "Send";
        sendMessageButton.GetComponentInChildren<Button3D>().OnUseButton = new UnityEvent();
        sendMessageButton.GetComponentInChildren<Button3D>().OnUseButton.AddListener(SendMessage);

        /*
         * Right hand side monitor
         */
        var topBar = GameObject.Instantiate(dscrManagerViewport.transform.Find("Top Tab"));
        topBar.name = "Top Tab Right";
        topBar.GetComponentInChildren<TextMeshPro>().text = "DSCR";
        GameObject.Destroy(topBar.GetComponentInChildren<Button3D>().gameObject);
    }

    [HarmonyPatch(typeof(WorldSpaceClicker), "ScanForHitbox")]
    [HarmonyPostfix]
    public static void HandleInput(WorldSpaceClicker __instance) {
        // Clicking the text box wasn't working and I couldn't figure out why, so we just manually enable it
        // Horrendous approach, please do not copy this
        if (__instance.mainClickDown && dscrInputViewport.gameObject.activeSelf && !__instance.InRightSide) {
            dscrInput.GetComponent<InputDisplaySelector>().OnRelease(__instance);
        }
    }

    public override void Open() {
        if (DSCRSocket.Callsign == null) {
            dscrManagerViewport.SetActive(true);
        } else {
            dscrInputViewport.SetActive(true);
        }
        DSCRWindow.TryShow();
    }

    public override void Close() {
        dscrManagerViewport.SetActive(false);
        dscrInputViewport.SetActive(false);
    }

    private void SetCallsign() {
        var callsign = callsignInput.label.text
            .Where(c => (c - '0') <= 7 && (c - '0') >= 0)
            .Join(delimiter: "");

        // TODO: What if callsign isn't 4 chars after clamping to [0..8]?
        
        if (!int.TryParse(callsign, out var callsignBase8)) {
            // TODO: Player facing error message
            DSCR.Plugin.Logger.LogInfo("Bad callsign: " + callsign);
            return;
        }

        var callsignBase10 = calculatorWindow.EuclideanBaseChange(callsignBase8, 8, 10);
        DSCRWindow.SetCallsign(callsignBase10);
        dscrManagerViewport.SetActive(false);
        dscrInputViewport.SetActive(true);
    }

    private static void SendMessage() {
        var message = TextDummyManager.Instance_PuzzleInput.currText.ToUpper();
        var compilerResult = new CompilerResult();
        var signalMessage = ConsoleDisplay.instance.compiler.CompileStringToSignal(message, ref compilerResult);
        if (compilerResult.compilerResultTag == CompilerResultTag.ERROR) {
            DSCR.Plugin.Logger.LogError("Compilation Error: " + compilerResult.ErrorMsg);
        }

        var signals = signalMessage.signals.Join(delimiter: ",");
        var compiledMessage = "M," + signals;

        DSCRSocket.QueueSend(compiledMessage);

        TextDummyManager.Instance_PuzzleInput.Clear();
    }
}
