#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

/// <summary>
/// Editor only (mode A, Windows + VIVE Hub). The XR Interaction Simulator is set to start automatically in the Editor
/// (for testing on the Mac without a headset). But it does NOT step aside when a real headset is connected:
/// by default it even REMOVES the real headset's HMD device from the Input System ("Remove Other HMD Devices"),
/// which would break head tracking in Play mode.
/// This guard runs after XR has started (BeforeSceneLoad comes after XR start) and before the simulator is created
/// (AfterSceneLoad). If a real headset is active, the automatic simulator start is switched off for this Play session
/// only (in memory; the setting is restored when Play stops and the asset file is never changed).
/// On the Mac (no headset) nothing changes and the simulator starts as before.
/// </summary>
static class XRSimulatorHeadsetGuard
{
    const string SettingsTypeName = "UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings";
    static Object settings;
    static FieldInfo autoField;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void BeforeSceneLoad()
    {
        if (!XRSettings.isDeviceActive) return; // no real headset (e.g. Mac): keep the simulator

        var type = typeof(XRInteractionSimulator).Assembly.GetType(SettingsTypeName);
        var getInstance = type?.GetMethod("GetInstanceOrLoadOnly", BindingFlags.NonPublic | BindingFlags.Static);
        autoField = type?.GetField("m_AutomaticallyInstantiateSimulatorPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
        settings = getInstance?.Invoke(null, null) as Object;
        if (settings == null || autoField == null)
        {
            Debug.LogWarning("[XRSimulatorGuard] Could not read the XR Interaction Simulator settings. " +
                             "If head tracking does not work with the headset, turn the simulator off in " +
                             "Project Settings > XR Plug-in Management > XR Interaction Toolkit.");
            return;
        }
        if (!(bool)autoField.GetValue(settings)) return; // simulator not set to start anyway

        autoField.SetValue(settings, false);
        EditorApplication.playModeStateChanged += Restore;
        Debug.Log($"[XRSimulatorGuard] Real headset active ({XRSettings.loadedDeviceName}): " +
                  "XR Interaction Simulator NOT started in this Play session.");
    }

    static void Restore(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= Restore;
        if (settings != null && autoField != null) autoField.SetValue(settings, true);
        settings = null;
    }
}
#endif
