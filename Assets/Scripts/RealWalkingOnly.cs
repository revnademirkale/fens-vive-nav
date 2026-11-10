using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

/// <summary>
/// The participant may only move by really walking.
/// Turns off every XRI locomotion provider from the template (Move, Turn, Teleportation, Climb, Jump, Gravity)
/// and the CharacterController, so the rig can never be pushed away from the real position
/// by virtual walls or by gravity. Put this on the XR Origin.
/// </summary>
[DefaultExecutionOrder(-100)]
public class RealWalkingOnly : MonoBehaviour
{
    void Awake()
    {
        int count = 0;
        // All providers in the scene (Move, Snap/Continuous Turn, Teleportation, Climb, Jump, Gravity)
        // derive from LocomotionProvider in XRI 3.x
        foreach (var p in FindObjectsByType<LocomotionProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!p.enabled) continue;
            p.enabled = false;
            count++;
            Debug.Log($"[RealWalkingOnly] Disabled {p.GetType().Name} on {p.gameObject.name}");
        }

        var body = GetComponent<XRBodyTransformer>();
        if (body != null && body.enabled) { body.enabled = false; count++; }

        var cc = GetComponent<CharacterController>();
        if (cc != null && cc.enabled) { cc.enabled = false; count++; }

        Debug.Log($"[RealWalkingOnly] Real walking only: {count} locomotion components disabled.");
    }
}
