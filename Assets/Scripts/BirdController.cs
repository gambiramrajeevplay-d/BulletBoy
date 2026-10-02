using UnityEngine;

/// <summary>
/// Bullet Boy style bird.
///
/// Bird travels from +X toward -X.
///
/// Detection system:
/// 1. Forward detection for cannons / obstacles.
/// 2. DOWNWARD detection for moving cannons / obstacles underneath bird.
/// 3. Predictive detection for moving cannons.
/// 4. Full movement sphere/capsule sweep.
/// 5. Final overlap protection.
///
/// The bird is never allowed to finish a frame inside a cannon or obstacle.
/// </summary>
public class BirdController : MonoBehaviour
{
    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]

    [SerializeField] private float moveSpeed = 4f;

    [SerializeField] private Vector3 moveDirection = Vector3.left;


    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Locked Rotation")]

    [SerializeField] private bool lockRotation = true;

    [SerializeField]
    private Vector3 lockedEulerRotation =
        new Vector3(0f, -103f, 0f);


    // =========================================================
    // NORMAL BIRD FLYING
    // =========================================================

    [Header("Up / Down Flying")]

    [Tooltip("Normal bird vertical bob amount.")]
    [SerializeField] private float verticalMovementAmount = 2.5f;

    [Tooltip("Normal bird vertical bob speed.")]
    [SerializeField] private float verticalMovementSpeed = 1.2f;

    [Tooltip("Maximum vertical avoidance speed.")]
    [SerializeField] private float avoidanceSpeed = 8f;

    [Tooltip("Higher = faster response.")]
    [SerializeField] private float verticalSmoothness = 8f;


    // =========================================================
    // CANNON / OBSTACLE DETECTION
    // =========================================================

    [Header("Cannon / Obstacle Detection")]

    [SerializeField] private bool detectCannons = true;

    [SerializeField] private bool detectObstacles = true;

    [SerializeField] private string obstacleTag = "Obstacle";


    // =========================================================
    // FORWARD DETECTION
    // =========================================================

    [Header("Forward Detection")]

    [Tooltip("How far ahead the bird searches.")]
    [SerializeField] private float lookAheadDistance = 10f;

    [Tooltip("Distance at which bird starts avoiding.")]
    [SerializeField] private float brakeDistance = 5f;

    [Tooltip("Minimum distance kept from cannon.")]
    [SerializeField] private float stopDistance = 1f;

    [Tooltip("Extra prediction distance.")]
    [SerializeField] private float predictionDistance = 1.5f;


    // =========================================================
    // BIRD SAFETY RADIUS
    // =========================================================

    [Header("Bird Clearance")]

    [Tooltip("Approximate radius of bird.")]
    [SerializeField] private float clearanceRadius = 1.1f;

    [Tooltip("Safety multiplier.")]
    [SerializeField] private float safetyRadiusMultiplier = 1.2f;


    // =========================================================
    // DOWNWARD DETECTION
    // =========================================================

    [Header("DOWNWARD Cannon Detection")]

    [Tooltip(
        "Extra ray/cast looking DOWN from the bird. " +
        "This detects moving cannons coming underneath."
    )]
    [SerializeField] private bool useDownwardDetection = true;

    [Tooltip("How far below the bird to scan.")]
    [SerializeField] private float downwardDetectionDistance = 5f;

    [Tooltip("Radius of downward spherecast.")]
    [SerializeField] private float downwardDetectionRadius = 0.9f;

    [Tooltip("How much higher the bird should move when something is underneath.")]
    [SerializeField] private float downwardAvoidanceHeight = 2.5f;

    [Tooltip("How strongly the bird reacts to something underneath.")]
    [SerializeField] private float downwardAvoidanceSpeed = 9f;


    // =========================================================
    // EXTRA SIDE DETECTION
    // =========================================================

    [Header("Side Safety Detection")]

    [Tooltip("Additional left/right rays prevent the bird from clipping cannon edges.")]
    [SerializeField] private bool useSideDetection = true;

    [SerializeField] private float sideDetectionDistance = 2.5f;

    [SerializeField] private float sideDetectionOffset = 0.8f;


    // =========================================================
    // HEIGHT SEARCH
    // =========================================================

    [Header("Height Search")]

    [SerializeField] private float searchStep = 0.4f;

    [SerializeField] private float maximumUp = 6f;

    [SerializeField] private float maximumDown = 4f;

    [SerializeField] private float minimumHeight = 1f;

    [SerializeField] private float checkInterval = 0.03f;


    // =========================================================
    // MOVING CANNON PREDICTION
    // =========================================================

    [Header("Moving Cannon Prediction")]

    [Tooltip(
        "Predict where moving cannons will be shortly in the future."
    )]
    [SerializeField] private bool predictMovingCannons = true;

    [Tooltip("How far into the future to predict.")]
    [SerializeField] private float movingCannonPredictionTime = 0.15f;

    [Tooltip("Extra safety distance for moving cannons.")]
    [SerializeField] private float movingCannonSafetyPadding = 0.75f;


    // =========================================================
    // HARD COLLISION PROTECTION
    // =========================================================

    [Header("Hard Collision Protection")]

    [SerializeField] private float sweepRadiusMultiplier = 1.2f;

    [SerializeField] private float overlapRadiusMultiplier = 1.15f;

    [SerializeField] private float collisionBuffer = 0.08f;

    [SerializeField] private bool syncPhysicsTransforms = true;


    // =========================================================
    // STUCK
    // =========================================================

    [Header("Destroy When Completely Blocked")]

    [SerializeField] private bool destroyIfNoWay = true;

    [SerializeField] private float stuckDestroyTime = 1.5f;


    // =========================================================
    // ANIMATION
    // =========================================================

    [Header("Animation")]

    [SerializeField] private Animator animator;

    [SerializeField] private string flyAnimationName = "Fly";


    // =========================================================
    // DESTROY AREA
    // =========================================================

    [Header("Destroy Outside Spawn Area")]

    [SerializeField] private bool destroyOutsideSpawnArea = true;

    [SerializeField] private Transform spawnArea;

    [SerializeField] private float spawnAreaWidth = 30f;

    [SerializeField] private float spawnAreaDepth = 10f;

    [SerializeField] private float destroyDistance = 10f;


    // =========================================================
    // PLAYER HIT
    // =========================================================

    [Header("Bird Hit Falling")]

    [SerializeField] private float hitGravity = 20f;

    [SerializeField] private float hitForwardSpeed = 4f;

    [SerializeField] private float hitDownwardSpeed = 2f;

    [SerializeField] private float hitTorque = 8f;


    [Header("Bird Hit Lifetime")]

    [SerializeField] private float hitDestroyDelay = 5f;


    // =========================================================
    // LEGACY / SPAWNER COMPATIBILITY
    // =========================================================

    [Header("Legacy Fields")]

    [SerializeField] private float cannonDetectionDistance = 8f;

    [SerializeField] private float cannonDetectionRadius = 1.5f;

    [SerializeField] private LayerMask cannonDetectionLayers = ~0;

    [SerializeField] private float obstacleDetectionDistance = 8f;

    [SerializeField] private float obstacleDetectionRadius = 1.5f;

    [SerializeField] private LayerMask obstacleDetectionLayers = ~0;

    [SerializeField] private float avoidanceHeight = 4f;

    [SerializeField] private float returnSpeed = 3f;

    [SerializeField] private float minimumAvoidanceDuration = 0.8f;

    [SerializeField] private float avoidanceCooldown = 0.25f;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField] private bool showRaycast = true;

    [SerializeField] private bool showDownwardRay = true;


    // =========================================================
    // PRIVATE STATE
    // =========================================================

    private float actualSpeed;

    private float startingHeight;

    private float verticalPhase;

    private float currentY;

    private float targetY;

    private float checkTimer;

    private float stuckTimer;

    private bool blockerAhead;

    private float blockerDistance = float.MaxValue;

    private bool noGapFound;

    private bool hasBeenHit;

    private Rigidbody rb;

    private float hitTimer;


    // =========================================================
    // DOWNWARD STATE
    // =========================================================

    private bool blockerBelow;

    private float blockerBelowDistance = float.MaxValue;

    private float emergencyTargetY;


    // =========================================================
    // BUFFERS
    // =========================================================

    private const int BUFFER_SIZE = 64;

    private static readonly Collider[] overlapBuffer =
        new Collider[BUFFER_SIZE];

    private static readonly RaycastHit[] hitBuffer =
        new RaycastHit[BUFFER_SIZE];


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // Force -X movement.

        if (moveDirection.sqrMagnitude < 0.001f)
            moveDirection = Vector3.left;

        moveDirection.y = 0f;
        moveDirection.z = 0f;

        moveDirection.x =
            -Mathf.Abs(moveDirection.x);

        if (Mathf.Abs(moveDirection.x) < 0.001f)
            moveDirection = Vector3.left;

        moveDirection.Normalize();


        actualSpeed = moveSpeed;


        startingHeight =
            transform.position.y;

        currentY =
            startingHeight;

        targetY =
            startingHeight;

        emergencyTargetY =
            startingHeight;


        verticalPhase =
            Random.Range(
                0.01f,
                Mathf.PI * 2f);


        if (animator == null)
            animator =
                GetComponentInChildren<Animator>();


        rb =
            GetComponent<Rigidbody>();


        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }


        if (animator != null &&
            !string.IsNullOrEmpty(
                flyAnimationName))
        {
            animator.Play(
                flyAnimationName,
                0,
                Random.Range(0f, 1f));
        }


        ApplyLockedRotation();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (hasBeenHit)
        {
            UpdateHitLifetime();
            return;
        }


        MoveBird();

        CheckStuck();

        CheckDestroyArea();
    }


    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        if (!hasBeenHit)
            ApplyLockedRotation();
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (!hasBeenHit ||
            rb == null)
            return;


        rb.AddForce(
            Vector3.down *
            hitGravity,
            ForceMode.Acceleration);
    }


    // =========================================================
    // LOCK ROTATION
    // =========================================================

    private void ApplyLockedRotation()
    {
        if (lockRotation)
        {
            transform.rotation =
                Quaternion.Euler(
                    lockedEulerRotation);
        }
    }


    // =========================================================
    // MAIN BIRD MOVEMENT
    // =========================================================

    private void MoveBird()
    {
        // Make moving/rotating cannon colliders
        // match their current Transform.

        if (syncPhysicsTransforms)
            Physics.SyncTransforms();


        Vector3 from =
            transform.position;


        // =====================================================
        // FORWARD DETECTION
        // =====================================================

        blockerDistance =
            GetForwardBlockerDistance(
                from,
                moveDirection);


        blockerAhead =
            blockerDistance <
            brakeDistance;


        // =====================================================
        // DOWNWARD DETECTION
        // =====================================================

        blockerBelowDistance =
            GetDownwardBlockerDistance(
                from);


        blockerBelow =
            useDownwardDetection &&
            blockerBelowDistance <
            downwardDetectionDistance;


        // =====================================================
        // HORIZONTAL SPEED
        // =====================================================

        float speedFactor = 1f;


        if (blockerAhead)
        {
            speedFactor =
                Mathf.Clamp01(
                    (blockerDistance -
                     stopDistance) /
                    Mathf.Max(
                        0.01f,
                        brakeDistance -
                        stopDistance));
        }


        float horizontalDistance =
            actualSpeed *
            speedFactor *
            Time.deltaTime;


        // =====================================================
        // NORMAL TARGET POSITION
        // =====================================================

        Vector3 desiredPosition =
            from;


        desiredPosition.x +=
            moveDirection.x *
            horizontalDistance;


        desiredPosition.y =
            SmoothHeight(
                desiredPosition);


        // =====================================================
        // MOVING CANNON BELOW
        // =====================================================

        if (blockerBelow)
        {
            /*
             * A cannon is underneath the bird.
             *
             * Do NOT allow the bird to descend into it.
             *
             * Move upward immediately.
             */

            float requiredY =
                from.y +
                downwardAvoidanceHeight;


            requiredY =
                Mathf.Clamp(
                    requiredY,
                    minimumHeight,
                    startingHeight +
                    maximumUp);


            emergencyTargetY =
                Mathf.Max(
                    emergencyTargetY,
                    requiredY);


            desiredPosition.y =
                Mathf.MoveTowards(
                    currentY,
                    emergencyTargetY,
                    downwardAvoidanceSpeed *
                    Time.deltaTime);
        }
        else
        {
            emergencyTargetY =
                desiredPosition.y;
        }


        // =====================================================
        // SIDE DETECTION
        // =====================================================

        if (useSideDetection)
        {
            desiredPosition =
                ApplySideSafety(
                    from,
                    desiredPosition);
        }


        // =====================================================
        // MOVING CANNON PREDICTION
        // =====================================================

        if (predictMovingCannons)
        {
            desiredPosition =
                ApplyMovingCannonPrediction(
                    from,
                    desiredPosition);
        }


        // =====================================================
        // COMPLETE MOVEMENT SWEEP
        // =====================================================

        Vector3 delta =
            desiredPosition -
            from;


        float distance =
            delta.magnitude;


        if (distance > 0.00001f)
        {
            Vector3 direction =
                delta / distance;


            float hitDistance =
                SweepBird(
                    from,
                    direction,
                    distance +
                    predictionDistance);


            if (hitDistance >= 0f)
            {
                float allowed =
                    Mathf.Max(
                        0f,
                        hitDistance -
                        collisionBuffer);


                desiredPosition =
                    from +
                    direction *
                    Mathf.Min(
                        allowed,
                        distance);
            }
        }


        // =====================================================
        // FINAL POSITION CHECK
        // =====================================================

        if (IsPositionBlocked(
            desiredPosition))
        {
            /*
             * Destination is occupied.
             *
             * Try moving upward first.
             */

            float safeY =
                FindEmergencyHeight(
                    from);


            Vector3 emergencyPosition =
                desiredPosition;


            emergencyPosition.y =
                safeY;


            if (!IsPositionBlocked(
                    emergencyPosition) &&
                IsMovementSafe(
                    from,
                    emergencyPosition))
            {
                desiredPosition =
                    emergencyPosition;
            }
            else
            {
                /*
                 * No safe route.
                 *
                 * STOP.
                 *
                 * Never move through the cannon.
                 */

                desiredPosition =
                    from;
            }
        }


        // =====================================================
        // FINAL ABSOLUTE GUARD
        // =====================================================

        if (IsPositionBlocked(
            desiredPosition))
        {
            desiredPosition =
                from;
        }


        // =====================================================
        // APPLY
        // =====================================================

        currentY =
            desiredPosition.y;


        transform.position =
            desiredPosition;
    }


    // =========================================================
    // FORWARD BLOCKER DETECTION
    // =========================================================

    private float GetForwardBlockerDistance(
        Vector3 origin,
        Vector3 direction)
    {
        float nearest =
            float.MaxValue;


        float radius =
            GetSafeRadius();


        float distance =
            lookAheadDistance +
            predictionDistance;


        int mask =
            GetDetectionMask();


        // -----------------------------------------------------
        // CENTER RAY
        // -----------------------------------------------------

        int hits =
            Physics.RaycastNonAlloc(
                origin,
                direction,
                hitBuffer,
                distance,
                mask,
                QueryTriggerInteraction.Collide);


        for (int i = 0;
             i < hits;
             i++)
        {
            if (!IsBlocker(
                hitBuffer[i].collider))
                continue;


            nearest =
                Mathf.Min(
                    nearest,
                    hitBuffer[i].distance);
        }


        // -----------------------------------------------------
        // SPHERE CAST
        // -----------------------------------------------------

        hits =
            Physics.SphereCastNonAlloc(
                origin,
                radius,
                direction,
                hitBuffer,
                distance,
                mask,
                QueryTriggerInteraction.Collide);


        for (int i = 0;
             i < hits;
             i++)
        {
            if (!IsBlocker(
                hitBuffer[i].collider))
                continue;


            nearest =
                Mathf.Min(
                    nearest,
                    hitBuffer[i].distance);
        }


        return nearest;
    }


    // =========================================================
    // DOWNWARD DETECTION
    // =========================================================

    private float GetDownwardBlockerDistance(
        Vector3 origin)
    {
        if (!useDownwardDetection)
            return float.MaxValue;


        float nearest =
            float.MaxValue;


        int mask =
            GetDetectionMask();


        // -----------------------------------------------------
        // DOWNWARD RAY
        // -----------------------------------------------------

        RaycastHit rayHit;


        if (Physics.Raycast(
            origin,
            Vector3.down,
            out rayHit,
            downwardDetectionDistance,
            mask,
            QueryTriggerInteraction.Collide))
        {
            if (IsBlocker(
                rayHit.collider))
            {
                nearest =
                    rayHit.distance;
            }
        }


        // -----------------------------------------------------
        // DOWNWARD SPHERECAST
        // -----------------------------------------------------

        float radius =
            Mathf.Max(
                0.05f,
                downwardDetectionRadius);


        int hits =
            Physics.SphereCastNonAlloc(
                origin,
                radius,
                Vector3.down,
                hitBuffer,
                downwardDetectionDistance,
                mask,
                QueryTriggerInteraction.Collide);


        for (int i = 0;
             i < hits;
             i++)
        {
            if (!IsBlocker(
                hitBuffer[i].collider))
                continue;


            nearest =
                Mathf.Min(
                    nearest,
                    hitBuffer[i].distance);
        }


        return nearest;
    }


    // =========================================================
    // SIDE SAFETY
    // =========================================================

    private Vector3 ApplySideSafety(
        Vector3 from,
        Vector3 desired)
    {
        float radius =
            Mathf.Max(
                0.05f,
                clearanceRadius);


        Vector3 leftOrigin =
            from +
            Vector3.forward *
            sideDetectionOffset;


        Vector3 rightOrigin =
            from -
            Vector3.forward *
            sideDetectionOffset;


        bool leftBlocked =
            SideBlocked(
                leftOrigin,
                moveDirection,
                sideDetectionDistance,
                radius);


        bool rightBlocked =
            SideBlocked(
                rightOrigin,
                moveDirection,
                sideDetectionDistance,
                radius);


        if (leftBlocked &&
            !rightBlocked)
        {
            desired.y +=
                avoidanceSpeed *
                Time.deltaTime;
        }
        else if (rightBlocked &&
                 !leftBlocked)
        {
            desired.y +=
                avoidanceSpeed *
                Time.deltaTime;
        }


        desired.y =
            Mathf.Clamp(
                desired.y,
                minimumHeight,
                startingHeight +
                maximumUp);


        return desired;
    }


    private bool SideBlocked(
        Vector3 origin,
        Vector3 direction,
        float distance,
        float radius)
    {
        int hits =
            Physics.SphereCastNonAlloc(
                origin,
                radius,
                direction,
                hitBuffer,
                distance,
                GetDetectionMask(),
                QueryTriggerInteraction.Collide);


        for (int i = 0;
             i < hits;
             i++)
        {
            if (IsBlocker(
                hitBuffer[i].collider))
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // MOVING CANNON PREDICTION
    // =========================================================

    private Vector3 ApplyMovingCannonPrediction(
        Vector3 from,
        Vector3 desired)
    {
        int mask =
            GetDetectionMask();


        Collider[] nearby =
            overlapBuffer;


        int count =
            Physics.OverlapSphereNonAlloc(
                from,
                lookAheadDistance,
                nearby,
                mask,
                QueryTriggerInteraction.Collide);


        for (int i = 0;
             i < count;
             i++)
        {
            Collider collider =
                nearby[i];


            if (collider == null)
                continue;


            BulletCannon cannon =
                collider.GetComponentInParent<BulletCannon>();


            if (cannon == null)
                continue;


            if (!cannon.IsMoving() &&
                !cannon.IsRotating())
            {
                continue;
            }


            Vector3 currentPosition =
                cannon.GetCurrentCannonPosition();


            Vector3 previousPosition =
                cannon.GetPreviousCannonPosition();


            Vector3 movement =
                currentPosition -
                previousPosition;


            /*
             * Predict where the moving cannon will be.
             */

            Vector3 predictedPosition =
                currentPosition +
                movement *
                (
                    movingCannonPredictionTime /
                    Mathf.Max(
                        Time.deltaTime,
                        0.001f)
                );


            Vector3 difference =
                predictedPosition -
                desired;


            /*
             * If the predicted cannon is close to the bird's
             * intended position, move the bird upward.
             */

            float safety =
                GetSafeRadius() +
                movingCannonSafetyPadding;


            if (difference.magnitude <
                safety)
            {
                float requiredY =
                    predictedPosition.y +
                    safety;


                if (requiredY >
                    desired.y)
                {
                    desired.y =
                        requiredY;
                }
            }


            /*
             * Moving up/down cannon:
             *
             * Check predicted vertical position.
             */

            if (cannon.IsMovingUpDown())
            {
                float verticalDifference =
                    predictedPosition.y -
                    desired.y;


                if (verticalDifference >
                    -safety &&
                    verticalDifference <
                    safety)
                {
                    desired.y =
                        predictedPosition.y +
                        safety;
                }
            }
        }


        desired.y =
            Mathf.Clamp(
                desired.y,
                minimumHeight,
                startingHeight +
                maximumUp);


        return desired;
    }


    // =========================================================
    // SWEEP BIRD
    // =========================================================

    private float SweepBird(
        Vector3 origin,
        Vector3 direction,
        float distance)
    {
        float radius =
            Mathf.Max(
                0.05f,
                clearanceRadius *
                sweepRadiusMultiplier);


        float nearest =
            float.MaxValue;


        int mask =
            GetDetectionMask();


        // -----------------------------------------------------
        // SPHERE CAST
        // -----------------------------------------------------

        int hits =
            Physics.SphereCastNonAlloc(
                origin,
                radius,
                direction,
                hitBuffer,
                distance,
                mask,
                QueryTriggerInteraction.Collide);


        for (int i = 0;
             i < hits;
             i++)
        {
            if (!IsBlocker(
                hitBuffer[i].collider))
                continue;


            nearest =
                Mathf.Min(
                    nearest,
                    hitBuffer[i].distance);
        }


        // -----------------------------------------------------
        // CAPSULE CAST
        // -----------------------------------------------------

        float capsuleHeight =
            Mathf.Max(
                radius * 2f,
                clearanceRadius * 2.5f);


        float half =
            Mathf.Max(
                radius,
                capsuleHeight * 0.5f);


        Vector3 point1 =
            origin +
            Vector3.up *
            (half - radius);


        Vector3 point2 =
            origin -
            Vector3.up *
            (half - radius);


        hits =
            Physics.CapsuleCastNonAlloc(
                point1,
                point2,
                radius,
                direction,
                hitBuffer,
                distance,
                mask,
                QueryTriggerInteraction.Collide);


        for (int i = 0;
             i < hits;
             i++)
        {
            if (!IsBlocker(
                hitBuffer[i].collider))
                continue;


            nearest =
                Mathf.Min(
                    nearest,
                    hitBuffer[i].distance);
        }


        if (nearest ==
            float.MaxValue)
        {
            return -1f;
        }


        return nearest;
    }


    // =========================================================
    // MOVEMENT SAFETY
    // =========================================================

    private bool IsMovementSafe(
        Vector3 from,
        Vector3 to)
    {
        Vector3 delta =
            to - from;


        float distance =
            delta.magnitude;


        if (distance <= 0.00001f)
            return true;


        Vector3 direction =
            delta / distance;


        float hit =
            SweepBird(
                from,
                direction,
                distance);


        return hit < 0f;
    }


    // =========================================================
    // SMOOTH HEIGHT
    // =========================================================

    private float SmoothHeight(
        Vector3 position)
    {
        checkTimer -=
            Time.deltaTime;


        if (checkTimer <= 0f)
        {
            checkTimer =
                Mathf.Max(
                    0.01f,
                    checkInterval);


            targetY =
                FindBestHeight(
                    position);
        }


        float smoothed =
            Mathf.Lerp(
                currentY,
                targetY,
                1f -
                Mathf.Exp(
                    -verticalSmoothness *
                    Time.deltaTime));


        float maxStep =
            Mathf.Max(
                0.01f,
                avoidanceSpeed) *
            Time.deltaTime;


        return
            currentY +
            Mathf.Clamp(
                smoothed -
                currentY,
                -maxStep,
                maxStep);
    }


    // =========================================================
    // FIND BEST HEIGHT
    // =========================================================

    private float FindBestHeight(
        Vector3 position)
    {
        float wave =
            startingHeight +
            Mathf.Sin(
                Time.time *
                verticalMovementSpeed +
                verticalPhase) *
            verticalMovementAmount;


        float low =
            Mathf.Max(
                startingHeight -
                maximumDown,
                minimumHeight);


        float high =
            startingHeight +
            maximumUp;


        wave =
            Mathf.Clamp(
                wave,
                low,
                high);


        noGapFound = false;


        float step =
            Mathf.Max(
                0.1f,
                searchStep);


        float bestY =
            wave;


        float bestCost =
            float.MaxValue;


        float bestClearDistance =
            -1f;


        float fallbackY =
            wave;


        int maxSteps =
            Mathf.CeilToInt(
                (high - low) /
                step) + 1;


        for (int i = 0;
             i <= maxSteps;
             i++)
        {
            int k =
                (i + 1) / 2;


            float sign =
                i % 2 == 1
                    ? 1f
                    : -1f;


            float y =
                wave +
                sign *
                k *
                step;


            if (y < low ||
                y > high)
            {
                continue;
            }


            float clearDistance;


            if (IsHeightClear(
                position,
                y,
                lookAheadDistance,
                out clearDistance))
            {
                float cost =
                    Mathf.Abs(
                        y - wave) +
                    0.5f *
                    Mathf.Abs(
                        y - currentY);


                if (cost < bestCost)
                {
                    bestCost =
                        cost;

                    bestY =
                        y;
                }
            }
            else
            {
                if (clearDistance >
                    bestClearDistance)
                {
                    bestClearDistance =
                        clearDistance;

                    fallbackY =
                        y;
                }
            }
        }


        if (bestCost <
            float.MaxValue)
        {
            return bestY;
        }


        noGapFound = true;

        return fallbackY;
    }


    // =========================================================
    // HEIGHT CLEAR
    // =========================================================

    private bool IsHeightClear(
        Vector3 position,
        float y,
        float lookAhead,
        out float clearDistance)
    {
        Vector3 origin =
            new Vector3(
                position.x,
                y,
                position.z);


        float radius =
            GetSafeRadius();


        clearDistance =
            lookAhead;


        // -----------------------------------------------------
        // POSITION CHECK
        // -----------------------------------------------------

        int overlaps =
            Physics.OverlapSphereNonAlloc(
                origin,
                radius,
                overlapBuffer,
                GetDetectionMask(),
                QueryTriggerInteraction.Collide);


        for (int i = 0;
             i < overlaps;
             i++)
        {
            if (IsBlocker(
                overlapBuffer[i]))
            {
                clearDistance = 0f;

                return false;
            }
        }


        // -----------------------------------------------------
        // FORWARD CHECK
        // -----------------------------------------------------

        int hits =
            Physics.SphereCastNonAlloc(
                origin,
                radius,
                moveDirection,
                hitBuffer,
                lookAhead,
                GetDetectionMask(),
                QueryTriggerInteraction.Collide);


        for (int i = 0;
             i < hits;
             i++)
        {
            if (!IsBlocker(
                hitBuffer[i].collider))
                continue;


            clearDistance =
                Mathf.Min(
                    clearDistance,
                    hitBuffer[i].distance);


            return false;
        }


        // -----------------------------------------------------
        // DOWNWARD CHECK AT CANDIDATE HEIGHT
        // -----------------------------------------------------

        if (useDownwardDetection)
        {
            float below =
                GetDownwardBlockerDistance(
                    origin);


            if (below <
                downwardDetectionDistance)
            {
                clearDistance =
                    Mathf.Min(
                        clearDistance,
                        below);


                return false;
            }
        }


        return true;
    }


    // =========================================================
    // POSITION BLOCK CHECK
    // =========================================================

    private bool IsPositionBlocked(
        Vector3 position)
    {
        float radius =
            clearanceRadius *
            overlapRadiusMultiplier;


        int count =
            Physics.OverlapSphereNonAlloc(
                position,
                radius,
                overlapBuffer,
                GetDetectionMask(),
                QueryTriggerInteraction.Collide);


        for (int i = 0;
             i < count;
             i++)
        {
            if (IsBlocker(
                overlapBuffer[i]))
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // EMERGENCY HEIGHT
    // =========================================================

    private float FindEmergencyHeight(
        Vector3 position)
    {
        float low =
            Mathf.Max(
                startingHeight -
                maximumDown,
                minimumHeight);


        float high =
            startingHeight +
            maximumUp;


        float step =
            Mathf.Max(
                0.25f,
                searchStep);


        // Try above first.
        for (float y = currentY;
             y <= high;
             y += step)
        {
            Vector3 test =
                new Vector3(
                    position.x,
                    y,
                    position.z);


            if (!IsPositionBlocked(test) &&
                IsHeightClear(
                    position,
                    y,
                    lookAheadDistance,
                    out _))
            {
                return y;
            }
        }


        // Then below.
        for (float y = currentY;
             y >= low;
             y -= step)
        {
            Vector3 test =
                new Vector3(
                    position.x,
                    y,
                    position.z);


            if (!IsPositionBlocked(test) &&
                IsHeightClear(
                    position,
                    y,
                    lookAheadDistance,
                    out _))
            {
                return y;
            }
        }


        return currentY;
    }


    // =========================================================
    // DETECTION MASK
    // =========================================================

    private int GetDetectionMask()
    {
        int mask = 0;


        if (detectCannons)
        {
            mask |=
                cannonDetectionLayers.value;
        }


        if (detectObstacles)
        {
            mask |=
                obstacleDetectionLayers.value;
        }


        if (mask == 0)
        {
            mask =
                Physics.AllLayers;
        }


        return mask;
    }


    // =========================================================
    // SAFE RADIUS
    // =========================================================

    private float GetSafeRadius()
    {
        return Mathf.Max(
            0.05f,
            clearanceRadius *
            safetyRadiusMultiplier);
    }


    // =========================================================
    // BLOCKER
    // =========================================================

    private bool IsBlocker(
        Collider collider)
    {
        if (collider == null)
            return false;


        if (ShouldIgnoreCollider(
            collider))
        {
            return false;
        }


        // -----------------------------------------------------
        // CANNON
        // -----------------------------------------------------

        if (detectCannons)
        {
            BulletCannon cannon =
                collider.GetComponentInParent<BulletCannon>();


            if (cannon != null)
            {
                return true;
            }
        }


        // -----------------------------------------------------
        // OBSTACLE
        // -----------------------------------------------------

        if (detectObstacles)
        {
            if (HasObstacleTagInHierarchy(
                collider.transform))
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // OBSTACLE TAG
    // =========================================================

    private bool HasObstacleTagInHierarchy(
        Transform target)
    {
        Transform current =
            target;


        while (current != null)
        {
            if (current.CompareTag(
                obstacleTag))
            {
                return true;
            }


            current =
                current.parent;
        }


        return false;
    }


    // =========================================================
    // IGNORE
    // =========================================================

    private bool ShouldIgnoreCollider(
        Collider other)
    {
        if (other == null)
            return true;


        // Own collider.
        if (other.transform.root ==
            transform.root)
        {
            return true;
        }


        // Other birds.
        if (other.GetComponentInParent<BirdController>() != null)
        {
            return true;
        }


        // Player.
        if (other.GetComponentInParent<BulletBoyPlayer>() != null)
        {
            return true;
        }


        return false;
    }


    // =========================================================
    // STUCK
    // =========================================================

    private void CheckStuck()
    {
        if (!destroyIfNoWay)
            return;


        bool stuck =
            blockerAhead &&
            noGapFound &&
            blockerDistance <=
            stopDistance + 0.25f;


        if (stuck)
        {
            stuckTimer +=
                Time.deltaTime;


            if (stuckTimer >=
                stuckDestroyTime)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }


    // =========================================================
    // PLAYER HIT
    // =========================================================

    public void HitByPlayer(
        Vector3 playerFlightDirection,
        float playerFlightSpeed)
    {
        if (hasBeenHit)
            return;


        hasBeenHit = true;

        hitTimer = 0f;


        if (animator != null)
            animator.enabled = false;


        rb =
            GetComponent<Rigidbody>();


        if (rb == null)
        {
            rb =
                gameObject.AddComponent<Rigidbody>();
        }


        rb.isKinematic = false;

        rb.useGravity = false;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;


        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;


        Vector3 direction =
            playerFlightDirection;


        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                moveDirection;
        }


        direction.Normalize();


        float forwardSpeed =
            Mathf.Max(
                hitForwardSpeed,
                playerFlightSpeed *
                0.25f);


        rb.velocity =
            direction *
            forwardSpeed;


        rb.velocity +=
            Vector3.down *
            hitDownwardSpeed;


        Vector3 tumbleAxis =
            Vector3.Cross(
                Vector3.up,
                direction);


        if (tumbleAxis.sqrMagnitude <
            0.001f)
        {
            tumbleAxis =
                Vector3.right;
        }


        rb.AddTorque(
            tumbleAxis.normalized *
            hitTorque,
            ForceMode.Impulse);
    }


    // =========================================================
    // HIT LIFETIME
    // =========================================================

    private void UpdateHitLifetime()
    {
        if (hitDestroyDelay <= 0f)
            return;


        hitTimer +=
            Time.deltaTime;


        if (hitTimer >=
            hitDestroyDelay)
        {
            Destroy(gameObject);
        }
    }


    // =========================================================
    // DESTROY AREA
    // =========================================================

    private void CheckDestroyArea()
    {
        if (!destroyOutsideSpawnArea ||
            spawnArea == null)
        {
            return;
        }


        float forwardPosition =
            Vector3.Dot(
                transform.position -
                spawnArea.position,
                moveDirection.normalized);


        if (forwardPosition >
            spawnAreaWidth +
            destroyDistance)
        {
            Destroy(gameObject);

            return;
        }


        Vector3 localPosition =
            spawnArea.InverseTransformPoint(
                transform.position);


        if (Mathf.Abs(
            localPosition.z) >
            spawnAreaDepth +
            destroyDistance)
        {
            Destroy(gameObject);
        }
    }


    // =========================================================
    // PUBLIC SETTERS
    // =========================================================

    public void SetDirection(
        Vector3 direction)
    {
        if (direction.sqrMagnitude >
            0.001f)
        {
            direction.y = 0f;
            direction.z = 0f;

            direction.x =
                -Mathf.Abs(
                    direction.x);


            if (Mathf.Abs(
                direction.x) <
                0.001f)
            {
                direction =
                    Vector3.left;
            }


            moveDirection =
                direction.normalized;
        }
    }


    public void SetSpeed(
        float speed)
    {
        actualSpeed =
            speed;
    }


    public void SetVerticalMovement(
        float amount,
        float speed)
    {
        if (amount > 0f)
            verticalMovementAmount =
                amount;


        if (speed > 0f)
            verticalMovementSpeed =
                speed;
    }


    public void RandomizeVerticalPhase()
    {
        verticalPhase =
            Random.Range(
                0.01f,
                Mathf.PI * 2f);
    }


    public void SetCannonDetection(
        bool enabled,
        float distance,
        float radius,
        LayerMask layers)
    {
        detectCannons =
            enabled;

        cannonDetectionDistance =
            distance;

        cannonDetectionRadius =
            radius;

        cannonDetectionLayers =
            layers;
    }


    public void SetObstacleDetection(
        bool enabled,
        float distance,
        float radius,
        LayerMask layers)
    {
        detectObstacles =
            enabled;

        obstacleDetectionDistance =
            distance;

        obstacleDetectionRadius =
            radius;

        obstacleDetectionLayers =
            layers;
    }


    public void SetAvoidanceSettings(
        float height,
        float speed,
        float returnSpeedValue,
        float duration,
        float cooldown)
    {
        avoidanceHeight =
            height;

        returnSpeed =
            returnSpeedValue;

        minimumAvoidanceDuration =
            duration;

        avoidanceCooldown =
            cooldown;


        if (speed > 0f)
            avoidanceSpeed =
                speed;
    }


    public void SetDestroySettings(
        Transform area,
        float width,
        float depth,
        float destroyDistanceValue)
    {
        spawnArea =
            area;

        spawnAreaWidth =
            Mathf.Abs(width);

        spawnAreaDepth =
            Mathf.Abs(depth);

        destroyDistance =
            Mathf.Max(
                0f,
                destroyDistanceValue);
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        if (!showRaycast)
            return;


        Vector3 direction =
            moveDirection.sqrMagnitude >
            0.001f
                ? moveDirection.normalized
                : Vector3.left;


        // Bird radius.
        Gizmos.color =
            Color.yellow;


        Gizmos.DrawWireSphere(
            transform.position,
            GetSafeRadius());


        // Forward look.
        Gizmos.color =
            blockerAhead
                ? Color.red
                : Color.cyan;


        Gizmos.DrawRay(
            transform.position,
            direction *
            lookAheadDistance);


        // -----------------------------------------------------
        // DOWNWARD RAY
        // -----------------------------------------------------

        if (showDownwardRay &&
            useDownwardDetection)
        {
            Gizmos.color =
                blockerBelow
                    ? Color.red
                    : Color.green;


            Gizmos.DrawRay(
                transform.position,
                Vector3.down *
                downwardDetectionDistance);


            Gizmos.DrawWireSphere(
                transform.position +
                Vector3.down *
                downwardDetectionDistance,
                downwardDetectionRadius);
        }


        // Target Y.
        if (Application.isPlaying)
        {
            Gizmos.color =
                noGapFound
                    ? Color.magenta
                    : Color.green;


            Gizmos.DrawWireSphere(
                new Vector3(
                    transform.position.x,
                    targetY,
                    transform.position.z),
                0.25f);
        }
    }
}