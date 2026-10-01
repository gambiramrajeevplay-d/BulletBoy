using UnityEngine;

public class BirdController : MonoBehaviour
{
    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]

    [SerializeField] private float moveSpeed = 4f;

    [SerializeField]
    private Vector3 moveDirection = Vector3.left;


    // =========================================================
    // ANIMATION
    // =========================================================

    [Header("Animation")]

    [SerializeField] private Animator animator;

    [SerializeField] private string flyAnimationName = "Fly";


    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Rotation")]

    [SerializeField] private bool rotateToMovementDirection = true;

    [SerializeField] private float rotationSpeed = 8f;


    // =========================================================
    // NORMAL VERTICAL MOVEMENT
    // =========================================================

    [Header("Vertical Flying")]

    [SerializeField] private float verticalMovementAmount = 4.5f;

    [SerializeField] private float verticalMovementSpeed = 1.4f;

    [SerializeField] private float verticalSmoothness = 5f;


    // =========================================================
    // CANNON DETECTION
    // =========================================================

    [Header("Cannon Detection")]

    [SerializeField] private bool detectCannons = true;

    [Tooltip("How far ahead the bird detects a cannon.")]
    [SerializeField] private float cannonDetectionDistance = 8f;

    [Tooltip("Radius of the detection sphere.")]
    [SerializeField] private float cannonDetectionRadius = 1.5f;

    [SerializeField] private LayerMask cannonDetectionLayers = ~0;


    // =========================================================
    // OBSTACLE DETECTION
    // =========================================================

    [Header("Obstacle Detection")]

    [SerializeField] private bool detectObstacles = true;

    [Tooltip("Tag used by objects that birds must avoid.")]
    [SerializeField] private string obstacleTag = "Obstacle";

    [Tooltip("How far ahead the bird detects obstacles.")]
    [SerializeField] private float obstacleDetectionDistance = 8f;

    [Tooltip("Radius of the obstacle detection sphere.")]
    [SerializeField] private float obstacleDetectionRadius = 1.5f;

    [SerializeField] private LayerMask obstacleDetectionLayers = ~0;


    // =========================================================
    // AVOIDANCE
    // =========================================================

    [Header("Bird Avoidance")]

    [SerializeField] private float avoidanceHeight = 4f;

    [SerializeField] private float avoidanceSpeed = 8f;

    [SerializeField] private float returnSpeed = 3f;

    [SerializeField] private float minimumAvoidanceDuration = 0.8f;

    [SerializeField] private float emergencyAvoidanceDistance = 2.5f;

    [SerializeField] private float emergencyAvoidanceSpeed = 14f;

    [SerializeField] private float avoidanceCooldown = 0.25f;


    // =========================================================
    // AVOIDANCE LIMITS
    // =========================================================

    [Header("Avoidance Limits")]

    [SerializeField] private float maximumUpAvoidance = 6f;

    [SerializeField] private float maximumDownAvoidance = 4f;


    // =========================================================
    // MINIMUM HEIGHT
    // =========================================================

    [Header("Minimum Height")]

    [SerializeField] private float minimumHeight = 1f;


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
    // BIRD HIT / FALLING
    // =========================================================

    [Header("Bird Hit Falling")]

    [SerializeField] private float hitGravity = 20f;

    [SerializeField] private float hitForwardSpeed = 4f;

    [SerializeField] private float hitDownwardSpeed = 2f;

    [SerializeField] private float hitTorque = 8f;


    // =========================================================
    // HIT LIFETIME
    // =========================================================

    [Header("Bird Hit Lifetime")]

    [SerializeField] private float hitDestroyDelay = 5f;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField] private bool showRaycast = true;

    [SerializeField] private bool showAvoidanceLogs = false;


    // =========================================================
    // PRIVATE MOVEMENT
    // =========================================================

    private float actualSpeed;

    private float verticalPhase;

    private float startingHeight;

    private float targetAvoidanceOffset;

    private float currentAvoidanceOffset;

    private bool avoidingObstacle;

    private float avoidanceTimer;

    private float avoidanceCooldownTimer;

    private Vector3 lastMovementDirection;


    // =========================================================
    // CURRENT OBSTACLE
    // =========================================================

    private Collider currentObstacle;

    private float obstacleClearTimer;

    private const float OBSTACLE_CLEAR_TIME = 0.25f;


    // =========================================================
    // HIT STATE
    // =========================================================

    private bool hasBeenHit;

    private Rigidbody rb;

    private float hitTimer;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // FORCE -X
        // -----------------------------------------------------

        if (moveDirection.sqrMagnitude < 0.001f)
        {
            moveDirection = Vector3.left;
        }

        moveDirection.y = 0f;
        moveDirection.z = 0f;

        moveDirection.x = -Mathf.Abs(moveDirection.x);

        if (Mathf.Abs(moveDirection.x) < 0.001f)
        {
            moveDirection = Vector3.left;
        }

        moveDirection.Normalize();


        // -----------------------------------------------------
        // MOVEMENT
        // -----------------------------------------------------

        actualSpeed = moveSpeed;

        startingHeight = transform.position.y;

        verticalPhase = Random.Range(
            0f,
            Mathf.PI * 2f
        );

        lastMovementDirection = moveDirection;


        // -----------------------------------------------------
        // ANIMATOR
        // -----------------------------------------------------

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }


        // -----------------------------------------------------
        // RIGIDBODY
        // -----------------------------------------------------

        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }


        // -----------------------------------------------------
        // FLY ANIMATION
        // -----------------------------------------------------

        if (animator != null &&
            !string.IsNullOrEmpty(flyAnimationName))
        {
            animator.Play(
                flyAnimationName,
                0,
                Random.Range(0f, 1f)
            );
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Once hit, Rigidbody controls the bird.
        if (hasBeenHit)
        {
            UpdateHitLifetime();
            return;
        }

        UpdateAvoidanceCooldown();

        DetectObstacle();

        MoveBird();

        RotateBird();

        CheckDestroyArea();
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (!hasBeenHit)
            return;

        if (rb == null)
            return;

        rb.AddForce(
            Vector3.down * hitGravity,
            ForceMode.Acceleration
        );
    }


    // =========================================================
    // DETECT OBSTACLE
    // =========================================================

    private void DetectObstacle()
    {
        if (avoidingObstacle)
        {
            CheckCurrentObstacle();
            return;
        }

        if (avoidanceCooldownTimer > 0f)
            return;

        Vector3 origin =
            transform.position;

        Vector3 direction =
            moveDirection.normalized;


        // =====================================================
        // CANNON
        // =====================================================

        if (detectCannons)
        {
            Collider cannonCollider =
                FindCannonAhead(
                    origin,
                    direction
                );

            if (cannonCollider != null)
            {
                StartAvoidance(
                    cannonCollider
                );

                return;
            }
        }


        // =====================================================
        // OBSTACLE
        // =====================================================

        if (detectObstacles)
        {
            Collider obstacleCollider =
                FindObstacleAhead(
                    origin,
                    direction
                );

            if (obstacleCollider != null)
            {
                StartAvoidance(
                    obstacleCollider
                );

                return;
            }
        }
    }


    // =========================================================
    // FIND CANNON
    // =========================================================

    private Collider FindCannonAhead(
        Vector3 origin,
        Vector3 direction)
    {
        RaycastHit[] hits =
            Physics.SphereCastAll(
                origin,
                cannonDetectionRadius,
                direction,
                cannonDetectionDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            );

        if (hits == null ||
            hits.Length == 0)
        {
            return null;
        }

        float closestDistance =
            float.MaxValue;

        Collider closestCollider = null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (ShouldIgnoreCollider(hit.collider))
                continue;

            BulletCannon cannon =
                hit.collider.GetComponentInParent<BulletCannon>();

            if (cannon == null)
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance =
                    hit.distance;

                closestCollider =
                    hit.collider;
            }
        }

        return closestCollider;
    }


    // =========================================================
    // FIND OBSTACLE
    // =========================================================

    private Collider FindObstacleAhead(
        Vector3 origin,
        Vector3 direction)
    {
        RaycastHit[] hits =
            Physics.SphereCastAll(
                origin,
                obstacleDetectionRadius,
                direction,
                obstacleDetectionDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            );

        if (hits == null ||
            hits.Length == 0)
        {
            return null;
        }

        float closestDistance =
            float.MaxValue;

        Collider closestCollider = null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (ShouldIgnoreCollider(hit.collider))
                continue;

            if (hit.collider.GetComponentInParent<BulletCannon>() != null)
                continue;

            if (!HasObstacleTagInHierarchy(
                hit.collider.transform))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance =
                    hit.distance;

                closestCollider =
                    hit.collider;
            }
        }

        return closestCollider;
    }


    // =========================================================
    // CHECK TAG IN HIERARCHY
    // =========================================================

    private bool HasObstacleTagInHierarchy(
        Transform target)
    {
        Transform current =
            target;

        while (current != null)
        {
            if (current.CompareTag(obstacleTag))
                return true;

            current =
                current.parent;
        }

        return false;
    }


    // =========================================================
    // IGNORE COLLIDER
    // =========================================================

    private bool ShouldIgnoreCollider(
        Collider collider)
    {
        if (collider == null)
            return true;

        if (collider.transform.root ==
            transform.root)
        {
            return true;
        }

        if (collider.GetComponentInParent<BirdController>() != null)
        {
            return true;
        }

        if (collider.GetComponentInParent<BulletBoyPlayer>() != null)
        {
            return true;
        }

        return false;
    }


    // =========================================================
    // START AVOIDANCE
    // =========================================================

    private void StartAvoidance(
        Collider obstacle)
    {
        if (obstacle == null)
            return;

        avoidingObstacle = true;

        avoidanceTimer = 0f;

        obstacleClearTimer = 0f;

        currentObstacle = obstacle;

        float normalHeight =
            GetNormalFlightHeight();

        float heightDifference =
            transform.position.y -
            normalHeight;

        if (heightDifference >= 0f)
        {
            targetAvoidanceOffset =
                -Mathf.Abs(avoidanceHeight);
        }
        else
        {
            targetAvoidanceOffset =
                Mathf.Abs(avoidanceHeight);
        }

        float distance =
            Vector3.Distance(
                transform.position,
                obstacle.ClosestPoint(
                    transform.position
                )
            );

        if (distance <=
            emergencyAvoidanceDistance)
        {
            float emergencyHeight =
                Mathf.Abs(avoidanceHeight) * 1.35f;

            if (targetAvoidanceOffset > 0f)
            {
                targetAvoidanceOffset =
                    Mathf.Min(
                        emergencyHeight,
                        maximumUpAvoidance
                    );
            }
            else
            {
                targetAvoidanceOffset =
                    -Mathf.Min(
                        emergencyHeight,
                        maximumDownAvoidance
                    );
            }
        }

        if (showAvoidanceLogs)
        {
            Debug.Log(
                "Bird Avoidance Started: " +
                obstacle.name
            );
        }
    }


    // =========================================================
    // CHECK CURRENT OBSTACLE
    // =========================================================

    private void CheckCurrentObstacle()
    {
        if (currentObstacle == null)
        {
            FinishAvoidance();
            return;
        }

        Vector3 closestPoint =
            currentObstacle.ClosestPoint(
                transform.position
            );

        float distance =
            Vector3.Distance(
                transform.position,
                closestPoint
            );

        if (distance <=
            obstacleDetectionRadius + 1f)
        {
            obstacleClearTimer = 0f;
            return;
        }

        obstacleClearTimer +=
            Time.deltaTime;

        if (obstacleClearTimer >=
            OBSTACLE_CLEAR_TIME)
        {
            FinishAvoidance();
        }
    }


    // =========================================================
    // FINISH AVOIDANCE
    // =========================================================

    private void FinishAvoidance()
    {
        avoidingObstacle = false;

        targetAvoidanceOffset = 0f;

        currentObstacle = null;

        obstacleClearTimer = 0f;

        avoidanceCooldownTimer =
            avoidanceCooldown;
    }


    // =========================================================
    // UPDATE AVOIDANCE
    // =========================================================

    private void UpdateAvoidance()
    {
        if (!avoidingObstacle)
            return;

        avoidanceTimer +=
            Time.deltaTime;

        if (avoidanceTimer <
            minimumAvoidanceDuration)
        {
            return;
        }

        CheckCurrentObstacle();
    }


    // =========================================================
    // UPDATE COOLDOWN
    // =========================================================

    private void UpdateAvoidanceCooldown()
    {
        if (avoidanceCooldownTimer <= 0f)
            return;

        avoidanceCooldownTimer -=
            Time.deltaTime;
    }


    // =========================================================
    // NORMAL HEIGHT
    // =========================================================

    private float GetNormalFlightHeight()
    {
        return startingHeight +
               Mathf.Sin(
                   Time.time *
                   verticalMovementSpeed +
                   verticalPhase
               ) *
               verticalMovementAmount;
    }


    // =========================================================
    // MOVE BIRD
    // =========================================================

    private void MoveBird()
    {
        Vector3 forwardMovement =
            moveDirection.normalized *
            actualSpeed *
            Time.deltaTime;

        transform.position +=
            forwardMovement;

        float sineMovement =
            Mathf.Sin(
                Time.time *
                verticalMovementSpeed +
                verticalPhase
            ) *
            verticalMovementAmount;

        UpdateAvoidance();

        float desiredAvoidance =
            avoidingObstacle
                ? targetAvoidanceOffset
                : 0f;

        float verticalSpeed =
            avoidingObstacle
                ? avoidanceSpeed
                : returnSpeed;

        if (avoidingObstacle &&
            currentObstacle != null)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    currentObstacle.ClosestPoint(
                        transform.position
                    )
                );

            if (distance <=
                emergencyAvoidanceDistance)
            {
                verticalSpeed =
                    emergencyAvoidanceSpeed;
            }
        }

        currentAvoidanceOffset =
            Mathf.MoveTowards(
                currentAvoidanceOffset,
                desiredAvoidance,
                verticalSpeed *
                Time.deltaTime
            );

        currentAvoidanceOffset =
            Mathf.Clamp(
                currentAvoidanceOffset,
                -maximumDownAvoidance,
                maximumUpAvoidance
            );

        float targetY =
            startingHeight +
            sineMovement +
            currentAvoidanceOffset;

        targetY =
            Mathf.Max(
                targetY,
                minimumHeight
            );

        float smoothAmount =
            avoidingObstacle
                ? Mathf.Max(
                    1f,
                    verticalSmoothness
                )
                : verticalSmoothness;

        float newY =
            Mathf.Lerp(
                transform.position.y,
                targetY,
                Mathf.Clamp01(
                    smoothAmount *
                    Time.deltaTime
                )
            );

        transform.position =
            new Vector3(
                transform.position.x,
                newY,
                transform.position.z
            );

        lastMovementDirection =
            moveDirection.normalized;
    }


    // =========================================================
    // ROTATE BIRD
    // =========================================================

    private void RotateBird()
    {
        if (!rotateToMovementDirection)
            return;

        Vector3 direction =
            lastMovementDirection;

        float verticalVelocity =
            Mathf.Cos(
                Time.time *
                verticalMovementSpeed +
                verticalPhase
            );

        direction.y =
            verticalVelocity * 0.35f;

        if (Mathf.Abs(
            currentAvoidanceOffset) > 0.05f)
        {
            float avoidanceDirection =
                Mathf.Sign(
                    currentAvoidanceOffset
                );

            direction.y +=
                avoidanceDirection * 0.65f;
        }

        if (direction.sqrMagnitude >
            0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
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

        avoidingObstacle = false;

        targetAvoidanceOffset = 0f;

        currentAvoidanceOffset = 0f;

        currentObstacle = null;


        // -----------------------------------------------------
        // STOP FLY ANIMATION
        // -----------------------------------------------------

        if (animator != null)
        {
            animator.enabled = false;
        }


        // -----------------------------------------------------
        // GET / CREATE RIGIDBODY
        // -----------------------------------------------------

        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }


        // -----------------------------------------------------
        // ENABLE PHYSICS
        // -----------------------------------------------------

        rb.isKinematic = false;

        rb.useGravity = false;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;


        // -----------------------------------------------------
        // RESET
        // -----------------------------------------------------

        rb.velocity = Vector3.zero;

        rb.angularVelocity = Vector3.zero;


        // -----------------------------------------------------
        // DIRECTION
        // -----------------------------------------------------

        Vector3 direction =
            playerFlightDirection;

        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                moveDirection;
        }

        direction.Normalize();


        // -----------------------------------------------------
        // SPEED
        // -----------------------------------------------------

        float forwardSpeed =
            Mathf.Max(
                hitForwardSpeed,
                playerFlightSpeed * 0.25f
            );


        // -----------------------------------------------------
        // INITIAL FALL VELOCITY
        // -----------------------------------------------------

        rb.velocity =
            direction *
            forwardSpeed;

        rb.velocity +=
            Vector3.down *
            hitDownwardSpeed;


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
            hitTorque,
            ForceMode.Impulse
        );
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
        if (!destroyOutsideSpawnArea)
            return;

        if (spawnArea == null)
            return;

        Vector3 localPosition =
            spawnArea.InverseTransformPoint(
                transform.position
            );

        float forwardPosition =
            Vector3.Dot(
                transform.position -
                spawnArea.position,
                moveDirection.normalized
            );

        if (forwardPosition >
            spawnAreaWidth +
            destroyDistance)
        {
            Destroy(gameObject);
            return;
        }

        if (Mathf.Abs(
            localPosition.z) >
            spawnAreaDepth +
            destroyDistance)
        {
            Destroy(gameObject);
        }
    }


    // =========================================================
    // SET DIRECTION
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
                -Mathf.Abs(direction.x);

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


    // =========================================================
    // SET SPEED
    // =========================================================

    public void SetSpeed(
        float speed)
    {
        actualSpeed = speed;
    }


    // =========================================================
    // SET VERTICAL MOVEMENT
    // =========================================================

    public void SetVerticalMovement(
        float amount,
        float speed)
    {
        verticalMovementAmount =
            amount;

        verticalMovementSpeed =
            speed;
    }


    // =========================================================
    // RANDOM PHASE
    // =========================================================

    public void RandomizeVerticalPhase()
    {
        verticalPhase =
            Random.Range(
                0f,
                Mathf.PI * 2f
            );
    }


    // =========================================================
    // SET CANNON DETECTION
    // =========================================================

    public void SetCannonDetection(
        bool enabled,
        float distance,
        float radius,
        LayerMask layers)
    {
        detectCannons = enabled;

        cannonDetectionDistance =
            distance;

        cannonDetectionRadius =
            radius;

        cannonDetectionLayers =
            layers;
    }


    // =========================================================
    // SET OBSTACLE DETECTION
    // =========================================================

    public void SetObstacleDetection(
        bool enabled,
        float distance,
        float radius,
        LayerMask layers)
    {
        detectObstacles = enabled;

        obstacleDetectionDistance =
            distance;

        obstacleDetectionRadius =
            radius;

        obstacleDetectionLayers =
            layers;
    }


    // =========================================================
    // SET AVOIDANCE
    // =========================================================

    public void SetAvoidanceSettings(
        float height,
        float speed,
        float returnSpeedValue,
        float duration,
        float cooldown)
    {
        avoidanceHeight =
            height;

        avoidanceSpeed =
            speed;

        returnSpeed =
            returnSpeedValue;

        minimumAvoidanceDuration =
            duration;

        avoidanceCooldown =
            cooldown;
    }


    // =========================================================
    // SET DESTROY
    // =========================================================

    public void SetDestroySettings(
        Transform area,
        float width,
        float depth,
        float destroyDistanceValue)
    {
        spawnArea = area;

        spawnAreaWidth =
            Mathf.Abs(width);

        spawnAreaDepth =
            Mathf.Abs(depth);

        destroyDistance =
            Mathf.Max(
                0f,
                destroyDistanceValue
            );
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        if (!showRaycast)
            return;

        Vector3 origin =
            transform.position;

        Vector3 direction =
            moveDirection.sqrMagnitude >
            0.001f
                ? moveDirection.normalized
                : Vector3.left;


        if (detectCannons)
        {
            Gizmos.color =
                Color.yellow;

            Gizmos.DrawRay(
                origin,
                direction *
                cannonDetectionDistance
            );

            Gizmos.DrawWireSphere(
                origin +
                direction *
                cannonDetectionDistance,
                cannonDetectionRadius
            );
        }


        if (detectObstacles)
        {
            Gizmos.color =
                Color.red;

            Gizmos.DrawRay(
                origin,
                direction *
                obstacleDetectionDistance
            );

            Gizmos.DrawWireSphere(
                origin +
                direction *
                obstacleDetectionDistance,
                obstacleDetectionRadius
            );
        }


        if (avoidingObstacle)
        {
            Gizmos.color =
                Color.green;

            float normalHeight =
                GetNormalFlightHeight();

            Vector3 target =
                transform.position;

            target.y =
                normalHeight +
                targetAvoidanceOffset;

            Gizmos.DrawLine(
                transform.position,
                target
            );

            Gizmos.DrawWireSphere(
                target,
                0.3f
            );
        }
    }
}