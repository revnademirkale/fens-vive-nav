using UnityEngine;

/// <summary>
/// Lines up the virtual building with the real one (idea from offsetFix.cs of the original project,
/// but triggered by the operator instead of a 3 s timer).
/// The participant stands on the start mark of a target and faces its startYaw direction;
/// the operator presses "Align". The XR Origin is turned (yaw only) around the head and moved
/// (horizontally) so that the head is exactly on the virtual start point, looking in startYaw.
/// ONLY the XR Origin moves. The FENS model and the Steam Audio static geometry never move.
/// Put this on the XR Origin.
/// </summary>
public class WorldAlignment : MonoBehaviour
{
    [Tooltip("XR Origin (XR Rig). Default: this object")]
    public Transform xrOrigin;
    [Tooltip("Main Camera (the participant's head)")]
    public Transform head;
    [Tooltip("Height of the floor the participant stands on, in the model (FENS floor 2 = 7.86)")]
    public float floorY = 7.86f;

    public bool IsAligned { get; private set; }
    public string AlignedTargetId { get; private set; } = "";
    public int AlignmentCount { get; private set; }

    /// What one alignment did (for the log).
    public struct Result
    {
        public string targetId;
        public Vector3 startPoint;
        public float startYaw;
        public Vector3 headBefore, headAfter;
        public float headYawBefore;
        public float rotationDeg;      // yaw added to the XR Origin
        public Vector3 translation;    // movement of the XR Origin after the rotation
        public Vector3 originAfter;
        public float originYawAfter;
    }

    void Awake()
    {
        if (xrOrigin == null) xrOrigin = transform;
    }

    public bool AlignTo(TargetDef target, out Result result)
    {
        result = default;
        if (xrOrigin == null || head == null || target == null || !target.IsValid)
        {
            Debug.LogError("[WorldAlignment] Missing XR Origin / head reference or invalid target.");
            return false;
        }

        // CharacterController overrides direct position changes, so pause it if it is on
        var cc = xrOrigin.GetComponent<CharacterController>();
        bool ccWasEnabled = cc != null && cc.enabled;
        if (ccWasEnabled) cc.enabled = false;

        result.targetId = target.id;
        result.startPoint = new Vector3(target.Start.x, floorY, target.Start.z);
        result.startYaw = target.startYaw;
        result.headBefore = head.position;
        result.headYawBefore = head.eulerAngles.y;

        // 1) turn the rig around the head (vertical axis only) so the head looks along startYaw
        result.rotationDeg = Mathf.DeltaAngle(result.headYawBefore, target.startYaw);
        xrOrigin.RotateAround(head.position, Vector3.up, result.rotationDeg);

        // 2) slide the rig so the head is above the start point; the rig stands on the floor
        Vector3 h = head.position;
        Vector3 before = xrOrigin.position;
        Vector3 p = before + new Vector3(target.Start.x - h.x, 0f, target.Start.z - h.z);
        p.y = floorY;
        xrOrigin.position = p;
        result.translation = p - before;

        if (ccWasEnabled) cc.enabled = true;

        result.headAfter = head.position;
        result.originAfter = xrOrigin.position;
        result.originYawAfter = xrOrigin.eulerAngles.y;

        IsAligned = true;
        AlignedTargetId = target.id;
        AlignmentCount++;
        Debug.Log($"[WorldAlignment] Aligned to start of {target.id}: rotated {result.rotationDeg:F1} deg, " +
                  $"moved ({result.translation.x:F2}, {result.translation.y:F2}, {result.translation.z:F2}) m");
        return true;
    }
}
