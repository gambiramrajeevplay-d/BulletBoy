using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(0)]
[RequireComponent(typeof(Rigidbody))]
public class BulletBoyPlayer : MonoBehaviour
{
    // =========================================================
    // PLAYER ROTATION
    // =========================================================

    [Header("Player Rotation")]

    [SerializeField]
    private Vector3 playerBaseRotation =
        new Vector3(0f, 0f, -90f);


    // =========================================================
    // FLIGHT
    // =========================================================

    [Header("Flight")]

    [SerializeField]
    private float flightSpeed = 18f;

    [SerializeField]
    private float maxStepDistance = 0.4f;

    [SerializeField]
    private bool flattenLaunchDirection = true;

    [SerializeField]
    private float maxFlightTime = 10f;


    // =========================================================
    // CANNON SEQUENCE GUIDANCE
    // =========================================================

    [Header("Cannon Sequence Guidance")]

    [SerializeField]
    private bool useSequenceGuidance = true;

    [Tooltip("Extra safety switch. When OFF, tapping never automatically steers the player toward the target cannon. The player must already be travelling toward it.")]
    [SerializeField]
    private bool allowAutomaticAimToTarget = false;

    [SerializeField]
    private float sequenceGuidanceDelay = 0.12f;

    [SerializeField]
    private float sequenceGuidanceStrength = 7f;

    [SerializeField]
    private float sequenceGuidanceTurnRate = 360f;

    [Tooltip("Legacy value kept for inspector compatibility. Direct-distance capture is disabled; capture now requires a valid aimed flight path.")]
    [SerializeField]
    private float sequenceDirectCaptureDistance = 0f;

    [Range(0f, 1f)]
    [Tooltip("How closely the player's current flight direction must point toward the moving cannon before capture is allowed. 1 = perfectly aimed.")]
    [SerializeField]
    private float captureDirectionDotThreshold = 0.75f;

    [SerializeField]
    private float sequenceCapturePadding = 1.25f;


    // =========================================================
    // SMOOTH LAUNCH
    // =========================================================

    [Header("Smooth Launch")]

    [SerializeField]
    private float launchAccelTime = 0.10f;

    [Range(0.05f, 1f)]
    [SerializeField]
    private float launchStartSpeedFactor = 0.55f;


    // =========================================================
    // SMOOTH CANNON ENTRY
    // =========================================================

    [Header("Smooth Cannon Entry")]

    [SerializeField]
    private float cannonEntrySmoothTime = 0.09f;

    [SerializeField]
    private float cannonEntryRotationSpeed = 14f;

    [SerializeField]
    private float cannonEntryCompleteDistance = 0.35f;

    [SerializeField]
    private float cannonEntryMaxTime = 0.5f;


    // =========================================================
    // CANNON CAPTURE
    // =========================================================

    [Header("Cannon Capture")]

    [SerializeField]
    private bool ignoreDepthAxis = true;

    [SerializeField]
    private float missMargin = 1.5f;


    // =========================================================
    // VISUAL
    // =========================================================

    [Header("Visual")]

    [SerializeField]
    private Transform visualModel;

    [SerializeField]
    private float visualSpinSpeed = 720f;


    // =========================================================
    // OBSTACLE HIT PHYSICS
    // =========================================================

    [Header("Obstacle Hit Physics")]

    [SerializeField]
    private string obstacleTag = "Obstacle";

    [SerializeField]
    private bool sweepObstacles = true;

    [SerializeField]
    private float sweepRadius = 0.35f;

    [SerializeField]
    private float obstacleBounceForce = 4f;

    [SerializeField]
    private float obstacleForwardForce = 2f;

    [SerializeField]
    private float obstacleSeparation = 0.05f;


    // =========================================================
    // BIRD HIT DELAY
    // =========================================================

    [Header("Bird Hit Delay")]

    [Tooltip("Delay after entering the bird trigger before the player starts falling.")]
    [SerializeField]
    private float birdHitDelay = 0.1f;


    // =========================================================
    // MISSED CANNON FALLING
    // =========================================================

    [Header("Missed Cannon Falling")]

    [SerializeField]
    private float missedCannonGravity = 20f;

    [SerializeField]
    private float missedCannonForwardSpeed = 4f;

    [SerializeField]
    private float missedCannonDownwardSpeed = 2f;

