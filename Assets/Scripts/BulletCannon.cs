
using System.Collections;
using UnityEngine;

public class BulletCannon : MonoBehaviour
{
    [Header("Cannon Settings")]
    [Tooltip("Enable for a continuously rotating cannon.")]
    [SerializeField] private bool rotateCannon = false;

    [Tooltip("Enable for an up/down moving cannon.")]
    [SerializeField] private bool moveUpDown = false;

    [Tooltip("Enable for a left/right moving cannon.")]
    [SerializeField] private bool moveLeftRight = false;

    [Tooltip("Enable only on the final cannon.")]
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
    [Tooltip("Vertical movement speed.")]
    [SerializeField] private float moveSpeed = 2f;

    [Tooltip("Minimum local Y position relative to starting position.")]
    [SerializeField] private float minY = -2f;

    [Tooltip("Maximum local Y position relative to starting position.")]
    [SerializeField] private float maxY = 2f;

    [Tooltip("Start by moving upward.")]
    [SerializeField] private bool startMovingUp = true;


    [Header("Left / Right Movement")]
    [Tooltip("Horizontal movement speed.")]
    [SerializeField] private float horizontalMoveSpeed = 2f;

    [Tooltip("Minimum local X position relative to starting position.")]
    [SerializeField] private float minX = -2f;

    [Tooltip("Maximum local X position relative to starting position.")]
    [SerializeField] private float maxX = 2f;

    [Tooltip("Start by moving toward positive X / right.")]
    [SerializeField] private bool startMovingRight = true;


    [Header("Final Cannon")]
    [SerializeField] private float finalLaunchSpeed = 45f;


    [Header("Target Detection")]
    [Tooltip("Distance required for the player to enter the next cannon.")]
    [SerializeField] private float aimRadius = 1.5f;


    [Header("Cannon Visual")]
    [Tooltip("Assign the visual/mesh child of the cannon here.")]
    [SerializeField] private Transform cannonVisual;


    [Header("Entry Effect")]
    [Tooltip("Recoil distance when the player enters.")]
    [SerializeField] private float entryRecoilDistance = 0.25f;

    [Tooltip("Squash amount when the player enters.")]
    [SerializeField] private float entrySquashAmount = 0.78f;

    [Tooltip("Stretch amount when the player enters.")]
    [SerializeField] private float entryStretchAmount = 1.08f;

    [Tooltip("Duration of the entry squash.")]
    [SerializeField] private float entrySquashDuration = 0.045f;

    [Tooltip("Duration of the entry stretch.")]
    [SerializeField] private float entryStretchDuration = 0.08f;

    [Tooltip("Duration of the entry return.")]
    [SerializeField] private float entryReturnDuration = 0.10f;


    [Header("Launch Effect")]
    [Tooltip("How far the cannon visually moves backward when firing.")]
    [SerializeField] private float recoilDistance = 0.65f;

    [Tooltip("How quickly the cannon squashes.")]
    [SerializeField] private float squashDuration = 0.055f;

    [Tooltip("How quickly the cannon stretches.")]
    [SerializeField] private float stretchDuration = 0.10f;

    [Tooltip("How quickly the cannon returns to normal.")]
    [SerializeField] private float returnDuration = 0.12f;

    [Tooltip("Scale on the cannon's forward axis during squash.")]
    [SerializeField] private float squashAmount = 0.65f;

    [Tooltip("Scale on the cannon's forward axis during stretch.")]
    [SerializeField] private float stretchAmount = 1.18f;

    [Tooltip("Extra backward movement during the initial recoil.")]
    [SerializeField] private float recoilSnap = 1.15f;


    // =========================================================
    // LAUNCH PARTICLE EFFECT
    // =========================================================

    [Header("Launch Particle Effect")]
    [Tooltip("Particle effect prefab spawned inside the cannon when the player is launched.")]
    [SerializeField] private ParticleSystem launchParticleEffect;

    [Tooltip("Local X position offset from the entry point. Negative values move the effect backward on X.")]
    [SerializeField] private float particleXOffset = -0.35f;

    [Tooltip("Local Y position offset from the entry point.")]
    [SerializeField] private float particleYOffset = 0f;

