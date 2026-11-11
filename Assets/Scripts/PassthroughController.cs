using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_STANDALONE_WIN
using UnityEngine.XR.OpenXR;
using VIVE.OpenXR.Passthrough;
#endif

/// <summary>
/// Turns on the VIVE camera passthrough behind the virtual content (underlay) when the app starts.
/// API from the VIVE OpenXR Plugin 2.5.1 (PassthroughAPI.CreatePlanarPassthrough, as in the package sample
/// PassthroughSample_Planar.cs). Needs the OpenXR feature "VIVE XR Passthrough".
/// For the camera image to show through, the camera background must be transparent (alpha 0),
/// HDR off (URP's HDR colour buffer has no alpha channel) and post-processing off
/// (URP 14 has no option to keep alpha through post-processing).
/// Without an XR device (e.g. Mac Editor + simulator) it does nothing.
/// Put this on the Main Camera.
/// </summary>
public class PassthroughController : MonoBehaviour
{
    public bool enablePassthrough = true;
    public Camera targetCamera;
    [Tooltip("Seconds to wait for the OpenXR session before giving up")]
    public float sessionTimeout = 15f;
    public int createAttempts = 5;

    public bool IsActive { get; private set; }

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_STANDALONE_WIN
    XrPassthroughHTC passthroughId = 0;
#endif

    IEnumerator Start()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (!enablePassthrough) yield break;

        if (!XRSettings.isDeviceActive)
        {
            Debug.Log("[Passthrough] No XR headset active (e.g. Mac Editor / simulator): passthrough skipped.");
            yield break;
        }

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_STANDALONE_WIN
        var feature = OpenXRSettings.Instance != null ? OpenXRSettings.Instance.GetFeature<VivePassthrough>() : null;
        if (feature == null || !feature.enabled)
        {
            Debug.LogWarning("[Passthrough] OpenXR feature 'VIVE XR Passthrough' is not enabled for this platform " +
                             "(Project Settings > XR Plug-in Management > OpenXR). Passthrough skipped.");
            yield break;
        }

        float waited = 0f;
        while (!feature.XrSessionCreated && waited < sessionTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!feature.XrSessionCreated)
        {
            Debug.LogWarning("[Passthrough] OpenXR session not created in time. Passthrough skipped.");
            yield break;
        }

        for (int i = 1; i <= createAttempts; i++)
        {
            var result = PassthroughAPI.CreatePlanarPassthrough(
                out passthroughId, VIVE.OpenXR.CompositionLayer.LayerType.Underlay, OnPassthroughSessionDestroyed);
            if (result == VIVE.OpenXR.XrResult.XR_SUCCESS)
            {
                IsActive = true;
                PrepareCamera();
                Debug.Log("[Passthrough] Planar passthrough started (underlay).");
                yield break;
            }
            Debug.LogWarning($"[Passthrough] CreatePlanarPassthrough failed ({result}), attempt {i}/{createAttempts}.");
            yield return new WaitForSecondsRealtime(1f);
        }
        Debug.LogError("[Passthrough] Could not start passthrough.");
#else
        Debug.Log("[Passthrough] VIVE OpenXR is not available on this platform: passthrough skipped.");
#endif
    }

    /// Transparent background, no HDR, no post-processing, so alpha = 0 reaches the compositor.
    void PrepareCamera()
    {
        if (targetCamera == null) return;
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        targetCamera.allowHDR = false;
        var data = targetCamera.GetUniversalAdditionalCameraData();
        if (data != null) data.renderPostProcessing = false;
    }

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_STANDALONE_WIN
    void OnPassthroughSessionDestroyed(XrPassthroughHTC id)
    {
        PassthroughAPI.DestroyPassthrough(id);
        passthroughId = 0;
        IsActive = false;
    }

    void OnDestroy()
    {
        if (IsActive && passthroughId != 0)
        {
            PassthroughAPI.DestroyPassthrough(passthroughId);
            passthroughId = 0;
            IsActive = false;
        }
    }
#endif
}
