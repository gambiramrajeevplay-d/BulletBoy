using System.Collections;
using UnityEngine;

/*
 * Runs BEFORE CannonManager (-50) and BulletBoyPlayer (0), so the cannon's
 * position/rotation is final for the frame when input and flight are processed.
 */
[DefaultExecutionOrder(-100)]
public class BulletCannon : MonoBehaviour
{
    // =========================================================
    // CANNON SETTINGS
    // =========================================================

    [Header("Cannon Settings")]

    [Tooltip("Enable for a continuously rotating cannon.")]
    [SerializeField] private bool rotateCannon = false;

    [Tooltip("Enable for an up/down moving cannon.")]
    [SerializeField] private bool moveUpDown = false;

    [Tooltip("Enable for a left/right moving cannon.")]
    [SerializeField] private bool moveLeftRight = false;

    [Tooltip("Only used for the cannon's launch speed.")]
    [SerializeField] private bool isFinalCannon = false;


    // =========================================================
    // AUTOMATIC LAUNCH
    // =========================================================

    [Header("Automatic Launch")]

    [Tooltip("If enabled, the player automatically launches after entering this cannon.")]
    [SerializeField] private bool automaticLaunch = false;

    [Tooltip("Delay after entering the cannon before automatic launch.")]
    [SerializeField] private float automaticLaunchDelay = 0.1f;


    // =========================================================
    // CANNON POINTS
    // =========================================================

    [Header("Cannon Points")]

    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform exitPoint;


    // =========================================================
    // LAUNCH
    // =========================================================

    [Header("Launch")]

    [SerializeField] private float launchSpeed = 18f;


    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Rotation")]

    [Tooltip("Degrees per second.")]
    [SerializeField] private float rotateSpeed = 90f;


    // =========================================================
    // UP / DOWN MOVEMENT
    // =========================================================

    [Header("Up / Down Movement")]

    [SerializeField] private float moveSpeed = 2f;

    [SerializeField] private float minY = -2f;

    [SerializeField] private float maxY = 2f;

    [SerializeField] private bool startMovingUp = true;


    // =========================================================
    // LEFT / RIGHT MOVEMENT
    // =========================================================

    [Header("Left / Right Movement")]

    [SerializeField] private float horizontalMoveSpeed = 2f;

    [SerializeField] private float minX = -2f;

    [SerializeField] private float maxX = 2f;

    [SerializeField] private bool startMovingRight = true;


    // =========================================================
    // CAPTURE
    // =========================================================

    [Header("Capture")]

    [Tooltip("Capture radius around the Entry Point for MOVING cannons.")]
    [SerializeField] private float movingCannonCaptureRadius = 2.5f;

    [Tooltip("Capture radius around the Entry Point for STATIONARY cannons.")]
    [SerializeField] private float stationaryCaptureRadius = 2.25f;

    [Tooltip("Capture radius around the cannon body for MOVING cannons.")]
    [SerializeField] private float movingBodyRadius = 1.5f;

    [Tooltip("Capture radius around the cannon body for STATIONARY cannons.")]
    [SerializeField] private float stationaryBodyRadius = 1.25f;

    [Tooltip("Extra tolerance added to every capture radius.")]
    [SerializeField] private float capturePadding = 0.4f;

    [Tooltip("Minimum capture radius for any cannon.")]
    [SerializeField] private float aimRadius = 1.5f;


    // =========================================================
    // HIGH SPEED CAPTURE
    // =========================================================

    [Header("High Speed Capture Assist")]

    [Tooltip(
        "Extra radius available to the player capture system " +
        "when approaching this cannon at high speed."
    )]
    [SerializeField] private float highSpeedCaptureRadius = 1.5f;


    // =========================================================
    // FINAL CANNON
    // =========================================================

    [Header("Final Cannon")]

    [SerializeField] private float finalLaunchSpeed = 45f;


    // =========================================================
    // CANNON VISUAL
    // =========================================================

    [Header("Cannon Visual")]

    [Tooltip("Assign the visual/mesh child of the cannon here.")]
    [SerializeField] private Transform cannonVisual;


    // =========================================================
    // ENTRY EFFECT
    // =========================================================