    [Tooltip("Local Z position offset from the entry point.")]
    [SerializeField] private float particleZOffset = 0f;

    [Tooltip("Destroy the spawned particle object after this many seconds.")]
    [SerializeField] private float particleLifetime = 2f;


    // =========================================================
    // PRIVATE VARIABLES
    // =========================================================

    private BulletBoyPlayer player;

    private float currentX;
    private float lockedY;
    private float lockedZ;

    // Up / Down
    private float startingY;
    private bool movingUp;

    // Left / Right
    private float startingX;
    private bool movingRight;

    // Visual effect
    private Vector3 originalVisualPosition;
    private Vector3 originalVisualScale;

    private Coroutine effectCoroutine;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        Vector3 startingRotation =
            transform.localEulerAngles;


        currentX = startingRotation.x;
        lockedY = startingRotation.y;
        lockedZ = startingRotation.z;


        // -----------------------------------------------------
        // UP / DOWN
        // -----------------------------------------------------

        startingY =
            transform.localPosition.y;

        movingUp =
            startMovingUp;


        // -----------------------------------------------------
        // LEFT / RIGHT
        // -----------------------------------------------------

        startingX =
            transform.localPosition.x;

        movingRight =
            startMovingRight;


        // -----------------------------------------------------
        // CANNON VISUAL
        // -----------------------------------------------------

        // If no visual is assigned,
        // use the cannon itself.
        if (cannonVisual == null)
        {
            cannonVisual =
                transform;
        }


        originalVisualPosition =
            cannonVisual.localPosition;

        originalVisualScale =
            cannonVisual.localScale;
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        ApplyRotation();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        UpdateRotation();

        UpdateVerticalMovement();

        UpdateHorizontalMovement();


