using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Adapted from soundModulation.cs of the original HoloLens 2 project:
/// when the target sound is BEHIND the listener (more than 90 degrees from the facing direction)
/// and there is a clear line of sight, the volume is lowered linearly from 0 dB (at 90 deg)
/// to maxAttenuationDb (at 180 deg). With a wall in between it stays at 0 dB
/// (Steam Audio occlusion/transmission already handles that case).
/// Line of sight = no FENS model collider between head and sound.
/// The value is written to the exposed AudioMixer parameter (AttenVolume in NewAudioMixer).
/// Put this on the TargetSound object. It also adds 3 columns to the trial CSV.
/// </summary>
public class RearAttenuation : MonoBehaviour, ILogColumnProvider
{
    public AudioMixer mixer;
    public string exposedParameter = "AttenVolume";

    [Tooltip("Main Camera (the participant's head)")]
    public Transform head;

    [Tooltip("Layers that block the line of sight (the FENS model collider layer)")]
    public LayerMask occluderLayers;

    public float maxAttenuationDb = -10f;

    public bool LineOfSight { get; private set; }
    public float AzimuthDeg { get; private set; }
    public float AttenuationDb { get; private set; }

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    void Update()
    {
        if (mixer == null || head == null) return;

        // Horizontal angle between where the head faces and where the sound is (0 = ahead, +-180 = behind)
        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        Vector3 toSound = Vector3.ProjectOnPlane(transform.position - head.position, Vector3.up);
        AzimuthDeg = (forward.sqrMagnitude > 1e-6f && toSound.sqrMagnitude > 1e-6f)
            ? Vector3.SignedAngle(forward, toSound, Vector3.up)
            : 0f;

        LineOfSight = !IsBlocked(head.position, transform.position);

        float behind = Mathf.Abs(AzimuthDeg);
        AttenuationDb = (LineOfSight && behind > 90f)
            ? Mathf.Lerp(0f, maxAttenuationDb, (behind - 90f) / 90f)
            : 0f;

        // Value jumps instead of fading, as in the original (a jump is perceived as a system change)
        mixer.SetFloat(exposedParameter, AttenuationDb);
    }

    void OnDisable()
    {
        if (mixer != null) mixer.SetFloat(exposedParameter, 0f);
    }

    /// Two linecasts (both directions), because single-sided wall faces are only hit from the front.
    bool IsBlocked(Vector3 a, Vector3 b)
    {
        int mask = occluderLayers.value;
        if (mask == 0) return false;
        return Physics.Linecast(a, b, mask, QueryTriggerInteraction.Ignore) ||
               Physics.Linecast(b, a, mask, QueryTriggerInteraction.Ignore);
    }

    // ---- extra CSV columns
    public IEnumerable<string> ColumnNames => new[] { "sound_azimuth_deg", "line_of_sight", "rear_atten_db" };

    public void AppendValues(List<string> row)
    {
        row.Add(AzimuthDeg.ToString("0.##", Inv));
        row.Add(LineOfSight ? "1" : "0");
        row.Add(AttenuationDb.ToString("0.##", Inv));
    }
}
