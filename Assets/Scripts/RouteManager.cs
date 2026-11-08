using UnityEngine;

// Which of the two FENS floor-2 routes is active
public enum RouteId { A_West, B_East }

// Experimental condition: line on the floor + sound, or sound only
public enum NavCondition { Visual, AudioOnly }

/// <summary>
/// Sets up one navigation trial on FENS floor 2:
/// - draws the route as a line on the floor (Visual condition only)
/// - moves the target sound to the end of the route
/// - places the player (XR Origin) at the start, facing the first corridor
/// - logs the completion time when the player reaches the target
/// Change Route / Condition in the Inspector; the line updates in the Scene view.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class RouteManager : MonoBehaviour
{
    [Header("Experiment settings")]
    public RouteId route = RouteId.A_West;
    public NavCondition condition = NavCondition.Visual;

    [Header("Scene references")]
    [Tooltip("XR Origin (XR Rig)")]
    public Transform xrOrigin;
    [Tooltip("Main Camera inside XR Origin > Camera Offset")]
    public Transform playerHead;
    [Tooltip("Object with an AudioSource (+ Steam Audio Source)")]
    public Transform targetSound;

    [Header("Geometry (FENS floor 2)")]
    public float floorY = 7.86f;       // top of the floor-2 slab in the model
    public float lineHeight = 0.02f;   // line slightly above the floor
    public float soundHeight = 1.5f;   // target sound at roughly head height
    public float reachRadius = 1.5f;   // metres from target that count as "arrived"

    [Header("Line look (keep it dim: pupil size reacts to light)")]
    public Material lineMaterial;      // optional; a plain unlit material is created if empty
    public Color lineColor = new Color(0.35f, 0.45f, 0.6f);
    public float lineWidth = 0.15f;

    // Corner points on the floor as (X, Z) in Unity world coordinates.
    // First point = start (top of the stairs), last point = target.
    static readonly Vector2[] RouteA =
    {
        new Vector2(-7.2f, -3.8f), new Vector2(-7.2f, 0.2f), new Vector2(-29.7f, 0.2f),
        new Vector2(-29.7f, -30.3f), new Vector2(-15f, -30.3f)
    };
    static readonly Vector2[] RouteB =
    {
        new Vector2(-7.2f, -3.8f), new Vector2(-7.2f, 0.2f), new Vector2(24.5f, 0.2f),
        new Vector2(24.5f, -30.3f), new Vector2(17f, -30.3f)
    };

    LineRenderer line;
    float startTime;
    bool finished;

    Vector2[] Corners => route == RouteId.A_West ? RouteA : RouteB;
    Vector2 Target => Corners[Corners.Length - 1];

    void OnEnable() => Apply();

    void Start()
    {
        if (!Application.isPlaying) return;
        Apply();
        PlacePlayer();
        startTime = Time.time;
        finished = false;
        Debug.Log($"[RouteManager] Trial started. Route {route}, condition {condition}");
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            Apply(); // keep the Scene view preview up to date while editing
            return;
        }
        CheckTargetReached();
    }

    /// Draws the line and moves the target sound according to the current settings.
    public void Apply()
    {
        if (line == null) line = GetComponent<LineRenderer>();

        // Lay the line flat on the floor (it faces this object's Z axis)
        var flat = Quaternion.Euler(90f, 0f, 0f);
        if (transform.rotation != flat) transform.rotation = flat;

        var c = Corners;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.TransformZ;
        line.positionCount = c.Length;
        for (int i = 0; i < c.Length; i++)
            line.SetPosition(i, new Vector3(c[i].x, floorY + lineHeight, c[i].y));
        line.startWidth = line.endWidth = lineWidth;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (lineMaterial != null)
        {
            line.sharedMaterial = lineMaterial;
        }
        else if (line.sharedMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            line.sharedMaterial = new Material(shader);
        }
        var mat = line.sharedMaterial;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", lineColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", lineColor);

        // Visual condition shows the line; audio-only hides it
        line.enabled = condition == NavCondition.Visual;

        if (targetSound != null)
        {
            var p = new Vector3(Target.x, floorY + soundHeight, Target.y);
            if (targetSound.position != p) targetSound.position = p;
        }
    }

    /// Puts the player at the start of the route, facing the first corridor.
    void PlacePlayer()
    {
        if (xrOrigin == null) return;
        var c = Corners;
        Vector2 dir = c[1] - c[0];
        float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

        // CharacterController overrides direct position changes, so pause it briefly
        var cc = xrOrigin.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        xrOrigin.SetPositionAndRotation(new Vector3(c[0].x, floorY, c[0].y), Quaternion.Euler(0f, yaw, 0f));
        if (cc != null) cc.enabled = true;
    }

    void CheckTargetReached()
    {
        if (finished || playerHead == null) return;
        var head = new Vector2(playerHead.position.x, playerHead.position.z);
        if (Vector2.Distance(head, Target) > reachRadius) return;

        finished = true;
        float duration = Time.time - startTime;
        Debug.Log($"[RouteManager] Target reached. Route {route}, condition {condition}, time {duration:F1} s");

        if (targetSound != null)
        {
            var src = targetSound.GetComponent<AudioSource>();
            if (src != null) src.Stop();
        }
    }

    // Shows the route in the Scene view even in the audio-only condition (not visible in VR)
    void OnDrawGizmos()
    {
        var c = Corners;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < c.Length; i++)
        {
            var p = new Vector3(c[i].x, floorY + 0.1f, c[i].y);
            Gizmos.DrawSphere(p, 0.3f);
            if (i > 0) Gizmos.DrawLine(new Vector3(c[i - 1].x, floorY + 0.1f, c[i - 1].y), p);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(Target.x, floorY + 0.1f, Target.y), reachRadius);
    }
}
