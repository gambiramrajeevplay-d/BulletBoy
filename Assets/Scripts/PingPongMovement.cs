using UnityEngine;

public class PingPongMovement : MonoBehaviour
{
    public float distance = 5f;
    public float speed = 2f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float movement = Mathf.PingPong(Time.time * speed, distance);

        //To move obj from front to back 
        //transform.position = startPosition + transform.forward * movement;

        //To move obj from left to right
        //transform.position = startPosition + Vector3.right * movement;

        //To move obj from up to down
        transform.position = startPosition + Vector3.up * movement;
    }
}