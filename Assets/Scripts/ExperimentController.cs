using System;
using UnityEngine;

public enum TrialState { Idle, Running }

/// <summary>
/// Runs the experiment: the operator chooses target + condition while Idle,
/// then starts and stops each trial. Arrival at the target is detected automatically and logged;
/// the trial itself is always closed by the operator.
/// Commands come from OperatorInput (keyboard now, headset controller later).
/// </summary>
public class ExperimentController : MonoBehaviour
{
    [Header("Session")]
    [Tooltip("Set this BEFORE pressing Play, e.g. P01")]
    public string participantId = "P00";

    [Header("Current selection (operator changes these with the keys)")]
    public int targetIndex = 0;
    public NavCondition condition = NavCondition.None;

    [Header("References")]
    public RouteManager routeManager;
    public TrialLogger logger;
    public OperatorFeedback feedback;

    [Header("Options")]
    [Tooltip("At trial start, move the virtual building so the head is at the target's start point " +
             "(temporary, until WorldAlignment exists)")]
    public bool placePlayerOnStart = true;

    public TrialState State { get; private set; } = TrialState.Idle;
    public int TrialNumber { get; private set; }
    public float TrialElapsed => State == TrialState.Running ? Time.time - trialStartTime : 0f;
    public bool Arrived { get; private set; }

    /// Raised whenever state, target or condition changes (used by OperatorFeedback).
    public event Action Changed;

    float trialStartTime;

    TargetSet Targets => routeManager != null ? routeManager.targetSet : null;
    public int TargetCount => Targets != null ? Targets.Count : 0;
    public TargetDef CurrentTarget => Targets != null ? Targets.Get(targetIndex) : null;
    public string CurrentTargetId => CurrentTarget != null ? CurrentTarget.id : "-";

    void Start()
    {
        if (routeManager == null || Targets == null || TargetCount == 0)
        {
            Debug.LogError("[Experiment] RouteManager or its TargetSet is missing. " +
                           "Run the menu FENS > Setup Experiment Scene.");
            enabled = false;
            return;
        }
        targetIndex = Mathf.Clamp(targetIndex, 0, TargetCount - 1);
        routeManager.Arrived += OnArrived;
        routeManager.EndTrial(); // make sure nothing is visible / audible before the first trial
        Debug.Log($"[Experiment] Ready. Participant {participantId}, target {CurrentTargetId}, condition {condition}");
        Changed?.Invoke();
    }

    void OnDestroy()
    {
        if (routeManager != null) routeManager.Arrived -= OnArrived;
    }

    // ---------------------------------------------------------------- operator commands

    public void NextTarget() => SelectTarget(+1);
    public void PreviousTarget() => SelectTarget(-1);
    public void NextCondition() => SelectCondition(+1);
    public void PreviousCondition() => SelectCondition(-1);

    public void ToggleTrial()
    {
        if (State == TrialState.Idle) StartTrial();
        else StopTrial();
    }

    void SelectTarget(int step)
    {
        if (!CanChangeSelection() || TargetCount == 0) return;
        targetIndex = (targetIndex + step + TargetCount) % TargetCount;
        Debug.Log($"[Experiment] Target: {CurrentTargetId} ({targetIndex + 1}/{TargetCount})");
        if (feedback != null) feedback.Confirm();
        Changed?.Invoke();
    }

    void SelectCondition(int step)
    {
        if (!CanChangeSelection()) return;
        int n = Enum.GetValues(typeof(NavCondition)).Length;
        condition = (NavCondition)(((int)condition + step + n) % n);
        Debug.Log($"[Experiment] Condition: {condition}");
        if (feedback != null) feedback.Confirm();
        Changed?.Invoke();
    }

    bool CanChangeSelection()
    {
        if (State == TrialState.Idle) return true;
        Debug.LogWarning("[Experiment] Stop the running trial before changing target or condition.");
        if (feedback != null) feedback.Reject();
        return false;
    }

    // ---------------------------------------------------------------- trial lifetime

    public void StartTrial()
    {
        if (State != TrialState.Idle) return;
        var target = CurrentTarget;
        if (target == null || !target.IsValid)
        {
            Debug.LogError($"[Experiment] Target {CurrentTargetId} has fewer than 2 path points.");
            if (feedback != null) feedback.Reject();
            return;
        }

        TrialNumber++;
        Arrived = false;
        if (placePlayerOnStart) routeManager.PlacePlayerAtStart(target);
        routeManager.BeginTrial(target, condition);
        if (logger != null) logger.BeginTrial(participantId, TrialNumber, target.id, condition);
        trialStartTime = Time.time;
        State = TrialState.Running;

        Debug.Log($"[Experiment] Trial {TrialNumber} STARTED. Participant {participantId}, " +
                  $"target {target.id}{(target.isWarmup ? " (warm-up)" : "")}, condition {condition}");
        if (feedback != null) feedback.Confirm();
        Changed?.Invoke();
    }

    public void StopTrial()
    {
        if (State != TrialState.Running) return;
        float elapsed = TrialElapsed;
        routeManager.EndTrial();
        if (logger != null) logger.EndTrial(true);
        State = TrialState.Idle;

        Debug.Log($"[Experiment] Trial {TrialNumber} STOPPED after {elapsed:F1} s. " +
                  $"Target {CurrentTargetId}, condition {condition}, reached: {(Arrived ? "yes" : "no")}");
        if (feedback != null) feedback.Confirm();
        Changed?.Invoke();
    }

    void OnArrived()
    {
        if (State != TrialState.Running) return;
        Arrived = true;
        if (logger != null) logger.MarkArrival();
        Debug.Log($"[Experiment] Trial {TrialNumber}: target {CurrentTargetId} REACHED at {TrialElapsed:F1} s " +
                  "(operator still has to stop the trial)");
        Changed?.Invoke();
    }

    void OnApplicationQuit()
    {
        if (State == TrialState.Running && logger != null) logger.EndTrial(false, "application quit");
    }
}
