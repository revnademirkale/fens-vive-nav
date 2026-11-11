using SteamAudio;
using UnityEngine;

/// <summary>
/// One place to turn Steam Audio reflections on/off per platform
/// (real-time reflections can be heavy on the standalone headset).
/// Controls the reflections of the target sound and the listener reverb.
/// Change the checkboxes in the Inspector; they are applied when Play starts.
/// Put this on the TargetSound object.
/// Runs before the Steam Audio components (SteamAudioListener reads applyReverb in OnEnable).
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SteamAudioReflectionsSwitch : MonoBehaviour
{
    [Tooltip("Editor and Windows PC streaming")]
    public bool reflectionsOnPC = true;
    [Tooltip("Standalone headset (Android build)")]
    public bool reflectionsOnAndroid = true;

    public SteamAudioSource source;
    [Tooltip("SteamAudioListener on the Main Camera (listener reverb)")]
    public SteamAudioListener listener;

    public bool ReflectionsOn =>
        Application.platform == RuntimePlatform.Android ? reflectionsOnAndroid : reflectionsOnPC;

    void Awake()
    {
        bool on = ReflectionsOn;
        if (source != null) source.reflections = on;
        if (listener != null) listener.applyReverb = on;
        Debug.Log($"[Audio] Steam Audio reflections {(on ? "ON" : "OFF")} ({Application.platform})");
    }
}
