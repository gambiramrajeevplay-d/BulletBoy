using System.Collections;
using UnityEngine;

/*
 * Runs BEFORE CannonManager (-50) and BulletBoyPlayer (0), so the cannon's
 * position/rotation is final for the frame when input and flight are processed.
 */
[DefaultExecutionOrder(-100)]
public class BulletCannon : MonoBehaviour
{
    [Header("Cannon Settings")]

    [Tooltip("Enable for a continuously rotating cannon.")]
    [SerializeField] private bool rotateCannon = false;

    [Tooltip("Enable for an up/down moving cannon.")]
    [SerializeField] private bool moveUpDown = false;

    [Tooltip("Enable for a left/right moving cannon.")]
    [SerializeField] private bool moveLeftRight = false;

    [Tooltip("Only used for the cannon's launch speed. CannonManager determines the actual sequence.")]
    [SerializeField] private bool isFinalCannon = false;


    [Header("Cannon Points")]

    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform exitPoint;


    [Header("Launch")]

    [SerializeField] private float launchSpeed = 18f;


    [Header("Rotation")]

    [Tooltip("Degrees per second.")]
    [SerializeField] private float rotateSpeed = 90f;


    [Header("Up / Down Movement")]

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float minY = -2f;
    [SerializeField] private float maxY = 2f;
    [SerializeField] private bool startMovingUp = true;


    [Header("Left / Right Movement")]

    [SerializeField] private float horizontalMoveSpeed = 2f;
    [SerializeField] private float minX = -2f;
    [SerializeField] private float maxX = 2f;
    [SerializeField] private bool startMovingRight = true;


    [Header("Capture (used by the player)")]

    [Tooltip("Capture radius around the Entry Point for MOVING cannons.")]
    [SerializeField] private float movingCannonCaptureRadius = 2.5f;

    [Tooltip("Capture radius around the Entry Point for STATIONARY cannons.")]
    [SerializeField] private float stationaryCaptureRadius = 2.25f;

    [Tooltip("Capture radius around the cannon body for MOVING cannons.")]
    [SerializeField] private float movingBodyRadius = 1.5f;

    [Tooltip("Capture radius around the cannon body for STATIONARY cannons.")]
    [SerializeField] private float stationaryBodyRadius = 1.25f;

    [Tooltip("Extra tolerance added to every capture radius (helps low-FPS devices).")]
    [SerializeField] private float capturePadding = 0.4f;

    [Tooltip("Minimum capture radius for any cannon.")]
    [SerializeField] private float aimRadius = 1.5f;


    [Header("Final Cannon")]

    [SerializeField] private float finalLaunchSpeed = 45f;


    [Header("Cannon Visual")]

    [Tooltip("Assign the visual/mesh child of the cannon here.")]
    [SerializeField] private Transform cannonVisual;


    [Header("Entry Effect")]

    [SerializeField] private float entryRecoilDistance = 0.25f;
    [SerializeField] private float entrySquashAmount = 0.78f;
    [SerializeField] private float entryStretchAmount = 1.08f;
    [SerializeField] private float entrySquashDuration = 0.045f;
    [SerializeField] private float entryStretchDuration = 0.08f;
    [SerializeField] private float entryReturnDuration = 0.10f;


    [Header("Launch Effect")]

    [SerializeField] private float recoilDistance = 0.65f;
    [SerializeField] private float squashDuration = 0.055f;
    [SerializeField] private float stretchDuration = 0.10f;
    [SerializeField] private float returnDuration = 0.12f;
    [SerializeField] private float squashAmount = 0.65f;
    [SerializeField] private float stretchAmount = 1.18f;
    [SerializeField] private float recoilSnap = 1.15f;


    [Header("Launch Particle Effect")]

    [SerializeField] private ParticleSystem launchParticleEffect;
    [SerializeField] private float particleXOffset = -0.35f;
    [SerializeField] private float particleYOffset = 0f;
    [SerializeField] private float particleZOffset = 0f;
    [SerializeField] private float particleLifetime = 2f;


    // =========================================================
    // PRIVATE VARIABLES
    // =========================================================

    private BulletBoyPlayer player;

    private float currentX;
    private float lockedY;
    private float lockedZ;

    private float startingY;
    private float currentY;
    private bool movingUp;

    private float startingX;
    private float currentXPosition;
    private bool movingRight;

    private Vector3 originalVisualPosition;
    private Vector3 originalVisualScale;

    private Coroutine effectCoroutine;

