using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

/// <summary>
/// Android (standalone headset) OpenXR setup done through the Unity API instead of clicking in
/// Project Settings > XR Plug-in Management > OpenXR > Android tab (those clicks were not being saved).
///   FENS > Android > Configure OpenXR for VIVE   : changes the Android OpenXR features and saves them
///   FENS > Android > Report XR + Player settings : only prints the current state (changes nothing)
/// Player settings (min API, graphics API, ...) are only REPORTED here, never changed.
/// </summary>
public static class FensAndroidXRSetup
{
    const BuildTargetGroup Android = BuildTargetGroup.Android;
    const string ViveFeatureSetId = "com.htc.vive.openxr.featureset.vivexr";
    const string Tag = "[FENS Android XR]";

    [MenuItem("FENS/Android/Configure OpenXR for VIVE")]
    static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("FENS", "Stop Play mode first (OpenXR features cannot change while XR is running).", "OK");
            return;
        }

        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(Android);
        if (settings == null)
        {
            Debug.LogError($"{Tag} No OpenXR settings for Android. Is OpenXR ticked in XR Plug-in Management > Android tab?");
            return;
        }

        var log = new StringBuilder();
        var changed = new List<Object>();

        // 1) "VIVE XR Support" feature group selected for Android (so the Project Settings UI shows it ticked)
        var set = OpenXRFeatureSetManager.GetFeatureSetWithId(Android, ViveFeatureSetId);
        if (set == null)
            log.AppendLine("! Feature group 'VIVE XR Support' not found (is the VIVE OpenXR package installed?)");
        else if (!set.isEnabled)
        {
            set.isEnabled = true;
            OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(Android);
            log.AppendLine("+ Feature group 'VIVE XR Support' selected");
        }

        // 2) Features: on / off
        SetFeature<VIVE.OpenXR.VIVEFocus3Feature>(settings, true, log, changed);                    // VIVE XR Support
        SetFeature<VIVE.OpenXR.VIVEFocus3Profile>(settings, true, log, changed);                    // VIVE Focus 3 Controller Interaction
        SetFeature<VIVE.OpenXR.Passthrough.VivePassthrough>(settings, true, log, changed);          // VIVE XR Passthrough (the current one)
        SetFeatureByTypeName(settings, "ViveCompositionLayerPassthrough", false, log, changed);    // ...(Passthrough) (Deprecated) -> off
        SetFeature<UnityEngine.XR.OpenXR.Features.MetaQuestSupport.MetaQuestFeature>(settings, false, log, changed); // Meta Quest Support -> off
        // (Only one feature may own the Android OpenXR loader: VIVE XR Support and Meta Quest Support cannot both be on.)

        // 3) Save
        foreach (var o in changed) EditorUtility.SetDirty(o);
        EditorUtility.SetDirty(settings);
        var pkg = AssetDatabase.LoadMainAssetAtPath("Assets/XR/Settings/Open XR Package Settings.asset");
        if (pkg != null) EditorUtility.SetDirty(pkg);
        var editorSettings = AssetDatabase.LoadMainAssetAtPath("Assets/XR/Settings/OpenXR Editor Settings.asset");
        if (editorSettings != null) EditorUtility.SetDirty(editorSettings);
        AssetDatabase.SaveAssets();

        Debug.Log($"{Tag} Android OpenXR configured and saved:\n{(log.Length > 0 ? log.ToString() : "(everything was already set)\n")}");
        Report();
    }

    // Values approved by the team on 2026-11-11 (see CLAUDE.md / Docs/RUNNING.md)
    const AndroidSdkVersions ApprovedMinSdk = AndroidSdkVersions.AndroidApiLevel29;
    static readonly GraphicsDeviceType[] ApprovedGraphicsApis = { GraphicsDeviceType.OpenGLES3 };
    const UIOrientation ApprovedOrientation = UIOrientation.LandscapeLeft;
    const string ApprovedPackageName = "com.ens492.fensvivenav";

    [MenuItem("FENS/Android/Apply approved Player settings")]
    static void ApplyApprovedPlayerSettings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("FENS", "Stop Play mode first.", "OK");
            return;
        }
        var nbt = NamedBuildTarget.Android;
        var sb = new StringBuilder();
        sb.AppendLine($"{Tag} Approved Android Player settings (before -> after):");

        // Minimum API level
        var minBefore = PlayerSettings.Android.minSdkVersion;
        PlayerSettings.Android.minSdkVersion = ApprovedMinSdk;
        sb.AppendLine($"   Minimum API Level  : {minBefore} -> {PlayerSettings.Android.minSdkVersion}");

        // Graphics APIs (Auto off, only OpenGLES3)
        bool autoBefore = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
        string apisBefore = autoBefore ? "Auto" : string.Join(", ", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android));
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, ApprovedGraphicsApis);
        sb.AppendLine($"   Graphics APIs      : {apisBefore} -> {string.Join(", ", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android))}");

        // Default orientation (Player Settings > Resolution and Presentation; shared by Android and iOS)
        var oriBefore = PlayerSettings.defaultInterfaceOrientation;
        PlayerSettings.defaultInterfaceOrientation = ApprovedOrientation;
        sb.AppendLine($"   Default Orientation: {oriBefore} -> {PlayerSettings.defaultInterfaceOrientation}");

        // Package name (Android only; the headset log folder path changes with it)
        string pkgBefore = PlayerSettings.GetApplicationIdentifier(nbt);
        PlayerSettings.SetApplicationIdentifier(nbt, ApprovedPackageName);
        sb.AppendLine($"   Package Name       : {pkgBefore} -> {PlayerSettings.GetApplicationIdentifier(nbt)}");

        if (PlayerSettings.colorSpace != ColorSpace.Linear)
            sb.AppendLine("   WARNING: color space is not Linear (OpenGLES with OpenXR needs Linear). Not changed.");

        AssetDatabase.SaveAssets();
        sb.AppendLine("Saved. Also do File > Save Project.");
        Debug.Log(sb.ToString());
        Report();
    }

    [MenuItem("FENS/Android/Report XR + Player settings")]
    static void Report()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{Tag} ANDROID REPORT (nothing is changed by this report)");

        // --- XR Plug-in Management
        var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(Android);
        var loaders = general != null && general.Manager != null
            ? string.Join(", ", general.Manager.activeLoaders.Where(l => l != null).Select(l => l.name))
            : "(none)";
        sb.AppendLine($"XR Plug-in Management (Android): loaders = {loaders}; Initialize XR on Startup = {(general != null && general.InitManagerOnStart)}");

        // --- OpenXR features
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(Android);
        if (settings == null)
        {
            sb.AppendLine("OpenXR settings for Android: MISSING");
        }
        else
        {
            var set = OpenXRFeatureSetManager.GetFeatureSetWithId(Android, ViveFeatureSetId);
            sb.AppendLine($"Feature group 'VIVE XR Support': {(set == null ? "not found" : set.isEnabled ? "ON" : "off")}");
            sb.AppendLine($"Render mode: {settings.renderMode}");

            var all = settings.GetFeatures<OpenXRFeature>();
            sb.AppendLine("Interaction profiles ON:");
            foreach (var f in all.Where(f => f.enabled && f is OpenXRInteractionFeature))
                sb.AppendLine($"   [x] {UiName(f)}");
            sb.AppendLine("Features ON:");
            foreach (var f in all.Where(f => f.enabled && !(f is OpenXRInteractionFeature)))
                sb.AppendLine($"   [x] {UiName(f)}");
            sb.AppendLine("Checked (must be OFF):");
            foreach (var f in all.Where(f => f is UnityEngine.XR.OpenXR.Features.MetaQuestSupport.MetaQuestFeature
                                          || f.GetType().Name == "ViveCompositionLayerPassthrough"))
                sb.AppendLine($"   [{(f.enabled ? "x" : " ")}] {UiName(f)}{(f.enabled ? "   <-- PROBLEM: should be off" : "")}");
        }

        // --- Player settings (read only)
        var nbt = NamedBuildTarget.Android;
        sb.AppendLine("Player Settings (Android) - READ ONLY:");
        sb.AppendLine($"   Package name         : {PlayerSettings.GetApplicationIdentifier(nbt)}");
        sb.AppendLine($"   Scripting backend    : {PlayerSettings.GetScriptingBackend(nbt)}");
        sb.AppendLine($"   Target architectures : {PlayerSettings.Android.targetArchitectures}");
        sb.AppendLine($"   Minimum API level    : {PlayerSettings.Android.minSdkVersion}");
        sb.AppendLine($"   Target API level     : {PlayerSettings.Android.targetSdkVersion}");
        bool autoApi = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
        sb.AppendLine($"   Graphics APIs        : {(autoApi ? "Auto" : string.Join(", ", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)))}");
        sb.AppendLine($"   Color space          : {PlayerSettings.colorSpace}");
        sb.AppendLine($"   Default orientation  : {PlayerSettings.defaultInterfaceOrientation}");
        sb.AppendLine($"   Texture compression  : {EditorUserBuildSettings.androidBuildSubtarget}");
        sb.AppendLine($"   Active build target  : {EditorUserBuildSettings.activeBuildTarget}");
        bool androidModule = BuildPipeline.IsBuildTargetSupported(Android, BuildTarget.Android);
        sb.AppendLine($"   Android Build Support module installed: {(androidModule ? "yes" : "NO (Unity Hub > Installs > Add modules)")}");

        // --- OpenXR project validation for Android (same list as Project Settings > XR Plug-in Management > Project Validation)
        var issues = new List<OpenXRFeature.ValidationRule>();
        OpenXRProjectValidation.GetCurrentValidationIssues(issues, Android);
        sb.AppendLine(issues.Count == 0 ? "OpenXR validation (Android): no issues" : $"OpenXR validation (Android): {issues.Count} issue(s)");
        foreach (var i in issues)
            sb.AppendLine($"   {(i.error ? "ERROR  " : "warning")} {i.message}");

        Debug.Log(sb.ToString());
    }

    // ---------------------------------------------------------------- helpers

    static void SetFeature<T>(OpenXRSettings settings, bool on, StringBuilder log, List<Object> changed) where T : OpenXRFeature
    {
        var f = settings.GetFeature<T>();
        if (f == null) { log.AppendLine($"! {typeof(T).Name} not found in Android settings"); return; }
        Apply(f, on, log, changed);
    }

    static void SetFeatureByTypeName(OpenXRSettings settings, string typeName, bool on, StringBuilder log, List<Object> changed)
    {
        var f = settings.GetFeatures<OpenXRFeature>().FirstOrDefault(x => x.GetType().Name == typeName);
        if (f == null) { log.AppendLine($"! {typeName} not found in Android settings"); return; }
        Apply(f, on, log, changed);
    }

    static void Apply(OpenXRFeature f, bool on, StringBuilder log, List<Object> changed)
    {
        if (f.enabled == on) return;
        f.enabled = on;
        if (f.enabled != on)
        {
            log.AppendLine($"! Could not turn {(on ? "ON" : "OFF")}: {UiName(f)} (another feature group may require it)");
            return;
        }
        changed.Add(f);
        log.AppendLine($"{(on ? "+ ON " : "- OFF")} {UiName(f)}");
    }

    static string UiName(OpenXRFeature f)
    {
        var attr = f.GetType().GetCustomAttribute<OpenXRFeatureAttribute>();
        return attr != null && !string.IsNullOrEmpty(attr.UiName) ? attr.UiName : f.name;
    }
}
