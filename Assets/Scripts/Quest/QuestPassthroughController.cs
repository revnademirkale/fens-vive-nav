using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

/// <summary>
/// META QUEST passthrough. Used only when the app runs with the Meta Quest OpenXR setup
/// (FENS > Android (Meta Quest) > Configure OpenXR for Meta Quest). The VIVE PassthroughController
/// is not changed: on a Quest build its VIVE feature is off, so it skips itself.
///
/// This component is NOT placed in the scene (so the scene file stays the same as on main).
/// After the scene loads, a bootstrap finds the camera that has the VIVE PassthroughController
/// (or Camera.main) and adds this component there, but only if the OpenXR feature
/// "Meta Quest: AR Camera (Passthrough)" is enabled.
///
/// On Meta Quest, passthrough is controlled by AR Foundation: an enabled ARCameraManager = passthrough on
/// (Unity OpenXR: Meta 1.0, "Camera" page). ARSession / ARCameraManager are added at runtime if missing.
/// Camera background must be transparent (alpha 0), HDR off, post-processing off (same as the VIVE version).
/// </summary>
public class QuestPassthroughController : MonoBehaviour
{
    const string MetaCameraFeatureType = "ARCameraFeature"; // Meta Quest: AR Camera (Passthrough)

    public Camera targetCamera;
    [Tooltip("Seconds to wait for the AR session before giving up")]
    public float sessionTimeout = 15f;

    public bool IsActive { get; private set; }

    ARCameraManager cameraManager;

    /// True if the current OpenXR settings have the Meta passthrough feature enabled (= this is a Quest setup).
    public static bool MetaPassthroughEnabled()
    {
        var settings = OpenXRSettings.Instance;
        if (settings == null) return false;
        return settings.GetFeatures<OpenXRFeature>()
                       .Any(f => f != null && f.enabled && f.GetType().Name == MetaCameraFeatureType);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!XRSettings.isDeviceActive) return;   // Mac Editor / simulator: nothing to do
        if (!MetaPassthroughEnabled()) return;    // VIVE setup: VIVE PassthroughController handles it
        if (FindObjectOfType<QuestPassthroughController>() != null) return;

        Camera cam = null;
        var vive = FindObjectOfType<PassthroughController>();
        if (vive != null) cam = vive.targetCamera != null ? vive.targetCamera : vive.GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[QuestPassthrough] No camera found. Passthrough skipped.");
            return;
        }
        var q = cam.gameObject.AddComponent<QuestPassthroughController>();
        q.targetCamera = cam;
        Debug.Log($"[QuestPassthrough] Added to '{cam.name}'.");
    }

    IEnumerator Start()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (targetCamera == null) yield break;

        // 1) AR Session (needed for any AR Foundation subsystem, including the camera / passthrough)
        if (FindObjectOfType<ARSession>() == null)
        {
            new GameObject("AR Session (Quest Passthrough)").AddComponent<ARSession>();
            Debug.Log("[QuestPassthrough] No ARSession in the scene: one was added at runtime.");
        }

        float waited = 0f;
        while (ARSession.state < ARSessionState.Ready && ARSession.state != ARSessionState.Unsupported
               && waited < sessionTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        if (ARSession.state < ARSessionState.Ready)
        {
            Debug.LogWarning($"[QuestPassthrough] AR session not ready (state: {ARSession.state}). " +
                             "Is 'Meta Quest: AR Session' enabled? Passthrough skipped.");
            yield break;
        }

        // 2) AR Camera Manager on the camera = passthrough on
        cameraManager = targetCamera.GetComponent<ARCameraManager>();
        if (cameraManager == null) cameraManager = targetCamera.gameObject.AddComponent<ARCameraManager>();
        cameraManager.enabled = true;
        yield return null;

        if (cameraManager.subsystem == null)
        {
            Debug.LogWarning("[QuestPassthrough] No camera subsystem. Enable 'Meta Quest: AR Camera (Passthrough)'. Passthrough skipped.");
            yield break;
        }

        IsActive = true;
        PrepareCamera();
        Debug.Log("[QuestPassthrough] Meta passthrough started.");
    }

    /// Transparent background, no HDR, no post-processing, so alpha = 0 reaches the compositor.
    void PrepareCamera()
    {
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        targetCamera.allowHDR = false;
        var data = targetCamera.GetUniversalAdditionalCameraData();
        if (data != null) data.renderPostProcessing = false;
    }

    void OnDestroy()
    {
        if (cameraManager != null) cameraManager.enabled = false;
        IsActive = false;
    }
}