        if (player != null &&
            player.IsInsideCannon())
        {
            player.SyncWithCannon(
                entryPoint,
                GetCannonRotation()
            );
        }
    }


    // =========================================================
    // ROTATION
    // =========================================================

    private void UpdateRotation()
    {
        if (rotateCannon)
        {
            RotateContinuously();
        }
    }


    private void RotateContinuously()
    {
        currentX +=
            rotateSpeed *
            Time.deltaTime;


        if (currentX >= 360f)
        {
            currentX -= 360f;
        }


        ApplyRotation();
    }


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
        {
            return;
        }


        float direction =
            movingUp ? 1f : -1f;


        float newY =
            transform.localPosition.y +
            direction *
            moveSpeed *
            Time.deltaTime;


        // Reached top.
        if (newY >= startingY + maxY)
        {
            newY =
                startingY + maxY;

            movingUp =
                false;
        }


        // Reached bottom.
        if (newY <= startingY + minY)
        {
            newY =
                startingY + minY;

            movingUp =
                true;
        }


        Vector3 position =
            transform.localPosition;


        position.y =
            newY;


        transform.localPosition =
            position;
    }


    // =========================================================
    // LEFT / RIGHT MOVEMENT
    // =========================================================

    private void UpdateHorizontalMovement()
    {
        if (!moveLeftRight)
        {
            return;
        }


        float direction =
            movingRight ? 1f : -1f;


        float newX =
            transform.localPosition.x +
            direction *
            horizontalMoveSpeed *
            Time.deltaTime;


        // -----------------------------------------------------
        // Reached right limit.
        // -----------------------------------------------------

        if (newX >= startingX + maxX)
        {
            newX =
                startingX + maxX;

            movingRight =
                false;
        }


        // -----------------------------------------------------
        // Reached left limit.
        // -----------------------------------------------------

        if (newX <= startingX + minX)
        {
            newX =
                startingX + minX;

            movingRight =
                true;
        }


        Vector3 position =
            transform.localPosition;


        position.x =
            newX;


        transform.localPosition =
            position;
    }


    // =========================================================
    // PLAYER ENTERS CANNON
    // =========================================================

    public void EnterCannon(
        BulletBoyPlayer bulletPlayer)
    {
        if (bulletPlayer == null)
        {
            return;
        }


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


        player =
            bulletPlayer;


        player.EnterCannon(
            entryPoint,
            exitPoint,
            GetCannonRotation()
        );


        // =====================================================
        // PLAYER ENTER EFFECT
        // =====================================================

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


        float speed =
            launchSpeed;


        if (isFinalCannon)
        {
            speed =
                finalLaunchSpeed;
        }


        Vector3 launchDirection =
            exitPoint.forward;


        Quaternion launchRotation =
            GetCannonRotation();


        // =====================================================
        // SPAWN LAUNCH PARTICLE
        // =====================================================

        SpawnLaunchParticle();


        // =====================================================
        // PLAYER LAUNCH EFFECT
        // =====================================================

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
    // SPAWN LAUNCH PARTICLE
    // =========================================================

    private void SpawnLaunchParticle()
    {
        if (launchParticleEffect == null)
        {
            return;
        }


        if (entryPoint == null)
        {
            return;
        }


        // -----------------------------------------------------
        // Position relative to Entry Point.
        //
        // Negative X = backward
        // Positive X = forward
        // -----------------------------------------------------

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


        Quaternion spawnRotation =
            entryPoint.rotation;


        ParticleSystem spawnedParticle =
            Instantiate(
                launchParticleEffect,
                spawnPosition,
                spawnRotation
            );


        // Make particle follow cannon.
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
    // LAUNCH EFFECT
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
        {
            return;
        }


        if (effectCoroutine != null)
        {
            StopCoroutine(
                effectCoroutine
            );
        }


        // Reset before starting another effect.
        cannonVisual.localPosition =
            originalVisualPosition;

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


        // -----------------------------------------------------
        // 1. SQUASH + RECOIL
        // -----------------------------------------------------

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


        float elapsed =
            0f;


        while (elapsed < squashTime)
        {
            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    squashTime
                );


            // Fast punch into squash.
            float smoothT =
                1f -
                Mathf.Pow(
                    1f - t,
                    4f
                );


            cannonVisual.localPosition =
                Vector3.Lerp(
                    normalPosition,
                    recoilPosition,
                    smoothT
                );


            cannonVisual.localScale =
                Vector3.Lerp(
                    normalScale,
                    squashScale,
                    smoothT
                );


            yield return null;
        }


        cannonVisual.localPosition =
            recoilPosition;

        cannonVisual.localScale =
            squashScale;


        // -----------------------------------------------------
        // 2. STRETCH FORWARD
        // -----------------------------------------------------

        Vector3 stretchScale =
            normalScale;


        stretchScale.z =
            normalScale.z *
            stretch;


        elapsed =
            0f;


        while (elapsed < stretchTime)
        {
            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    stretchTime
                );


            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            cannonVisual.localPosition =
                Vector3.Lerp(
                    recoilPosition,
                    normalPosition,
                    smoothT
                );


            cannonVisual.localScale =
                Vector3.Lerp(
                    squashScale,
                    stretchScale,
                    smoothT
                );


            yield return null;
        }


        cannonVisual.localPosition =
            normalPosition;

        cannonVisual.localScale =
            stretchScale;


        // -----------------------------------------------------
        // 3. RETURN TO NORMAL
        // -----------------------------------------------------

        elapsed =
            0f;


        while (elapsed < returnTime)
        {
            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    returnTime
                );


            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            cannonVisual.localPosition =
                Vector3.Lerp(
                    normalPosition,
                    originalVisualPosition,
                    smoothT
                );


            cannonVisual.localScale =
                Vector3.Lerp(
                    stretchScale,
                    originalVisualScale,
                    smoothT
                );


            yield return null;
        }


        // Final reset.
        cannonVisual.localPosition =
            originalVisualPosition;

        cannonVisual.localScale =
            originalVisualScale;


        effectCoroutine =
            null;
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


    public Quaternion GetCannonRotation()
    {
        return transform.rotation;
    }


    public float GetLaunchSpeed()
    {
        if (isFinalCannon)
        {
            return finalLaunchSpeed;
        }

        return launchSpeed;
    }


    public float GetAimRadius()
    {
        return aimRadius;
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
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        if (entryPoint != null)
        {
            Gizmos.color =
                Color.green;

            Gizmos.DrawSphere(
                entryPoint.position,
                0.15f
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
