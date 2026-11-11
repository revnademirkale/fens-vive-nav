using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Turns operator button presses into experiment commands.
/// (A) PC streaming: keyboard on the PC.  (B) Standalone headset: VIVE Focus 3 controller buttons.
/// Both are always active; use whichever is available.
/// Default keys avoid the keys used by the XR Interaction Simulator (WASD, QE, arrows, 0-9, etc.).
/// The project uses the new Input System only, so UnityEngine.Input.GetKeyDown cannot be used.
/// The controller bindings use the generic XR controller usages, so they work with the
/// VIVE Focus 3 controller profile and with the XR Interaction Simulator in the Editor.
/// The XRI interactors are switched off by ControllerVisibility, so these buttons do not grab/teleport/click UI.
/// </summary>
public class OperatorInput : MonoBehaviour
{
    public ExperimentController experiment;

    [Header("Keyboard")]
    public Key previousTargetKey = Key.B;
    public Key nextTargetKey = Key.N;
    public Key previousConditionKey = Key.O;
    public Key nextConditionKey = Key.P;
    public Key startStopKey = Key.Enter;
    public Key alignKey = Key.U;
    public Key modelViewKey = Key.F;

    [Header("Headset controller (VIVE Focus 3)")]
    public bool useController = true;
    [Tooltip("Seconds the trigger must be held for start/stop and for align (prevents accidental presses)")]
    public float holdSeconds = 1f;
    [Tooltip("During a trial only start/stop works on the controller; other buttons are ignored silently " +
             "(no error beep that the participant could hear)")]
    public bool ignoreOtherControllerButtonsDuringTrial = true;

    // Binding paths (Input System). {Usage} = control with that usage; works for any XR controller layout.
    const string R = "<XRController>{RightHand}/";
    const string L = "<XRController>{LeftHand}/";
    public string nextTargetBinding = R + "{PrimaryButton}";       // right A
    public string previousTargetBinding = R + "{SecondaryButton}"; // right B
    public string nextConditionBinding = L + "{PrimaryButton}";    // left X
    public string previousConditionBinding = L + "{SecondaryButton}"; // left Y
    public string modelViewBinding = L + "{GripButton}";           // left grip
    public string alignBinding = L + "{TriggerButton}";            // left trigger (hold)
    public string startStopBinding = R + "{TriggerButton}";        // right trigger (hold)

    InputAction nextTarget, previousTarget, nextCondition, previousCondition, modelView, align, startStop;
    readonly List<InputAction> actions = new List<InputAction>();

    void OnEnable()
    {
        if (!useController) return;
        string hold = $"hold(duration={holdSeconds.ToString(CultureInfo.InvariantCulture)})";
        nextTarget = Make("Next target", nextTargetBinding);
        previousTarget = Make("Previous target", previousTargetBinding);
        nextCondition = Make("Next condition", nextConditionBinding);
        previousCondition = Make("Previous condition", previousConditionBinding);
        modelView = Make("Model view", modelViewBinding);
        align = Make("Align", alignBinding, hold);
        startStop = Make("Start/stop", startStopBinding, hold);
    }

    void OnDisable()
    {
        foreach (var a in actions) { a.Disable(); a.Dispose(); }
        actions.Clear();
        nextTarget = previousTarget = nextCondition = previousCondition = modelView = align = startStop = null;
    }

    InputAction Make(string name, string binding, string interactions = null)
    {
        var a = new InputAction(name, InputActionType.Button, binding, interactions);
        a.Enable();
        actions.Add(a);
        return a;
    }

    void Update()
    {
        if (experiment == null) return;
        PollKeyboard();
        PollController();
    }

    void PollKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[previousTargetKey].wasPressedThisFrame) experiment.PreviousTarget();
        if (kb[nextTargetKey].wasPressedThisFrame) experiment.NextTarget();
        if (kb[previousConditionKey].wasPressedThisFrame) experiment.PreviousCondition();
        if (kb[nextConditionKey].wasPressedThisFrame) experiment.NextCondition();
        if (kb[startStopKey].wasPressedThisFrame || kb[Key.NumpadEnter].wasPressedThisFrame) experiment.ToggleTrial();
        if (kb[alignKey].wasPressedThisFrame) experiment.Align();
        if (kb[modelViewKey].wasPressedThisFrame) experiment.CycleModelView();
    }

    void PollController()
    {
        if (startStop == null) return; // controller input off

        // Start/stop: hold the right trigger (fires once, when the hold time is reached)
        if (startStop.WasPerformedThisFrame())
        {
            Debug.Log("[OperatorInput] Controller: start/stop (right trigger held)");
            experiment.ToggleTrial();
            return;
        }

        if (ignoreOtherControllerButtonsDuringTrial && experiment.State == TrialState.Running) return;

        if (nextTarget.WasPressedThisFrame()) experiment.NextTarget();
        if (previousTarget.WasPressedThisFrame()) experiment.PreviousTarget();
        if (nextCondition.WasPressedThisFrame()) experiment.NextCondition();
        if (previousCondition.WasPressedThisFrame()) experiment.PreviousCondition();
        if (modelView.WasPressedThisFrame()) experiment.CycleModelView();
        if (align.WasPerformedThisFrame())
        {
            Debug.Log("[OperatorInput] Controller: align (left trigger held)");
            experiment.Align();
        }
    }
}
