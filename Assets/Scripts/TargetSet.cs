using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One navigation target: where the participant starts, which way they face,
/// and the path (corner points) from the start to the target.
/// </summary>
[Serializable]
public class TargetDef
{
    [Tooltip("Short name used in logs and file names, e.g. W, T1, T2")]
    public string id = "T1";

    [Tooltip("Warm-up target (practice, not part of the 4 measured targets)")]
    public bool isWarmup;

    [Tooltip("Corner points in world coordinates. Y is ignored (the floor height is used).\n" +
             "First point = start position, last point = target.")]
    public List<Vector3> path = new List<Vector3>();

    [Tooltip("Direction the participant faces at the start, in degrees (0 = +Z, 90 = +X).")]
    public float startYaw;

    public bool IsValid => path != null && path.Count >= 2;
    public Vector3 Start => path[0];
    public Vector3 Target => path[path.Count - 1];
}

/// <summary>
/// The list of targets for the experiment (1 warm-up + 4 targets).
/// Edit the values in the Inspector: select the TargetSet asset in the Project window.
/// The current values are PLACEHOLDERS until the real target points are decided.
/// </summary>
[CreateAssetMenu(fileName = "TargetSet", menuName = "FENS/Target Set")]
public class TargetSet : ScriptableObject
{
    public List<TargetDef> targets = new List<TargetDef>();

    public int Count => targets != null ? targets.Count : 0;

    public TargetDef Get(int index)
    {
        if (Count == 0) return null;
        return targets[Mathf.Clamp(index, 0, Count - 1)];
    }

    /// Yaw (degrees) that looks along the first path segment.
    public static float YawOfFirstSegment(TargetDef t)
    {
        if (t == null || !t.IsValid) return 0f;
        Vector3 d = t.path[1] - t.path[0];
        return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
    }
}
