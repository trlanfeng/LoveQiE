using UnityEngine;

/// <summary>Ordered sprite frames for a car drawn facing up. Gameplay owns move/stop timing.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CarSpriteAnimator : MonoBehaviour
{
    [Tooltip("Ordered driving frames. Frame zero is also the parked sprite. All frames face up.")]
    [SerializeField] private Sprite[] driveFrames = new Sprite[0];
    [Min(1f)] [SerializeField] private float framesPerSecond = 20f;
    private SpriteRenderer spriteRenderer;
    private float animationTime;
    public bool IsPlaying { get; private set; }
    public int FrameIndex { get; private set; }
    public int FrameCount => driveFrames == null ? 0 : driveFrames.Length;
    public Vector2 Facing { get; private set; } = Vector2.up;

    private void Awake() { spriteRenderer = GetComponent<SpriteRenderer>(); }
    private void OnEnable() { ResetPose(); }

    public void BeginMove(Vector3 worldDelta)
    {
        if (worldDelta.sqrMagnitude <= Mathf.Epsilon) return;
        Facing = new Vector2(worldDelta.x, worldDelta.y).normalized;
        // Use the actual world displacement: the red car has mirrored horizontal controls.
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg - 90f);
        animationTime = 0;
        IsPlaying = true;
        ShowFrame(0);
    }

    private void Update()
    {
        if (!IsPlaying || FrameCount == 0) return;
        animationTime += Time.deltaTime;
        ShowFrame(Mathf.FloorToInt(animationTime * Mathf.Max(1f, framesPerSecond)) % FrameCount);
    }

    public void Stop()
    {
        IsPlaying = false;
        animationTime = 0;
        ShowFrame(0);
    }

    public void ResetPose()
    {
        Stop();
        Facing = Vector2.up;
        transform.rotation = Quaternion.identity;
    }

    private void ShowFrame(int index)
    {
        FrameIndex = index;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (FrameCount > index && driveFrames[index] != null) spriteRenderer.sprite = driveFrames[index];
    }

    private void OnDisable() { Stop(); }
}