    private bool canMoveVisualWithoutAffectingMovement;

    private Vector3 previousEntryPosition;
    private Vector3 currentEntryPosition;

    private Vector3 previousCannonPosition;
    private Vector3 currentCannonPosition;


    // =========================================================
    // AWAKE / START
    // =========================================================

    private void Awake()
    {
        Vector3 startingRotation = transform.localEulerAngles;

        currentX = startingRotation.x;
        lockedY = startingRotation.y;
        lockedZ = startingRotation.z;

        startingY = transform.localPosition.y;
        currentY = startingY;
        movingUp = startMovingUp;

        startingX = transform.localPosition.x;
        currentXPosition = startingX;
        movingRight = startMovingRight;

        if (cannonVisual == null)
            cannonVisual = transform;

        originalVisualPosition = cannonVisual.localPosition;
        originalVisualScale = cannonVisual.localScale;

        canMoveVisualWithoutAffectingMovement =
            cannonVisual != transform;

        CachePositions();
    }

    private void Start()
    {
        ApplyRotation();
        CachePositions();
    }

    private void CachePositions()
    {
        currentCannonPosition = transform.position;
        previousCannonPosition = currentCannonPosition;

        if (entryPoint != null)
        {
            currentEntryPosition = entryPoint.position;
            previousEntryPosition = currentEntryPosition;
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Positions BEFORE movement.
        previousCannonPosition = transform.position;

        if (entryPoint != null)
            previousEntryPosition = entryPoint.position;

        UpdateRotation();
        UpdateVerticalMovement();
        UpdateHorizontalMovement();

        // Positions AFTER movement.
        currentCannonPosition = transform.position;

        if (entryPoint != null)
            currentEntryPosition = entryPoint.position;
    }


    // =========================================================
    // ROTATION
    // =========================================================

    private void UpdateRotation()
    {
        if (!rotateCannon)
            return;

        currentX += rotateSpeed * Time.deltaTime;

        if (currentX >= 360f)
            currentX -= 360f;

        ApplyRotation();
    }

    private void ApplyRotation()
    {
        transform.localRotation =
            Quaternion.Euler(currentX, lockedY, lockedZ);
    }


    // =========================================================
    // UP / DOWN MOVEMENT
    // =========================================================

    private void UpdateVerticalMovement()
    {
        if (!moveUpDown)
            return;

        float direction = movingUp ? 1f : -1f;

        float newY =
            currentY + direction * moveSpeed * Time.deltaTime;

        if (newY >= startingY + maxY)
        {
            newY = startingY + maxY;
            movingUp = false;
        }

        if (newY <= startingY + minY)
        {
            newY = startingY + minY;
            movingUp = true;
        }

        currentY = newY;

        Vector3 position = transform.localPosition;
        position.y = currentY;
        transform.localPosition = position;
    }


    // =========================================================
    // LEFT / RIGHT MOVEMENT
    // =========================================================

    private void UpdateHorizontalMovement()
    {
        if (!moveLeftRight)
            return;

        float direction = movingRight ? 1f : -1f;

        float newX =
            currentXPosition +
            direction * horizontalMoveSpeed * Time.deltaTime;

        if (newX >= startingX + maxX)
        {
            newX = startingX + maxX;
            movingRight = false;
        }

        if (newX <= startingX + minX)
        {
            newX = startingX + minX;
            movingRight = true;
        }

        currentXPosition = newX;

        Vector3 position = transform.localPosition;
        position.x = currentXPosition;
        transform.localPosition = position;
    }


    // =========================================================
    // ENTER CANNON
    // =========================================================

    public void EnterCannon(BulletBoyPlayer bulletPlayer)
    {
        if (bulletPlayer == null)
            return;

        if (entryPoint == null)
        {
            Debug.LogError(gameObject.name + ": Entry Point is missing.");
            return;
        }

        if (exitPoint == null)
        {
            Debug.LogError(gameObject.name + ": Exit Point is missing.");
            return;
        }

        player = bulletPlayer;

        player.EnterCannon(this);

        PlayEntryEffect();
    }


    // =========================================================
    // LAUNCH PLAYER
    // =========================================================

    public void LaunchPlayer()
    {
        if (player == null)
        {
            Debug.LogWarning(
                gameObject.name + ": No player is inside this cannon."
            );
            return;
        }

        if (exitPoint == null)
        {
            Debug.LogError(gameObject.name + ": Exit Point is missing.");
            return;
        }

        float speed = GetLaunchSpeed();

        // Read the direction NOW, before the recoil effect runs.
        Vector3 launchDirection = exitPoint.forward;
        Quaternion launchRotation = GetCannonRotation();

        SpawnLaunchParticle();
        PlayLaunchEffect();

        player.LaunchFromCannon(
            exitPoint,
            launchDirection,
            speed,
            launchRotation
        );

        player = null;
    }


    // =========================================================
    // PARTICLE
    // =========================================================

    private void SpawnLaunchParticle()
    {
        if (launchParticleEffect == null || entryPoint == null)
            return;

        Vector3 localOffset =
            new Vector3(particleXOffset, particleYOffset, particleZOffset);

        Vector3 spawnPosition = entryPoint.TransformPoint(localOffset);

        ParticleSystem spawnedParticle =
            Instantiate(
                launchParticleEffect,
                spawnPosition,
                entryPoint.rotation
            );

        spawnedParticle.transform.SetParent(transform, true);
        spawnedParticle.Play();

        Destroy(spawnedParticle.gameObject, particleLifetime);
    }


    // =========================================================
    // EFFECTS
    // =========================================================

    private void PlayEntryEffect()
    {
        StartEffect(
            entryRecoilDistance,
            entrySquashAmount,
            entryStretchAmount,
            entrySquashDuration,
            entryStretchDuration,
            entryReturnDuration,
            0.9f
        );
    }

    private void PlayLaunchEffect()
    {
        StartEffect(
            recoilDistance,
            squashAmount,
            stretchAmount,
            squashDuration,
            stretchDuration,
            returnDuration,
            recoilSnap
        );
    }

    private void StartEffect(
        float recoil,
        float squash,
        float stretch,
        float squashTime,
        float stretchTime,
        float returnTime,
        float snap)
    {
        if (cannonVisual == null)
            return;

        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        if (canMoveVisualWithoutAffectingMovement)
            cannonVisual.localPosition = originalVisualPosition;

        cannonVisual.localScale = originalVisualScale;

        effectCoroutine =
            StartCoroutine(
                CannonEffectRoutine(
                    recoil, squash, stretch,
                    squashTime, stretchTime, returnTime, snap
                )
            );
    }

    private IEnumerator CannonEffectRoutine(
        float recoil,
        float squash,
        float stretch,
        float squashTime,
        float stretchTime,
        float returnTime,
        float snap)
    {
        Vector3 normalPosition = originalVisualPosition;
        Vector3 normalScale = originalVisualScale;

        Vector3 recoilPosition =
            normalPosition - Vector3.forward * recoil * snap;

        Vector3 squashScale = normalScale;
        squashScale.z = normalScale.z * squash;

        Vector3 stretchScale = normalScale;
        stretchScale.z = normalScale.z * stretch;

        // Squash
        yield return AnimateVisual(
            normalPosition, recoilPosition,
            normalScale, squashScale,
            squashTime, true
        );

        // Stretch
        yield return AnimateVisual(
            recoilPosition, normalPosition,
            squashScale, stretchScale,
            stretchTime, false
        );

        // Return
        yield return AnimateVisual(
            normalPosition, originalVisualPosition,
            stretchScale, originalVisualScale,
            returnTime, false
        );

        effectCoroutine = null;
    }

    private IEnumerator AnimateVisual(
        Vector3 fromPosition,
        Vector3 toPosition,
        Vector3 fromScale,
        Vector3 toScale,
        float duration,
        bool easeOutQuart)
    {
        duration = Mathf.Max(0.0001f, duration);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            float smoothT =
                easeOutQuart
                    ? 1f - Mathf.Pow(1f - t, 4f)
                    : Mathf.SmoothStep(0f, 1f, t);

            if (canMoveVisualWithoutAffectingMovement)
            {
                cannonVisual.localPosition =
                    Vector3.Lerp(fromPosition, toPosition, smoothT);
            }

            cannonVisual.localScale =
                Vector3.Lerp(fromScale, toScale, smoothT);

            yield return null;
        }

        if (canMoveVisualWithoutAffectingMovement)
            cannonVisual.localPosition = toPosition;

        cannonVisual.localScale = toScale;
    }


