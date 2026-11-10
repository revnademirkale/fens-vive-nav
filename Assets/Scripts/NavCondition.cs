/// <summary>
/// The four experimental conditions. Every participant does all four.
/// </summary>
public enum NavCondition
{
    None,         // no help: no line, no sound
    VisualOnly,   // path line only
    AudioOnly,    // target sound only
    AudioVisual   // line + sound
}

public static class NavConditionExtensions
{
    public static bool ShowsLine(this NavCondition c) =>
        c == NavCondition.VisualOnly || c == NavCondition.AudioVisual;

    public static bool PlaysSound(this NavCondition c) =>
        c == NavCondition.AudioOnly || c == NavCondition.AudioVisual;
}
