using System.Collections.Generic;
using UnityEngine;

public enum ModelViewMode { Hidden, Transparent, Visible }

/// <summary>
/// Shows / hides the virtual FENS model. In the real building the participant sees the real walls
/// through passthrough, so the model is hidden. Only the renderers are switched:
/// colliders (line of sight) and the exported Steam Audio geometry keep working.
/// Transparent mode helps the operator check by eye that the model lines up with the real building.
/// Put this on the FENS model root.
/// </summary>
public class ModelVisibility : MonoBehaviour
{
    [Tooltip("Mode at start in the Editor (no passthrough there)")]
    public ModelViewMode startModeInEditor = ModelViewMode.Visible;
    [Tooltip("Mode at start in a build (headset / PC streaming build)")]
    public ModelViewMode startModeInBuild = ModelViewMode.Hidden;

    [Tooltip("Semi-transparent material used in Transparent mode (created by the FENS setup menu)")]
    public Material transparentMaterial;

    [Tooltip("Layer of the model colliders. Collisions with every other layer are switched off at start, " +
             "so nothing (e.g. the XR rig) can ever be pushed by the virtual walls.")]
    public int colliderLayer = -1;

    public ModelViewMode Mode { get; private set; }

    readonly List<Renderer> renderers = new List<Renderer>();
    readonly List<Material[]> originalMaterials = new List<Material[]>();

    void Awake()
    {
        GetComponentsInChildren(true, renderers);
        foreach (var r in renderers) originalMaterials.Add(r.sharedMaterials);

        if (colliderLayer >= 0 && colliderLayer < 32)
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(colliderLayer, i, true);

        SetMode(Application.isEditor ? startModeInEditor : startModeInBuild);
    }

    public void Cycle()
    {
        SetMode((ModelViewMode)(((int)Mode + 1) % 3));
    }

    public void SetMode(ModelViewMode mode)
    {
        if (mode == ModelViewMode.Transparent && transparentMaterial == null)
        {
            Debug.LogWarning("[ModelVisibility] No transparent material set; using Visible instead.");
            mode = ModelViewMode.Visible;
        }
        Mode = mode;

        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            r.enabled = mode != ModelViewMode.Hidden;
            if (mode == ModelViewMode.Transparent)
            {
                var mats = new Material[originalMaterials[i].Length];
                for (int k = 0; k < mats.Length; k++) mats[k] = transparentMaterial;
                r.sharedMaterials = mats;
            }
            else
            {
                r.sharedMaterials = originalMaterials[i];
            }
        }
        Debug.Log($"[ModelVisibility] FENS model: {mode}");
    }
}
