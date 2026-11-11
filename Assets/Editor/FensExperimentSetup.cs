using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: FENS > Setup Experiment Scene
/// Adds and connects the experiment objects in the open scene (step 1 here, audio / model /
/// alignment / passthrough in FensExperimentSetup.Step2.cs). Safe to run more than once:
/// existing objects and an existing TargetSet asset are kept, only missing pieces are added.
/// Everything is recorded with Undo (Cmd+Z reverts it).
/// </summary>
public static partial class FensExperimentSetup
{
    const string AssetFolder = "Assets/Experiment";
    const string TargetSetPath = AssetFolder + "/TargetSet.asset";

    [MenuItem("FENS/Setup Experiment Scene")]
    public static void Setup()
    {
        var log = new List<string>();

        // 1) TargetSet asset with placeholder targets
        var set = AssetDatabase.LoadAssetAtPath<TargetSet>(TargetSetPath);
        if (set == null)
        {
            if (!AssetDatabase.IsValidFolder(AssetFolder)) AssetDatabase.CreateFolder("Assets", "Experiment");
            set = ScriptableObject.CreateInstance<TargetSet>();
            FillPlaceholders(set);
            AssetDatabase.CreateAsset(set, TargetSetPath);
            AssetDatabase.SaveAssets();
            log.Add($"Created {TargetSetPath} with {set.Count} PLACEHOLDER targets.");
        }
        else log.Add($"Kept existing {TargetSetPath}.");

        // 2) RouteManager (already in the scene)
        var rm = Object.FindFirstObjectByType<RouteManager>(FindObjectsInactive.Include);
        if (rm == null)
        {
            EditorUtility.DisplayDialog("FENS setup", "No RouteManager found in the open scene.\n" +
                "Open Assets/Scenes/SampleScene first.", "OK");
            return;
        }
        Undo.RecordObject(rm, "FENS setup");
        rm.targetSet = set;

        var origin = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
        if (rm.xrOrigin == null && origin != null) rm.xrOrigin = origin.transform;
        if (rm.playerHead == null && origin != null && origin.Camera != null) rm.playerHead = origin.Camera.transform;
        EditorUtility.SetDirty(rm);
        if (rm.xrOrigin == null || rm.playerHead == null)
            log.Add("WARNING: RouteManager has no XR Origin / Main Camera reference. Set them in the Inspector.");

        // Target sound must stay silent until a trial starts
        if (rm.targetSound != null)
        {
            var src = rm.targetSound.GetComponent<AudioSource>();
            if (src != null && src.playOnAwake)
            {
                Undo.RecordObject(src, "FENS setup");
                src.playOnAwake = false;
                EditorUtility.SetDirty(src);
                log.Add("TargetSound: Play On Awake turned off.");
            }
        }

        // 3) Experiment object with controller, input, logger, feedback
        var exp = Object.FindFirstObjectByType<ExperimentController>(FindObjectsInactive.Include);
        GameObject expGo;
        if (exp == null)
        {
            expGo = new GameObject("Experiment");
            Undo.RegisterCreatedObjectUndo(expGo, "FENS setup");
            exp = Undo.AddComponent<ExperimentController>(expGo);
            log.Add("Created 'Experiment' object.");
        }
        else expGo = exp.gameObject;

        var input = GetOrAdd<OperatorInput>(expGo, log);
        var logger = GetOrAdd<TrialLogger>(expGo, log);
        var feedback = GetOrAdd<OperatorFeedback>(expGo, log);

        Undo.RecordObject(exp, "FENS setup");
        exp.routeManager = rm;
        exp.logger = logger;
        exp.feedback = feedback;
        EditorUtility.SetDirty(exp);

        Undo.RecordObject(input, "FENS setup");
        input.experiment = exp;
        EditorUtility.SetDirty(input);

        Undo.RecordObject(logger, "FENS setup");
        logger.playerHead = rm.playerHead;
        EditorUtility.SetDirty(logger);

        Undo.RecordObject(feedback, "FENS setup");
        feedback.experiment = exp;
        feedback.playerHead = rm.playerHead;
        EditorUtility.SetDirty(feedback);

        // 4) Real walking only: no virtual locomotion, no CharacterController
        if (rm.xrOrigin != null) GetOrAdd<RealWalkingOnly>(rm.xrOrigin.gameObject, log);

        // 5) Step 2: model colliders + visibility, audio, alignment, passthrough
        SetupStep2(rm, exp, logger, feedback, log);

        // 6) Step 3: controller buttons for the operator, controller visuals hidden
        SetupControllers(rm, exp, log);

        EditorSceneManager.MarkSceneDirty(expGo.scene);
        Selection.activeGameObject = expGo;

        string msg = string.Join("\n", log) + "\n\nNow save the scene: File > Save (Cmd+S).";
        Debug.Log("[FENS setup]\n" + msg);
        EditorUtility.DisplayDialog("FENS setup done", msg, "OK");
    }

