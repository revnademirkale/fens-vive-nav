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
/// META QUEST version of FensAndroidXRSetup (the VIVE file is not changed).
/// Switches the Android OpenXR setup between headsets in the SAME project:
///   FENS > Android (Meta Quest) > Configure OpenXR for Meta Quest : Meta features ON, VIVE features OFF
///   FENS > Android (Meta Quest) > Apply Quest Player settings     : same values as VIVE, Quest package name
///   FENS > Android (Meta Quest) > Report                          : prints the current state
/// To go back to VIVE: FENS > Android > Configure OpenXR for VIVE (+ Apply approved Player settings).
/// (Only one feature may own the Android OpenXR loader: VIVE XR Support OR Meta Quest Support.)
/// Feature types are found by name, so this file compiles with or without each package.
/// </summary>
public static class FensQuestAndroidXRSetup
{
    const BuildTargetGroup Android = BuildTargetGroup.Android;
    const string MetaFeatureSetId = "com.unity.openxr.featureset.meta";      // Unity OpenXR: Meta
    const string ViveFeatureSetId = "com.htc.vive.openxr.featureset.vivexr"; // VIVE OpenXR
    const string QuestPackageName = "com.ens492.fensquestnav";
    const string Tag = "[FENS Quest XR]";

    // Meta Quest: ON
    static readonly string[] MetaOn =
    {
        "MetaQuestFeature",                    // Meta Quest Support (owns the Android loader)
        "ARSessionFeature",                    // Meta Quest: AR Session
        "ARCameraFeature",                     // Meta Quest: AR Camera (Passthrough)
        "OculusTouchControllerProfile",        // Oculus Touch controllers
        "MetaQuestTouchPlusControllerProfile", // Quest 3 / 3S controllers
        "MetaQuestTouchProControllerProfile",  // Quest Pro controllers
    };
    // VIVE: OFF while building for Quest
    static readonly string[] ViveOff =
    {
        "VIVEFocus3Feature",                   // VIVE XR Support (owns the Android loader)
        "VivePassthrough",                     // VIVE XR Passthrough
        "ViveCompositionLayerPassthrough",     // VIVE passthrough (deprecated)
        "VIVEFocus3Profile",                   // VIVE Focus 3 controllers
    };

