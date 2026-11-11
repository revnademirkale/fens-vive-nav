using System;
using UnityEngine;

/// <summary>
/// Shows one navigation target on FENS floor 2 (original version by Revna; extended for 4 conditions):
/// - draws the path as a line on the floor (VisualOnly / AudioVisual)
/// - moves the target sound to the end of the path and plays it (AudioOnly / AudioVisual)
/// - detects when the participant's head reaches the target and raises <see cref="Arrived"/>
/// The ExperimentController decides which target/condition is active and when a trial runs.
/// Outside Play mode the "Editor preview" fields control what is drawn in the Scene view.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class RouteManager : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("TargetSet asset with the warm-up + 4 targets")]
    public TargetSet targetSet;

    [Header("Editor preview (Scene view only, not used in Play mode)")]
    public int previewTarget = 0;
    public NavCondition previewCondition = NavCondition.VisualOnly;

    [Header("Scene references")]
    [Tooltip("XR Origin (XR Rig)")]
    public Transform xrOrigin;
    [Tooltip("Main Camera inside XR Origin > Camera Offset")]
    public Transform playerHead;
    [Tooltip("Object with an AudioSource (+ Steam Audio Source)")]
    public Transform targetSound;

    [Header("Geometry (FENS floor 2)")]
    public float floorY = 7.86f;       // top of the floor-2 slab in the model
    public float lineHeight = 0.02f;   // line slightly above the floor
    public float soundHeight = 1.5f;   // target sound at roughly head height
    public float reachRadius = 1.5f;   // metres from target that count as "arrived"

    [Header("Line look (keep it dim: pupil size reacts to light)")]
    public Material lineMaterial;      // optional; a plain unlit material is created if empty
    public Color lineColor = new Color(0.35f, 0.45f, 0.6f);
    public float lineWidth = 0.15f;

    /// Raised once per trial, the first frame the head is within reachRadius of the target.
    public event Action Arrived;

    LineRenderer line;
    TargetDef current;
    NavCondition currentCondition;
    bool trialRunning;
    bool arrived;

    public bool HasArrived => arrived;

    void OnEnable()
    {
        if (Application.isPlaying)
        {
            // Nothing is shown and nothing plays until a trial starts
            SetupLine();
            line.enabled = false;
            StopSound();
        }
        else
        {
            ApplyPreview();
        }
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            ApplyPreview(); // keep the Scene view preview up to date while editing
            return;
        }
        CheckTargetReached();
    }

    // ---------------------------------------------------------------- trial control (called by ExperimentController)

    /// Draws the line / places + plays the sound for this target and condition, and starts arrival detection.
    public void BeginTrial(TargetDef target, NavCondition condition)
    {
        current = target;
        currentCondition = condition;
        arrived = false;
        trialRunning = target != null && target.IsValid;
        if (!trialRunning)
        {
            Debug.LogWarning("[RouteManager] Target has fewer than 2 path points; nothing to show.");
            return;
        }

        DrawPath(target);
        line.enabled = condition.ShowsLine();
        PlaceSound(target);
        if (condition.PlaysSound()) PlaySound(); else StopSound();
    }

    /// Hides the line, stops the sound, stops arrival detection.
    public void EndTrial()
    {
        trialRunning = false;
        if (line != null) line.enabled = false;
        StopSound();
    }

    // (Placing the player at the start is done by WorldAlignment.)

    // ---------------------------------------------------------------- drawing

    void SetupLine()
    {
        if (line == null) line = GetComponent<LineRenderer>();

        // Lay the line flat on the floor (it faces this object's Z axis)
        var flat = Quaternion.Euler(90f, 0f, 0f);
        if (transform.rotation != flat) transform.rotation = flat;

        line.useWorldSpace = true;
        line.alignment = LineAlignment.TransformZ;
        line.startWidth = line.endWidth = lineWidth;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (lineMaterial != null)
        {
            line.sharedMaterial = lineMaterial;
        }
        else if (line.sharedMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            line.sharedMaterial = new Material(shader);
        }
        var mat = line.sharedMaterial;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", lineColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", lineColor);
    }

    void DrawPath(TargetDef t)
    {
        SetupLine();
        line.positionCount = t.path.Count;
        for (int i = 0; i < t.path.Count; i++)
            line.SetPosition(i, new Vector3(t.path[i].x, floorY + lineHeight, t.path[i].z));
    }

    void PlaceSound(TargetDef t)
    {
        if (targetSound == null) return;
        var p = new Vector3(t.Target.x, floorY + soundHeight, t.Target.z);
        if (targetSound.position != p) targetSound.position = p;
    }

    void PlaySound()
    {
        var src = targetSound != null ? targetSound.GetComponent<AudioSource>() : null;
        if (src != null && !src.isPlaying) src.Play();
    }

    void StopSound()
    {
        var src = targetSound != null ? targetSound.GetComponent<AudioSource>() : null;
        if (src != null) src.Stop();
    }

    /// Edit mode: show the preview target so the path can be checked in the Scene view.
    void ApplyPreview()
    {
        SetupLine();
        var t = targetSet != null ? targetSet.Get(previewTarget) : null;
        if (t == null || !t.IsValid)
        {
            line.positionCount = 0;
            return;
        }
        DrawPath(t);
        line.enabled = previewCondition.ShowsLine();
        PlaceSound(t);
    }

    // ---------------------------------------------------------------- arrival

    void CheckTargetReached()
    {
        if (!trialRunning || arrived || playerHead == null || current == null) return;
        var head = new Vector2(playerHead.position.x, playerHead.position.z);
        var target = new Vector2(current.Target.x, current.Target.z);
        if (Vector2.Distance(head, target) > reachRadius) return;

        arrived = true;
        StopSound(); // as in the original version: the sound stops when the target is reached
        Arrived?.Invoke();
    }

    // ---------------------------------------------------------------- Scene view gizmos (never visible in the headset)

    void OnDrawGizmos()
    {
        if (targetSet == null) return;
        for (int k = 0; k < targetSet.Count; k++)
        {
            var t = targetSet.targets[k];
            if (t == null || !t.IsValid) continue;
            bool selected = Application.isPlaying ? t == current : k == previewTarget;
            float y = floorY + 0.1f;

            Gizmos.color = selected ? Color.yellow : new Color(1f, 1f, 0f, 0.25f);
            for (int i = 0; i < t.path.Count; i++)
            {
                var p = new Vector3(t.path[i].x, y, t.path[i].z);
                if (selected) Gizmos.DrawSphere(p, 0.3f);
                if (i > 0) Gizmos.DrawLine(new Vector3(t.path[i - 1].x, y, t.path[i - 1].z), p);
            }

            // start point + facing direction (green)
            var s = new Vector3(t.Start.x, y, t.Start.z);
            var dir = Quaternion.Euler(0f, t.startYaw, 0f) * Vector3.forward;
            Gizmos.color = selected ? Color.green : new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(s, 0.4f);
            Gizmos.DrawLine(s, s + dir * 1.5f);

            // arrival radius (red)
            Gizmos.color = selected ? Color.red : new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawWireSphere(new Vector3(t.Target.x, y, t.Target.z), reachRadius);
        }
    }
}
