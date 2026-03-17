using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public VirtualJoystick joystick;
    public float moveSpeed = 8f;
    public Transform cameraTransform;

    [Header("Configuración Cámara PC")]
    public float mouseSensitivity = 2f;

    [Header("Configuración Cámara Mobile")]
    public float touchSensitivity = 0.15f;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    private float yaw = 0f;
    private float pitch = 0f;

    // Touch: guarda qué finger ID está controlando la cámara
    private int cameraFingerId = -1;
    private Vector2 lastTouchPosition;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform != null)
            pitch = cameraTransform.localEulerAngles.x;

        yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        Transform cam = cameraTransform != null ? cameraTransform : Camera.main.transform;

        // --- CÁMARA ---
        float deltaX = 0f;
        float deltaY = 0f;

        // Mouse (PC / Unity Remote también lo usa)
        deltaX = Input.GetAxis("Mouse X") * mouseSensitivity;
        deltaY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Touch (Mobile) — solo lado derecho de la pantalla
        HandleTouchCamera(ref deltaX, ref deltaY);

        yaw += deltaX;
        pitch -= deltaY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // --- MOVIMIENTO ---
        Vector2 input = Vector2.zero;

        if (joystick != null)
            input = joystick.GetInput();

        if (input.magnitude < 0.1f)
        {
            input.x = Input.GetAxis("Horizontal");
            input.y = Input.GetAxis("Vertical");
        }

        if (input.magnitude > 0.1f)
        {
            Vector3 forward = cam.forward;
            Vector3 right = cam.right;

            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 finalDirection = (forward * input.y + right * input.x).normalized;
            transform.Translate(finalDirection * moveSpeed * Time.deltaTime, Space.World);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleTouchCamera(ref float deltaX, ref float deltaY)
    {
        float halfScreen = Screen.width * 0.5f;

        foreach (Touch touch in Input.touches)
        {
            // Solo nos interesa el lado derecho de la pantalla
            if (touch.phase == TouchPhase.Began)
            {
                // Si el finger empieza en el lado derecho y no hay otro finger de cámara
                if (touch.position.x > halfScreen && cameraFingerId == -1)
                {
                    cameraFingerId = touch.fingerId;
                    lastTouchPosition = touch.position;
                }
            }
            else if (touch.phase == TouchPhase.Moved && touch.fingerId == cameraFingerId)
            {
                Vector2 delta = touch.position - lastTouchPosition;
                deltaX = delta.x * touchSensitivity;
                deltaY = delta.y * touchSensitivity;
                lastTouchPosition = touch.position;
            }
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                     && touch.fingerId == cameraFingerId)
            {
                cameraFingerId = -1;
            }
        }
    }
}