using System.Collections.Generic;
using UnityEditor;
// Steam Audio types by alias (a plain "using SteamAudio;" would clash with Material, SerializedObject, ...)
using SteamAudioGeometry = SteamAudio.SteamAudioGeometry;
using SteamAudioManager = SteamAudio.SteamAudioManager;
using SteamAudioSource = SteamAudio.SteamAudioSource;
using SteamAudioListener = SteamAudio.SteamAudioListener;
using SteamAudioMaterial = SteamAudio.SteamAudioMaterial;
using DistanceAttenuationInput = SteamAudio.DistanceAttenuationInput;
using AirAbsorptionInput = SteamAudio.AirAbsorptionInput;
using DirectivityInput = SteamAudio.DirectivityInput;
using OcclusionInput = SteamAudio.OcclusionInput;
using OcclusionType = SteamAudio.OcclusionType;
using TransmissionType = SteamAudio.TransmissionType;
using TransmissionInput = SteamAudio.TransmissionInput;
using ReflectionsType = SteamAudio.ReflectionsType;
using ReverbType = SteamAudio.ReverbType;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

/// <summary>
/// Step 2 of the scene setup (run through FENS > Setup Experiment Scene, or the separate menus):
///  - FENS model: own collider layer, MeshColliders, ModelVisibility + transparent material
///  - target sound: Steam Audio settings like the original FENS-2 scene, but with a
///    frequency-dependent wall material so sound behind walls is muffled instead of silent;
///    routed to the NewAudioMixer "SFX" group; RearAttenuation; reflections switch; listener reverb
///  - WorldAlignment on the XR Origin, PassthroughController on the Main Camera
/// All changes go through Unity's API with Undo; nothing edits scene/prefab YAML by hand.
/// </summary>
public static partial class FensExperimentSetup
{
    const string ModelPrefabPath = "Assets/models/FENS/simplified.prefab";
    const string ModelLayerName = "FENSModel";
    const string MixerPath = "Assets/audio/NewAudioMixer.mixer";
    const string MixerGroupName = "SFX";                       // as in the original FENS-2 scene
    const string WallMaterialPath = AssetFolder + "/FENS_Wall_SteamAudio.asset";
    const string GhostMaterialPath = AssetFolder + "/FENS_ModelGhost.mat";

    // ------------------------------------------------------------------ menus

    [MenuItem("FENS/Step 2 only/Setup Model (colliders + visibility)")]
    static void MenuModel() => RunStep2Part(includeModel: true, includeAudio: false, includeAlign: false);

    [MenuItem("FENS/Step 2 only/Setup Audio (Steam Audio + rear attenuation)")]
    static void MenuAudio() => RunStep2Part(includeModel: true, includeAudio: true, includeAlign: false);

    [MenuItem("FENS/Step 2 only/Setup Alignment + Passthrough")]
    static void MenuAlign() => RunStep2Part(includeModel: true, includeAudio: false, includeAlign: true);

    [MenuItem("FENS/Re-export Steam Audio geometry")]
    static void MenuExport()
    {
        SteamAudioManager.ExportActiveScene();
        Debug.Log("[FENS setup] Steam Audio geometry re-exported. Save the scene (Cmd+S).");
    }

    static void RunStep2Part(bool includeModel, bool includeAudio, bool includeAlign)
    {
        var rm = Object.FindFirstObjectByType<RouteManager>(FindObjectsInactive.Include);
        var exp = Object.FindFirstObjectByType<ExperimentController>(FindObjectsInactive.Include);
        if (rm == null || exp == null)
        {
            EditorUtility.DisplayDialog("FENS setup", "Run FENS > Setup Experiment Scene first.", "OK");
            return;
        }
        var log = new List<string>();
        int layer = includeModel ? SetupModel(exp, log) : LayerMask.NameToLayer(ModelLayerName);
        if (includeAudio) SetupAudio(rm, exp.logger, layer, log);
        if (includeAlign) SetupAlignment(rm, exp, exp.feedback, log);
        Finish(rm.gameObject, log);
    }

    static void SetupStep2(RouteManager rm, ExperimentController exp, TrialLogger logger,
                           OperatorFeedback feedback, List<string> log)
    {
        int layer = SetupModel(exp, log);
        SetupAudio(rm, logger, layer, log);
        SetupAlignment(rm, exp, feedback, log);
    }

