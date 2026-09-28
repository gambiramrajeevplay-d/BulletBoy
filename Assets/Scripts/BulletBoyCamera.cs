using UnityEngine;

public class BulletBoyCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera Position")]
    [SerializeField] private float distance = 12f;
    [SerializeField] private float height = 8f;
    [SerializeField] private float sideOffset = -8f;

    [Header("Smoothness")]
    [SerializeField] private float positionSmooth = 5f;

    private BulletBoyPlayer player;

    private void Start()
    {
        if (target != null)
        {
            player = target.GetComponent<BulletBoyPlayer>();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (player == null)
        {
            player = target.GetComponent<BulletBoyPlayer>();
        }

        /*
         * Camera follows the player:
         *
         * X = Side Offset
         * Y = Height
         * Z = Distance
         *
         * Camera rotation is NEVER changed.
         */

        Vector3 desiredPosition =
            target.position
            + Vector3.right * sideOffset
            + Vector3.up * height
            - Vector3.forward * distance;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            positionSmooth * Time.deltaTime
        );
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
        {
            player =
                target.GetComponent<BulletBoyPlayer>();
        }
    }
}