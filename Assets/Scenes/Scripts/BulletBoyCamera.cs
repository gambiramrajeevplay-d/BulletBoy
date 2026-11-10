using UnityEngine;

public class BulletBoyCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Optional. Leave empty if the player is spawned at runtime. The camera finds the spawned BulletBoyPlayer and assigns itself as the target.")]
    [SerializeField] private Transform target;

    [Tooltip("How often (seconds) the camera searches for the player while it has none.")]
    [SerializeField] private float findPlayerInterval = 0.1f;

    [Tooltip("Place the camera at its normal position the moment the player is found, instead of flying in from its start position.")]
    [SerializeField] private bool snapToPlayerWhenFound = true;

    [Header("Normal Camera Position")]
    [SerializeField] private float distance = 12f;
    [SerializeField] private float height = 8f;
    [SerializeField] private float sideOffset = -8f;

    [Header("Normal Camera Smoothness")]
    [SerializeField] private float positionSmooth = 5f;

    // =========================================================
    // FINAL LAUNCH CAMERA
    // =========================================================

    [Header("FINAL LAUNCH CAMERA")]

    [Tooltip("Enable the cinematic camera when the final cannon launches.")]
    [SerializeField] private bool enableFinalLaunchCamera = true;

    [Tooltip("Distance from player during final launch.")]
    [SerializeField] private float finalDistance = 6f;

    [Tooltip("Height relative to player during final launch.")]
    [SerializeField] private float finalHeight = 3f;

    [Tooltip("Horizontal offset from player during final launch.")]
    [SerializeField] private float finalSideOffset = -3f;

    [Tooltip("How quickly the camera follows the player during final launch.")]
    [SerializeField] private float finalFollowSpeed = 8f;

    [Tooltip("How quickly the camera rotates toward the player.")]
    [SerializeField] private float finalLookSpeed = 7f;

    [Tooltip("Extra height of the point the camera looks at.")]
    [SerializeField] private float finalLookHeight = 0.5f;


    // =========================================================
    // FINAL FOV
    // =========================================================

    [Header("FINAL LAUNCH FOV")]

    [SerializeField] private bool changeFinalFOV = true;

    [SerializeField] private float normalFOV = 60f;

    [Tooltip("Lower value = stronger zoom.")]
    [SerializeField] private float finalFOV = 45f;

    [SerializeField] private float fovSmooth = 5f;


    // =========================================================
    // REFERENCES
    // =========================================================

    private BulletBoyPlayer player;
    private CannonManager cannonManager;

    private Camera cam;

    private bool finalLaunchStarted;
    private float nextFindTime;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        cam = GetComponent<Camera>();

        cannonManager =
            FindObjectOfType<CannonManager>();

        if (cam != null)
        {
            normalFOV = cam.fieldOfView;
        }

        // Use the Inspector target if one was assigned.
        // Otherwise try to find the spawned player right away.
        if (target != null)
        {
            player =
                target.GetComponent<BulletBoyPlayer>();
        }
        else
        {
            TryFindPlayer(false);
        }
    }


    // =========================================================
    // FIND / ASSIGN SPAWNED PLAYER
    // =========================================================

    /*
     * Searches the scene for the spawned BulletBoyPlayer and assigns it
     * as the camera target. Returns true when a target is available.
     */
    private bool TryFindPlayer(bool allowSnap)
    {
        BulletBoyPlayer found =
            FindObjectOfType<BulletBoyPlayer>();

        if (found == null)
            return false;

        AssignTarget(found.transform, allowSnap);

        return true;
    }

    private void AssignTarget(Transform newTarget, bool allowSnap)
    {
        if (newTarget == null)
            return;

        target = newTarget;

        player =
            target.GetComponent<BulletBoyPlayer>();

        finalLaunchStarted = false;

        if (allowSnap && snapToPlayerWhenFound)
        {
            transform.position = GetNormalDesiredPosition();
        }

        Debug.Log(
            "BulletBoyCamera: Target assigned -> " + target.name
        );
    }


    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        // -----------------------------------------------------
        // WAIT FOR SPAWNED PLAYER
        // -----------------------------------------------------

        // Covers: player not spawned yet, or the player was
        // destroyed and a new one was spawned (respawn).
        if (target == null)
        {
            if (Time.unscaledTime < nextFindTime)
                return;

            nextFindTime =
                Time.unscaledTime +
                Mathf.Max(0.02f, findPlayerInterval);

            if (!TryFindPlayer(true))
                return;
        }


        // -----------------------------------------------------
        // FIND PLAYER COMPONENT
        // -----------------------------------------------------

        if (player == null)
        {
            player =
                target.GetComponent<BulletBoyPlayer>();
        }


        // -----------------------------------------------------
        // FIND CANNON MANAGER
        // -----------------------------------------------------

        if (cannonManager == null)
        {
            cannonManager =
                FindObjectOfType<CannonManager>();
        }


        // -----------------------------------------------------
        // CHECK FINAL LAUNCH
        // -----------------------------------------------------

        if (!finalLaunchStarted &&
            enableFinalLaunchCamera &&
            player != null &&
            player.IsFlying() &&
            cannonManager != null &&
            cannonManager.IsLevelFinished())
        {
            StartFinalLaunchCamera();
        }


        // -----------------------------------------------------
        // NORMAL / FINAL CAMERA
        // -----------------------------------------------------

        if (!finalLaunchStarted)
        {
            UpdateNormalCamera();
        }
        else
        {
            UpdateFinalLaunchCamera();
        }


        // -----------------------------------------------------
        // FOV
        // -----------------------------------------------------

        UpdateFOV();
    }


    // =========================================================
    // NORMAL CAMERA
    // =========================================================

    private Vector3 GetNormalDesiredPosition()
    {
        return
            target.position
            + Vector3.right * sideOffset
            + Vector3.up * height
            - Vector3.forward * distance;
    }

    private void UpdateNormalCamera()
    {
        Vector3 desiredPosition =
            GetNormalDesiredPosition();


        float smooth =
            1f -
            Mathf.Exp(
                -Mathf.Max(
                    0.01f,
                    positionSmooth
                ) *
                Time.deltaTime
            );


        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                smooth
            );

        /*
         * Normal camera keeps its existing rotation.
         */
    }


    // =========================================================
    // START FINAL CAMERA
    // =========================================================

    private void StartFinalLaunchCamera()
    {
        finalLaunchStarted = true;

        /*
         * Do NOT instantly teleport the camera.
         *
         * The camera will smoothly move from its current
         * position into the final cinematic position.
         */

        Debug.Log(
            "BulletBoyCamera: FINAL LAUNCH CAMERA STARTED"
        );
    }


    // =========================================================
    // FINAL LAUNCH CAMERA
    // =========================================================

    private void UpdateFinalLaunchCamera()
    {
        if (target == null)
            return;


        /*
         * The player is moving through the world.
         *
         * We calculate the camera position every frame from
         * the CURRENT player position.
         *
         * Therefore the camera follows the player throughout
         * the entire final flight.
         */

        Vector3 desiredPosition =
            target.position
            + Vector3.right * finalSideOffset
            + Vector3.up * finalHeight
            - Vector3.forward * finalDistance;


        float positionLerp =
            1f -
            Mathf.Exp(
                -Mathf.Max(
                    0.01f,
                    finalFollowSpeed
                ) *
                Time.deltaTime
            );


        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                positionLerp
            );


        // -----------------------------------------------------
        // LOOK AT PLAYER
        // -----------------------------------------------------

        Vector3 lookTarget =
            target.position +
            Vector3.up * finalLookHeight;


        Vector3 direction =
            lookTarget -
            transform.position;


        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );


            float rotationLerp =
                1f -
                Mathf.Exp(
                    -Mathf.Max(
                        0.01f,
                        finalLookSpeed
                    ) *
                    Time.deltaTime
                );


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    rotationLerp
                );
        }
    }


    // =========================================================
    // FOV
    // =========================================================

    private void UpdateFOV()
    {
        if (cam == null)
            return;

        if (!changeFinalFOV)
            return;


        float desiredFOV =
            finalLaunchStarted
                ? finalFOV
                : normalFOV;


        float smooth =
            1f -
            Mathf.Exp(
                -Mathf.Max(
                    0.01f,
                    fovSmooth
                ) *
                Time.deltaTime
            );


        cam.fieldOfView =
            Mathf.Lerp(
                cam.fieldOfView,
                desiredFOV,
                smooth
            );
    }


    // =========================================================
    // SET TARGET
    // =========================================================

    // Optional: call this from your spawner right after Instantiate().
    // Example: Camera.main.GetComponent<BulletBoyCamera>().SetTarget(spawned.transform);
    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            target = null;
            player = null;
            return;
        }

        AssignTarget(newTarget, true);
    }


    // =========================================================
    // RESET CAMERA
    // =========================================================

    public void ResetCamera()
    {
        finalLaunchStarted = false;

        if (cam != null)
        {
            cam.fieldOfView =
                normalFOV;
        }
    }
}