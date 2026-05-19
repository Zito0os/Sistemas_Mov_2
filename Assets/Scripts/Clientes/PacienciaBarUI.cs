using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class PacienciaBarUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Image de relleno con Image Type = Filled, Fill Method = Horizontal")]
    public Image imagenRelleno;

    [Tooltip("Colores de la barra según el tiempo restante")]
    public Color colorLlena   = Color.green;
    public Color colorMitad   = Color.yellow;
    public Color colorUrgente = Color.red;

    [Tooltip("Umbral (0-1) a partir del cual la barra se vuelve roja")]
    public float umbralUrgente = 0.3f;

    [Tooltip("Umbral (0-1) a partir del cual la barra se vuelve amarilla")]
    public float umbralMitad   = 0.6f;

    [Header("Billboard")]
    [Tooltip("Si está activo, la barra siempre mira a la cámara principal")]
    public bool mirarCamara = true;

    // ── ESTADO INTERNO ────────────────────────────────────────────────────────

    private ClienteIA _clienteIA;
    private Camera    _camPrincipal;

    // ── CICLO ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _clienteIA    = GetComponentInParent<ClienteIA>();
        _camPrincipal = Camera.main;

        if (_clienteIA == null)
            Debug.LogWarning("[PacienciaBarUI] No se encontró ClienteIA en el padre.");

        // SUSCRIPCIÓN EN AWAKE — no en OnEnable/OnDisable.
        // Este Canvas empieza desactivado (SetActive false), entonces OnEnable
        // nunca se dispara y el evento nunca se suscribe. Awake sí se llama
        // cuando el prefab del cliente se instancia, aunque el Canvas esté inactivo.
        ClienteIA.alActualizarPaciencia += AlActualizarPaciencia;
        ClienteIA.alIrseCliente         += AlIrseCliente;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        ClienteIA.alActualizarPaciencia -= AlActualizarPaciencia;
        ClienteIA.alIrseCliente         -= AlIrseCliente;
    }

    private void LateUpdate()
    {
        if (!mirarCamara) return;
        if (_camPrincipal == null)
        {
            _camPrincipal = Camera.main;
            return;
        }
        Vector3 dir = transform.position - _camPrincipal.transform.position;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    // ── CALLBACKS ─────────────────────────────────────────────────────────────

    private void AlActualizarPaciencia(ClienteIA cliente, float proporcion)
    {
        if (cliente != _clienteIA) return;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        ActualizarVisual(proporcion);
    }

    private void AlIrseCliente(ClienteIA cliente, bool pago)
    {
        if (cliente != _clienteIA) return;
        gameObject.SetActive(false);
    }

    // ── VISUAL ────────────────────────────────────────────────────────────────

    private void ActualizarVisual(float proporcion)
    {
        if (imagenRelleno == null) return;

        imagenRelleno.fillAmount = proporcion;

        if (proporcion <= umbralUrgente)
            imagenRelleno.color = colorUrgente;
        else if (proporcion <= umbralMitad)
            imagenRelleno.color = Color.Lerp(colorUrgente, colorMitad,
                (proporcion - umbralUrgente) / (umbralMitad - umbralUrgente));
        else
            imagenRelleno.color = Color.Lerp(colorMitad, colorLlena,
                (proporcion - umbralMitad) / (1f - umbralMitad));
    }
}