    const string SimulatorSettingsPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
    const string SimulatorPrefabPath =
        "Assets/Samples/XR Interaction Toolkit/3.1.2/XR Device Simulator/XRInteractionSimulator/XR Interaction Simulator.prefab";

    /// Turns on the XR Interaction Simulator for Play mode in the Editor (never in headset builds).
    /// The XRI settings class is internal, so the fields are set through SerializedObject.
    [MenuItem("FENS/Enable Simulator (Editor only)")]
    public static void EnableSimulator()
    {
        var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SimulatorSettingsPath);
        if (settings == null)
        {
            Debug.LogError($"[FENS simulator] Settings asset not found at {SimulatorSettingsPath}. " +
                           "Open Edit > Project Settings > XR Plug-in Management > XR Interaction Toolkit once, then retry.");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[FENS simulator] Prefab not found at {SimulatorPrefabPath}. " +
                           "Import the 'XR Device Simulator' sample from the Package Manager first.");
            return;
        }

        var so = new SerializedObject(settings);
        var autoProp = so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab");
        var editorOnlyProp = so.FindProperty("m_AutomaticallyInstantiateInEditorOnly");
        var classicProp = so.FindProperty("m_UseClassic");
        var prefabProp = so.FindProperty("m_SimulatorPrefab");
        if (autoProp == null || editorOnlyProp == null || classicProp == null || prefabProp == null)
        {
            Debug.LogError("[FENS simulator] Expected fields not found in XRDeviceSimulatorSettings " +
                           "(different XRI version?). Nothing changed.");
            return;
        }

        autoProp.boolValue = true;        // create the simulator automatically in Play mode
        editorOnlyProp.boolValue = true;  // only in the Editor, never in a headset build
        classicProp.boolValue = false;    // new XR Interaction Simulator, not the classic one
        prefabProp.objectReferenceValue = prefab;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Debug.Log("[FENS simulator] XR Interaction Simulator enabled:\n" +
                  $"  Use XR Interaction Simulator in scenes = {autoProp.boolValue}\n" +
                  $"  Instantiate In Editor Only = {editorOnlyProp.boolValue}\n" +
                  $"  Use Classic XR Device Simulator = {classicProp.boolValue}\n" +
                  $"  Prefab = {AssetDatabase.GetAssetPath(prefabProp.objectReferenceValue)}");
    }

    static T GetOrAdd<T>(GameObject go, List<string> log) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c != null) return c;
        log.Add($"Added {typeof(T).Name} to '{go.name}'.");
        return Undo.AddComponent<T>(go);
    }

    /// PLACEHOLDER targets on floor 2 (built from Revna's two routes). Replace with the real points later.
    static void FillPlaceholders(TargetSet set)
    {
        const float y = 7.86f;
        Vector3 P(float x, float z) => new Vector3(x, y, z);

        void Add(string id, bool warmup, params Vector3[] path)
        {
            var t = new TargetDef { id = id, isWarmup = warmup, path = new List<Vector3>(path) };
            t.startYaw = TargetSet.YawOfFirstSegment(t);
            set.targets.Add(t);
        }

        Add("W", true, P(-7.2f, -3.8f), P(-7.2f, 0.2f), P(2f, 0.2f));                                   // warm-up
        Add("T1", false, P(-7.2f, -3.8f), P(-7.2f, 0.2f), P(-29.7f, 0.2f), P(-29.7f, -30.3f), P(-15f, -30.3f)); // old route A
        Add("T2", false, P(-7.2f, -3.8f), P(-7.2f, 0.2f), P(24.5f, 0.2f), P(24.5f, -30.3f), P(17f, -30.3f));    // old route B
        Add("T3", false, P(-7.2f, -3.8f), P(-7.2f, 0.2f), P(-29.7f, 0.2f), P(-29.7f, -15f));
        Add("T4", false, P(-7.2f, -3.8f), P(-7.2f, 0.2f), P(24.5f, 0.2f), P(24.5f, -15f));
    }
}
