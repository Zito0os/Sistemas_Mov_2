using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public VirtualJoystick joystick;
    public float moveSpeed = 10f;
    public Transform cameraTransform;

    void Update()
    {
        Vector2 input = joystick.GetInput();
        Vector3 moveDirection = new Vector3(input.x, 0, input.y);

        if (moveDirection.magnitude > 0.1f)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 finalDirection = (forward * moveDirection.z + right * moveDirection.x).normalized;

            transform.Translate(finalDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
