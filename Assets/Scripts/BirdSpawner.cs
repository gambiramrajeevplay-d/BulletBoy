using System.Collections;
using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    // =========================================================
    // BIRD
    // =========================================================

    [Header("Bird")]
    [SerializeField] private GameObject birdPrefab;


    // =========================================================
    // SPAWN SETTINGS
    // =========================================================

    [Header("Spawn Settings")]

    [SerializeField] private int birdCount = 5;

    [Tooltip("Time between each bird spawn.")]
    [SerializeField] private float spawnInterval = 3f;


    // =========================================================
    // SPAWN AREA
    // =========================================================

    [Header("Spawn Area")]

    [Tooltip(
        "Distance from the spawner's center on the +X side " +
        "where birds will spawn."
    )]
    [SerializeField] private float spawnWidth = 30f;

    [SerializeField] private float minHeight = 5f;

    [SerializeField] private float maxHeight = 12f;

    [SerializeField] private float spawnDepth = 10f;


    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]

    [Tooltip(
        "Birds are forced to travel along -X. " +
        "Y movement is handled separately."
    )]
    [SerializeField] private Vector3 flightDirection = Vector3.left;


    // =========================================================
    // VERTICAL MOVEMENT
    // =========================================================

    [Header("Bird Vertical Movement")]

    [SerializeField] private float verticalMovementAmount = 4.5f;

    [SerializeField] private float verticalMovementSpeed = 1.4f;


    // =========================================================
    // RANDOMIZATION
    // =========================================================

    [Header("Randomization")]

    [SerializeField] private bool randomScale = true;

    [SerializeField] private float minScale = 0.8f;

    [SerializeField] private float maxScale = 1.2f;

    [SerializeField] private bool randomVerticalOffset = true;


    // =========================================================
    // CANNON DETECTION
    // =========================================================

    [Header("Cannon Detection")]

    [SerializeField] private bool detectCannons = true;

    [Tooltip("How far ahead the bird checks for cannons.")]
    [SerializeField] private float cannonDetectionDistance = 6f;

    [Tooltip("Width of the cannon detection sphere.")]
    [SerializeField] private float cannonDetectionRadius = 1f;

    [Tooltip("Layers containing cannons.")]
    [SerializeField] private LayerMask cannonDetectionLayers = ~0;


    // =========================================================
    // OBSTACLE DETECTION
    // =========================================================

    [Header("Obstacle Detection")]

    [SerializeField] private bool detectObstacles = true;

    [Tooltip("How far ahead the bird checks for obstacles.")]
    [SerializeField] private float obstacleDetectionDistance = 5f;

    [Tooltip("Width of the obstacle detection sphere.")]
    [SerializeField] private float obstacleDetectionRadius = 0.9f;

    [Tooltip(
        "Layers containing objects the birds should avoid."
    )]
    [SerializeField] private LayerMask obstacleDetectionLayers = ~0;


    // =========================================================
    // AVOIDANCE
    // =========================================================

    [Header("Bird Avoidance")]

    [Tooltip(
        "How far above/below its normal flight path " +
        "the bird moves to avoid an obstacle."
    )]
    [SerializeField] private float avoidanceHeight = 3.5f;

    [Tooltip("Speed used while moving up/down to avoid.")]
    [SerializeField] private float avoidanceSpeed = 6f;

    [Tooltip("Speed used when returning to normal flight.")]
    [SerializeField] private float returnSpeed = 3.5f;

    [Tooltip(
        "How long the bird stays in avoidance mode after detecting " +
        "an obstacle."
    )]
    [SerializeField] private float avoidanceDuration = 1.2f;

    [Tooltip(
        "Small delay before another obstacle can trigger avoidance."
    )]
    [SerializeField] private float avoidanceCooldown = 0.2f;


    // =========================================================
    // BIRD LIFETIME
    // =========================================================

    [Header("Bird Lifetime")]

    [Tooltip(
        "Extra distance after the left-side spawn boundary " +
        "before the bird is destroyed."
    )]
    [SerializeField] private float destroyDistance = 10f;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField] private bool showGizmos = true;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        StartCoroutine(SpawnBirds());
    }


    // =========================================================
    // SPAWN BIRDS
    // =========================================================

    private IEnumerator SpawnBirds()
    {
        for (int i = 0; i < birdCount; i++)
        {
            SpawnBird();

            yield return new WaitForSeconds(
                Mathf.Max(0f, spawnInterval)
            );
        }
    }


    // =========================================================
    // SPAWN SINGLE BIRD
    // =========================================================

    private void SpawnBird()
    {
        if (birdPrefab == null)
        {
            Debug.LogWarning(
                "BirdSpawner: Bird Prefab is not assigned."
            );

            return;
        }


        // =====================================================
        // SPAWN POSITION
        // =====================================================

        Vector3 spawnPosition =
            transform.position;

        /*
         * IMPORTANT:
         *
         * Birds ALWAYS spawn on the +X side.
         *
         * There is NO random X anymore.
         *
         * Example:
         *
         * Spawner X = 0
         * Spawn Width = 30
         *
         * Bird spawns at:
         *
         * X = +30
         */
        spawnPosition.x +=
            Mathf.Abs(spawnWidth);

        /*
         * Random height.
         */
        spawnPosition.y +=
            Random.Range(
                minHeight,
                maxHeight
            );

        /*
         * Random Z only.
         *
         * This prevents every bird from appearing
         * directly on top of another bird.
         */
        spawnPosition.z +=
            Random.Range(
                -spawnDepth,
                spawnDepth
            );


        // =====================================================
        // CREATE BIRD
        // =====================================================

        GameObject bird =
            Instantiate(
                birdPrefab,
                spawnPosition,
                Quaternion.identity
            );


        // =====================================================
        // RANDOM SCALE
        // =====================================================

        if (randomScale)
        {
            float scale =
                Random.Range(
                    minScale,
                    maxScale
                );

            bird.transform.localScale *= scale;
        }


        // =====================================================
        // GET CONTROLLER
        // =====================================================

        BirdController controller =
            bird.GetComponent<BirdController>();

        if (controller == null)
        {
            Debug.LogWarning(
                "BirdSpawner: Bird prefab does not have " +
                "a BirdController component."
            );

            return;
        }


        // =====================================================
        // FORCE -X MOVEMENT
        // =====================================================

        Vector3 direction =
            flightDirection;

        /*
         * The bird must travel from +X to -X.
         *
         * Force X to negative.
         */
        direction.x =
            -Mathf.Abs(direction.x);

        /*
         * Vertical movement is controlled separately
         * by BirdController.
         */
        direction.y = 0f;

        /*
         * Keep the bird travelling on the X axis.
         */
        direction.z = 0f;

        /*
         * Safety fallback.
         */
        if (direction.sqrMagnitude < 0.001f)
        {
            direction =
                Vector3.left;
        }

        direction.Normalize();

        controller.SetDirection(
            direction
        );


        // =====================================================
        // VERTICAL MOVEMENT
        // =====================================================

        controller.SetVerticalMovement(
            verticalMovementAmount,
            verticalMovementSpeed
        );


        // =====================================================
        // CANNON DETECTION
        // =====================================================

        controller.SetCannonDetection(
            detectCannons,
            cannonDetectionDistance,
            cannonDetectionRadius,
            cannonDetectionLayers
        );


        // =====================================================
        // OBSTACLE DETECTION
        // =====================================================

        controller.SetObstacleDetection(
            detectObstacles,
            obstacleDetectionDistance,
            obstacleDetectionRadius,
            obstacleDetectionLayers
        );


        // =====================================================
        // AVOIDANCE
        // =====================================================

        controller.SetAvoidanceSettings(
            avoidanceHeight,
            avoidanceSpeed,
            returnSpeed,
            avoidanceDuration,
            avoidanceCooldown
        );


        // =====================================================
        // RANDOM VERTICAL PHASE
        // =====================================================

        if (randomVerticalOffset)
        {
            controller.RandomizeVerticalPhase();
        }


        // =====================================================
        // DESTROY SETTINGS
        // =====================================================

        controller.SetDestroySettings(
            transform,
            spawnWidth,
            spawnDepth,
            destroyDistance
        );
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;


        // =====================================================
        // SPAWN POINT
        // =====================================================

        Gizmos.color =
            Color.green;

        Vector3 spawnPoint =
            transform.position;

        spawnPoint.x +=
            Mathf.Abs(spawnWidth);

        Gizmos.DrawWireSphere(
            spawnPoint,
            0.5f
        );


        // =====================================================
        // SPAWN HEIGHT AREA
        // =====================================================

        Gizmos.color =
            Color.yellow;

        Vector3 center =
            transform.position;

        center.x +=
            Mathf.Abs(spawnWidth);

        center.y +=
            (minHeight + maxHeight) * 0.5f;


        Vector3 size =
            new Vector3(
                0.1f,
                maxHeight - minHeight,
                spawnDepth * 2f
            );

        Gizmos.DrawWireCube(
            center,
            size
        );


        // =====================================================
        // DESTROY POINT
        // =====================================================

        Gizmos.color =
            Color.red;

        Vector3 destroyPoint =
            transform.position;

        destroyPoint.x -=
            Mathf.Abs(spawnWidth) +
            destroyDistance;

        Gizmos.DrawWireSphere(
            destroyPoint,
            0.5f
        );


        // =====================================================
        // FLIGHT DIRECTION
        // =====================================================

        Gizmos.color =
            Color.cyan;

        Vector3 direction =
            Vector3.left;

        Gizmos.DrawRay(
            spawnPoint,
            direction * 5f
        );
    }
}