    [SerializeField]
    private float missedCannonTorque = 8f;


    // =========================================================
    // PRIVATE
    // =========================================================

    private Rigidbody rb;

    private bool isInsideCannon;
    private bool isFlying;
    private bool isEnteringCannon;

    private Vector3 flightDirection;

    private Quaternion cannonPlayerOffset;

    private BulletCannon currentCannon;
    private BulletCannon targetCannon;
    private BulletCannon capturingCannon;

    private bool hasMissedTarget;

    private float flightTimer;

    private float lastFlightSpeed;

    private float sequenceGuidanceTimer;

    private bool sequenceGuidanceLogged;

    private Vector3 previousFlightPosition;

    private bool inBarrel;

    private Transform launchExitPoint;

    private Quaternion flightRotation;

    private Vector3 followOffset;

    private Vector3 followOffsetVelocity;

    private Quaternion rotOffset =
        Quaternion.identity;

    private bool preserveOffsetOnEnter;

    private float entryElapsed;

    private int captureFrame = -1;

    private CannonManager manager;


    // =========================================================
    // BIRD HIT STATE
    // =========================================================

    private bool birdHitPending;

    private Coroutine birdHitCoroutine;


    // =========================================================
    // MANAGER
    // =========================================================

    private CannonManager Manager
    {
        get
        {
            if (manager == null)
                manager =
                    FindObjectOfType<CannonManager>();

            return manager;
        }
    }


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody>();

        rb.useGravity = false;

        rb.isKinematic = true;

        rb.interpolation =
            RigidbodyInterpolation.None;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        if (visualModel == null &&
            transform.childCount > 0)
        {
            visualModel =
                transform.GetChild(0);
        }

        cannonPlayerOffset =
            Quaternion.Euler(
                playerBaseRotation
            );
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        transform.rotation =
            cannonPlayerOffset;

