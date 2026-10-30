using UnityEngine;

public class ShieldRotate : MonoBehaviour
{
    public GameObject wheelReference;
    public enum RotationAxis
    {
        X,
        Y,
        Z,
        NagetiveZ,
        NagetiveY
    }

    public RotationAxis rotationAxis = RotationAxis.Y;
    public float speed = 200f;

    void Update()
    {
        Vector3 rotation = Vector3.zero;

        switch (rotationAxis)
        {
            case RotationAxis.X:
                rotation = Vector3.right;
                break;

            case RotationAxis.Y:
                rotation = Vector3.up;
                break;

            case RotationAxis.Z:
                rotation = Vector3.forward;
                break;

            case RotationAxis.NagetiveZ:
                rotation = Vector3.back;
                break;
            case RotationAxis.NagetiveY:
                rotation = Vector3.down;
                break;
        }

        transform.Rotate(rotation * speed * Time.deltaTime);
    }
}
