using TMPro;
using UnityEngine;

/// <summary>
/// Operator feedback inside the headset:
/// - a small status text in front of the eyes, ONLY while no trial is running
/// - a short beep when a command is accepted (higher) or refused (lower)
/// The text and the beeps are created at runtime; nothing has to be set up by hand.
/// </summary>
public class OperatorFeedback : MonoBehaviour
{
    public ExperimentController experiment;
    [Tooltip("Main Camera (the text is attached in front of it)")]
    public Transform playerHead;

    [Header("Status text")]
    public float distance = 1.5f;      // metres in front of the eyes
    public float heightOffset = -0.35f; // a bit below eye level
    public float fontSize = 0.4f;
    public Color textColor = new Color(0.8f, 0.8f, 0.8f, 0.9f);

    [Header("Beep")]
    [Range(0f, 1f)] public float beepVolume = 0.3f;

    TextMeshPro text;
    AudioSource beepSource;
    AudioClip okClip, errorClip;

    void Start()
    {
        CreateText();
        CreateBeep();
        if (experiment != null) experiment.Changed += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (experiment != null) experiment.Changed -= Refresh;
    }

    public void Confirm() => Play(okClip);
    public void Reject() => Play(errorClip);

    void Refresh()
    {
        if (text == null || experiment == null) return;

        bool running = experiment.State == TrialState.Running;
        text.gameObject.SetActive(!running); // no text at all during a trial
        if (running) return;

        var t = experiment.CurrentTarget;
        string targetLabel = t != null ? t.id + (t.isWarmup ? " (warm-up)" : "") : "-";
        text.text =
            $"ID: {experiment.participantId}   Trial: {experiment.TrialNumber + 1}\n" +
            $"Target: {targetLabel}  [{experiment.targetIndex + 1}/{experiment.TargetCount}]\n" +
            $"Condition: {experiment.condition}\n" +
            "READY";
    }

    void CreateText()
    {
        if (playerHead == null) return;
        var go = new GameObject("OperatorStatusText");
        go.transform.SetParent(playerHead, false);
        go.transform.localPosition = new Vector3(0f, heightOffset, distance);
        go.transform.localRotation = Quaternion.identity;

        text = go.AddComponent<TextMeshPro>();
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.rectTransform.sizeDelta = new Vector2(1.2f, 0.4f);
    }

    void CreateBeep()
    {
        beepSource = gameObject.AddComponent<AudioSource>();
        beepSource.playOnAwake = false;
        beepSource.spatialBlend = 0f; // plain 2D sound, not spatialised by Steam Audio
        beepSource.volume = beepVolume;
        okClip = MakeTone("beep_ok", 1200f, 0.07f);
        errorClip = MakeTone("beep_error", 300f, 0.2f);
    }

    void Play(AudioClip clip)
    {
        if (beepSource != null && clip != null) beepSource.PlayOneShot(clip, beepVolume);
    }

    static AudioClip MakeTone(string name, float freq, float seconds)
    {
        int rate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
        int n = Mathf.CeilToInt(rate * seconds);
        var data = new float[n];
        int fade = Mathf.Min(n / 4, rate / 200); // ~5 ms fade in/out avoids clicks
        for (int i = 0; i < n; i++)
        {
            float env = 1f;
            if (i < fade) env = i / (float)fade;
            else if (i > n - fade) env = (n - i) / (float)fade;
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / rate) * env;
        }
        var clip = AudioClip.Create(name, n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