        manager =
            FindObjectOfType<CannonManager>();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (isFlying)
            UpdateFlight();
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (!isFlying &&
            !isInsideCannon &&
            !isEnteringCannon &&
            !rb.isKinematic)
        {
            rb.AddForce(
                Vector3.down *
                missedCannonGravity,
                ForceMode.Acceleration
            );
        }
    }


    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        float dt =
            Time.deltaTime;

        if (isEnteringCannon)
        {
            UpdateEntering(dt);
        }
        else if (isInsideCannon &&
                 currentCannon != null)
        {
            FollowCurrentCannon(dt);
        }
        else if (isFlying)
        {
            DecayRotationOffset(dt);

            transform.rotation =
                flightRotation *
                rotOffset;
        }

        SpinVisual();
    }


    // =========================================================
    // VISUAL SPIN
    // =========================================================

    private void SpinVisual()
    {
        if (visualModel == null ||
            visualSpinSpeed == 0f)
        {
            return;
        }

        visualModel.Rotate(
            Vector3.up,
            visualSpinSpeed *
            Time.deltaTime,
            Space.Self
        );
    }


    // =========================================================
    // SPRING
    // =========================================================

    private void StepPositionSpring(float dt)
    {
        float smooth =
            Mathf.Max(
                0.01f,
                cannonEntrySmoothTime
            );

        float w =
            2f / smooth;

        float e =
            Mathf.Exp(
                -w * dt
            );

        Vector3 temp =
            (
                followOffsetVelocity +
                w * followOffset
            ) * dt;

        followOffset =
            (followOffset + temp) * e;

        followOffsetVelocity =
            (followOffsetVelocity - w * temp) * e;

        if (followOffset.sqrMagnitude <
                0.000001f &&
            followOffsetVelocity.sqrMagnitude <
                0.0001f)
        {
            followOffset =
                Vector3.zero;

            followOffsetVelocity =
                Vector3.zero;
        }
    }


    // =========================================================
    // ROTATION DECAY
    // =========================================================

    private void DecayRotationOffset(float dt)
    {
        float k =
            1f -
            Mathf.Exp(
                -Mathf.Max(
                    0.1f,
                    cannonEntryRotationSpeed
                ) * dt
            );

        rotOffset =
            Quaternion.Slerp(
                rotOffset,
                Quaternion.identity,
                k
            );
    }


    // =========================================================
    // APPLY FOLLOW
    // =========================================================

    private void ApplyFollowTransform(
        BulletCannon cannon)
    {
        Transform entryPoint =
            cannon.GetEntryPoint();

        if (entryPoint == null)
            return;

        transform.position =
            entryPoint.position +
            followOffset;

        transform.rotation =
            cannon.GetCannonRotation() *
            cannonPlayerOffset *
            rotOffset;
    }


    // =========================================================
    // LIMIT APPROACH
    // =========================================================

    private Vector3 LimitApproachVelocity(
        Vector3 velocity,
        Vector3 offset)
    {
        float magnitude =
            offset.magnitude;

        if (magnitude < 0.0001f)
            return velocity;

        float w =
            2f /
            Mathf.Max(
                0.01f,
                cannonEntrySmoothTime
            );

        Vector3 toTarget =
            -offset / magnitude;

        float toward =
            Vector3.Dot(
                velocity,
                toTarget
            );

        float maxToward =
            w * magnitude * 0.9f;

        if (toward > maxToward)
        {
            velocity -=
                toTarget *
                (toward - maxToward);
        }

        return velocity;
    }


    // =========================================================
    // ENTERING
    // =========================================================

    private void UpdateEntering(float dt)
    {
        if (capturingCannon == null)
        {
            isEnteringCannon = false;
            return;
        }

        if (Time.frameCount == captureFrame)
            return;

        entryElapsed += dt;

        StepPositionSpring(dt);

        DecayRotationOffset(dt);

        ApplyFollowTransform(
            capturingCannon
        );

        if (followOffset.magnitude <=
                cannonEntryCompleteDistance ||
            entryElapsed >=
                cannonEntryMaxTime)
        {
            CompleteEntry();
        }
    }


    private void CompleteEntry()
    {
        BulletCannon cannon =
            capturingCannon;

        capturingCannon = null;

        isEnteringCannon = false;

        if (cannon == null)
            return;

        preserveOffsetOnEnter = true;

        cannon.EnterCannon(this);

        if (Manager != null)
        {
            Manager.PlayerEnteredCannon(
                cannon
            );
        }
    }


    // =========================================================
    // FOLLOW CANNON
    // =========================================================

    private void FollowCurrentCannon(
        float dt)
    {
        StepPositionSpring(dt);

        DecayRotationOffset(dt);

        ApplyFollowTransform(
            currentCannon
        );
    }


    // =========================================================
    // ENTER CANNON
    // =========================================================

    public void EnterCannon(
        BulletCannon cannon)
    {
        if (cannon == null)
        {
            Debug.LogError(
                "BulletBoyPlayer: Cannon is missing."
            );

            return;
        }

        if (cannon.GetEntryPoint() == null ||
            cannon.GetExitPoint() == null)
        {
            Debug.LogError(
                "BulletBoyPlayer: Cannon entry/exit point is missing."
            );

            return;
        }

        if (!preserveOffsetOnEnter)
        {
            followOffset =
                Vector3.zero;

            followOffsetVelocity =
                Vector3.zero;

            rotOffset =
                Quaternion.identity;
        }

        preserveOffsetOnEnter = false;

        isEnteringCannon = false;

        isInsideCannon = true;

        isFlying = false;

        inBarrel = false;

        currentCannon = cannon;

        targetCannon = null;

        capturingCannon = null;

        flightDirection =
            Vector3.zero;

        hasMissedTarget = false;

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        rb.useGravity = false;

        rb.isKinematic = true;
    }


    // =========================================================
    // LAUNCH
    // =========================================================

    public void LaunchFromCannon(
        Transform exitPoint,
        Vector3 launchDirection,
        float speed,
        Quaternion cannonRotation)
    {
        if (exitPoint == null)
        {
            Debug.LogError(
                "BulletBoyPlayer: Exit Point is missing."
            );

            return;
        }

        if (launchDirection.sqrMagnitude <
            0.001f)
        {
            Debug.LogError(
                "BulletBoyPlayer: Launch direction is invalid."
            );

            return;
        }

        isEnteringCannon = false;

        isInsideCannon = false;

        isFlying = true;

        currentCannon = null;

        capturingCannon = null;

        launchExitPoint =
            exitPoint;

        inBarrel = true;

        flightRotation =
            cannonRotation *
            cannonPlayerOffset;

        followOffset =
            Vector3.zero;

        followOffsetVelocity =
            Vector3.zero;

        Vector3 direction =
            launchDirection.normalized;

        if (flattenLaunchDirection)
        {
            Vector3 flat =
                direction;

            flat.z = 0f;

            if (flat.sqrMagnitude >
                0.01f)
            {
                direction =
                    flat.normalized;
            }
        }

        flightDirection =
            direction;

        flightSpeed =
            Mathf.Max(
                speed,
                0.01f
            );

        lastFlightSpeed =
            flightSpeed *
            launchStartSpeedFactor;

        hasMissedTarget = false;

        flightTimer = 0f;

        sequenceGuidanceTimer = 0f;

        sequenceGuidanceLogged = false;

        previousFlightPosition =
            transform.position;

        birdHitPending = false;

        if (birdHitCoroutine != null)
        {
            StopCoroutine(
                birdHitCoroutine
            );

            birdHitCoroutine = null;
        }

        rb.useGravity = false;

        rb.isKinematic = true;

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;
    }


    // =========================================================
    // SPEED
    // =========================================================

    private float SpeedAt(float age)
    {
        if (launchAccelTime <=
            0.0001f)
        {
            return flightSpeed;
        }

        float t =
            Mathf.Clamp01(
                age /
                launchAccelTime
            );

        return flightSpeed *
               Mathf.Lerp(
                   launchStartSpeedFactor,
                   1f,
                   Mathf.SmoothStep(
                       0f,
                       1f,
                       t
                   )
               );
    }


    // =========================================================
    // FLIGHT
    // =========================================================

    private void UpdateFlight()
    {
        float dt =
            Time.deltaTime;

        if (dt <= 0f)
            return;

        flightTimer += dt;

        Vector3 start =
            transform.position;

        previousFlightPosition =
            start;

        float speed =
            SpeedAt(
                flightTimer -
                dt * 0.5f
            );

        lastFlightSpeed =
            speed;

        float remaining =
            dt;


        // -----------------------------------------------------
        // BARREL
        // -----------------------------------------------------

        if (inBarrel)
        {
            if (launchExitPoint == null)
            {
                inBarrel = false;
            }
            else
            {
                Vector3 toExit =
                    launchExitPoint.position -
                    start;

                float distanceToExit =
                    toExit.magnitude;

                float step =
                    speed * dt;

                if (distanceToExit > step)
                {
                    transform.position =
                        start +
                        toExit *
                        (step /
                        distanceToExit);

                    return;
                }

                transform.position =
                    launchExitPoint.position;

                inBarrel = false;

                remaining =
                    Mathf.Max(
                        0f,
                        dt -
                        distanceToExit /
                        Mathf.Max(
                            0.01f,
                            speed
                        )
                    );

                start =
                    transform.position;

                if (remaining <=
                    0.0001f)
                {
                    return;
                }
            }
        }


        float distance =
            speed *
            remaining;

        float tBase =
            1f -
            remaining /
            dt;

        UpdateSequenceGuidance(dt);


        // -----------------------------------------------------
        // OBSTACLE / BIRD SWEEP
        // -----------------------------------------------------

        bool obstacleHit =
            SweepForObstacle(
                start,
                distance,
                out float obstacleDistance,
                out Vector3 obstacleNormal
            );

        if (obstacleHit)
        {
            distance =
                obstacleDistance;
        }


        // -----------------------------------------------------
        // CANNON CAPTURE
        // -----------------------------------------------------

        if (targetCannon != null &&
            TryCaptureAlongPath(
                start,
                distance,
                tBase))
        {
            return;
        }


        transform.position =
            start +
            flightDirection *
            distance;


        // -----------------------------------------------------
        // OBSTACLE
        // -----------------------------------------------------

        if (obstacleHit)
        {
            if (!isFlying)
                return;

            if (obstacleNormal.sqrMagnitude <
                0.001f)
            {
                obstacleNormal =
                    -flightDirection;
            }

            HitObstacle(
                obstacleNormal.normalized
            );

            return;
        }

        CheckForMiss();
    }


    // =========================================================
    // BIRD / OBSTACLE SWEEP
    // =========================================================

    private bool SweepForObstacle(
        Vector3 origin,
        float distance,
        out float hitDistance,
        out Vector3 hitNormal)
    {
        hitDistance =
            distance;

        hitNormal =
            -flightDirection;

        if (!sweepObstacles ||
            distance <= 0.0001f)
        {
            return false;
        }

        RaycastHit[] hits =
            Physics.SphereCastAll(
                origin,
                sweepRadius,
                flightDirection,
                distance,
                ~0,
                QueryTriggerInteraction.Collide
            );

        float best =
            float.MaxValue;

        bool found =
            false;

        BirdController hitBird =
            null;

        Vector3 bestNormal =
            -flightDirection;

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            Collider c =
                hits[i].collider;

            if (c == null ||
                c.transform.IsChildOf(transform))
            {
                continue;
            }


            // -------------------------------------------------
            // BIRD
            // -------------------------------------------------

            BirdController bird =
                c.GetComponentInParent<BirdController>();

            if (bird != null)
            {
                if (hits[i].distance < best)
                {
                    best =
                        hits[i].distance;

                    bestNormal =
                        hits[i].normal;

                    hitBird =
                        bird;

                    found =
                        true;
                }

                continue;
            }


            // -------------------------------------------------
            // NORMAL OBSTACLE
            // -------------------------------------------------

            if (!c.CompareTag(obstacleTag))
                continue;

            if (hits[i].distance < best)
            {
                best =
                    hits[i].distance;

                bestNormal =
                    hits[i].normal;

                hitBird =
                    null;

                found =
                    true;
            }
        }

        if (!found)
            return false;

        hitDistance =
            Mathf.Max(
                0f,
                best
            );

        hitNormal =
            bestNormal;


        // -----------------------------------------------------
        // BIRD HIT
        // -----------------------------------------------------

        if (hitBird != null)
        {
            hitBird.HitByPlayer(
                flightDirection,
                lastFlightSpeed
            );

            StartBirdHitDelay();

            return true;
        }

        return true;
    }


    // =========================================================
    // START BIRD HIT DELAY
    // =========================================================

    private void StartBirdHitDelay()
    {
        if (birdHitPending)
            return;

        birdHitPending = true;

        if (birdHitCoroutine != null)
        {
            StopCoroutine(
                birdHitCoroutine
            );
        }

        birdHitCoroutine =
            StartCoroutine(
                BirdHitDelayRoutine()
            );
    }


    // =========================================================
    // BIRD HIT DELAY ROUTINE
    // =========================================================

    private IEnumerator BirdHitDelayRoutine()
    {
        float delay =
            Mathf.Max(
                0f,
                birdHitDelay
            );

        yield return new WaitForSeconds(delay);

        birdHitCoroutine = null;

        if (!birdHitPending)
            yield break;

        if (!isFlying)
            yield break;

        StartBirdCollisionFall();

        birdHitPending = false;
    }


    // =========================================================
    // FLAT
    // =========================================================

    private Vector3 Flat(Vector3 v)
    {
        if (ignoreDepthAxis)
        {
            v.z = 0f;
        }

        return v;
    }


    // =========================================================
    // SEQUENCE GUIDANCE
    // =========================================================

    private void UpdateSequenceGuidance(
        float dt)
    {
        if (!useSequenceGuidance ||
            !allowAutomaticAimToTarget ||
            targetCannon == null ||
            isEnteringCannon ||
            hasMissedTarget)
        {
            return;
        }

        sequenceGuidanceTimer +=
            dt;

        if (sequenceGuidanceTimer <
            Mathf.Max(
                0f,
                sequenceGuidanceDelay
            ))
        {
            return;
        }

        Transform targetEntry =
            targetCannon.GetEntryPoint();

        if (targetEntry == null)
            return;

        Vector3 toTarget =
            targetEntry.position -
            transform.position;

        if (ignoreDepthAxis)
        {
            toTarget.z = 0f;
        }

        if (toTarget.sqrMagnitude <
            0.0001f)
        {
            return;
        }

        Vector3 desiredDirection =
            toTarget.normalized;

        if (!sequenceGuidanceLogged)
        {
            sequenceGuidanceLogged =
                true;

            Debug.Log(
                "BulletBoyPlayer: Sequence guidance -> " +
                targetCannon.gameObject.name
            );
        }

        float maxRadians =
            Mathf.Max(
                0f,
                sequenceGuidanceTurnRate
            ) *
            Mathf.Deg2Rad *
            dt;

        Vector3 turnedDirection =
            Vector3.RotateTowards(
                flightDirection,
                desiredDirection,
                maxRadians,
                0f
            );

        float blend =
            1f -
            Mathf.Exp(
                -Mathf.Max(
                    0.01f,
                    sequenceGuidanceStrength
                ) *
                dt
            );

        flightDirection =
            Vector3.Slerp(
                turnedDirection,
                desiredDirection,
                blend
            ).normalized;
    }


    // =========================================================
    // CAPTURE
    // =========================================================

    private bool TryCaptureAlongPath(
        Vector3 start,
        float distance,
        float tBase)
    {
        if (targetCannon == null)
            return false;

        if (targetCannon.GetEntryPoint() == null)
            return false;

        Vector3 prevEntry =
            targetCannon.GetPreviousEntryPosition();

        Vector3 currEntry =
            targetCannon.GetCurrentEntryPosition();

        Vector3 prevBody =
            targetCannon.GetPreviousCannonPosition();

        Vector3 currBody =
            targetCannon.GetCurrentCannonPosition();

        float entryRadius =
            targetCannon.GetCaptureRadius() +
            Mathf.Max(
                0f,
                sequenceCapturePadding
            ) +
            targetCannon.GetHighSpeedCaptureRadius();

        float bodyRadius =
            targetCannon.GetBodyRadius() +
            Mathf.Max(
                0f,
                sequenceCapturePadding * 0.5f
            );

        Vector3 currentEntry =
            targetCannon.GetCurrentEntryPosition();

        // IMPORTANT:
        // Never capture a cannon just because the player happens to be
        // physically close to it. The cannon must be in FRONT of the player
        // and the current flight direction must actually point toward it.
        Vector3 toEntry =
            Flat(currentEntry - start);

        if (toEntry.sqrMagnitude <= 0.0001f)
            return false;

        Vector3 toEntryDirection =
            toEntry.normalized;

        float aimDot =
            Vector3.Dot(
                Flat(flightDirection).normalized,
                toEntryDirection
            );

        if (aimDot < captureDirectionDotThreshold)
            return false;

        int steps =
            Mathf.Clamp(
                Mathf.CeilToInt(
                    distance /
                    Mathf.Max(
                        0.05f,
                        maxStepDistance
                    )
                ),
                1,
                128
            );

        for (int i = 1;
             i <= steps;
             i++)
        {
            float f0 =
                (float)(i - 1) /
                steps;

            float f1 =
                (float)i /
                steps;

            Vector3 p0 =
                start +
                flightDirection *
                (distance * f0);

            Vector3 p1 =
                start +
                flightDirection *
                (distance * f1);

            float t0 =
                tBase +
                (1f - tBase) *
                f0;

            float t1 =
                tBase +
                (1f - tBase) *
                f1;

            Vector3 e0 =
                Vector3.Lerp(
                    prevEntry,
                    currEntry,
                    t0
                );

            Vector3 e1 =
                Vector3.Lerp(
                    prevEntry,
                    currEntry,
                    t1
                );

            Vector3 b0 =
                Vector3.Lerp(
                    prevBody,
                    currBody,
                    t0
                );

            Vector3 b1 =
                Vector3.Lerp(
                    prevBody,
                    currBody,
                    t1
                );

            float entryDistance =
                BulletCannon.SegmentSegmentDistance(
                    Flat(p0),
                    Flat(p1),
                    Flat(e0),
                    Flat(e1)
                );

            float bodyDistance =
                BulletCannon.SegmentSegmentDistance(
                    Flat(p0),
                    Flat(p1),
                    Flat(b0),
                    Flat(b1)
                );

            if (entryDistance <= entryRadius ||
                bodyDistance <= bodyRadius)
            {
                transform.position =
                    p1;

                BeginCannonEntry();

                return true;
            }
        }

        return false;
    }


    // =========================================================
    // MISS
    // =========================================================

    private void CheckForMiss()
    {
        if (targetCannon == null ||
            hasMissedTarget)
        {
            return;
        }

        if (flightTimer >=
            maxFlightTime)
        {
            MissedTargetCannon();
            return;
        }

        Vector3 entry =
            targetCannon.GetCurrentEntryPosition();

        float passed =
            Vector3.Dot(
                transform.position -
                entry,
                flightDirection
            );

        float allowance =
            targetCannon.GetCaptureRadius() +
            targetCannon.GetMovementExtent() +
            Mathf.Max(
                0f,
                missMargin
            ) +
            Mathf.Max(
                0f,
                sequenceCapturePadding
            );

        if (passed > allowance)
        {
            MissedTargetCannon();
        }
    }


    // =========================================================
    // BEGIN CANNON ENTRY
    // =========================================================

    private void BeginCannonEntry()
    {
        if (targetCannon == null ||
            isEnteringCannon)
        {
            return;
        }

        BulletCannon cannon =
            targetCannon;

        Transform entryPoint =
            cannon.GetEntryPoint();

        if (entryPoint == null)
            return;

        targetCannon = null;

        hasMissedTarget = false;

        capturingCannon =
            cannon;

        isEnteringCannon = true;

        isFlying = false;

        isInsideCannon = false;

        inBarrel = false;

        rb.isKinematic = true;

        rb.useGravity = false;

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        followOffset =
            transform.position -
            entryPoint.position;

        Vector3 playerVelocity =
            flightDirection *
            lastFlightSpeed;

        Vector3 entryVelocity =
            Vector3.zero;

        float dt =
            Time.deltaTime;

        if (dt > 0.0001f)
        {
            entryVelocity =
                (
                    cannon.GetCurrentEntryPosition() -
                    cannon.GetPreviousEntryPosition()
                ) / dt;
        }

        followOffsetVelocity =
            LimitApproachVelocity(
                playerVelocity -
                entryVelocity,
                followOffset
            );

        Quaternion targetRotation =
            cannon.GetCannonRotation() *
            cannonPlayerOffset;

        rotOffset =
            Quaternion.Inverse(
                targetRotation
            ) *
            transform.rotation;

        entryElapsed = 0f;

        captureFrame =
            Time.frameCount;
    }


    // =========================================================
    // BIRD COLLISION FALL
    // =========================================================

    private void StartBirdCollisionFall()
    {
        if (hasMissedTarget)
            return;

        hasMissedTarget = true;

        isEnteringCannon = false;

        isFlying = false;

        isInsideCannon = false;

        inBarrel = false;

        currentCannon = null;

        targetCannon = null;

        capturingCannon = null;


        // -----------------------------------------------------
        // ENABLE PHYSICS
        // -----------------------------------------------------

        rb.isKinematic = false;

        rb.useGravity = true;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;


        // -----------------------------------------------------
        // RESET
        // -----------------------------------------------------

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;


        // -----------------------------------------------------
        // KEEP MOVING FORWARD
        // -----------------------------------------------------

        Vector3 direction =
            flightDirection;

        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                Vector3.right;
        }

        direction.Normalize();

        rb.velocity =
            direction *
            missedCannonForwardSpeed +
            Vector3.down *
            missedCannonDownwardSpeed;


        // -----------------------------------------------------
        // TUMBLE
        // -----------------------------------------------------

        Vector3 tumbleAxis =
            Vector3.Cross(
                Vector3.up,
                direction
            );

        if (tumbleAxis.sqrMagnitude <
            0.001f)
        {
            tumbleAxis =
                Vector3.right;
        }

        rb.AddTorque(
            tumbleAxis.normalized *
            missedCannonTorque,
            ForceMode.Impulse
        );

        Debug.Log(
            "BulletBoyPlayer: Hit bird and started falling."
        );
    }


    // =========================================================
    // TRIGGER ENTER
    // =========================================================

    private void OnTriggerEnter(
        Collider other)
    {
        if (!isFlying)
            return;

        if (birdHitPending)
            return;


        // -----------------------------------------------------
        // BIRD
        // -----------------------------------------------------

        BirdController bird =
            other.GetComponentInParent<BirdController>();

        if (bird != null)
        {
            bird.HitByPlayer(
                flightDirection,
                lastFlightSpeed
            );

            StartBirdHitDelay();

            return;
        }


        // -----------------------------------------------------
        // NORMAL OBSTACLE
        // -----------------------------------------------------

        if (!other.gameObject.CompareTag(
            obstacleTag))
        {
            return;
        }

        Vector3 hitNormal =
            transform.position -
            other.ClosestPoint(
                transform.position
            );

        if (hitNormal.sqrMagnitude <
            0.001f)
        {
            hitNormal =
                -flightDirection;
        }

        HitObstacle(
            hitNormal.normalized
        );
    }


    // =========================================================
    // COLLISION ENTER
    // =========================================================
    // Kept for normal obstacles.
    // Birds are intended to use Trigger colliders.

    private void OnCollisionEnter(
        Collision collision)
    {
        if (!isFlying)
            return;

        BirdController bird =
            collision.collider
                .GetComponentInParent<BirdController>();

        if (bird != null)
        {
            bird.HitByPlayer(
                flightDirection,
                lastFlightSpeed
            );

            StartBirdHitDelay();

            return;
        }

        if (!collision.gameObject.CompareTag(
            obstacleTag))
        {
            return;
        }

        Vector3 hitNormal =
            Vector3.zero;

        if (collision.contactCount > 0)
        {
            hitNormal =
                collision.GetContact(0).normal;
        }

        if (hitNormal.sqrMagnitude <
            0.001f)
        {
            hitNormal =
                -flightDirection;
        }

        HitObstacle(
            hitNormal.normalized
        );
    }


    // =========================================================
    // NORMAL OBSTACLE HIT
    // =========================================================

    private void HitObstacle(
        Vector3 hitNormal)
    {
        if (hasMissedTarget)
            return;

        hasMissedTarget = true;

        isEnteringCannon = false;

        isFlying = false;

        isInsideCannon = false;

        inBarrel = false;

        currentCannon = null;

        targetCannon = null;

        capturingCannon = null;

        transform.position +=
            hitNormal *
            obstacleSeparation;

        rb.isKinematic = false;

        rb.useGravity = true;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        rb.velocity =
            hitNormal *
            obstacleBounceForce +
            flightDirection *
            obstacleForwardForce;

        Vector3 tumbleAxis =
            Vector3.Cross(
                Vector3.up,
                hitNormal
            );

        if (tumbleAxis.sqrMagnitude <
            0.001f)
        {
            tumbleAxis =
                Vector3.right;
        }

        rb.AddTorque(
            tumbleAxis.normalized *
            8f,
            ForceMode.Impulse
        );

        Debug.Log(
            "BulletBoyPlayer: Hit obstacle and started falling."
        );

        if (Manager != null)
        {
            Manager.PlayerMissedCannon();
        }
    }


    // =========================================================
    // MISSED TARGET CANNON
    // =========================================================

    private void MissedTargetCannon()
    {
        if (hasMissedTarget)
            return;

        hasMissedTarget = true;

        isEnteringCannon = false;

        isFlying = false;

        isInsideCannon = false;

        inBarrel = false;

        currentCannon = null;

        targetCannon = null;

        capturingCannon = null;

        rb.isKinematic = false;

        rb.useGravity = true;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        rb.velocity =
            flightDirection *
            missedCannonForwardSpeed +
            Vector3.down *
            missedCannonDownwardSpeed;

        Vector3 tumbleAxis =
            Vector3.Cross(
                Vector3.up,
                flightDirection
            );

        if (tumbleAxis.sqrMagnitude <
            0.001f)
        {
            tumbleAxis =
                Vector3.right;
        }

        rb.AddTorque(
            tumbleAxis.normalized *
            missedCannonTorque,
            ForceMode.Impulse
        );

        Debug.Log(
            "BulletBoyPlayer: Missed target cannon and started falling."
        );

        if (Manager != null)
        {
            Manager.PlayerMissedCannon();
        }
    }


    // =========================================================
    // TARGET CANNON
    // =========================================================

    public void SetTargetCannon(
        BulletCannon cannon)
    {
        targetCannon =
            cannon;

        hasMissedTarget =
            false;

        if (cannon != null)
        {
            Debug.Log(
                "BulletBoyPlayer: Target cannon set to " +
                cannon.gameObject.name
            );
        }
    }


    public void ClearTargetCannon()
    {
        targetCannon =
            null;

        hasMissedTarget =
            false;
    }


    // =========================================================
    // STATE
    // =========================================================

    public bool IsInsideCannon()
    {
        return isInsideCannon;
    }


    public bool IsEnteringCannon()
    {
        return isEnteringCannon;
    }


    public bool IsFlying()
    {
        return isFlying;
    }


    public Vector3 GetPreviousFlightPosition()
    {
        return previousFlightPosition;
    }


    public Vector3 GetFlightDirection()
    {
        return flightDirection;
    }


    public float GetFlightSpeed()
    {
        return flightSpeed;
    }
}