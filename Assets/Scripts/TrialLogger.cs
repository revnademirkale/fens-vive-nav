using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Writes one CSV file per trial (one row per frame) and one summary CSV per participant (one row per trial).
/// Folder: Application.persistentDataPath/FENS_logs/&lt;participantId&gt;/
/// Idea and columns adapted from dataLogging.cs in the original HoloLens 2 project.
/// Extra columns (e.g. eye tracking later) come from ILogColumnProvider components on this GameObject
/// or listed in "Extra Column Sources". Alignments are written to &lt;participantId&gt;_alignment.csv.
/// </summary>
public class TrialLogger : MonoBehaviour
{
    [Tooltip("Main Camera (the participant's head)")]
    public Transform playerHead;

    [Tooltip("A head movement larger than this in one frame counts as a tracking jump (valid = 0). " +
             "3 m as in trackingErrFix.cs of the original project.")]
    public float maxJumpMeters = 3f;

    [Tooltip("Seconds between writes to disk, so data survives a crash")]
    public float flushInterval = 1f;

    [Tooltip("Other components that add columns (must implement ILogColumnProvider), e.g. RearAttenuation")]
    public List<MonoBehaviour> extraColumnSources = new List<MonoBehaviour>();

    public const string RootFolderName = "FENS_logs";
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    StreamWriter writer;
    readonly List<string> row = new List<string>(64);
    readonly List<ILogColumnProvider> extraColumns = new List<ILogColumnProvider>();

    // current trial
    string participantId, targetId, condition, alignedTo;
    int trialNumber;
    double trialStartTime;
    long trialStartUnixMs;
    double arrivalTime = -1;
    int frameCount, invalidFrames;
    float lastFlush;
    Vector3 lastGoodPos;
    bool hasLastGood;
    string currentFilePath;

    public bool IsLogging => writer != null;
    public string CurrentFilePath => currentFilePath;

    public static string RootFolder => Path.Combine(Application.persistentDataPath, RootFolderName);

    void Awake()
    {
        foreach (var mb in GetComponents<MonoBehaviour>())
            if (mb is ILogColumnProvider p) extraColumns.Add(p);
        foreach (var mb in extraColumnSources)
            if (mb is ILogColumnProvider p && !extraColumns.Contains(p)) extraColumns.Add(p);
    }

    void Start()
    {
        Debug.Log($"[TrialLogger] Log folder: {RootFolder}");
    }

    // ---------------------------------------------------------------- trial lifetime

    public void BeginTrial(string participant, int trial, string target, NavCondition cond, string alignedToTarget = "")
    {
        if (writer != null) EndTrial(false, "replaced by new trial");

        participantId = Sanitize(participant);
        trialNumber = trial;
        targetId = Sanitize(target);
        condition = cond.ToString();
        alignedTo = string.IsNullOrEmpty(alignedToTarget) ? "none" : Sanitize(alignedToTarget);
        trialStartTime = Time.timeAsDouble;
        trialStartUnixMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        arrivalTime = -1;
        frameCount = invalidFrames = 0;
        hasLastGood = false;
        lastFlush = Time.unscaledTime;

        string folder = Path.Combine(RootFolder, participantId);
        Directory.CreateDirectory(folder);
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", Inv);
        currentFilePath = Path.Combine(folder,
            $"{participantId}_trial{trialNumber:00}_{targetId}_{condition}_{stamp}.csv");

        writer = new StreamWriter(currentFilePath, false, new UTF8Encoding(false));
        writer.WriteLine(string.Join(",", Header()));
        writer.Flush();
    }

    /// Call on the frame the target is reached. Rows after this have arrived = 1.
    public void MarkArrival()
    {
        if (writer == null || arrivalTime >= 0) return;
        arrivalTime = Time.timeAsDouble - trialStartTime;
    }

    /// Closes the trial file and appends one row to the participant's summary file.
    public void EndTrial(bool stoppedByOperator, string note = "")
    {
        if (writer == null) return;
        double duration = Time.timeAsDouble - trialStartTime;
        writer.Flush();
        writer.Close();
        writer = null;

        AppendSummary(duration, stoppedByOperator, note);
        Debug.Log($"[TrialLogger] Saved {frameCount} rows ({invalidFrames} invalid) to {currentFilePath}");
    }

    void OnApplicationQuit() => EndTrial(false, "application quit");
    void OnDisable() => EndTrial(false, "logger disabled");

    // ---------------------------------------------------------------- per-frame rows

    void LateUpdate()
    {
        if (writer == null || playerHead == null) return;

        Vector3 pos = playerHead.position;
        Quaternion rot = playerHead.rotation;
        bool valid = IsTrackingValid(pos, rot);
        if (!valid) invalidFrames++;

        double tTrial = Time.timeAsDouble - trialStartTime;
        Vector3 e = rot.eulerAngles;

        row.Clear();
        row.Add(participantId);
        row.Add(trialNumber.ToString(Inv));
        row.Add(targetId);
        row.Add(condition);
        row.Add(F(Time.timeAsDouble));
        row.Add(F(tTrial));
        row.Add(DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString(Inv));
        row.Add(F(pos.x)); row.Add(F(pos.y)); row.Add(F(pos.z));
        row.Add(F(e.y)); row.Add(F(e.x)); row.Add(F(e.z));                  // yaw, pitch, roll
        row.Add(F(rot.x)); row.Add(F(rot.y)); row.Add(F(rot.z)); row.Add(F(rot.w));
        row.Add(valid ? "1" : "0");
        row.Add(arrivalTime >= 0 ? "1" : "0");
        foreach (var p in extraColumns) p.AppendValues(row);

        writer.WriteLine(string.Join(",", row));
        frameCount++;

        if (Time.unscaledTime - lastFlush >= flushInterval)
        {
            writer.Flush();
            lastFlush = Time.unscaledTime;
        }
    }

