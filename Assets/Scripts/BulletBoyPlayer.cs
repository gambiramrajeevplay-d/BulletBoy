using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BulletBoyPlayer : MonoBehaviour
{
    [Header("Player Rotation")]
    [SerializeField]
    private Vector3 playerBaseRotation =
        new Vector3(0f, 0f, -90f);


    [Header("Flight")]
    [SerializeField]
    private float flightSpeed = 18f;


    [Header("Visual")]
    [SerializeField]
    private Transform visualModel;

    [SerializeField]
    private float visualSpinSpeed = 720f;


    [Header("Obstacle Hit Physics")]
    [Tooltip("How strongly the player is pushed away from the obstacle.")]
    [SerializeField]
    private float obstacleBounceForce = 4f;

    [Tooltip("Additional forward momentum after hitting an obstacle.")]
    [SerializeField]
    private float obstacleForwardForce = 2f;

    [Tooltip("Gravity strength used when falling after hitting an obstacle.")]
    [SerializeField]
    private float obstacleGravity = 20f;

    [Tooltip("Small position offset used to prevent the player from sticking inside the collider.")]
    [SerializeField]
    private float obstacleSeparation = 0.05f;


    private Rigidbody rb;

    private bool isInsideCannon;
    private bool isFlying;

    private Vector3 flightDirection;

    private Quaternion cannonPlayerOffset;

    private BulletCannon currentCannon;
    private BulletCannon targetCannon;

    private bool hasMissedTarget;


    // =========================================================
    // INITIALIZE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

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


    private void Start()
    {
        transform.rotation =
            cannonPlayerOffset;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        /*
         * Flight movement.
         */

        if (isFlying)
        {
            UpdateFlight();
        }
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        /*
         * When the player has fallen after
         * hitting an obstacle, use custom gravity.
         */

        if (!isFlying &&
            !isInsideCannon &&
            !rb.isKinematic)
        {
            rb.AddForce(
                Vector3.down *
                obstacleGravity,
                ForceMode.Acceleration
            );
        }
    }


    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        /*
         * When inside a cannon, follow the
         * cannon AFTER the cannon has finished
         * rotating for this frame.
         */

        if (isInsideCannon &&
            currentCannon != null)
        {
            FollowCurrentCannon();
        }


        /*
         * Rotate only the visual model.
         */

        SpinVisual();
    }


    // =========================================================
    // VISUAL SPIN
    // =========================================================

    private void SpinVisual()
    {
        if (visualModel == null)
            return;

        if (visualSpinSpeed == 0f)
            return;

        visualModel.Rotate(
            Vector3.up,
            visualSpinSpeed *
            Time.deltaTime,
            Space.Self
        );
    }


    // =========================================================
    // FOLLOW CURRENT CANNON
    // =========================================================

    private void FollowCurrentCannon()
    {
        if (currentCannon == null)
            return;


        Transform entryPoint =
            currentCannon.GetEntryPoint();


        if (entryPoint == null)
            return;


        /*
         * Follow the exact entry position.
         */

        transform.position =
            entryPoint.position;


        /*
         * Follow the exact cannon rotation.
         */

        transform.rotation =
            currentCannon.GetCannonRotation() *
            cannonPlayerOffset;
    }


    // =========================================================
    // ENTER CANNON
    // =========================================================

    public void EnterCannon(
        Transform entryPoint,
        Transform exitPoint,
        Quaternion cannonRotation)
    {
        if (entryPoint == null)
        {
            Debug.LogError(
                "BulletBoyPlayer: Entry Point is missing."
            );

            return;
        }


        if (exitPoint == null)
        {
            Debug.LogError(
                "BulletBoyPlayer: Exit Point is missing."
            );

            return;
        }


        /*
         * Player is no longer flying.
         */

        isFlying = false;


        /*
         * Player is now inside the cannon.
         */

        isInsideCannon = true;


        /*
         * Store the cannon that currently
         * contains the player.
         */

        currentCannon =
            entryPoint.GetComponentInParent<BulletCannon>();


        /*
         * Reset flight data.
         */

        flightDirection =
            Vector3.zero;


        /*
         * Reset miss state.
         */

        hasMissedTarget = false;


        /*
         * Stop Rigidbody movement.
         */

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        rb.useGravity = false;

        rb.isKinematic = true;


        /*
         * Put player at the entry point.
         */

        transform.position =
            entryPoint.position;


        /*
         * Match cannon rotation.
         */

        transform.rotation =
            cannonRotation *
            cannonPlayerOffset;
    }


    // =========================================================
    // SYNC WITH CANNON
    // =========================================================

    /*
     * Kept for compatibility with BulletCannon.
     *
     * The actual follow is handled by
     * LateUpdate().
     */

    public void SyncWithCannon(
        Transform entryPoint,
        Quaternion cannonRotation)
    {
        if (!isInsideCannon)
            return;

        /*
         * Do not continuously update the
         * transform here.
         *
         * LateUpdate handles it.
         */
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


        /*
         * Stop following the cannon.
         */

        isInsideCannon = false;

        isFlying = true;

        currentCannon = null;


        /*
         * Start exactly at the exit point.
         */

        transform.position =
            exitPoint.position;


        /*
         * Capture the cannon rotation
         * at the exact moment of launch.
         */

        transform.rotation =
            cannonRotation *
            cannonPlayerOffset;


        /*
         * Capture launch direction.
         *
         * This direction NEVER changes
         * during the flight.
         */

        flightDirection =
            launchDirection.normalized;


        /*
         * Store launch speed.
         */

        flightSpeed =
            Mathf.Max(
                speed,
                0.01f
            );


        /*
         * Reset miss detection.
         */

        hasMissedTarget = false;


        /*
         * Flight uses kinematic movement.
         */

        rb.useGravity = false;

        rb.isKinematic = true;

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;
    }


    // =========================================================
    // FLIGHT
    // =========================================================

    private void UpdateFlight()
    {
        /*
         * No gravity.
         *
         * Straight-line movement.
         */

        transform.position +=
            flightDirection *
            flightSpeed *
            Time.deltaTime;


        CheckTargetCannon();
    }


    // =========================================================
    // OBSTACLE COLLISION
    // =========================================================

    private void OnCollisionEnter(Collision collision)
    {
        /*
         * Ignore collisions when not flying.
         */

        if (!isFlying)
            return;


        /*
         * Only react to objects tagged
         * "Obstacle".
         */

        if (!collision.gameObject.CompareTag("Obstacle"))
            return;


        /*
         * Get the collision normal.
         */

        Vector3 hitNormal =
            Vector3.zero;

        if (collision.contactCount > 0)
        {
            hitNormal =
                collision.GetContact(0).normal;
        }


        /*
         * If for some reason the normal is invalid,
         * use the opposite flight direction.
         */

        if (hitNormal.sqrMagnitude < 0.001f)
        {
            hitNormal =
                -flightDirection;
        }


        HitObstacle(
            hitNormal.normalized
        );
    }


    // =========================================================
    // OBSTACLE TRIGGER
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        /*
         * Ignore triggers when not flying.
         */

        if (!isFlying)
            return;


        /*
         * Only react to objects tagged
         * "Obstacle".
         */

        if (!other.gameObject.CompareTag("Obstacle"))
            return;


        /*
         * Calculate a direction pointing
         * away from the obstacle.
         */

        Vector3 hitNormal =
            transform.position -
            other.ClosestPoint(transform.position);


        /*
         * If the closest-point calculation
         * produces an invalid direction,
         * use the opposite flight direction.
         */

        if (hitNormal.sqrMagnitude < 0.001f)
        {
            hitNormal =
                -flightDirection;
        }


        HitObstacle(
            hitNormal.normalized
        );
    }


    // =========================================================
    // HIT OBSTACLE
    // =========================================================

    private void HitObstacle(
        Vector3 hitNormal)
    {
        /*
         * Prevent the obstacle from being
         * processed more than once.
         */

        if (hasMissedTarget)
            return;


        hasMissedTarget = true;


        /*
         * Stop the normal Bullet Boy flight.
         */

        isFlying = false;

        isInsideCannon = false;

        currentCannon = null;

        targetCannon = null;


        /*
         * Move the player slightly away from
         * the obstacle before enabling physics.
         *
         * This prevents the player from remaining
         * stuck inside the collider.
         */

        transform.position +=
            hitNormal *
            obstacleSeparation;


        /*
         * Switch from kinematic flight movement
         * to real Rigidbody physics.
         */

        rb.isKinematic = false;

        rb.useGravity = true;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;


        /*
         * Remove the previous movement.
         */

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;


        /*
         * Give the player a small impact push.
         *
         * This creates the feeling that the
         * Bullet Boy bounced off the obstacle.
         */

        Vector3 impactVelocity =
            hitNormal *
            obstacleBounceForce;


        /*
         * Keep a little of the original flight
         * direction so the player doesn't simply
         * stop in place.
         */

        impactVelocity +=
            flightDirection *
            obstacleForwardForce;


        rb.velocity =
            impactVelocity;


        /*
         * Add some rotation so the player
         * doesn't look completely stiff while falling.
         */

        Vector3 tumbleAxis =
            Vector3.Cross(
                Vector3.up,
                hitNormal
            );

        if (tumbleAxis.sqrMagnitude < 0.001f)
        {
            tumbleAxis = Vector3.right;
        }


        rb.AddTorque(
            tumbleAxis.normalized *
            8f,
            ForceMode.Impulse
        );


        Debug.Log(
            "BulletBoyPlayer: Hit obstacle and started falling."
        );


        /*
         * Notify CannonManager using the
         * same failure behavior as a missed cannon.
         */

        CannonManager manager =
            FindObjectOfType<CannonManager>();


        if (manager != null)
        {
            manager.PlayerMissedCannon();
        }
        else
        {
            Debug.LogWarning(
                "BulletBoyPlayer: CannonManager not found."
            );
        }
    }


    // =========================================================
    // CHECK TARGET CANNON
    // =========================================================

    private void CheckTargetCannon()
    {
        if (targetCannon == null)
            return;


        if (hasMissedTarget)
            return;


        Transform targetEntry =
            targetCannon.GetEntryPoint();


        if (targetEntry == null)
            return;


        /*
         * First check whether the player
         * successfully reached the cannon.
         */

        float distance =
            Vector3.Distance(
                transform.position,
                targetEntry.position
            );


        if (distance <=
            targetCannon.GetAimRadius())
        {
            EnterTargetCannon();

            return;
        }


        /*
         * Check whether the player has
         * passed the target cannon.
         *
         * Dot product tells us whether
         * the player is now beyond the
         * target relative to the flight
         * direction.
         */

        Vector3 targetToPlayer =
            transform.position -
            targetEntry.position;


        float passedTarget =
            Vector3.Dot(
                targetToPlayer,
                flightDirection
            );


        /*
         * Player has passed the target
         * without entering its aim radius.
         */

        if (passedTarget > 0f)
        {
            MissedTargetCannon();
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

        isFlying = false;

        targetCannon = null;


        Debug.Log(
            "BulletBoyPlayer: Missed target cannon."
        );


        CannonManager manager =
            FindObjectOfType<CannonManager>();


        if (manager != null)
        {
            manager.PlayerMissedCannon();
        }
        else
        {
            Debug.LogWarning(
                "BulletBoyPlayer: CannonManager not found."
            );
        }
    }


    // =========================================================
    // ENTER TARGET CANNON
    // =========================================================

    private void EnterTargetCannon()
    {
        if (targetCannon == null)
            return;


        BulletCannon cannon =
            targetCannon;


        targetCannon = null;

        hasMissedTarget = false;


        /*
         * Enter the new cannon.
         */

        cannon.EnterCannon(
            this
        );


        /*
         * Tell manager that the player
         * reached the next cannon.
         */

        CannonManager manager =
            FindObjectOfType<CannonManager>();


        if (manager != null)
        {
            manager.PlayerEnteredCannon(
                cannon
            );
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

        hasMissedTarget = false;
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


    public bool IsFlying()
    {
        return isFlying;
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