using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    [Header("Configuración de partida")]
    [SerializeField, Min(1)] private int diaInicial = 1;
    [SerializeField, Min(1)] private int diasPorSemana = 3;
    [SerializeField] private bool iniciarEnMainMenu = true;

    [Header("Debug (runtime)")]
    [SerializeField] private int inicioSemanaActual = 1;

    [Header("Debug teclas")]
    [SerializeField] private bool habilitarAtajosDebug = true;
    [SerializeField] private KeyCode teclaSiguienteDia = KeyCode.R;

    // SINGLETON

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // GAME STATE

    public enum GameState
    {
        MainMenu,       // Pantalla de inicio
        StartDay,       // Transición breve: muestra número de día y dinero
        Playing,        // FASE ACTIVA: cocina + clientes simultáneamente
        Results,        // Resumen del día: ganancias, propinas, balance neto
        CuotaDePiso,    // Solo cada 7 días: cobro semanal del cartel
        GameOver        // 3 semanas sin pagar = fin de partida
    }

    /// <summary>Estado actual del juego.</summary>
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    // SEGUIMIENTO DE DÍAS

    /// <summary>Día actual de la partida (empieza en 1).</summary>
    public int CurrentDay { get; private set; } = 1;

    /// <summary>Cada cuántos días se cobra la cuota.</summary>
    public int DaysPerWeek => Mathf.Max(1, diasPorSemana);

    /// <summary>¿El día actual es día de cobro?</summary>
    public bool IsPaymentDay => CurrentDay % DaysPerWeek == 0;

    /// <summary>Semana actual (1-based).</summary>
    public int CurrentWeek => ((CurrentDay - 1) / DaysPerWeek) + 1;

    /// <summary>Día dentro de la semana (1..DaysPerWeek).</summary>
    public int DayInWeek => ((CurrentDay - 1) % DaysPerWeek) + 1;

    // EVENTOS

    /// <summary>Notifica a todos los sistemas cuando cambia la fase.</summary>
    public static event System.Action<GameState> OnStateChanged;

    /// <summary>Notifica cuando avanza el día (útil para HUD y DayCycleManager).</summary>
    public static event System.Action<int> OnDayChanged;

    /// <summary>Se dispara al llegar a GameOver.</summary>
    public static event System.Action OnGameOver;

    /// <summary>Se dispara cuando se pierde el progreso semanal y se regresa al inicio de semana.</summary>
    public static event System.Action<int, int> OnWeeklyProgressReset;
    // Parámetros: (semana, diaAlQueRegreso)

    // INICIO

    private void Start()
    {
        CurrentDay = Mathf.Max(1, diaInicial);
        inicioSemanaActual = ObtenerInicioDeSemana(CurrentDay);
        OnDayChanged?.Invoke(CurrentDay);

        ChangeState(iniciarEnMainMenu ? GameState.MainMenu : GameState.StartDay);
        Debug.Log($"Resolución: {Screen.width}x{Screen.height}, Aspect: {(float)Screen.width/Screen.height}");
    }

    private void OnEnable()
    {
        GestorClientes.alTerminarTurno += AlTerminarTurno;
    }

    private void OnDisable()
    {
        GestorClientes.alTerminarTurno -= AlTerminarTurno;
    }

    private void Update()
    {
        if (!habilitarAtajosDebug) return;
        if (!Input.GetKeyDown(teclaSiguienteDia)) return;

        if (CurrentState != GameState.Results)
        {
            Debug.Log($"[GameManager][DEBUG] Tecla {teclaSiguienteDia} ignorada. Estado actual: {CurrentState}. Debe ser Results.");
            return;
        }

        Debug.Log($"[GameManager][DEBUG] Tecla {teclaSiguienteDia} detectada en Results. Intentando avanzar al siguiente día...");
        AdvanceToNextState();
    }

    // CAMBIO DE ESTADO
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
        {
            Debug.LogWarning($"[GameManager] Ya estás en el estado: {newState}");
            return;
        }

        GameState previousState = CurrentState;
        CurrentState = newState;

        Debug.Log($"[GameManager] {previousState} → {newState}");

        OnStateChanged?.Invoke(newState);

        if (newState == GameState.GameOver)
            OnGameOver?.Invoke();
    }

    // AVANCE DEL LOOP

    /// <summary>
    /// Avanza al siguiente estado.
    /// Al salir de Results, revisa si es día de cobro semanal.
    /// Llámalo desde el botón "Continuar" de la pantalla de resultados.
    /// </summary>
    public void AdvanceToNextState()
    {
        switch (CurrentState)
        {
            case GameState.MainMenu:
                ChangeState(GameState.StartDay);
                break;

            case GameState.StartDay:
                ChangeState(GameState.Playing);
                break;

            case GameState.Playing:
                ChangeState(GameState.Results);
                break;

            case GameState.Results:
                Debug.Log($"[GameManager] Evaluando fin de turno — Día global: {CurrentDay} | Semana: {CurrentWeek} | Día en semana: {DayInWeek}/{DaysPerWeek}");

                // ¿Es fin de semana? → cobro del cartel antes de continuar
                if (IsPaymentDay)
                {
                    Debug.Log($"[GameManager] Día de cobro detectado (semana {CurrentWeek}). Entrando a CuotaDePiso.");
                    ChangeState(GameState.CuotaDePiso);
                }
                else
                {
                    Debug.Log("[GameManager] No es día de cobro. Avanzando al siguiente día.");
                    StartNextDay();
                }
                break;

            case GameState.CuotaDePiso:
                // CuotaDePiso.cs debe resolver con RegistrarResultadoCuota(...)
                Debug.Log("[GameManager] Esperando resultado de cuota. Llama RegistrarResultadoCuota(true/false).");
                break;

            case GameState.GameOver:
                Debug.Log("[GameManager] Game Over — usa RestartGame() para reiniciar.");
                break;
        }
    }

    /// <summary>
    /// Se llama desde CuotaDePiso al final del cobro.
    /// pagada = true  → avanza al siguiente día (nueva semana)
    /// pagada = false → pierde avance de la semana y vuelve al día inicial de esa semana
    /// </summary>
    public void RegistrarResultadoCuota(bool pagada)
    {
        if (CurrentState != GameState.CuotaDePiso)
        {
            Debug.LogWarning("[GameManager] RegistrarResultadoCuota llamado fuera del estado CuotaDePiso.");
            return;
        }

        if (pagada)
        {
            Debug.Log($"[GameManager] Cuota CUMPLIDA en semana {CurrentWeek}. Avanzando desde día {CurrentDay}.");
            StartNextDay();
            return;
        }

        int semana = CurrentWeek;
        CurrentDay = inicioSemanaActual;
        Debug.Log($"[GameManager] Cuota NO cumplida en semana {semana}. Regresando al punto de guardado (inicio de semana) día {CurrentDay}.");
        OnWeeklyProgressReset?.Invoke(semana, CurrentDay);
        OnDayChanged?.Invoke(CurrentDay);
        ChangeState(GameState.StartDay);
        RecargarEscenaDelDiaActual();
    }

    // HELPERS INTERNOS

    /// <summary>Incrementa el día y arranca el siguiente ciclo.</summary>
    private void StartNextDay()
    {
        CurrentDay++;
        inicioSemanaActual = ObtenerInicioDeSemana(CurrentDay);
        OnDayChanged?.Invoke(CurrentDay);
        Debug.Log($"[GameManager] Día {CurrentDay} comenzando | Semana actual: {CurrentWeek} | Día en semana: {DayInWeek}/{DaysPerWeek}");
        ChangeState(GameState.StartDay);
        RecargarEscenaDelDiaActual();
    }

    private void AlTerminarTurno(int pagosRecibidos, int timeouts, int diaTurno)
    {
        if (CurrentState == GameState.GameOver) return;

        Debug.Log($"[GameManager] Turno terminado | Pagos: {pagosRecibidos} | Timeouts: {timeouts}");

        if (CurrentState != GameState.Results)
            StartCoroutine(EsperarYMostrarResultados());
    }

    private System.Collections.IEnumerator EsperarYMostrarResultados()
    {
        yield return new WaitForSeconds(5f);
        ChangeState(GameState.Results);
    }

    private void RecargarEscenaDelDiaActual()
    {
        Scene escenaActiva = SceneManager.GetActiveScene();
        Debug.Log($"[GameManager] Recargando escena '{escenaActiva.name}' para iniciar día {CurrentDay}.");
        SceneManager.LoadScene(escenaActiva.name);
    }

    private int ObtenerInicioDeSemana(int dia)
    {
        int semanaBaseCero = (Mathf.Max(1, dia) - 1) / DaysPerWeek;
        return (semanaBaseCero * DaysPerWeek) + 1;
    }

    // REINICIO

    //Reinicia la partida desde el día 1
    public void RestartGame()
    {
        CurrentDay = Mathf.Max(1, diaInicial);
        inicioSemanaActual = ObtenerInicioDeSemana(CurrentDay);
        OnDayChanged?.Invoke(CurrentDay);
        Debug.Log("[GameManager] Reiniciando partida...");
        ChangeState(GameState.MainMenu);
    }

    // QUERIES
    public bool IsInState(GameState state) => CurrentState == state;
    public bool IsPlaying()                => CurrentState == GameState.Playing;
    public bool IsGameOver()               => CurrentState == GameState.GameOver;
}