    List<string> Header()
    {
        var h = new List<string>
        {
            "participant_id", "trial", "target", "condition",
            "time_app_s", "time_trial_s", "time_unix_ms",
            "head_pos_x", "head_pos_y", "head_pos_z",
            "head_yaw_deg", "head_pitch_deg", "head_roll_deg",
            "head_rot_qx", "head_rot_qy", "head_rot_qz", "head_rot_qw",
            "valid", "arrived"
        };
        foreach (var p in extraColumns) h.AddRange(p.ColumnNames);
        return h;
    }

    /// valid = no NaN / infinity in the head pose and no sudden jump larger than maxJumpMeters.
    bool IsTrackingValid(Vector3 pos, Quaternion rot)
    {
        bool finite = IsFinite(pos.x) && IsFinite(pos.y) && IsFinite(pos.z) &&
                      IsFinite(rot.x) && IsFinite(rot.y) && IsFinite(rot.z) && IsFinite(rot.w);
        if (!finite) return false;

        // Compare with the previous finite frame: only the frame where the jump happens is marked invalid
        bool jumped = hasLastGood && Vector3.Distance(pos, lastGoodPos) > maxJumpMeters;
        lastGoodPos = pos;
        hasLastGood = true;
        return !jumped;
    }

    // ---------------------------------------------------------------- summary file

    void AppendSummary(double duration, bool stoppedByOperator, string note)
    {
        string path = Path.Combine(RootFolder, participantId, $"{participantId}_summary.csv");
        bool isNew = !File.Exists(path);
        using (var w = new StreamWriter(path, true, new UTF8Encoding(false)))
        {
            if (isNew)
                w.WriteLine("participant_id,trial,target,condition,start_unix_ms,reached,arrival_time_s," +
                            "duration_s,stopped_by_operator,frames,invalid_frames,file,note,aligned_to");
            w.WriteLine(string.Join(",",
                participantId, trialNumber.ToString(Inv), targetId, condition,
                trialStartUnixMs.ToString(Inv),
                arrivalTime >= 0 ? "1" : "0",
                arrivalTime >= 0 ? F(arrivalTime) : "",
                F(duration),
                stoppedByOperator ? "1" : "0",
                frameCount.ToString(Inv), invalidFrames.ToString(Inv),
                Path.GetFileName(currentFilePath),
                Sanitize(note),
                alignedTo));
        }
    }

    // ---------------------------------------------------------------- alignment file

    /// One row per alignment: when, to which target's start, and how much the XR Origin was turned / moved.
    public void LogAlignment(string participant, int alignmentNumber, WorldAlignment.Result r)
    {
        string pid = Sanitize(participant);
        string folder = Path.Combine(RootFolder, pid);
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"{pid}_alignment.csv");
        bool isNew = !File.Exists(path);
        using (var w = new StreamWriter(path, true, new UTF8Encoding(false)))
        {
            if (isNew)
                w.WriteLine("participant_id,alignment,time_unix_ms,time_app_s,target,start_x,start_z,start_yaw_deg," +
                            "head_before_x,head_before_y,head_before_z,head_yaw_before_deg," +
                            "rotation_applied_deg,translation_x,translation_y,translation_z," +
                            "origin_after_x,origin_after_y,origin_after_z,origin_yaw_after_deg," +
                            "head_after_x,head_after_y,head_after_z");
            w.WriteLine(string.Join(",",
                pid, alignmentNumber.ToString(Inv),
                DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString(Inv), F(Time.timeAsDouble),
                Sanitize(r.targetId), F(r.startPoint.x), F(r.startPoint.z), F(r.startYaw),
                F(r.headBefore.x), F(r.headBefore.y), F(r.headBefore.z), F(r.headYawBefore),
                F(r.rotationDeg), F(r.translation.x), F(r.translation.y), F(r.translation.z),
                F(r.originAfter.x), F(r.originAfter.y), F(r.originAfter.z), F(r.originYawAfter),
                F(r.headAfter.x), F(r.headAfter.y), F(r.headAfter.z)));
        }
        Debug.Log($"[TrialLogger] Alignment {alignmentNumber} written to {path}");
    }

    // ---------------------------------------------------------------- helpers

    static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    static string F(double v) => v.ToString("0.#####", Inv);

    /// Keeps file names and CSV cells safe (no commas, slashes, spaces).
    static string Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "NA";
        var sb = new StringBuilder(s.Length);
        foreach (char c in s.Trim())
            sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
        return sb.ToString();
    }
}
