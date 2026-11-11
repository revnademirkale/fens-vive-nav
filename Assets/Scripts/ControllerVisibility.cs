using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Keeps the participant's view free of controller visuals (no virtual controller models, rays, tooltips/callouts),
/// so no condition (especially None / AudioOnly) shows any visual aid.
/// Also switches off all XRI interactors (ray / near-far / teleport / poke / gaze), so controller buttons
/// never grab, teleport or click UI. Controller BUTTON input still works (OperatorInput reads it directly).
/// Put this on the XR Origin. Re-applied twice a second, because XRI re-activates the controller objects
/// when tracking changes (controller <-> hand).
/// </summary>
public class ControllerVisibility : MonoBehaviour
{
    [Tooltip("OFF (default) = controller models, rays and callouts are hidden from the participant")]
    public bool showControllerVisuals = false;
    [Tooltip("ON (default) = all XRI interactors are switched off (no grab / teleport / UI clicks)")]
    public bool disableXRInteractors = true;

    [Tooltip("Optional. Empty = taken from the XR Input Modality Manager, or found by name ('Left Controller' / 'Right Controller')")]
    public List<Transform> controllerRoots = new List<Transform>();

    [Tooltip("Objects anywhere under the XR Origin whose name starts with one of these are hidden too")]
    public string[] alsoHideNamesStartingWith = { "Affordance Callouts", "Poke Point Affordances" };

    readonly HashSet<Renderer> hiddenRenderers = new HashSet<Renderer>();
    readonly HashSet<Canvas> hiddenCanvases = new HashSet<Canvas>();
    float nextApply;
    bool lastShow;
    int interactorsOff;

    void Start()
    {
        FindControllerRoots();
        lastShow = showControllerVisuals;
        Apply();
        Debug.Log($"[ControllerVisibility] Controller visuals {(showControllerVisuals ? "SHOWN" : "HIDDEN")}, " +
                  $"XRI interactors {(disableXRInteractors ? $"OFF ({interactorsOff})" : "on")}. Controller buttons still work.");
    }

    void Update()
    {
        if (showControllerVisuals != lastShow)
        {
            lastShow = showControllerVisuals;
            if (showControllerVisuals) RestoreVisuals();
            Debug.Log($"[ControllerVisibility] Controller visuals {(showControllerVisuals ? "SHOWN" : "HIDDEN")}");
        }
        if (Time.unscaledTime >= nextApply) Apply();
    }

    void Apply()
    {
        nextApply = Time.unscaledTime + 0.5f;
        if (disableXRInteractors) DisableInteractors();
        if (!showControllerVisuals) HideVisuals();
    }

    void FindControllerRoots()
    {
        controllerRoots.RemoveAll(t => t == null);
        if (controllerRoots.Count > 0) return;

        var modality = GetComponentInChildren<XRInputModalityManager>(true);
        if (modality != null)
        {
            if (modality.leftController != null) controllerRoots.Add(modality.leftController.transform);
            if (modality.rightController != null) controllerRoots.Add(modality.rightController.transform);
        }
        if (controllerRoots.Count == 0)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "Left Controller" || t.name == "Right Controller") controllerRoots.Add(t);
        }
        if (controllerRoots.Count == 0)
            Debug.LogWarning("[ControllerVisibility] No controller objects found under the XR Origin. Fill 'Controller Roots' by hand.");
    }

    void DisableInteractors()
    {
        foreach (var i in GetComponentsInChildren<XRBaseInteractor>(true))
        {
            var go = i.gameObject;
            // Interactors normally sit on their own child object -> switch the whole object off (also hides its line).
            // If it shares the object with something important (camera, controller pose), only the component goes off.
            bool sharedObject = go == gameObject || go.GetComponent<Camera>() != null || controllerRoots.Contains(go.transform);
            if (sharedObject)
            {
                if (i.enabled) { i.enabled = false; interactorsOff++; }
            }
            else if (go.activeSelf)
            {
                go.SetActive(false);
                interactorsOff++;
            }
        }
    }

    void HideVisuals()
    {
        foreach (var root in controllerRoots)
            if (root != null) HideUnder(root);

        if (alsoHideNamesStartingWith != null && alsoHideNamesStartingWith.Length > 0)
            foreach (var t in GetComponentsInChildren<Transform>(true))
                foreach (var prefix in alsoHideNamesStartingWith)
                    if (!string.IsNullOrEmpty(prefix) && t.name.StartsWith(prefix)) { HideUnder(t); break; }
    }

    void HideUnder(Transform root)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            if (r.enabled) { r.enabled = false; hiddenRenderers.Add(r); }
        foreach (var c in root.GetComponentsInChildren<Canvas>(true))
            if (c.enabled) { c.enabled = false; hiddenCanvases.Add(c); }
    }

    void RestoreVisuals()
    {
        foreach (var r in hiddenRenderers) if (r != null) r.enabled = true;
        foreach (var c in hiddenCanvases) if (c != null) c.enabled = true;
        hiddenRenderers.Clear();
        hiddenCanvases.Clear();
    }
}
