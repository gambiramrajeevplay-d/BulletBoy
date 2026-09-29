using UnityEngine;

/*
 * Runs AFTER BulletCannon (-100) and CannonManager (-50), so cannon
 * positions and rotations are always up to date for this frame.
 *
 * SMOOTH MOTION OVERVIEW
 * ----------------------
 * 1. Launch:   the player is NOT teleported to the exit point. It slides
 *              out of the barrel and accelerates (ease-in), so there is no
 *              jump when the shot starts.
 * 2. Flight:   sub-stepped straight flight (frame-rate independent).
 * 3. Capture:  the player keeps its speed and is pulled into the next
 *              cannon by a critically-damped spring measured RELATIVE to the
 *              cannon's entry point. Velocity is continuous, moving/rotating
 *              cannons are followed with zero lag, and any leftover offset
 *              keeps easing out inside the cannon (no snap on arrival).
 * 4. Rotation: handled with a relative rotation offset that eases to zero,
 *              so rotating cannons never make the player lag or pop.
 */
[DefaultExecutionOrder(0)]
[RequireComponent(typeof(Rigidbody))]
public class BulletBoyPlayer : MonoBehaviour
{
    [Header("Player Rotation")]

    [SerializeField]
    private Vector3 playerBaseRotation = new Vector3(0f, 0f, -90f);


    [Header("Flight")]

    [SerializeField]
    private float flightSpeed = 18f;

    [Tooltip("Flight is simulated in steps no longer than this. Prevents skipping cannons on low FPS (TV / mobile).")]
    [SerializeField]
    private float maxStepDistance = 0.4f;

    [Tooltip("Flatten the launch direction onto the XY plane so the player never drifts toward/away from the camera.")]
    [SerializeField]
    private bool flattenLaunchDirection = true;

    [Tooltip("If the target cannon is not reached within this many seconds the player counts as a miss.")]
    [SerializeField]
    private float maxFlightTime = 10f;


    [Header("Smooth Launch")]

    [Tooltip("Seconds the player takes to accelerate to full speed after being shot. 0 = instant.")]
    [SerializeField]
    private float launchAccelTime = 0.10f;

    [Tooltip("Speed at the very start of the shot as a fraction of full speed.")]
    [Range(0.05f, 1f)]
    [SerializeField]
    private float launchStartSpeedFactor = 0.55f;


    [Header("Smooth Cannon Entry")]

    [Tooltip("Lower = snappier pull into the cannon, higher = softer. 0.07 - 0.12 feels good.")]
    [SerializeField]
    private float cannonEntrySmoothTime = 0.09f;

    [Tooltip("How quickly the player's rotation eases into the cannon's rotation.")]
    [SerializeField]
    private float cannonEntryRotationSpeed = 14f;

    [Tooltip("The player counts as 'inside' (can be launched) once it is this close to the Entry Point. Any leftover distance keeps easing out smoothly.")]
    [SerializeField]
    private float cannonEntryCompleteDistance = 0.35f;

    [Tooltip("Safety: entry is completed after this many seconds no matter what.")]
    [SerializeField]
    private float cannonEntryMaxTime = 0.5f;


    [Header("Cannon Capture")]

    [Tooltip("Ignore the Z (depth) axis when testing capture. Recommended for side-view games.")]
    [SerializeField]
    private bool ignoreDepthAxis = true;

    [Tooltip("Extra distance the player must travel past a cannon (and its movement range) before it counts as a miss.")]
    [SerializeField]
    private float missMargin = 1.5f;


    [Header("Visual")]

    [SerializeField]
    private Transform visualModel;

    [SerializeField]
    private float visualSpinSpeed = 720f;


    [Header("Obstacle Hit Physics")]

    [SerializeField]
    private string obstacleTag = "Obstacle";

    [Tooltip("Sweep ahead for obstacles so they cannot be skipped on low FPS.")]
    [SerializeField]
    private bool sweepObstacles = true;

    [SerializeField]
    private float sweepRadius = 0.35f;

    [Tooltip("How strongly the player is pushed away from the obstacle.")]
    [SerializeField]
    private float obstacleBounceForce = 4f;

    [Tooltip("Additional forward momentum after hitting an obstacle.")]
    [SerializeField]
    private float obstacleForwardForce = 2f;

    [Tooltip("Small position offset used to prevent the player from sticking inside the collider.")]
    [SerializeField]
    private float obstacleSeparation = 0.05f;


    [Header("Missed Cannon Falling")]

    [SerializeField]
    private float missedCannonGravity = 20f;

