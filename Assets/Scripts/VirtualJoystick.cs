using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// VirtualJoystick — Versión "a prueba de bombas".
///
/// NO depende de IDragHandler ni de los eventos del EventSystem.
/// Lee Input.touches y Input.mousePosition directamente en Update().
/// Por eso funciona aunque el Drag Threshold esté mal configurado o haya
/// otro componente robando los eventos de drag.
///
/// Solo usa IPointerDownHandler para "saber qué dedo lo activó". A partir
/// de ahí trackea ese dedo manualmente cada frame.
///
/// Setup:
///   - El GameObject que tiene este script debe tener un Image con
///     Raycast Target activado (para que IPointerDown se dispare).
///   - joystickBackground = el RectTransform del fondo (define el radio).
///   - joystickHandle = el RectTransform de la palanquita visual.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referencias")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;

    [Header("Debug")]
    [SerializeField] private bool logsActivos = false;
    [SerializeField] private Vector2 inputVector = Vector2.zero;

    // Tracking del dedo activo
    private bool _activo = false;
    private int _fingerId = -999;     // -999 = nada / -1 = mouse
    private Camera _eventCamera;       // cámara del Canvas (null si Overlay)

    public Vector2 GetInput() => inputVector;

    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            _eventCamera = canvas.worldCamera;

        if (logsActivos)
            Debug.Log($"[VirtualJoystick] Listo. Canvas: {(canvas != null ? canvas.name : "NULL")} | EventCamera: {(_eventCamera != null ? _eventCamera.name : "Overlay")}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // El EventSystem nos dice qué dedo activó al joystick.
        // A partir de aquí lo trackeamos nosotros directamente desde Input.
        _activo = true;
        _fingerId = eventData.pointerId;  // -1 para mouse, 0+ para touches

        if (logsActivos)
            Debug.Log($"[VirtualJoystick] Activado por id: {_fingerId}");

        // Aplicar la posición inicial inmediatamente
        ActualizarConPosicion(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // El EventSystem también puede avisar que se soltó. Pero por seguridad
        // también detectamos en Update si el dedo ya no existe.
        if (eventData.pointerId == _fingerId)
            Soltar();
    }

    private void Update()
    {
        if (!_activo) return;

        // Mouse (fingerId -1 en el sistema de Unity)
        if (_fingerId == -1)
        {
            if (Input.GetMouseButton(0))
            {
                ActualizarConPosicion(Input.mousePosition);
            }
            else
            {
                Soltar();
            }
            return;
        }

        // Touch
        bool encontrado = false;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.fingerId != _fingerId) continue;

            encontrado = true;

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                Soltar();
                return;
            }

            ActualizarConPosicion(t.position);
            break;
        }

        // Si el dedo ya no aparece en Input.touches, soltar
        if (!encontrado)
            Soltar();
    }

    private void ActualizarConPosicion(Vector2 screenPosition)
    {
        if (joystickBackground == null) return;

        // Convertir pantalla → coordenadas locales del background
        Vector2 localPoint;
        bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            screenPosition,
            _eventCamera,
            out localPoint
        );

        if (!ok) return;

        float radius = joystickBackground.sizeDelta.x * 0.5f;
        if (radius <= 0f) return;

        Vector2 normalizado = localPoint / radius;
        inputVector = Vector2.ClampMagnitude(normalizado, 1.0f);

        if (joystickHandle != null)
            joystickHandle.anchoredPosition = inputVector * radius;
    }

    private void Soltar()
    {
        _activo = false;
        _fingerId = -999;
        inputVector = Vector2.zero;

        if (joystickHandle != null)
            joystickHandle.anchoredPosition = Vector2.zero;

        if (logsActivos)
            Debug.Log("[VirtualJoystick] Soltado.");
    }
}