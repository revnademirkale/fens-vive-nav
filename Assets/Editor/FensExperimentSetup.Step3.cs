using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Step 3 of the scene setup: operator control from the headset controllers.
///  - ControllerVisibility on the XR Origin (controller models / rays / callouts hidden, XRI interactors off)
///  - OperatorInput: controller buttons on (the bindings themselves live in OperatorInput)
/// Runs inside FENS > Setup Experiment Scene, or alone via FENS > Step 3 only > Setup Controllers.
/// </summary>
public static partial class FensExperimentSetup
{
    [MenuItem("FENS/Step 3 only/Setup Controllers (operator buttons + hide controller visuals)")]
    static void MenuControllers()
    {
        var rm = Object.FindFirstObjectByType<RouteManager>(FindObjectsInactive.Include);
        var exp = Object.FindFirstObjectByType<ExperimentController>(FindObjectsInactive.Include);
        if (rm == null || exp == null)
        {
            EditorUtility.DisplayDialog("FENS setup", "Run FENS > Setup Experiment Scene first.", "OK");
            return;
        }
        var log = new List<string>();
        SetupControllers(rm, exp, log);
        Finish(rm.gameObject, log);
    }

    static void SetupControllers(RouteManager rm, ExperimentController exp, List<string> log)
    {
        if (rm.xrOrigin == null)
        {
            log.Add("WARNING: RouteManager has no XR Origin; ControllerVisibility not added.");
        }
        else
        {
            var cv = GetOrAdd<ControllerVisibility>(rm.xrOrigin.gameObject, log);
            Undo.RecordObject(cv, "FENS setup");
            cv.showControllerVisuals = false;
            cv.disableXRInteractors = true;
            EditorUtility.SetDirty(cv);
            log.Add("ControllerVisibility (XR Origin): controller visuals HIDDEN, XRI interactors OFF.");
        }

        var input = exp.GetComponent<OperatorInput>();
        if (input != null)
        {
            Undo.RecordObject(input, "FENS setup");
            input.useController = true;
            input.experiment = exp;
            EditorUtility.SetDirty(input);
            log.Add("OperatorInput: controller buttons ON (start/stop and align = hold trigger " +
                    $"{input.holdSeconds:0.#} s).");
        }
    }
}