    [Header("Entry Effect")]

    [SerializeField] private float entryRecoilDistance = 0.25f;

    [SerializeField] private float entrySquashAmount = 0.78f;

    [SerializeField] private float entryStretchAmount = 1.08f;

    [SerializeField] private float entrySquashDuration = 0.045f;

    [SerializeField] private float entryStretchDuration = 0.08f;

    [SerializeField] private float entryReturnDuration = 0.10f;


    // =========================================================
    // NORMAL LAUNCH EFFECT
    // =========================================================

    [Header("Launch Effect")]

    [SerializeField] private float recoilDistance = 0.65f;

    [SerializeField] private float squashDuration = 0.055f;

    [SerializeField] private float stretchDuration = 0.10f;

    [SerializeField] private float returnDuration = 0.12f;

    [SerializeField] private float squashAmount = 0.65f;

    [SerializeField] private float stretchAmount = 1.18f;

    [SerializeField] private float recoilSnap = 1.15f;


    // =========================================================
    // FINAL LAUNCH EFFECT
    // =========================================================

    [Header("FINAL LAUNCH - BIG EFFECT")]

    [Tooltip("How far the cannon visually recoils.")]
    [SerializeField] private float finalRecoilDistance = 0.85f;

    [Tooltip("Final cannon squash scale. Lower = bigger squash.")]
    [SerializeField] private float finalSquashAmount = 0.42f;

    [Tooltip("Final cannon stretch scale. Higher = bigger stretch.")]
    [SerializeField] private float finalStretchAmount = 1.35f;

    [Tooltip("How quickly the cannon stretches.")]
    [SerializeField] private float finalStretchDuration = 0.18f;

    [Tooltip("How long the cannon stays stretched before the launch.")]
    [SerializeField] private float finalLaunchDelay = 0.12f;

    [Tooltip("How quickly the cannon snaps into the big squash.")]
    [SerializeField] private float finalSquashDuration = 0.08f;

    [Tooltip("How long the cannon takes to return to normal.")]
    [SerializeField] private float finalReturnDuration = 0.16f;

    [Tooltip("Strength of the recoil movement.")]
    [SerializeField] private float finalRecoilSnap = 1.25f;


    // =========================================================
    // LAUNCH PARTICLE
    // =========================================================

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
    private Coroutine automaticLaunchCoroutine;
    private Coroutine finalLaunchCoroutine;

    private bool canMoveVisualWithoutAffectingMovement;
    private bool launchInProgress;

    private Vector3 previousEntryPosition;
    private Vector3 currentEntryPosition;

    private Vector3 previousCannonPosition;
    private Vector3 currentCannonPosition;


