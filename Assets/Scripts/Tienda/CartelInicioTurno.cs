using UnityEngine;
using UnityEngine.UI;

public class CartelInicioTurno : MonoBehaviour
{
    [Header("Modelos del cartel")]
    [Tooltip("GameObject del cartel CERRADO — activo al inicio del día.")]
    [SerializeField] private GameObject modeloCerrado;

    [Tooltip("GameObject del cartel ABIERTO — se activa al confirmar el turno.")]
    [SerializeField] private GameObject modeloAbierto;

    [Header("Panel de confirmación")]
    [SerializeField] private GameObject panelConfirmacion;
    [SerializeField] private Button botonIniciar;
    [SerializeField] private Button botonCancelar;

    [Header("Debug")]
    [SerializeField] private bool logsActivos = true;

    private bool _turnoIniciado = false;

    // ── Ciclo de vida ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (panelConfirmacion != null)
            panelConfirmacion.SetActive(false);

        if (botonIniciar != null)
            botonIniciar.onClick.AddListener(ConfirmarInicioTurno);

        if (botonCancelar != null)
            botonCancelar.onClick.AddListener(CerrarPanel);
    }

    private void OnDestroy()
    {
        if (botonIniciar != null)
            botonIniciar.onClick.RemoveListener(ConfirmarInicioTurno);

        if (botonCancelar != null)
            botonCancelar.onClick.RemoveListener(CerrarPanel);
    }

    private void OnEnable()  => GameManager.OnStateChanged += AlCambiarEstado;
    private void OnDisable() => GameManager.OnStateChanged -= AlCambiarEstado;

    private void Start()
    {
        MostrarModelo(abierto: false);

        // FIX: igual que SistemaCompras — el GameManager es DontDestroyOnLoad.
        // Si la escena cargó DESPUÉS de que el evento StartDay ya se disparó,
        // este objeto nunca lo recibió. Leer el estado actual al nacer compensa eso.
        if (GameManager.Instance != null)
            AlCambiarEstado(GameManager.Instance.CurrentState);
    }

    // ── API pública — Gestos.cs llama esto al tocar el tag "cartelTurno" ──────

    public void InteractuarConCartel()
    {
        if (_turnoIniciado)
        {
            if (logsActivos)
                Debug.Log("[CartelInicioTurno] Turno ya activo, interacción ignorada.");
            return;
        }

        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameManager.GameState.StartDay)
        {
            if (logsActivos)
                Debug.Log("[CartelInicioTurno] Solo disponible en StartDay.");
            return;
        }

        if (logsActivos)
            Debug.Log("[CartelInicioTurno] Cartel tocado → abriendo panel.");

        AbrirPanel();
    }

    // ── Panel ──────────────────────────────────────────────────────────────────

    private void AbrirPanel()
    {
        if (panelConfirmacion != null)
            panelConfirmacion.SetActive(true);
    }

    private void CerrarPanel()
    {
        if (panelConfirmacion != null)
            panelConfirmacion.SetActive(false);

        if (logsActivos)
            Debug.Log("[CartelInicioTurno] Panel cerrado.");
    }

    private void ConfirmarInicioTurno()
    {
        CerrarPanel();
        _turnoIniciado = true;
        MostrarModelo(abierto: true);

        if (logsActivos)
            Debug.Log("[CartelInicioTurno] Turno confirmado → Playing.");

        GameManager.Instance?.ChangeState(GameManager.GameState.Playing);
    }

    // ── Swap de modelos ────────────────────────────────────────────────────────

    private void MostrarModelo(bool abierto)
    {
        if (modeloCerrado != null) modeloCerrado.SetActive(!abierto);
        if (modeloAbierto != null) modeloAbierto.SetActive(abierto);
    }

    // ── Eventos ────────────────────────────────────────────────────────────────

    private void AlCambiarEstado(GameManager.GameState nuevoEstado)
    {
        if (nuevoEstado == GameManager.GameState.StartDay)
        {
            _turnoIniciado = false;
            MostrarModelo(abierto: false);
            CerrarPanel();

            if (logsActivos)
                Debug.Log("[CartelInicioTurno] Nuevo día → cartel cerrado.");
        }

        if (nuevoEstado != GameManager.GameState.StartDay)
            CerrarPanel();
    }
}