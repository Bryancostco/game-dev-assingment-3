using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using DeliveryGame;

/// <summary>
/// Editor utility that builds the full Canvas UI hierarchy, wires all references,
/// and duplicates pickup/dropoff pairs for the active level scene.
/// Run via the top menu: DeliveryGame > Setup Level UI
/// </summary>
public static class LevelSetupTool
{
    [MenuItem("DeliveryGame/Setup Level_01 UI")]
    public static void SetupLevel01()
    {
        SetupLevel(1, 5, 180f,
            "Complete 5 deliveries before time runs out!\nDrive to the blue markers to pick up packages, then deliver them to the orange zones.");
    }

    [MenuItem("DeliveryGame/Setup Level_02 UI")]
    public static void SetupLevel02()
    {
        SetupLevel(2, 8, 150f,
            "Complete 8 deliveries in 150 seconds!\nDistances are longer and more obstacles block your path.");
    }

    // -------------------------------------------------------------------------
    private static void SetupLevel(int levelNum, int deliveries, float time, string briefing)
    {
        // ── EventSystem ──────────────────────────────────────────────────────
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            es.AddComponent<EventSystem>();
            // New Input System UI module instead of legacy StandaloneInputModule
            es.AddComponent<InputSystemUIInputModule>();
        }

        // ── Canvas ───────────────────────────────────────────────────────────
        Canvas existingCanvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasGO;
        if (existingCanvas == null)
        {
            canvasGO = new GameObject("Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            Canvas c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasGO = existingCanvas.gameObject;
        }

        // ── HUD Panel ────────────────────────────────────────────────────────
        GameObject hudPanel = GetOrCreateStretchPanel(canvasGO, "HUDPanel", true);

        // Delivery counter — top-left
        var deliveryCounterText = CreateTMP(hudPanel, "DeliveryCounterText", $"Deliveries: 0 / {deliveries}",
            new Vector2(0, 1), new Vector2(260, 36), new Vector2(140, -24));

        // Timer — top-centre
        var timerText = CreateTMP(hudPanel, "TimerText", "Time: 3:00",
            new Vector2(0.5f, 1), new Vector2(180, 36), new Vector2(0, -24));

        // Health bar — top-right area
        GameObject healthBg = CreateRect(hudPanel, "HealthBarBackground",
            new Vector2(1, 1), new Vector2(200, 22), new Vector2(-110, -24));
        Image healthBgImg = healthBg.AddComponent<Image>();
        healthBgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        GameObject healthFillGO = CreateRect(healthBg, "HealthBarFill", Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform hfRT = healthFillGO.GetComponent<RectTransform>();
        hfRT.anchorMin = Vector2.zero;
        hfRT.anchorMax = Vector2.one;
        hfRT.offsetMin = Vector2.zero;
        hfRT.offsetMax = Vector2.zero;
        Image healthFillImg = healthFillGO.AddComponent<Image>();
        healthFillImg.color = new Color(0.2f, 0.85f, 0.2f);
        healthFillImg.type = Image.Type.Filled;
        healthFillImg.fillMethod = Image.FillMethod.Horizontal;

        // Minimap — top-right corner
        GameObject minimapGO = CreateRect(hudPanel, "MinimapDisplay",
            new Vector2(1, 1), new Vector2(160, 160), new Vector2(-90, -90));
        RawImage minimapRaw = minimapGO.AddComponent<RawImage>();
        minimapRaw.color = new Color(0.3f, 0.3f, 0.3f); // grey until RenderTexture assigned

        // Objective — bottom-centre
        var objectiveText = CreateTMP(hudPanel, "ObjectiveText", "Pick up a package",
            new Vector2(0.5f, 0), new Vector2(500, 36), new Vector2(0, 70));
        objectiveText.alignment = TextAlignmentOptions.Center;

        // Notification group — centre screen
        GameObject notifGroupGO = CreateRect(hudPanel, "NotificationGroup",
            new Vector2(0.5f, 0.5f), new Vector2(480, 44), new Vector2(0, 120));
        CanvasGroup notifCG = notifGroupGO.AddComponent<CanvasGroup>();
        notifCG.alpha = 0f;
        var notifText = CreateTMP(notifGroupGO, "NotificationText", "",
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(notifText.gameObject);
        notifText.alignment = TextAlignmentOptions.Center;
        notifText.color = Color.yellow;

        // Briefing — centre screen, fades after 3.5 s
        GameObject briefingPanelGO = CreateRect(hudPanel, "BriefingPanel",
            new Vector2(0.5f, 0.5f), new Vector2(700, 90), new Vector2(0, 0));
        Image briefingBg = briefingPanelGO.AddComponent<Image>();
        briefingBg.color = new Color(0f, 0f, 0f, 0.7f);
        CanvasGroup briefingCG = briefingPanelGO.AddComponent<CanvasGroup>();
        briefingCG.alpha = 1f;
        var briefingText = CreateTMP(briefingPanelGO, "BriefingText", briefing,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(briefingText.gameObject);
        briefingText.alignment = TextAlignmentOptions.Center;
        briefingText.fontSize = 15;

        // ── Pause Panel ──────────────────────────────────────────────────────
        GameObject pausePanel = GetOrCreateStretchPanel(canvasGO, "PausePanel", false);
        AddDimBackground(pausePanel);
        CreateTMP(pausePanel, "PauseTitleText", "PAUSED",
            new Vector2(0.5f, 0.5f), new Vector2(300, 50), new Vector2(0, 160))
            .alignment = TextAlignmentOptions.Center;
        var resumeBtn    = CreateButton(pausePanel, "ResumeButton",    "Resume",       new Vector2(0, 120));
        var restartBtn   = CreateButton(pausePanel, "RestartButton",   "Restart Level",new Vector2(0, 60));
        var quitMenuBtn  = CreateButton(pausePanel, "QuitToMenuButton","Main Menu",    new Vector2(0, 0));

        // ── Win Panel ────────────────────────────────────────────────────────
        GameObject winPanel = GetOrCreateStretchPanel(canvasGO, "WinPanel", false);
        AddDimBackground(winPanel);
        CreateTMP(winPanel, "WinTitleText", "Level Complete!",
            new Vector2(0.5f, 0.5f), new Vector2(400, 60), new Vector2(0, 150))
            .alignment = TextAlignmentOptions.Center;
        var winTimeText  = CreateTMP(winPanel, "WinTimeText", "Time: 0:00",
            new Vector2(0.5f, 0.5f), new Vector2(320, 36), new Vector2(0, 90));
        winTimeText.alignment = TextAlignmentOptions.Center;
        var winDelivText = CreateTMP(winPanel, "WinDeliveriesText", $"Delivered: 0 / {deliveries}",
            new Vector2(0.5f, 0.5f), new Vector2(320, 36), new Vector2(0, 44));
        winDelivText.alignment = TextAlignmentOptions.Center;
        var nextLevelBtn = CreateButton(winPanel, "NextLevelButton", "Next Level", new Vector2(0, -20));
        var winMenuBtn   = CreateButton(winPanel, "WinToMenuButton", "Main Menu",  new Vector2(0, -80));

        // ── Lose Panel ───────────────────────────────────────────────────────
        GameObject losePanel = GetOrCreateStretchPanel(canvasGO, "LosePanel", false);
        AddDimBackground(losePanel);
        var loseReasonText = CreateTMP(losePanel, "LoseReasonText", "Time's Up!",
            new Vector2(0.5f, 0.5f), new Vector2(400, 60), new Vector2(0, 100));
        loseReasonText.alignment = TextAlignmentOptions.Center;
        var retryBtn    = CreateButton(losePanel, "RetryButton",    "Retry",     new Vector2(0, 10));
        var loseMenuBtn = CreateButton(losePanel, "LoseToMenuButton","Main Menu", new Vector2(0, -50));

        // ── PauseMenuController — wire buttons ───────────────────────────────
        PauseMenuController pmc = canvasGO.GetComponent<PauseMenuController>();
        if (pmc == null) pmc = Undo.AddComponent<PauseMenuController>(canvasGO);

        WireButton(resumeBtn,    pmc.OnResume);
        WireButton(restartBtn,   pmc.OnRestartLevel);
        WireButton(quitMenuBtn,  pmc.OnQuitToMenu);
        WireButton(nextLevelBtn, pmc.OnNextLevel);
        WireButton(winMenuBtn,   pmc.OnWinToMenu);
        WireButton(retryBtn,     pmc.OnRetry);
        WireButton(loseMenuBtn,  pmc.OnLoseToMenu);

        // ── SceneUIConnector ─────────────────────────────────────────────────
        SceneUIConnector connector = Object.FindFirstObjectByType<SceneUIConnector>();
        if (connector != null)
        {
            SerializedObject so = new SerializedObject(connector);
            so.FindProperty("_totalDeliveries").intValue   = deliveries;
            so.FindProperty("_levelTime").floatValue       = time;

            SerializedProperty refs = so.FindProperty("_uiRefs");
            refs.FindPropertyRelative("DeliveryCounter").objectReferenceValue  = deliveryCounterText;
            refs.FindPropertyRelative("TimerText").objectReferenceValue        = timerText;
            refs.FindPropertyRelative("HealthBarFill").objectReferenceValue    = healthFillImg;
            refs.FindPropertyRelative("MinimapDisplay").objectReferenceValue   = minimapRaw;
            refs.FindPropertyRelative("ObjectiveText").objectReferenceValue    = objectiveText;
            refs.FindPropertyRelative("NotificationText").objectReferenceValue = notifText;
            refs.FindPropertyRelative("NotificationGroup").objectReferenceValue= notifCG;
            refs.FindPropertyRelative("BriefingPanel").objectReferenceValue    = briefingCG;
            refs.FindPropertyRelative("BriefingText").objectReferenceValue     = briefingText;
            refs.FindPropertyRelative("BriefingMessage").stringValue           = briefing;
            refs.FindPropertyRelative("HUDPanel").objectReferenceValue         = hudPanel;
            refs.FindPropertyRelative("PausePanel").objectReferenceValue       = pausePanel;
            refs.FindPropertyRelative("WinPanel").objectReferenceValue         = winPanel;
            refs.FindPropertyRelative("LosePanel").objectReferenceValue        = losePanel;
            refs.FindPropertyRelative("WinTimeText").objectReferenceValue      = winTimeText;
            refs.FindPropertyRelative("WinDeliveriesText").objectReferenceValue= winDelivText;
            refs.FindPropertyRelative("NextLevelButton").objectReferenceValue  = nextLevelBtn;
            refs.FindPropertyRelative("LoseReasonText").objectReferenceValue   = loseReasonText;
            so.ApplyModifiedProperties();
            Debug.Log("[LevelSetupTool] SceneUIConnector wired.");
        }
        else
        {
            Debug.LogWarning("[LevelSetupTool] SceneUIConnector not found in scene.");
        }

        // ── InputActionAsset → VehicleController ─────────────────────────────
        VehicleController vc = Object.FindFirstObjectByType<VehicleController>();
        if (vc != null)
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem/PlayerInputActions.inputactions");
            if (asset != null)
            {
                SerializedObject vcSO = new SerializedObject(vc);
                vcSO.FindProperty("_inputActionsAsset").objectReferenceValue = asset;
                vcSO.ApplyModifiedProperties();
                Debug.Log("[LevelSetupTool] InputActionAsset wired to VehicleController.");
            }
            else
            {
                Debug.LogWarning("[LevelSetupTool] PlayerInputActions.inputactions not found at Assets/InputSystem/.");
            }

            // Tag vehicle root as Player
            if (!vc.gameObject.CompareTag("Player"))
            {
                Undo.RecordObject(vc.gameObject, "Set Player Tag");
                vc.gameObject.tag = "Player";
                Debug.Log("[LevelSetupTool] Tagged vehicle as Player.");
            }
        }
        else
        {
            Debug.LogWarning("[LevelSetupTool] VehicleController not found — add the script to your vehicle and re-run.");
        }

        // ── Pickup / Dropoff pairs ────────────────────────────────────────────
        SetupPickupsAndDropoffs(deliveries);

        // ── Mark scene dirty so Unity prompts to save ─────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[LevelSetupTool] Level {levelNum} setup complete!\n" +
                  "Remaining manual steps:\n" +
                  "  1. Assign audio clips to AudioManager (Assets/Audio/)\n" +
                  "  2. Assign WheelColliders + wheel meshes to VehicleController\n" +
                  "  3. Create a 256x256 RenderTexture, assign to MiniMapCamera Target Texture\n" +
                  "     and drag the same RenderTexture into MinimapDisplay Raw Image\n" +
                  "  4. Reposition the duplicate Pickup/Dropoff pairs around the map\n" +
                  "  5. Save the scene (Ctrl+S)");
    }

    // ── Pickup / Dropoff duplication ─────────────────────────────────────────
    private static void SetupPickupsAndDropoffs(int totalDeliveries)
    {
        PackagePickup  firstPickup  = Object.FindFirstObjectByType<PackagePickup>();
        PackageDropoff firstDropoff = Object.FindFirstObjectByType<PackageDropoff>();

        if (firstPickup == null || firstDropoff == null)
        {
            Debug.LogWarning("[LevelSetupTool] Could not find Pickup/Dropoff in scene — add them manually.");
            return;
        }

        // Fix deliveryId on the first pair
        SetSerializedInt(firstPickup,  "_deliveryId", 0);
        SetSerializedInt(firstDropoff, "_deliveryId", 0);

        int existing = Object.FindObjectsByType<PackagePickup>(FindObjectsSortMode.None).Length;

        for (int i = existing; i < totalDeliveries; i++)
        {
            // Pickup
            GameObject pu = Object.Instantiate(firstPickup.gameObject);
            Undo.RegisterCreatedObjectUndo(pu, $"Duplicate Pickup {i}");
            pu.name = $"Pickup_{i + 1:D2}";
            pu.transform.SetParent(firstPickup.transform.parent, false);
            // Scatter in a grid so they're not stacked — user repositions manually
            pu.transform.position = firstPickup.transform.position
                                  + new Vector3((i % 3) * 20f - 20f, 0f, (i / 3 + 1) * 20f);
            SetSerializedInt(pu.GetComponent<PackagePickup>(), "_deliveryId", i);

            // Dropoff
            GameObject dr = Object.Instantiate(firstDropoff.gameObject);
            Undo.RegisterCreatedObjectUndo(dr, $"Duplicate Dropoff {i}");
            dr.name = $"Dropoff_{i + 1:D2}";
            dr.transform.SetParent(firstDropoff.transform.parent, false);
            dr.transform.position = firstDropoff.transform.position
                                  + new Vector3(-(i % 3) * 20f + 20f, 0f, (i / 3 + 1) * 20f);
            SetSerializedInt(dr.GetComponent<PackageDropoff>(), "_deliveryId", i);
        }

        Debug.Log($"[LevelSetupTool] Pickup/Dropoff pairs ready ({totalDeliveries} total). Move them around the map.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static GameObject GetOrCreateStretchPanel(GameObject parent, string name, bool active)
    {
        Transform existing = parent.transform.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent.transform, false);
        Stretch(go);
        go.SetActive(active);
        return go;
    }

    private static void AddDimBackground(GameObject panel)
    {
        Image img = panel.GetComponent<Image>();
        if (img == null) img = panel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
    }

    private static TextMeshProUGUI CreateTMP(GameObject parent, string name, string text,
        Vector2 anchor, Vector2 size, Vector2 pos)
    {
        GameObject go = CreateRect(parent, name, anchor, size, pos);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = 20;
        tmp.color     = Color.white;
        return tmp;
    }

    private static Button CreateButton(GameObject parent, string label, string name, Vector2 pos)
    {
        GameObject go = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(220, 55), pos);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);
        Button btn = go.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.3f, 0.3f);
        cb.pressedColor     = new Color(0.5f, 0.5f, 0.5f);
        btn.colors = cb;

        GameObject labelGO = new GameObject("Label");
        Undo.RegisterCreatedObjectUndo(labelGO, "Create Button Label");
        labelGO.transform.SetParent(go.transform, false);
        Stretch(labelGO);
        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private static GameObject CreateRect(GameObject parent, string name,
        Vector2 anchor, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin       = anchor;
        rt.anchorMax       = anchor;
        rt.pivot           = anchor;
        rt.sizeDelta       = size;
        rt.anchoredPosition= pos;
        return go;
    }

    private static void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    private static void WireButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(btn.onClick, action);
    }

    private static void SetSerializedInt(Object target, string property, int value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(property).intValue = value;
        so.ApplyModifiedProperties();
    }
}
