using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[HarmonyPatch]
public class DSCRWindow
{
    private const int LinesPerPage = 12;

    private static GameObject dscrRoot = null;
    private static TextMeshPro dscrOutput = null;
    private static DSCRSocket dscrSocket = null;
    private static ScrollBar3D scrollbar = null;
    private static ScrollArea scrollArea = null;
    private static CalculatorWindow calculatorWindow = null;

    private static float lineHeight;
    private static float totalDisplayHeight;

    [HarmonyPatch(typeof(ConsoleDisplay), "Awake")]
    [HarmonyPostfix]
    public static void Init(ConsoleDisplay __instance) {
        dscrRoot = new GameObject("DSCR");
        dscrRoot.SetActive(false);

        calculatorWindow = GameObject.Find("Calculator Window").GetComponent<CalculatorWindow>();

        var topBar = GameObject.Instantiate(__instance.readMessageGroup);
        topBar.transform.SetParent(dscrRoot.transform);
        topBar.transform.position = new Vector3(-27.75f, 5f, -0.005f);
        topBar.name = "DSCR Top Bar";
        topBar.GetComponentInChildren<TextMeshPro>().text = "DSCR";

        var logoutButton = topBar.transform.Find("Respond Button");
        logoutButton.GetComponentInChildren<TextMeshPro>().text = "Logout";
        logoutButton.GetComponentInChildren<Button3D>().OnUseButton = new UnityEvent();
        logoutButton.GetComponentInChildren<Button3D>().OnUseButton.AddListener(Hide);

        var dscrOutputObj = GameObject.Instantiate(GameObject.Find("Output Display"));
        dscrOutputObj.transform.SetParent(dscrRoot.transform);
        dscrOutputObj.transform.position = new Vector3(-27.9f, 5.55f, 0.22f);
        dscrOutputObj.name = "DSCR Output";
        dscrOutput = dscrOutputObj.GetComponent<TextMeshPro>();
        dscrOutput.fontSize = 0.9f;

        scrollbar = topBar.GetComponentInChildren<ScrollBar3D>();
        scrollbar.visuals = scrollbar.transform.Find("Scroll Visuals").gameObject;
        scrollbar.col = scrollbar.GetComponent<BoxCollider>();
        scrollbar.meshRenderer = scrollbar.GetComponentInChildren<MeshRenderer>();
        scrollArea = dscrOutput.GetComponent<ScrollArea>();
        scrollArea.initialConfigure = false;
        scrollbar.scrollArea = scrollArea.gameObject;

        totalDisplayHeight = dscrOutput.rectTransform.sizeDelta.y;
        lineHeight = totalDisplayHeight / LinesPerPage;
    }

    public static void SetCallsign(string callsign) {
        if (dscrSocket == null) {
            DSCRSocket.Callsign = callsign;
            var dscrSocketObject = new GameObject("DSCR Socket");
            dscrSocketObject.AddComponent<DSCRSocket>();
            dscrSocket = dscrSocketObject.GetComponent<DSCRSocket>();
            return;
        }

        // TODO: Update callsign if asked
    } 

    public static void TryShow() {
        if (dscrRoot.activeSelf) {
            return;
        }

        dscrRoot.SetActive(true);

        var console = GameObject.Find("Console Display").GetComponent<ConsoleDisplay>();
        console.display.gameObject.SetActive(false);
        console.readMessageGroup.gameObject.SetActive(false);
        console.monitorVisual.OnWipe();
    } 

    public static void Hide() {
        dscrRoot.SetActive(false);

        if (dscrSocket != null) {
            dscrSocket.Disconnect();
            GameObject.Destroy(dscrSocket);
            dscrSocket = null;
        }

        dscrOutput.text = "";
        DSCRSocket.Callsign = null;

        DSCRManagerWindow.infoDisplay.SwitchWindow(DSCRManagerWindow.infoDisplay.tabsWindow);

        var console = GameObject.Find("Console Display").GetComponent<ConsoleDisplay>();
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
        var signalMessage = new SignalMessage() with {signals = messageSignals};

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
            } else {
                compiledOutput += compiledMessage[i] + "\n";
            }
        }
        
        var senderBase10 = int.Parse(sender);
        var senderBase8 = calculatorWindow.EuclideanBaseChange(senderBase10, 10, 8);
        dscrOutput.text += $"{senderBase8}:{messageNumber}\n{compiledOutput}\n\n";

        dscrOutput.StartCoroutine(SetupScrollbar(compiledMessage.Count));
    }

    private static IEnumerator SetupScrollbar(int count) {
        yield return null;

        float num = (dscrOutput.textInfo.lineCount + 1) * lineHeight;
        float relativeMenuHeight = num / totalDisplayHeight;
        bool scrollToBottom = scrollbar.NormalizedScroll == 0f;
        scrollbar.ConfigureHeight(relativeMenuHeight, true);
        if (scrollToBottom) {
            scrollbar.ForceScrollTo(0f);
        }
        scrollArea.Configure(scrollbar, lineHeight + num, totalDisplayHeight);
    }
}
