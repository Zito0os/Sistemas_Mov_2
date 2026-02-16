using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float sensitivity = 100f; // Sensibilidad de la cámara
    public RectTransform joystickArea; // Área del joystick para ignorar toques
    public Transform playerBody; // Referencia al jugador (para la rotación)

    private Vector2 lastTouchPosition;
    private int cameraTouchID = -1; // ID del toque que controla la cámara
    private float rotationX = 0f; // Control de inclinación de la cámara

    void Update()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            // Si el toque es nuevo y no es sobre el joystick, asignarlo a la cámara
            if (touch.phase == TouchPhase.Began && cameraTouchID == -1 &&
                !RectTransformUtility.RectangleContainsScreenPoint(joystickArea, touch.position))
            {
                cameraTouchID = touch.fingerId;
                lastTouchPosition = touch.position;
            }
            // Si este toque ya está controlando la cámara, mover la vista
            else if (touch.fingerId == cameraTouchID)
            {
                if (touch.phase == TouchPhase.Moved)
                {
                    Vector2 delta = touch.position - lastTouchPosition;

                    // Rotación horizontal (gira el jugador)
                    float mouseX = delta.x * sensitivity * Time.deltaTime;
                    playerBody.Rotate(Vector3.up * mouseX);

                    // Rotación vertical (inclina solo la cámara)
                    float mouseY = delta.y * sensitivity * Time.deltaTime;
                    rotationX -= mouseY;
                    rotationX = Mathf.Clamp(rotationX, -80f, 80f);

                    // Aplicar la rotación vertical a la cámara
                    transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

                    lastTouchPosition = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    cameraTouchID = -1; // Liberar el toque de la cámara
                }
            }
        }
    }
}