    // =========================================================
    // AWAKE
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


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        ApplyRotation();
        CachePositions();
    }


    // =========================================================
    // CACHE POSITIONS
    // =========================================================

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
        previousCannonPosition = transform.position;

        if (entryPoint != null)
            previousEntryPosition = entryPoint.position;

        UpdateRotation();
        UpdateVerticalMovement();
        UpdateHorizontalMovement();

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

        if (currentX < 0f)
            currentX += 360f;

        ApplyRotation();
    }


    // =========================================================
    // APPLY ROTATION
    // =========================================================

    private void ApplyRotation()
    {
        transform.localRotation =
            Quaternion.Euler(
                currentX,
                lockedY,
                lockedZ
            );
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
            currentY +
            direction *
            moveSpeed *
            Time.deltaTime;

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
            direction *
            horizontalMoveSpeed *
            Time.deltaTime;

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

        if (launchInProgress)
            return;

        if (entryPoint == null)
        {
            Debug.LogError(
                gameObject.name +
                ": Entry Point is missing."
            );

            return;
        }

        if (exitPoint == null)
        {
            Debug.LogError(
                gameObject.name +
                ": Exit Point is missing."
            );

            return;
        }

        player = bulletPlayer;

        player.EnterCannon(this);

        PlayEntryEffect();

        if (automaticLaunch)
        {
            if (automaticLaunchCoroutine != null)
                StopCoroutine(automaticLaunchCoroutine);

            automaticLaunchCoroutine =
                StartCoroutine(
                    AutomaticLaunchRoutine()
                );
        }
    }


    // =========================================================
    // AUTOMATIC LAUNCH
    // =========================================================

    private IEnumerator AutomaticLaunchRoutine()
    {
        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                automaticLaunchDelay
            )
        );

        automaticLaunchCoroutine = null;

        if (player == null)
            yield break;

        CannonManager manager =
            FindObjectOfType<CannonManager>();

        if (manager != null)
        {
            if (!manager.PrepareAutomaticLaunch(this))
            {
                Debug.LogWarning(
                    gameObject.name +
                    ": Automatic launch was cancelled because the cannon " +
                    "sequence target could not be prepared."
                );

                yield break;
            }
        }

        LaunchPlayer();
    }


    // =========================================================
    // LAUNCH PLAYER
    // =========================================================

    public void LaunchPlayer()
    {
        if (player == null)
        {
            Debug.LogWarning(
                gameObject.name +
                ": No player is inside this cannon."
            );

            return;
        }

        if (exitPoint == null)
        {
            Debug.LogError(
                gameObject.name +
                ": Exit Point is missing."
            );

            return;
        }

        if (launchInProgress)
            return;

        launchInProgress = true;

        if (automaticLaunchCoroutine != null)
        {
            StopCoroutine(
                automaticLaunchCoroutine
            );

            automaticLaunchCoroutine = null;
        }

        /*
         * FINAL CANNON:
         *
         * Do NOT launch immediately.
         *
         * Sequence:
         * 1. Stretch
         * 2. Wait
         * 3. Big squash
         * 4. Launch player
         * 5. Return cannon to normal
         */
        if (isFinalCannon)
        {
            if (finalLaunchCoroutine != null)
                StopCoroutine(finalLaunchCoroutine);

            finalLaunchCoroutine =
                StartCoroutine(
                    FinalLaunchRoutine()
                );

            return;
        }

        /*
         * NORMAL CANNON:
         * Launch immediately.
         */
        LaunchPlayerImmediately();
    }


    // =========================================================
    // NORMAL IMMEDIATE LAUNCH
    // =========================================================

    private void LaunchPlayerImmediately()
    {
        if (player == null)
        {
            launchInProgress = false;
            return;
        }

        if (exitPoint == null)
        {
            launchInProgress = false;
            return;
        }

        float speed = GetLaunchSpeed();

        Vector3 launchDirection =
            exitPoint.forward;

        Quaternion launchRotation =
            GetCannonRotation();

        SpawnLaunchParticle();

        PlayLaunchEffect();

        player.LaunchFromCannon(
            exitPoint,
            launchDirection,
            speed,
            launchRotation
        );

        player = null;

        launchInProgress = false;
    }


    // =========================================================
    // FINAL LAUNCH ROUTINE
    // =========================================================

    private IEnumerator FinalLaunchRoutine()
    {
        if (player == null || exitPoint == null)
        {
            launchInProgress = false;
            finalLaunchCoroutine = null;
            yield break;
        }

        /*
         * -----------------------------------------------------
         * STEP 1
         * BIG STRETCH
         * -----------------------------------------------------
         */

        yield return PlayFinalStretchEffect();


        /*
         * -----------------------------------------------------
         * STEP 2
         * DELAY / HOLD
         * -----------------------------------------------------
         *
         * The cannon stays stretched here.
         *
         * This creates the feeling that the cannon is
         * charging before the final launch.
         */

        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                finalLaunchDelay
            )
        );


        /*
         * -----------------------------------------------------
         * STEP 3
         * BIG SQUASH
         * -----------------------------------------------------
         *
         * The cannon rapidly compresses.
         */

        yield return PlayFinalSquashEffect();


        /*
         * -----------------------------------------------------
         * STEP 4
         * LAUNCH AT THE SQUASH MOMENT
         * -----------------------------------------------------
         */

        if (player != null && exitPoint != null)
        {
            float speed =
                GetLaunchSpeed();

            /*
             * IMPORTANT:
             *
             * Direction is read HERE, at the actual launch
             * moment rather than before the stretch/delay.
             *
             * This keeps the launch direction correct even if
             * the cannon is rotating.
             */
            Vector3 launchDirection =
                exitPoint.forward;

            Quaternion launchRotation =
                GetCannonRotation();

            SpawnLaunchParticle();

            player.LaunchFromCannon(
                exitPoint,
                launchDirection,
                speed,
                launchRotation
            );

            player = null;
        }


        /*
         * -----------------------------------------------------
         * STEP 5
         * RETURN TO NORMAL
         * -----------------------------------------------------
         */

        yield return PlayFinalReturnEffect();

        finalLaunchCoroutine = null;
        launchInProgress = false;
    }


    // =========================================================
    // FINAL STRETCH
    // =========================================================

    private IEnumerator PlayFinalStretchEffect()
    {
        if (cannonVisual == null)
            yield break;

        Vector3 normalPosition =
            originalVisualPosition;

        Vector3 stretchPosition =
            originalVisualPosition -
            Vector3.forward *
            finalRecoilDistance *
            finalRecoilSnap;

        Vector3 normalScale =
            originalVisualScale;

        Vector3 stretchScale =
            normalScale;

        stretchScale.z =
            normalScale.z *
            finalStretchAmount;

        /*
         * Start from normal.
         */
        cannonVisual.localPosition =
            normalPosition;

        cannonVisual.localScale =
            normalScale;


        /*
         * Stretch the cannon.
         */
        yield return AnimateVisual(
            normalPosition,
            stretchPosition,
            normalScale,
            stretchScale,
            finalStretchDuration,
            true
        );
    }


    // =========================================================
    // FINAL SQUASH
    // =========================================================

    private IEnumerator PlayFinalSquashEffect()
    {
        if (cannonVisual == null)
            yield break;

        Vector3 stretchPosition =
            originalVisualPosition -
            Vector3.forward *
            finalRecoilDistance *
            finalRecoilSnap;

        Vector3 squashPosition =
            originalVisualPosition;

        Vector3 stretchScale =
            originalVisualScale;

        stretchScale.z =
            originalVisualScale.z *
            finalStretchAmount;

        Vector3 squashScale =
            originalVisualScale;

        squashScale.z =
            originalVisualScale.z *
            finalSquashAmount;

        /*
         * VERY FAST transition:
         *
         * STRETCH
         *      ↓
         * BIG SQUASH
         *
         * The player launches immediately after this.
         */
        yield return AnimateVisual(
            stretchPosition,
            squashPosition,
            stretchScale,
            squashScale,
            finalSquashDuration,
            true
        );
    }


    // =========================================================
    // FINAL RETURN
    // =========================================================

    private IEnumerator PlayFinalReturnEffect()
    {
        if (cannonVisual == null)
            yield break;

        Vector3 squashPosition =
            originalVisualPosition;

        Vector3 squashScale =
            originalVisualScale;

        squashScale.z =
            originalVisualScale.z *
            finalSquashAmount;

        yield return AnimateVisual(
            squashPosition,
            originalVisualPosition,
            squashScale,
            originalVisualScale,
            finalReturnDuration,
            false
        );
    }


    // =========================================================
    // PARTICLE
    // =========================================================

    private void SpawnLaunchParticle()
    {
        if (launchParticleEffect == null ||
            entryPoint == null)
        {
            return;
        }

        Vector3 localOffset =
            new Vector3(
                particleXOffset,
                particleYOffset,
                particleZOffset
            );

        Vector3 spawnPosition =
            entryPoint.TransformPoint(
                localOffset
            );

        ParticleSystem spawnedParticle =
            Instantiate(
                launchParticleEffect,
                spawnPosition,
                entryPoint.rotation
            );

        spawnedParticle.transform.SetParent(
            transform,
            true
        );

        spawnedParticle.Play();

        Destroy(
            spawnedParticle.gameObject,
            particleLifetime
        );
    }


    // =========================================================
    // ENTRY EFFECT
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


    // =========================================================
    // NORMAL LAUNCH EFFECT
    // =========================================================

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


    // =========================================================
    // START EFFECT
    // =========================================================

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
        {
            StopCoroutine(
                effectCoroutine
            );
        }

        if (canMoveVisualWithoutAffectingMovement)
        {
            cannonVisual.localPosition =
                originalVisualPosition;
        }

        cannonVisual.localScale =
            originalVisualScale;

        effectCoroutine =
            StartCoroutine(
                CannonEffectRoutine(
                    recoil,
                    squash,
                    stretch,
                    squashTime,
                    stretchTime,
                    returnTime,
                    snap
                )
            );
    }


    // =========================================================
    // CANNON EFFECT ROUTINE
    // =========================================================

    private IEnumerator CannonEffectRoutine(
        float recoil,
        float squash,
        float stretch,
        float squashTime,
        float stretchTime,
        float returnTime,
        float snap)
    {
        Vector3 normalPosition =
            originalVisualPosition;

        Vector3 normalScale =
            originalVisualScale;

        Vector3 recoilPosition =
            normalPosition -
            Vector3.forward *
            recoil *
            snap;

        Vector3 squashScale =
            normalScale;

        squashScale.z =
            normalScale.z *
            squash;

        Vector3 stretchScale =
            normalScale;

        stretchScale.z =
            normalScale.z *
            stretch;


        // =====================================================
        // SQUASH
        // =====================================================

        yield return AnimateVisual(
            normalPosition,
            recoilPosition,
            normalScale,
            squashScale,
            squashTime,
            true
        );


        // =====================================================
        // STRETCH
        // =====================================================

        yield return AnimateVisual(
            recoilPosition,
            normalPosition,
            squashScale,
            stretchScale,
            stretchTime,
            false
        );


        // =====================================================
        // RETURN
        // =====================================================

        yield return AnimateVisual(
            normalPosition,
            originalVisualPosition,
            stretchScale,
            originalVisualScale,
            returnTime,
            false
        );

        effectCoroutine = null;
    }


    // =========================================================
    // ANIMATE VISUAL
    // =========================================================

    private IEnumerator AnimateVisual(
        Vector3 fromPosition,
        Vector3 toPosition,
        Vector3 fromScale,
        Vector3 toScale,
        float duration,
        bool easeOutQuart)
    {
        duration =
            Mathf.Max(
                0.0001f,
                duration
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float smoothT =
                easeOutQuart
                    ? 1f -
                      Mathf.Pow(
                          1f - t,
                          4f
                      )
                    : Mathf.SmoothStep(
                        0f,
                        1f,
                        t
                    );

            if (canMoveVisualWithoutAffectingMovement)
            {
                cannonVisual.localPosition =
                    Vector3.Lerp(
                        fromPosition,
                        toPosition,
                        smoothT
                    );
            }

            cannonVisual.localScale =
                Vector3.Lerp(
                    fromScale,
                    toScale,
                    smoothT
                );

            yield return null;
        }

        if (canMoveVisualWithoutAffectingMovement)
        {
            cannonVisual.localPosition =
                toPosition;
        }

        cannonVisual.localScale =
            toScale;
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
        return isFinalCannon
            ? finalLaunchSpeed
            : launchSpeed;
    }


    public float GetAimRadius()
    {
        return aimRadius;
    }


    public bool IsMoving()
    {
        return moveLeftRight ||
               moveUpDown;
    }


    // =========================================================
    // CAPTURE RADIUS
    // =========================================================

    public float GetCaptureRadius()
    {
        float radius =
            IsMoving()
                ? movingCannonCaptureRadius
                : stationaryCaptureRadius;

        return Mathf.Max(
            radius,
            aimRadius
        ) +
        Mathf.Max(
            0f,
            capturePadding
        );
    }


    // =========================================================
    // HIGH SPEED CAPTURE RADIUS
    // =========================================================

    public float GetHighSpeedCaptureRadius()
    {
        return Mathf.Max(
            0f,
            highSpeedCaptureRadius
        );
    }


    // =========================================================
    // BODY RADIUS
    // =========================================================

    public float GetBodyRadius()
    {
        float radius =
            IsMoving()
                ? movingBodyRadius
                : stationaryBodyRadius;

        return radius +
               Mathf.Max(
                   0f,
                   capturePadding
               );
    }


    // =========================================================
    // MOVEMENT EXTENT
    // =========================================================

    public float GetMovementExtent()
    {
        float extent = 0f;

        if (moveLeftRight)
        {
            extent =
                Mathf.Max(
                    extent,
                    Mathf.Abs(
                        maxX - minX
                    )
                );
        }

        if (moveUpDown)
        {
            extent =
                Mathf.Max(
                    extent,
                    Mathf.Abs(
                        maxY - minY
                    )
                );
        }

        if (extent <= 0f)
            return 0f;

        float scale = 1f;

        if (transform.parent != null)
        {
            Vector3 s =
                transform.parent.lossyScale;

            scale =
                Mathf.Max(
                    Mathf.Abs(s.x),
                    Mathf.Max(
                        Mathf.Abs(s.y),
                        Mathf.Abs(s.z)
                    )
                );
        }

        return extent * scale;
    }


    // =========================================================
    // STATE GETTERS
    // =========================================================

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


    public bool IsAutomaticLaunch()
    {
        return automaticLaunch;
    }


    // =========================================================
    // SEGMENT / SEGMENT DISTANCE
    // =========================================================

    public static float SegmentSegmentDistance(
        Vector3 p1,
        Vector3 q1,
        Vector3 p2,
        Vector3 q2)
    {
        Vector3 d1 =
            q1 - p1;

        Vector3 d2 =
            q2 - p2;

        Vector3 r =
            p1 - p2;

        float a =
            Vector3.Dot(
                d1,
                d1
            );

        float e =
            Vector3.Dot(
                d2,
                d2
            );

        float f =
            Vector3.Dot(
                d2,
                r
            );

        float s;
        float t;

        const float epsilon =
            0.000001f;


        // Both segments are points.
        if (a <= epsilon &&
            e <= epsilon)
        {
            return Vector3.Distance(
                p1,
                p2
            );
        }


        // First segment is a point.
        if (a <= epsilon)
        {
            s = 0f;

            t =
                Mathf.Clamp01(
                    f / e
                );
        }
        else
        {
            float c =
                Vector3.Dot(
                    d1,
                    r
                );


            // Second segment is a point.
            if (e <= epsilon)
            {
                t = 0f;

                s =
                    Mathf.Clamp01(
                        -c / a
                    );
            }
            else
            {
                float b =
                    Vector3.Dot(
                        d1,
                        d2
                    );

                float denominator =
                    a * e -
                    b * b;

                if (Mathf.Abs(
                        denominator) >
                    epsilon)
                {
                    s =
                        Mathf.Clamp01(
                            (b * f -
                             c * e) /
                            denominator
                        );
                }
                else
                {
                    s = 0f;
                }

                float tNominal =
                    b * s + f;

                if (tNominal < 0f)
                {
                    t = 0f;

                    s =
                        Mathf.Clamp01(
                            -c / a
                        );
                }
                else if (tNominal > e)
                {
                    t = 1f;

                    s =
                        Mathf.Clamp01(
                            (b - c) / a
                        );
                }
                else
                {
                    t =
                        tNominal / e;
                }
            }
        }

        Vector3 closestPoint1 =
            p1 + d1 * s;

        Vector3 closestPoint2 =
            p2 + d2 * t;

        return Vector3.Distance(
            closestPoint1,
            closestPoint2
        );
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        if (entryPoint != null)
        {
            // Entry point.
            Gizmos.color =
                Color.green;

            Gizmos.DrawSphere(
                entryPoint.position,
                0.15f
            );


            // Normal capture radius.
            Gizmos.color =
                Color.cyan;

            Gizmos.DrawWireSphere(
                entryPoint.position,
                GetCaptureRadius()
            );


            // High-speed assist radius.
            Gizmos.color =
                Color.yellow;

            Gizmos.DrawWireSphere(
                entryPoint.position,
                GetCaptureRadius() +
                GetHighSpeedCaptureRadius()
            );
        }


        if (exitPoint != null)
        {
            Gizmos.color =
                Color.yellow;

            Gizmos.DrawSphere(
                exitPoint.position,
                0.2f
            );

            Gizmos.DrawLine(
                exitPoint.position,
                exitPoint.position +
                exitPoint.forward * 3f
            );
        }
    }
}