    // =========================================================
    // GETTERS
    // =========================================================

    public Transform GetEntryPoint()
    {
        return entryPoint;
    }

    public Transform GetExitPoint()
    {
        return exitPoint;
    }

    public Vector3 GetPreviousEntryPosition()
    {
        return previousEntryPosition;
    }

    public Vector3 GetCurrentEntryPosition()
    {
        if (entryPoint != null)
            return entryPoint.position;

        return currentEntryPosition;
    }

    public Vector3 GetPreviousCannonPosition()
    {
        return previousCannonPosition;
    }

    public Vector3 GetCurrentCannonPosition()
    {
        return transform.position;
    }

    public Quaternion GetCannonRotation()
    {
        return transform.rotation;
    }

    public float GetLaunchSpeed()
    {
        return isFinalCannon ? finalLaunchSpeed : launchSpeed;
    }

    public float GetAimRadius()
    {
        return aimRadius;
    }

    public bool IsMoving()
    {
        return moveLeftRight || moveUpDown;
    }

    /// <summary>Radius around the Entry Point that captures the player.</summary>
    public float GetCaptureRadius()
    {
        float radius =
            IsMoving()
                ? movingCannonCaptureRadius
                : stationaryCaptureRadius;

        return Mathf.Max(radius, aimRadius) +
               Mathf.Max(0f, capturePadding);
    }