    static void Finish(GameObject anyInScene, List<string> log)
    {
        EditorSceneManager.MarkSceneDirty(anyInScene.scene);
        AssetDatabase.SaveAssets();
        string msg = string.Join("\n", log) + "\n\nNow save the scene: File > Save (Cmd+S).";
        Debug.Log("[FENS setup]\n" + msg);
        EditorUtility.DisplayDialog("FENS setup done", msg, "OK");
    }

    // ------------------------------------------------------------------ model

    /// Returns the collider layer index (or -1).
    static int SetupModel(ExperimentController exp, List<string> log)
    {
        var root = FindModelRoot();
        if (root == null)
        {
            log.Add($"WARNING: FENS model ({ModelPrefabPath}) not found in the scene.");
            return -1;
        }

        int layer = EnsureLayer(ModelLayerName, log);
        if (layer < 0) return -1;

        // own layer for the whole model + MeshColliders (line-of-sight raycasts only)
        int colliders = 0, relayered = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var go = t.gameObject;
            if (go.layer != layer)
            {
                Undo.RecordObject(go, "FENS setup");
                go.layer = layer;
                relayered++;
            }
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null && go.GetComponent<Collider>() == null)
            {
                var mc = Undo.AddComponent<MeshCollider>(go);
                mc.sharedMesh = mf.sharedMesh;
                colliders++;
            }
        }
        if (relayered > 0) log.Add($"Model: {relayered} objects moved to layer '{ModelLayerName}' ({layer}).");
        if (colliders > 0) log.Add($"Model: added {colliders} MeshColliders.");

        // the model layer collides with nothing (the XR rig can never be pushed by virtual walls)
        for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(layer, i, true);
        log.Add($"Physics: layer '{ModelLayerName}' set to collide with no layer (also enforced at runtime).");

        // visibility + semi-transparent material for checking the alignment
        var ghost = EnsureGhostMaterial(log);
        var vis = GetOrAdd<ModelVisibility>(root, log);
        Undo.RecordObject(vis, "FENS setup");
        vis.transparentMaterial = ghost;
        vis.colliderLayer = layer;
        EditorUtility.SetDirty(vis);

        if (exp != null)
        {
            Undo.RecordObject(exp, "FENS setup");
            exp.modelVisibility = vis;
            EditorUtility.SetDirty(exp);
        }
        return layer;
    }

    static GameObject FindModelRoot()
    {
        foreach (var g in Object.FindObjectsByType<SteamAudioGeometry>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g.gameObject) != ModelPrefabPath) continue;
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(g.gameObject);
            return root != null ? root : g.gameObject;
        }
        return null;
    }

    static int EnsureLayer(string name, List<string> log)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0) return existing;

        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) { log.Add("WARNING: TagManager not found."); return -1; }
        var tagManager = new SerializedObject(assets[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var p = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(p.stringValue)) continue;
            p.stringValue = name;
            tagManager.ApplyModifiedProperties();
            log.Add($"Created layer '{name}' (index {i}).");
            return i;
        }
        log.Add("WARNING: no free layer slot (8-31).");
        return -1;
    }

    static Material EnsureGhostMaterial(List<string> log)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(GhostMaterialPath);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) { log.Add("WARNING: URP Unlit shader not found; no transparent material."); return null; }

        mat = new Material(shader) { name = "FENS_ModelGhost" };
        // URP Unlit, Surface Type = Transparent, Blend = Alpha, both sides, no depth write
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_Cull", (float)CullMode.Off);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetColor("_BaseColor", new Color(0.3f, 0.8f, 1f, 0.25f));

        if (!AssetDatabase.IsValidFolder(AssetFolder)) AssetDatabase.CreateFolder("Assets", "Experiment");
        AssetDatabase.CreateAsset(mat, GhostMaterialPath);
        log.Add($"Created {GhostMaterialPath} (semi-transparent, for alignment checks).");
        return mat;
    }

    // ------------------------------------------------------------------ audio

    static void SetupAudio(RouteManager rm, TrialLogger logger, int modelLayer, List<string> log)
    {
        if (rm.targetSound == null) { log.Add("WARNING: RouteManager has no TargetSound."); return; }
        var go = rm.targetSound.gameObject;
        var src = go.GetComponent<AudioSource>();
        var sa = go.GetComponent<SteamAudioSource>();
        if (src == null || sa == null) { log.Add("WARNING: TargetSound needs an AudioSource and a Steam Audio Source."); return; }

        // --- AudioSource: route to the mixer group and use the original's distance curve
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        Undo.RecordObject(src, "FENS setup");
        if (mixer != null)
        {
            var groups = mixer.FindMatchingGroups(MixerGroupName);
            if (groups != null && groups.Length > 0)
            {
                src.outputAudioMixerGroup = groups[0];
                log.Add($"TargetSound output -> {mixer.name}/{groups[0].name}.");
            }
            else log.Add($"WARNING: mixer group '{MixerGroupName}' not found.");
        }
        else log.Add($"WARNING: {MixerPath} not found.");

        src.playOnAwake = false;
        src.spatialize = true;
        src.minDistance = 1f;
        src.maxDistance = 500f;
        src.rolloffMode = AudioRolloffMode.Custom;
        src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, OriginalRolloffCurve());
        EditorUtility.SetDirty(src);

        // --- Steam Audio Source: like the target sounds in the original FENS-2 scene
        Undo.RecordObject(sa, "FENS setup");
        sa.directBinaural = true;
        sa.distanceAttenuation = true;
        sa.distanceAttenuationInput = DistanceAttenuationInput.PhysicsBased;
        sa.airAbsorption = true;
        sa.airAbsorptionInput = AirAbsorptionInput.SimulationDefined;
        sa.directivity = true;                                   // dipole weight 0 = omnidirectional
        sa.directivityInput = DirectivityInput.SimulationDefined;
        sa.occlusion = true;
        sa.occlusionInput = OcclusionInput.SimulationDefined;
        sa.occlusionType = OcclusionType.Raycast;
        sa.transmission = true;                                  // sound passes through walls ...
        sa.transmissionType = TransmissionType.FrequencyDependent; // ... high frequencies less -> muffled
        sa.transmissionInput = TransmissionInput.SimulationDefined;
        sa.maxTransmissionSurfaces = 3;
        sa.reflections = true;                                   // final value set by SteamAudioReflectionsSwitch
        sa.reflectionsType = ReflectionsType.Realtime;
        sa.applyHRTFToReflections = true;
        sa.reflectionsMixLevel = 2.04f;
        EditorUtility.SetDirty(sa);
        log.Add("TargetSound Steam Audio: distance + air absorption, occlusion, frequency-dependent transmission (3 surfaces), real-time reflections.");

        // --- listener reverb (as in the original: SteamAudioListener on the Main Camera)
        SteamAudioListener listener = null;
        if (rm.playerHead != null)
        {
            listener = GetOrAdd<SteamAudioListener>(rm.playerHead.gameObject, log);
            Undo.RecordObject(listener, "FENS setup");
            listener.applyReverb = true;
            listener.reverbType = ReverbType.Realtime;
            EditorUtility.SetDirty(listener);
        }

        // --- reflections on/off per platform
        var sw = GetOrAdd<SteamAudioReflectionsSwitch>(go, log);
        Undo.RecordObject(sw, "FENS setup");
        sw.source = sa;
        sw.listener = listener;
        EditorUtility.SetDirty(sw);

        // --- rear attenuation (soundModulation from the original)
        var rear = GetOrAdd<RearAttenuation>(go, log);
        Undo.RecordObject(rear, "FENS setup");
        rear.mixer = mixer;
        rear.exposedParameter = "AttenVolume";
        rear.head = rm.playerHead;
        rear.occluderLayers = modelLayer >= 0 ? (LayerMask)(1 << modelLayer) : (LayerMask)0;
        rear.maxAttenuationDb = -10f;
        EditorUtility.SetDirty(rear);
        if (modelLayer < 0) log.Add("WARNING: RearAttenuation has no occluder layer (model setup failed).");

        if (logger != null && !logger.extraColumnSources.Contains(rear))
        {
            Undo.RecordObject(logger, "FENS setup");
            logger.extraColumnSources.Add(rear);
            EditorUtility.SetDirty(logger);
            log.Add("TrialLogger: added RearAttenuation columns (sound_azimuth_deg, line_of_sight, rear_atten_db).");
        }

        // --- wall material: frequency-dependent transmission, then re-export the static geometry
        var wall = EnsureWallMaterial(log);
        var root = FindModelRoot();
        if (wall != null && root != null)
        {
            foreach (var geom in root.GetComponentsInChildren<SteamAudioGeometry>(true))
            {
                if (geom.material == wall) continue;
                Undo.RecordObject(geom, "FENS setup");
                geom.material = wall;
                EditorUtility.SetDirty(geom);
                log.Add($"Steam Audio Geometry on '{geom.gameObject.name}': material -> {wall.name}.");
            }
            SteamAudioManager.ExportActiveScene();
            log.Add("Steam Audio static geometry re-exported (see Console).");
        }
    }

    /// Original used the 'Custom' material (transmission 0.07 / 0.065 / 0.065: almost flat, so only quieter).
    /// Here about the same mid level, but more low and less high frequency gets through -> muffled.
    static SteamAudioMaterial EnsureWallMaterial(List<string> log)
    {
        var m = AssetDatabase.LoadAssetAtPath<SteamAudioMaterial>(WallMaterialPath);
        if (m != null) return m;
        m = ScriptableObject.CreateInstance<SteamAudioMaterial>();
        // absorption / scattering as the original 'Custom' material
        m.lowFreqAbsorption = 0f;
        m.midFreqAbsorption = 0f;
        m.highFreqAbsorption = 0f;
        m.scattering = 0.1f;
        // transmission (fraction of energy through ONE surface): low 0.15, mid 0.06, high 0.02
        m.lowFreqTransmission = 0.15f;
        m.midFreqTransmission = 0.06f;
        m.highFreqTransmission = 0.02f;
        if (!AssetDatabase.IsValidFolder(AssetFolder)) AssetDatabase.CreateFolder("Assets", "Experiment");
        AssetDatabase.CreateAsset(m, WallMaterialPath);
        log.Add($"Created {WallMaterialPath} (transmission low/mid/high = 0.15 / 0.06 / 0.02).");
        return m;
    }

    /// Custom rolloff curve of the target sounds in the original FENS-2 scene (time = distance / maxDistance).
    static AnimationCurve OriginalRolloffCurve()
    {
        return new AnimationCurve(
            new Keyframe(0.002f, 1f),
            new Keyframe(0.004f, 0.5f),
            new Keyframe(0.005333527f, 0.25f),
            new Keyframe(0.10666391f, 0.13287354f),
            new Keyframe(1f, 0.101989746f));
    }

    // ------------------------------------------------------------------ alignment + passthrough

    static void SetupAlignment(RouteManager rm, ExperimentController exp, OperatorFeedback feedback, List<string> log)
    {
        if (rm.xrOrigin != null)
        {
            var wa = GetOrAdd<WorldAlignment>(rm.xrOrigin.gameObject, log);
            Undo.RecordObject(wa, "FENS setup");
            wa.xrOrigin = rm.xrOrigin;
            wa.head = rm.playerHead;
            wa.floorY = rm.floorY;
            EditorUtility.SetDirty(wa);

            Undo.RecordObject(exp, "FENS setup");
            exp.worldAlignment = wa;
            exp.placePlayerOnStart = false; // the operator aligns with the Align key
            EditorUtility.SetDirty(exp);
            log.Add("ExperimentController: automatic placement at trial start OFF (use the Align key).");
        }
        else log.Add("WARNING: no XR Origin; WorldAlignment not added.");

        if (rm.playerHead != null)
        {
            var pc = GetOrAdd<PassthroughController>(rm.playerHead.gameObject, log);
            Undo.RecordObject(pc, "FENS setup");
            pc.targetCamera = rm.playerHead.GetComponent<Camera>();
            EditorUtility.SetDirty(pc);
        }

        if (feedback != null)
        {
            Undo.RecordObject(feedback, "FENS setup");
            feedback.beepVolume = 0.8f;
            EditorUtility.SetDirty(feedback);
        }
    }
}
