using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Turns operator button presses into experiment commands.
/// (A) PC streaming: keyboard on the PC.  (B) Standalone headset: controller buttons (to be added).
/// Default keys avoid the keys used by the XR Interaction Simulator (WASD, QE, arrows, 0-9, etc.).
/// The project uses the new Input System only, so UnityEngine.Input.GetKeyDown cannot be used.
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
    }

    void PollController()
    {
        // TODO (step B, standalone headset): read the VIVE controller buttons here and call the same
        // experiment methods (PreviousTarget / NextTarget / PreviousCondition / NextCondition / ToggleTrial).
    }
}