    /// <summary>Radius around the cannon body that captures the player.</summary>
    public float GetBodyRadius()
    {
        float radius =
            IsMoving()
                ? movingBodyRadius
                : stationaryBodyRadius;

        return radius + Mathf.Max(0f, capturePadding);
    }

    /// <summary>
    /// Total distance this cannon can travel (world units).
    /// Used so the player is never declared "missed" while the cannon
    /// can still move into its path.
    /// </summary>
    public float GetMovementExtent()
    {
        float extent = 0f;

        if (moveLeftRight)
            extent = Mathf.Max(extent, Mathf.Abs(maxX - minX));

        if (moveUpDown)
            extent = Mathf.Max(extent, Mathf.Abs(maxY - minY));

        if (extent <= 0f)
            return 0f;

        float scale = 1f;

        if (transform.parent != null)
        {
            Vector3 s = transform.parent.lossyScale;

            scale =
                Mathf.Max(
                    Mathf.Abs(s.x),
                    Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z))
                );
        }

        return extent * scale;
    }

    public bool IsRotating()
    {
        return rotateCannon;
    }

    public bool IsMovingUpDown()
    {
        return moveUpDown;
    }

    public bool IsMovingLeftRight()
    {
        return moveLeftRight;
    }

    public bool IsFinalCannon()
    {
        return isFinalCannon;
    }


    // =========================================================
    // SEGMENT DISTANCE (shared helper)
    // =========================================================

    public static float SegmentSegmentDistance(
        Vector3 p1,
        Vector3 q1,
        Vector3 p2,
        Vector3 q2)
    {
        Vector3 d1 = q1 - p1;
        Vector3 d2 = q2 - p2;
        Vector3 r = p1 - p2;

        float a = Vector3.Dot(d1, d1);
        float e = Vector3.Dot(d2, d2);
        float f = Vector3.Dot(d2, r);

        float s;
        float t;

        const float epsilon = 0.000001f;

        if (a <= epsilon && e <= epsilon)
            return Vector3.Distance(p1, p2);

        if (a <= epsilon)
        {
            s = 0f;
            t = Mathf.Clamp01(f / e);
        }
        else
        {
            float c = Vector3.Dot(d1, r);

            if (e <= epsilon)
            {
                t = 0f;
                s = Mathf.Clamp01(-c / a);
            }
            else
            {
                float b = Vector3.Dot(d1, d2);
                float denominator = a * e - b * b;

                if (Mathf.Abs(denominator) > epsilon)
                    s = Mathf.Clamp01((b * f - c * e) / denominator);
                else
                    s = 0f;

                float tNominal = b * s + f;

                if (tNominal < 0f)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else if (tNominal > e)
                {
                    t = 1f;
                    s = Mathf.Clamp01((b - c) / a);
                }
                else
                {
                    t = tNominal / e;
                }
            }
        }

        Vector3 closestPoint1 = p1 + d1 * s;
        Vector3 closestPoint2 = p2 + d2 * t;

        return Vector3.Distance(closestPoint1, closestPoint2);
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        if (entryPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(entryPoint.position, 0.15f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(entryPoint.position, GetCaptureRadius());
        }

        if (exitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(exitPoint.position, 0.2f);

            Gizmos.DrawLine(
                exitPoint.position,
                exitPoint.position + exitPoint.forward * 3f
            );
        }
    }
}