using System.Collections;
using UnityEngine;

/*
 * Runs AFTER BulletCannon (-100) and CannonManager (-50), so cannon
 * positions and rotations are always up to date for this frame.
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


    [Header("Cannon Entry Visual")]

    [SerializeField]
    private float cannonEntryDuration = 0.16f;

    [SerializeField]
    private float cannonEntryPull = 1.35f;

    [SerializeField]
    private float cannonEntryArc = 0.08f;


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

    private bool hasMissedTarget;
    private float flightTimer;

    private Vector3 previousFlightPosition;

    private Coroutine cannonEntryCoroutine;

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
        if (isEnteringCannon)
            return;

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
        if (isInsideCannon && currentCannon != null)
            FollowCurrentCannon();

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

    private void FollowCurrentCannon()
    {
        Transform entryPoint = currentCannon.GetEntryPoint();

        if (entryPoint == null)
            return;

        transform.position = entryPoint.position;

        transform.rotation =
            currentCannon.GetCannonRotation() * cannonPlayerOffset;
    }


    // =========================================================
    // ENTER CANNON (called by BulletCannon)
    // =========================================================

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

        isEnteringCannon = false;
        isFlying = false;
        isInsideCannon = true;

        currentCannon = cannon;
        targetCannon = null;

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

        if (cannonEntryCoroutine != null)
        {
            StopCoroutine(cannonEntryCoroutine);
            cannonEntryCoroutine = null;
        }

        isEnteringCannon = false;
        isInsideCannon = false;
        isFlying = true;

        currentCannon = null;

        transform.position = exitPoint.position;
        transform.rotation = cannonRotation * cannonPlayerOffset;

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

        hasMissedTarget = false;
        flightTimer = 0f;

        previousFlightPosition = transform.position;

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log("BulletBoyPlayer: Launched, direction = " + flightDirection);
    }


    // =========================================================
    // FLIGHT (sub-stepped, frame-rate independent)
    // =========================================================

    private void UpdateFlight()
    {
        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        Vector3 start = transform.position;

        previousFlightPosition = start;

        float distance = flightSpeed * dt;


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
            TryCaptureAlongPath(start, distance))
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


        flightTimer += dt;

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

    private bool TryCaptureAlongPath(Vector3 start, float distance)
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
            float t0 = (float)(i - 1) / steps;
            float t1 = (float)i / steps;

            Vector3 p0 = start + flightDirection * (distance * t0);
            Vector3 p1 = start + flightDirection * (distance * t1);

            // The cannon is interpolated over the same time slice.
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
    // BEGIN CANNON ENTRY
    // =========================================================

    private void BeginCannonEntry()
    {
        if (targetCannon == null || isEnteringCannon)
            return;

        BulletCannon cannon = targetCannon;

        targetCannon = null;
        hasMissedTarget = false;

        cannonEntryCoroutine =
            StartCoroutine(CannonEntryRoutine(cannon));
    }

    private IEnumerator CannonEntryRoutine(BulletCannon cannon)
    {
        if (cannon == null)
            yield break;

        Transform entryPoint = cannon.GetEntryPoint();
        Transform exitPoint = cannon.GetExitPoint();

        if (entryPoint == null || exitPoint == null)
            yield break;

        isEnteringCannon = true;
        isFlying = false;
        isInsideCannon = false;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 arcDirection =
            Vector3.Cross(
                flightDirection.sqrMagnitude > 0.001f
                    ? flightDirection
                    : Vector3.right,
                Vector3.forward
            );

        if (arcDirection.sqrMagnitude < 0.001f)
            arcDirection = Vector3.up;

        arcDirection.Normalize();

        float duration = Mathf.Max(0.01f, cannonEntryDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float rawT = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, rawT);
            float pullT = Mathf.Pow(smoothT, Mathf.Max(0.25f, cannonEntryPull));

            // Read every frame so a moving cannon is followed.
            Vector3 currentEntryPosition = entryPoint.position;

            Quaternion targetRotation =
                cannon.GetCannonRotation() * cannonPlayerOffset;

            if (rawT >= 1f)
            {
                transform.position = currentEntryPosition;
                transform.rotation = targetRotation;
            }
            else
            {
                Vector3 desired =
                    Vector3.Lerp(startPosition, currentEntryPosition, pullT);

                desired +=
                    arcDirection *
                    (Mathf.Sin(rawT * Mathf.PI) * cannonEntryArc);

                transform.position = desired;

                transform.rotation =
                    Quaternion.Slerp(
                        startRotation,
                        targetRotation,
                        Mathf.SmoothStep(0f, 1f, pullT)
                    );
            }

            yield return null;
        }

        isEnteringCannon = false;
        cannonEntryCoroutine = null;

        cannon.EnterCannon(this);

        if (Manager != null)
            Manager.PlayerEnteredCannon(cannon);
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

        StopEntryRoutine();

        isEnteringCannon = false;
        isFlying = false;
        isInsideCannon = false;

        currentCannon = null;
        targetCannon = null;

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

        StopEntryRoutine();

        isEnteringCannon = false;
        isFlying = false;
        isInsideCannon = false;

        currentCannon = null;
        targetCannon = null;

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

    private void StopEntryRoutine()
    {
        if (cannonEntryCoroutine != null)
        {
            StopCoroutine(cannonEntryCoroutine);
            cannonEntryCoroutine = null;
        }
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