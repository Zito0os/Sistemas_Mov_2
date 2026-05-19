using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Paneles de fase")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject startDayPanel;
    [SerializeField] private GameObject resultadosPanel;
    [SerializeField] private GameObject cuotaPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Pausa (overlay, independiente de la fase)")]
    [SerializeField] private GameObject pausePanel;
    
    [Header("Paneles toggle")]
    [SerializeField] private GameObject ordenesPanel;

    private bool pausaActiva = false;

    // ── Ciclo de vida ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => GameManager.OnStateChanged += AlCambiarEstado;
    private void OnDisable() => GameManager.OnStateChanged -= AlCambiarEstado;

    private void Start()
    {
        OcultarTodo();
        if (GameManager.Instance != null)
            AlCambiarEstado(GameManager.Instance.CurrentState);
    }

    // ── Lógica principal ───────────────────────────────────────────────────────

    private void AlCambiarEstado(GameManager.GameState estado)
    {
        // Cierra la pausa si estaba abierta al cambiar de fase
        if (pausaActiva) CerrarPausa();

        OcultarTodo();

        switch (estado)
        {
            case GameManager.GameState.MainMenu:
                // La escena del menú maneja su propia UI; aquí no se activa nada.
                break;

            case GameManager.GameState.StartDay:
                Mostrar(hudPanel);
                Mostrar(startDayPanel);
                break;

            case GameManager.GameState.Playing:
                Mostrar(hudPanel);
                break;

            case GameManager.GameState.Results:
                // El HUD sigue visible para que el jugador vea el contexto de dinero/día.
                Mostrar(hudPanel);
                Mostrar(resultadosPanel);
                break;

            case GameManager.GameState.CuotaDePiso:
                Mostrar(cuotaPanel);
                break;

            case GameManager.GameState.GameOver:
                Mostrar(gameOverPanel);
                break;
        }
    }

    // ── Pausa ─────────────────────────────────────────────────────────────────

    /// <summary>Llamado por el botón de pausa del HUD.</summary>
    public void TogglePausa()
    {
        if (pausaActiva) CerrarPausa();
        else AbrirPausa();
    }

    public void AbrirPausa()
    {
        pausaActiva = true;
        Time.timeScale = 0f;
        Mostrar(pausePanel);
    }

    public void CerrarPausa()
    {
        pausaActiva = false;
        Time.timeScale = 1f;
        Ocultar(pausePanel);
    }

    public void ToggleOrdenes()
    {
        if (ordenesPanel != null)
            ordenesPanel.SetActive(!ordenesPanel.activeSelf);
    }
    /// <summary>Llamado por el botón "Menú principal" del menú de pausa.</summary>
    public void IrAlMenuPrincipal()
    {
        CerrarPausa();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void OcultarTodo()
    {
        Ocultar(hudPanel);
        Ocultar(startDayPanel);
        Ocultar(resultadosPanel);
        Ocultar(cuotaPanel);
        Ocultar(gameOverPanel);
        // pausePanel NO se oculta aquí — lo maneja CerrarPausa()
    }

    private static void Mostrar(GameObject p) { if (p != null) p.SetActive(true); }
    private static void Ocultar(GameObject p)  { if (p != null) p.SetActive(false); }
}