    [SerializeField]
    private float missedCannonForwardSpeed = 4f;

    [SerializeField]
    private float missedCannonDownwardSpeed = 2f;

    [SerializeField]
    private float missedCannonTorque = 8f;


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

    private Vector3 previousFlightPosition;

    // Launch (barrel slide)
    private bool inBarrel;
    private Transform launchExitPoint;
    private Quaternion flightRotation;

    // Smooth follow state (relative to the cannon's Entry Point)
    private Vector3 followOffset;
    private Vector3 followOffsetVelocity;
    private Quaternion rotOffset = Quaternion.identity;
    private bool preserveOffsetOnEnter;

    private float entryElapsed;
    private int captureFrame = -1;

    private CannonManager manager;

    private CannonManager Manager
    {
        get
        {
            if (manager == null)
                manager = FindObjectOfType<CannonManager>();

            return manager;
        }
    }


    // =========================================================
    // INITIALIZE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (visualModel == null && transform.childCount > 0)
            visualModel = transform.GetChild(0);

        cannonPlayerOffset = Quaternion.Euler(playerBaseRotation);
    }

    private void Start()
    {
        transform.rotation = cannonPlayerOffset;
        manager = FindObjectOfType<CannonManager>();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (isFlying)
            UpdateFlight();
    }

    private void FixedUpdate()
    {
        if (!isFlying &&
            !isInsideCannon &&
            !isEnteringCannon &&
            !rb.isKinematic)
        {
            rb.AddForce(
                Vector3.down * missedCannonGravity,
                ForceMode.Acceleration
            );
        }
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        if (isEnteringCannon)
        {
            UpdateEntering(dt);
        }
        else if (isInsideCannon && currentCannon != null)
        {
            FollowCurrentCannon(dt);
        }
        else if (isFlying)
        {
            DecayRotationOffset(dt);

            transform.rotation = flightRotation * rotOffset;
        }

        SpinVisual();
    }

    private void SpinVisual()
    {
        if (visualModel == null || visualSpinSpeed == 0f)
            return;

        visualModel.Rotate(
            Vector3.up,
            visualSpinSpeed * Time.deltaTime,
            Space.Self
        );
    }


    // =========================================================
    // SMOOTH FOLLOW HELPERS
    // =========================================================

    /// <summary>
    /// Exact critically-damped spring toward zero offset. Frame-rate
    /// independent, and it keeps the incoming velocity (no snapping).
    /// </summary>
    private void StepPositionSpring(float dt)
    {
        float smooth = Mathf.Max(0.01f, cannonEntrySmoothTime);
        float w = 2f / smooth;
        float e = Mathf.Exp(-w * dt);

        Vector3 temp =
            (followOffsetVelocity + w * followOffset) * dt;

        followOffset =
            (followOffset + temp) * e;

        followOffsetVelocity =
            (followOffsetVelocity - w * temp) * e;

        if (followOffset.sqrMagnitude < 0.000001f &&
            followOffsetVelocity.sqrMagnitude < 0.0001f)
        {
            followOffset = Vector3.zero;
            followOffsetVelocity = Vector3.zero;
        }
    }

    private void DecayRotationOffset(float dt)
    {
        float k =
            1f - Mathf.Exp(
                -Mathf.Max(0.1f, cannonEntryRotationSpeed) * dt
            );

        rotOffset =
            Quaternion.Slerp(rotOffset, Quaternion.identity, k);
    }

    private void ApplyFollowTransform(BulletCannon cannon)
    {
        Transform entryPoint = cannon.GetEntryPoint();

        if (entryPoint == null)
            return;

        transform.position =
            entryPoint.position + followOffset;

        transform.rotation =
            cannon.GetCannonRotation() *
            cannonPlayerOffset *
            rotOffset;
    }

    /// <summary>
    /// Keeps the approach velocity but prevents it from carrying the
    /// player THROUGH the entry point (which would look like a bounce).
    /// </summary>
    private Vector3 LimitApproachVelocity(
        Vector3 velocity,
        Vector3 offset)
    {
        float magnitude = offset.magnitude;

        if (magnitude < 0.0001f)
            return velocity;

        float w = 2f / Mathf.Max(0.01f, cannonEntrySmoothTime);

        Vector3 toTarget = -offset / magnitude;

        float toward = Vector3.Dot(velocity, toTarget);

        float maxToward = w * magnitude * 0.9f;

        if (toward > maxToward)
            velocity -= toTarget * (toward - maxToward);

        return velocity;
    }


    // =========================================================
    // ENTERING (smooth capture)
    // =========================================================

    private void UpdateEntering(float dt)
    {
        if (capturingCannon == null)
        {
            isEnteringCannon = false;
            return;
        }

        // The capture frame already moved the player in Update().
        if (Time.frameCount == captureFrame)
            return;

        entryElapsed += dt;

        StepPositionSpring(dt);
        DecayRotationOffset(dt);

        ApplyFollowTransform(capturingCannon);

        if (followOffset.magnitude <= cannonEntryCompleteDistance ||
            entryElapsed >= cannonEntryMaxTime)
        {
            CompleteEntry();
        }
    }

    private void CompleteEntry()
    {
        BulletCannon cannon = capturingCannon;

        capturingCannon = null;
        isEnteringCannon = false;

        if (cannon == null)
            return;

        // Keep the leftover offset so it can ease out INSIDE the cannon.
        preserveOffsetOnEnter = true;

        cannon.EnterCannon(this);

        if (Manager != null)
            Manager.PlayerEnteredCannon(cannon);
    }


    // =========================================================
    // INSIDE CANNON
    // =========================================================

    private void FollowCurrentCannon(float dt)
    {
        StepPositionSpring(dt);
        DecayRotationOffset(dt);

        ApplyFollowTransform(currentCannon);
    }

    public void EnterCannon(BulletCannon cannon)
    {
        if (cannon == null)
        {
            Debug.LogError("BulletBoyPlayer: Cannon is missing.");
            return;
        }

        if (cannon.GetEntryPoint() == null || cannon.GetExitPoint() == null)
        {
            Debug.LogError("BulletBoyPlayer: Cannon entry/exit point is missing.");
            return;
        }

        if (!preserveOffsetOnEnter)
        {
            // Very first cannon of the level: start exactly on the entry point.
            followOffset = Vector3.zero;
            followOffsetVelocity = Vector3.zero;
            rotOffset = Quaternion.identity;
        }

        preserveOffsetOnEnter = false;

        isEnteringCannon = false;
        isFlying = false;
        isInsideCannon = true;
        inBarrel = false;

        currentCannon = cannon;
        targetCannon = null;
        capturingCannon = null;

        flightDirection = Vector3.zero;
        hasMissedTarget = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
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
            Debug.LogError("BulletBoyPlayer: Exit Point is missing.");
            return;
        }

        if (launchDirection.sqrMagnitude < 0.001f)
        {
            Debug.LogError("BulletBoyPlayer: Launch direction is invalid.");
            return;
        }

        isEnteringCannon = false;
        isInsideCannon = false;
        isFlying = true;

        currentCannon = null;
        capturingCannon = null;

        /*
         * NO teleport to the exit point. The player keeps its current
         * position and slides out of the barrel (see UpdateFlight).
         */

        launchExitPoint = exitPoint;
        inBarrel = true;

        flightRotation = cannonRotation * cannonPlayerOffset;

        // Residual position offset is no longer needed; the position
        // itself is already continuous.
        followOffset = Vector3.zero;
        followOffsetVelocity = Vector3.zero;

        Vector3 direction = launchDirection.normalized;

        if (flattenLaunchDirection)
        {
            Vector3 flat = direction;
            flat.z = 0f;

            if (flat.sqrMagnitude > 0.01f)
                direction = flat.normalized;
        }

        flightDirection = direction;

        flightSpeed = Mathf.Max(speed, 0.01f);
        lastFlightSpeed = flightSpeed * launchStartSpeedFactor;

        hasMissedTarget = false;
        flightTimer = 0f;

        previousFlightPosition = transform.position;

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log("BulletBoyPlayer: Launched, direction = " + flightDirection);
    }

    private float SpeedAt(float age)
    {
        if (launchAccelTime <= 0.0001f)
            return flightSpeed;

        float t = Mathf.Clamp01(age / launchAccelTime);

        return flightSpeed *
               Mathf.Lerp(
                   launchStartSpeedFactor,
                   1f,
                   Mathf.SmoothStep(0f, 1f, t)
               );
    }


    // =========================================================
    // FLIGHT (sub-stepped, frame-rate independent)
    // =========================================================

    private void UpdateFlight()
    {
        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        flightTimer += dt;

        Vector3 start = transform.position;

        previousFlightPosition = start;

        float speed = SpeedAt(flightTimer - dt * 0.5f);

        lastFlightSpeed = speed;

        float remaining = dt;


        /*
         * BARREL SLIDE: glide from where the player is to the exit point
         * instead of teleporting there.
         */

        if (inBarrel)
        {
            if (launchExitPoint == null)
            {
                inBarrel = false;
            }
            else
            {
                Vector3 toExit =
                    launchExitPoint.position - start;

                float distanceToExit = toExit.magnitude;

                float step = speed * dt;

                if (distanceToExit > step)
                {
                    transform.position =
                        start + toExit * (step / distanceToExit);

                    return;
                }

                transform.position = launchExitPoint.position;

                inBarrel = false;

                remaining =
                    Mathf.Max(
                        0f,
                        dt - distanceToExit / Mathf.Max(0.01f, speed)
                    );

                start = transform.position;

                if (remaining <= 0.0001f)
                    return;
            }
        }


        float distance = speed * remaining;

        float tBase = 1f - remaining / dt;


        /*
         * Obstacle sweep: never fly through an obstacle.
         */

        bool obstacleHit =
            SweepForObstacle(
                start,
                distance,
                out float obstacleDistance,
                out Vector3 obstacleNormal
            );

        if (obstacleHit)
            distance = obstacleDistance;


        /*
         * Cannon capture along the whole path.
         */

        if (targetCannon != null &&
            TryCaptureAlongPath(start, distance, tBase))
        {
            return;
        }


        transform.position =
            start + flightDirection * distance;


        if (obstacleHit)
        {
            if (obstacleNormal.sqrMagnitude < 0.001f)
                obstacleNormal = -flightDirection;

            HitObstacle(obstacleNormal.normalized);
            return;
        }


        CheckForMiss();
    }

    private bool SweepForObstacle(
        Vector3 origin,
        float distance,
        out float hitDistance,
        out Vector3 hitNormal)
    {
        hitDistance = distance;
        hitNormal = -flightDirection;

        if (!sweepObstacles || distance <= 0.0001f)
            return false;

        RaycastHit[] hits =
            Physics.SphereCastAll(
                origin,
                sweepRadius,
                flightDirection,
                distance,
                ~0,
                QueryTriggerInteraction.Collide
            );

        float best = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i].collider;

            if (c == null || c.transform.IsChildOf(transform))
                continue;

            if (!c.CompareTag(obstacleTag))
                continue;

            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                hitNormal = hits[i].normal;
                found = true;
            }
        }

        if (found)
            hitDistance = Mathf.Max(0f, best);

        return found;
    }

    private Vector3 Flat(Vector3 v)
    {
        if (ignoreDepthAxis)
            v.z = 0f;

        return v;
    }


    // =========================================================
    // CAPTURE TEST
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

        Vector3 prevEntry = targetCannon.GetPreviousEntryPosition();
        Vector3 currEntry = targetCannon.GetCurrentEntryPosition();

        Vector3 prevBody = targetCannon.GetPreviousCannonPosition();
        Vector3 currBody = targetCannon.GetCurrentCannonPosition();

        float entryRadius = targetCannon.GetCaptureRadius();
        float bodyRadius = targetCannon.GetBodyRadius();

        int steps =
            Mathf.Clamp(
                Mathf.CeilToInt(
                    distance / Mathf.Max(0.05f, maxStepDistance)
                ),
                1,
                128
            );

        for (int i = 1; i <= steps; i++)
        {
            float f0 = (float)(i - 1) / steps;
            float f1 = (float)i / steps;

            Vector3 p0 = start + flightDirection * (distance * f0);
            Vector3 p1 = start + flightDirection * (distance * f1);

            // The cannon is interpolated over the same time slice.
            float t0 = tBase + (1f - tBase) * f0;
            float t1 = tBase + (1f - tBase) * f1;

            Vector3 e0 = Vector3.Lerp(prevEntry, currEntry, t0);
            Vector3 e1 = Vector3.Lerp(prevEntry, currEntry, t1);

            Vector3 b0 = Vector3.Lerp(prevBody, currBody, t0);
            Vector3 b1 = Vector3.Lerp(prevBody, currBody, t1);

            float entryDistance =
                BulletCannon.SegmentSegmentDistance(
                    Flat(p0), Flat(p1), Flat(e0), Flat(e1)
                );

            float bodyDistance =
                BulletCannon.SegmentSegmentDistance(
                    Flat(p0), Flat(p1), Flat(b0), Flat(b1)
                );

            if (entryDistance <= entryRadius ||
                bodyDistance <= bodyRadius)
            {
                transform.position = p1;

                BeginCannonEntry();

                return true;
            }
        }

        return false;
    }


    // =========================================================
    // MISS TEST
    // =========================================================

    private void CheckForMiss()
    {
        if (targetCannon == null || hasMissedTarget)
            return;

        if (flightTimer >= maxFlightTime)
        {
            MissedTargetCannon();
            return;
        }

        Vector3 entry = targetCannon.GetCurrentEntryPosition();

        float passed =
            Vector3.Dot(
                transform.position - entry,
                flightDirection
            );

        float allowance =
            targetCannon.GetCaptureRadius() +
            targetCannon.GetMovementExtent() +
            Mathf.Max(0f, missMargin);

        if (passed > allowance)
            MissedTargetCannon();
    }


    // =========================================================
    // BEGIN CANNON ENTRY (smooth capture, no coroutine)
    // =========================================================

    private void BeginCannonEntry()
    {
        if (targetCannon == null || isEnteringCannon)
            return;

        BulletCannon cannon = targetCannon;

        Transform entryPoint = cannon.GetEntryPoint();

        if (entryPoint == null)
            return;

        targetCannon = null;
        hasMissedTarget = false;

        capturingCannon = cannon;

        isEnteringCannon = true;
        isFlying = false;
        isInsideCannon = false;
        inBarrel = false;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        /*
         * Work RELATIVE to the cannon so a moving / rotating cannon is
         * followed with zero lag.
         */

        followOffset =
            transform.position - entryPoint.position;

        // Player velocity minus the cannon's own entry velocity.
        Vector3 playerVelocity =
            flightDirection * lastFlightSpeed;

        Vector3 entryVelocity = Vector3.zero;

        float dt = Time.deltaTime;

        if (dt > 0.0001f)
        {
            entryVelocity =
                (cannon.GetCurrentEntryPosition() -
                 cannon.GetPreviousEntryPosition()) / dt;
        }

        followOffsetVelocity =
            LimitApproachVelocity(
                playerVelocity - entryVelocity,
                followOffset
            );

        // Rotation offset relative to the cannon's target rotation.
        Quaternion targetRotation =
            cannon.GetCannonRotation() * cannonPlayerOffset;

        rotOffset =
            Quaternion.Inverse(targetRotation) * transform.rotation;

        entryElapsed = 0f;

        captureFrame = Time.frameCount;
    }


    // =========================================================
    // OBSTACLE COLLISION
    // =========================================================

    private void OnCollisionEnter(Collision collision)
    {
        if (!isFlying)
            return;

        if (!collision.gameObject.CompareTag(obstacleTag))
            return;

        Vector3 hitNormal = Vector3.zero;

        if (collision.contactCount > 0)
            hitNormal = collision.GetContact(0).normal;

        if (hitNormal.sqrMagnitude < 0.001f)
            hitNormal = -flightDirection;

        HitObstacle(hitNormal.normalized);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFlying)
            return;

        if (!other.gameObject.CompareTag(obstacleTag))
            return;

        Vector3 hitNormal =
            transform.position -
            other.ClosestPoint(transform.position);

        if (hitNormal.sqrMagnitude < 0.001f)
            hitNormal = -flightDirection;

        HitObstacle(hitNormal.normalized);
    }

    private void HitObstacle(Vector3 hitNormal)
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

        transform.position += hitNormal * obstacleSeparation;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.velocity =
            hitNormal * obstacleBounceForce +
            flightDirection * obstacleForwardForce;

        Vector3 tumbleAxis = Vector3.Cross(Vector3.up, hitNormal);

        if (tumbleAxis.sqrMagnitude < 0.001f)
            tumbleAxis = Vector3.right;

        rb.AddTorque(
            tumbleAxis.normalized * 8f,
            ForceMode.Impulse
        );

        Debug.Log("BulletBoyPlayer: Hit obstacle and started falling.");

        if (Manager != null)
            Manager.PlayerMissedCannon();
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
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.velocity =
            flightDirection * missedCannonForwardSpeed +
            Vector3.down * missedCannonDownwardSpeed;

        Vector3 tumbleAxis = Vector3.Cross(Vector3.up, flightDirection);

        if (tumbleAxis.sqrMagnitude < 0.001f)
            tumbleAxis = Vector3.right;

        rb.AddTorque(
            tumbleAxis.normalized * missedCannonTorque,
            ForceMode.Impulse
        );

        Debug.Log("BulletBoyPlayer: Missed target cannon and started falling.");

        if (Manager != null)
            Manager.PlayerMissedCannon();
    }


    // =========================================================
    // TARGET CANNON
    // =========================================================

    public void SetTargetCannon(BulletCannon cannon)
    {
        targetCannon = cannon;
        hasMissedTarget = false;

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
        targetCannon = null;
        hasMissedTarget = false;
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