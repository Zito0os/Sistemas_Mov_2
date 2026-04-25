using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

    // Set de fingerIds que están siendo usados por la UI (joystick u otros controles).
    // Se actualiza en cada Update y se respeta durante toda la vida del touch,
    // NO solo en TouchPhase.Began. Esto soluciona el bug de:
    //   "el dedo del joystick cruza al lado derecho y mueve la cámara"
    private HashSet<int> _fingersUsadosPorUI = new HashSet<int>();

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (joystick == null)
            joystick = FindFirstObjectByType<VirtualJoystick>();

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

        // Si hay toques activos (incluye Unity Remote), usar touch para cámara.
        // Si no hay toques, usar mouse.
        //if (Input.touchSupported && Input.touchCount > 0)
        if (Input.touchCount > 0)
        {
            HandleTouchCamera(ref deltaX, ref deltaY);
        }
        else
        {
            deltaX = Input.GetAxis("Mouse X") * mouseSensitivity;
            deltaY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        }

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

    public void AplicarRotacionForzada(float yawObjetivo, float pitchObjetivo)
    {
        yaw = yawObjetivo;
        pitch = Mathf.Clamp(pitchObjetivo, minPitch, maxPitch);

        Transform cam = cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cam != null)
            cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleTouchCamera(ref float deltaX, ref float deltaY)
    {
        float halfScreen = Screen.width * 0.5f;

        // PASO 1: actualizar el set de fingers que la UI está usando.
        // Esto se hace ANTES de procesar la cámara, en cada frame, para que
        // el veto persista durante toda la vida del touch (no solo en Began).
        ActualizarFingersUsadosPorUI();

        foreach (Touch touch in Input.touches)
        {
            // VETO PRINCIPAL: si la UI usa este finger, NUNCA es para cámara.
            // Aplica en TODAS las fases (Began, Moved, Ended). Esto soluciona
            // el bug de "el dedo del joystick cruza la mitad y mueve la cámara".
            if (_fingersUsadosPorUI.Contains(touch.fingerId))
            {
                // Si por alguna razón este dedo era el de la cámara, lo soltamos
                if (touch.fingerId == cameraFingerId)
                    cameraFingerId = -1;
                continue;
            }

            if (touch.phase == TouchPhase.Began)
            {
                // Solo nos interesa el lado derecho de la pantalla
                if (touch.position.x > halfScreen &&
                    cameraFingerId == -1 &&
                    !IsTouchOverJoystick(touch.position))
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

        if (Input.touchCount == 0)
        {
            cameraFingerId = -1;
            _fingersUsadosPorUI.Clear();
        }
    }

    /// <summary>
    /// Mantiene actualizado el set de fingerIds que están siendo usados por la UI
    /// (joystick, botones, paneles). Una vez que un dedo es marcado como UI,
    /// se queda así hasta que se levante. Esto es CRÍTICO porque sin esta
    /// persistencia, un dedo del joystick que cruce al lado derecho durante
    /// el drag empezaría a mover la cámara.
    /// </summary>
    void ActualizarFingersUsadosPorUI()
    {
        if (EventSystem.current == null) return;

        // 1. Limpiar fingers que ya no están activos en pantalla
        HashSet<int> activeFingerIds = new HashSet<int>();
        foreach (Touch t in Input.touches)
            activeFingerIds.Add(t.fingerId);

        _fingersUsadosPorUI.RemoveWhere(id => !activeFingerIds.Contains(id));

        // 2. Detectar y agregar fingers nuevos que la UI esté procesando
        foreach (Touch touch in Input.touches)
        {
            // En TouchPhase.Began es cuando el EventSystem decide quién procesa este touch.
            // Si la UI lo agarra, lo marcamos para que el resto de su vida quede vetado.
            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    _fingersUsadosPorUI.Add(touch.fingerId);
            }
        }
    }

    bool IsTouchOverJoystick(Vector2 screenPosition)
    {
        if (joystick == null || joystick.joystickBackground == null)
            return false;

        Canvas canvas = joystick.joystickBackground.GetComponentInParent<Canvas>();
        Camera eventCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(
            joystick.joystickBackground,
            screenPosition,
            eventCamera
        );
    }

    public void SincronizarRotacion()
    {
        yaw   = transform.eulerAngles.y;
        pitch = cameraTransform != null
            ? cameraTransform.localEulerAngles.x
            : 0f;

        // Convertir pitch de 0-360 a -180/180
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
}