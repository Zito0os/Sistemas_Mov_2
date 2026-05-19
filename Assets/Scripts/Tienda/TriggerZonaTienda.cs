using UnityEngine;

/// <summary>
/// TriggerZonaTienda — Abre la tienda y detiene al jugador al entrar al área del mostrador.
/// Al salir restaura el movimiento y limpia el estado de touch de la cámara.
/// La cámara la controla PlayerController — CameraController ya no existe.
/// </summary>
public class TriggerZonaTienda : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Si se deja vacío se busca automáticamente.")]
    [SerializeField] private SistemaCompras sistemaCompras;

    [Tooltip("Si se deja vacío se busca automáticamente.")]
    [SerializeField] private PlayerController playerController;

    [Tooltip("Joystick virtual a deshabilitar mientras la tienda está abierta. Opcional.")]
    [SerializeField] private VirtualJoystick virtualJoystick;

    [Header("Configuración")]
    [SerializeField] private string tagJugador = "Player";

    [Header("Debug")]
    [SerializeField] private bool logsActivos = true;

    // ── Ciclo de vida ──────────────────────────────────────────────────────────

    private void Start()
    {
        if (sistemaCompras == null)
            sistemaCompras = FindFirstObjectByType<SistemaCompras>();

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (virtualJoystick == null)
            virtualJoystick = FindFirstObjectByType<VirtualJoystick>();

        if (sistemaCompras == null)
            Debug.LogError("[TriggerZonaTienda] No se encontró SistemaCompras.");

        if (playerController == null)
            Debug.LogWarning("[TriggerZonaTienda] No se encontró PlayerController.");

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning("[TriggerZonaTienda] El Collider no está marcado como Trigger.");
    }

    // ── Trigger ────────────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(tagJugador)) return;

        if (logsActivos)
            Debug.Log("[TriggerZonaTienda] Jugador entró → abriendo tienda, bloqueando controles.");

        sistemaCompras?.AbrirTienda();
        BloquearControles(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(tagJugador)) return;

        if (logsActivos)
            Debug.Log("[TriggerZonaTienda] Jugador salió → cerrando tienda, restaurando controles.");

        sistemaCompras?.CerrarTienda();
        BloquearControles(false);
    }

    // ── Control de movimiento y cámara ────────────────────────────────────────

    private void BloquearControles(bool bloquear)
    {
        if (virtualJoystick != null)
            virtualJoystick.enabled = !bloquear;

        if (playerController == null) return;

        // Bloquear/desbloquear input sin deshabilitar el componente,
        // para que OnTriggerExit siga disparandose correctamente.
        playerController.Bloquear(bloquear);
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>
    /// Llama esto desde el botón Cerrar de la tienda si el jugador
    /// cierra manualmente sin salir del trigger.
    /// </summary>
    public void CerrarManual()
    {
        sistemaCompras?.CerrarTienda();
        BloquearControles(false);
    }
}