    [MenuItem("FENS/Android (Meta Quest)/Configure OpenXR for Meta Quest")]
    static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("FENS", "Stop Play mode first.", "OK");
            return;
        }
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(Android);
        if (settings == null)
        {
            Debug.LogError($"{Tag} No OpenXR settings for Android. Tick OpenXR in XR Plug-in Management > Android.");
            return;
        }
        var log = new StringBuilder();

        // 1) VIVE off first (loader owner), then Meta on
        var vive = OpenXRFeatureSetManager.GetFeatureSetWithId(Android, ViveFeatureSetId);
        if (vive != null && vive.isEnabled) { vive.isEnabled = false; log.AppendLine("- Feature group 'VIVE XR Support' unselected"); }
        foreach (var n in ViveOff) Set(settings, n, false, log);

        var meta = OpenXRFeatureSetManager.GetFeatureSetWithId(Android, MetaFeatureSetId);
        if (meta == null) log.AppendLine("! Feature group 'Meta Quest' not found (is Unity OpenXR: Meta installed?)");
        else if (!meta.isEnabled) { meta.isEnabled = true; log.AppendLine("+ Feature group 'Meta Quest' selected"); }
        OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(Android);
        foreach (var n in MetaOn) Set(settings, n, true, log);

        // 2) Save
        EditorUtility.SetDirty(settings);
        foreach (var f in settings.GetFeatures<OpenXRFeature>()) EditorUtility.SetDirty(f);
        AssetDatabase.SaveAssets();

        Debug.Log($"{Tag} Android OpenXR set to META QUEST and saved:\n{(log.Length > 0 ? log.ToString() : "(everything was already set)\n")}");
        Report();
    }

    [MenuItem("FENS/Android (Meta Quest)/Apply Quest Player settings")]
    static void ApplyPlayerSettings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("FENS", "Stop Play mode first.", "OK");
            return;
        }
        var nbt = NamedBuildTarget.Android;
        var sb = new StringBuilder($"{Tag} Quest Player settings (before -> after):\n");

        // Same values as the VIVE setup (team decision 2026-11-11), only the package name differs
        var min = PlayerSettings.Android.minSdkVersion;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        sb.AppendLine($"   Minimum API Level  : {min} -> {PlayerSettings.Android.minSdkVersion}");

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        sb.AppendLine($"   Graphics APIs      : {string.Join(", ", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android))}");

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        sb.AppendLine($"   Default Orientation: {PlayerSettings.defaultInterfaceOrientation}");

        string pkg = PlayerSettings.GetApplicationIdentifier(nbt);
        PlayerSettings.SetApplicationIdentifier(nbt, QuestPackageName);
        sb.AppendLine($"   Package Name       : {pkg} -> {PlayerSettings.GetApplicationIdentifier(nbt)}");

        AssetDatabase.SaveAssets();
        sb.AppendLine("Saved. Do not commit ProjectSettings.asset for this change (see Docs/RUNNING_QUEST.md).");
        Debug.Log(sb.ToString());
    }

    [MenuItem("FENS/Android (Meta Quest)/Report")]
    static void Report()
    {
        var sb = new StringBuilder($"{Tag} REPORT (nothing is changed by this report)\n");
        var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(Android);
        var loaders = general != null && general.Manager != null
            ? string.Join(", ", general.Manager.activeLoaders.Where(l => l != null).Select(l => l.name)) : "(none)";
        sb.AppendLine($"Loaders (Android): {loaders}");

        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(Android);
        if (settings != null)
        {
            var all = settings.GetFeatures<OpenXRFeature>();
            sb.AppendLine("Must be ON (Quest):");
            foreach (var n in MetaOn) Line(sb, all, n, true);
            sb.AppendLine("Must be OFF (VIVE):");
            foreach (var n in ViveOff) Line(sb, all, n, false);
        }
        sb.AppendLine($"Package name: {PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)}");
        sb.AppendLine($"Active build target: {EditorUserBuildSettings.activeBuildTarget}");

        var issues = new List<OpenXRFeature.ValidationRule>();
        OpenXRProjectValidation.GetCurrentValidationIssues(issues, Android);
        sb.AppendLine($"OpenXR validation (Android): {issues.Count} issue(s)");
        foreach (var i in issues) sb.AppendLine($"   {(i.error ? "ERROR  " : "warning")} {i.message}");
        Debug.Log(sb.ToString());
    }

    // ---------------------------------------------------------------- helpers

    static void Set(OpenXRSettings settings, string typeName, bool on, StringBuilder log)
    {
        var f = settings.GetFeatures<OpenXRFeature>().FirstOrDefault(x => x != null && x.GetType().Name == typeName);
        if (f == null) return; // package not installed: nothing to do
        if (f.enabled == on) return;
        f.enabled = on;
        log.AppendLine(f.enabled == on ? $"{(on ? "+ ON " : "- OFF")} {UiName(f)}"
                                       : $"! Could not turn {(on ? "ON" : "OFF")}: {UiName(f)}");
    }

    static void Line(StringBuilder sb, OpenXRFeature[] all, string typeName, bool shouldBeOn)
    {
        var f = all.FirstOrDefault(x => x != null && x.GetType().Name == typeName);
        if (f == null) { sb.AppendLine($"   [-] {typeName} (not installed)"); return; }
        bool ok = f.enabled == shouldBeOn;
        sb.AppendLine($"   [{(f.enabled ? "x" : " ")}] {UiName(f)}{(ok ? "" : "   <-- PROBLEM")}");
    }

    static string UiName(OpenXRFeature f)
    {
        var a = f.GetType().GetCustomAttribute<OpenXRFeatureAttribute>();
        return a != null && !string.IsNullOrEmpty(a.UiName) ? a.UiName : f.name;
    }
}
