using UnityEngine;

/// <summary>
/// GameManager — Orquestador principal de Don Paco Taco.
///
/// FLUJO DEL JUEGO:
///
///   MainMenu
///      ↓
///   StartDay   ← muestra número de día y dinero disponible
///      ↓
///   Playing    ← FASE ACTIVA: clientes llegan, el jugador cocina y entrega
///      ↓
///   Results    ← resumen del día (ganancias, propinas, balance)
///      ↓
///   StartDay   ← repite para el siguiente día
///
///   Cada 7 días, después de Results, se inserta:
///   CuotaDePiso ← cobro semanal del cartel (¿tienes suficiente?)
///      → si no pagas 3 semanas consecutivas → GameOver
///
/// </summary>
/*
public class GameManager : MonoBehaviour
{
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
    private const int DAYS_PER_WEEK = 7;

    /// <summary>¿El día actual es día de cobro?</summary>
    public bool IsPaymentDay => CurrentDay % DAYS_PER_WEEK == 0;

    // EVENTOS

    /// <summary>Notifica a todos los sistemas cuando cambia la fase.</summary>
    public static event System.Action<GameState> OnStateChanged;

    /// <summary>Notifica cuando avanza el día (útil para HUD y DayCycleManager).</summary>
    public static event System.Action<int> OnDayChanged;

    /// <summary>Se dispara al llegar a GameOver.</summary>
    public static event System.Action OnGameOver;

    // INICIO

    private void Start()
    {
        ChangeState(GameState.MainMenu);
    }

    // CAMBIO DE ESTADO

    /// <summary>
    /// Cambia la fase del juego y notifica a todos los sistemas suscritos.
    ///
    /// ¿Quién llama a este método?
    ///   - UIManager        → "Jugar" en el menú          → ChangeState(StartDay)
    ///   - DayCycleManager  → timer del día expira         → ChangeState(Results)
    ///   - ResultsScreen    → "Continuar"                  → AdvanceToNextState()
    ///   - CuotaDePiso      → cobro completado             → ChangeState(StartDay)
    ///   - CuotaDePiso      → 3 semanas sin pagar          → ChangeState(GameOver)
    /// </summary>
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
                // ¿Es fin de semana? → cobro del cartel antes de continuar
                if (IsPaymentDay)
                    ChangeState(GameState.CuotaDePiso);
                else
                    StartNextDay();
                break;

            case GameState.CuotaDePiso:
                // CuotaDePiso.cs decide si GameOver o continua
                StartNextDay();
                break;

            case GameState.GameOver:
                Debug.Log("[GameManager] Game Over — usa RestartGame() para reiniciar.");
                break;
        }
    }

    // HELPERS INTERNOS

    /// <summary>Incrementa el día y arranca el siguiente ciclo.</summary>
    private void StartNextDay()
    {
        CurrentDay++;
        OnDayChanged?.Invoke(CurrentDay);
        Debug.Log($"[GameManager] Día {CurrentDay} comenzando.");
        ChangeState(GameState.StartDay);
    }

    // REINICIO

    //Reinicia la partida desde el día 1
    public void RestartGame()
    {
        CurrentDay = 1;
        Debug.Log("[GameManager] Reiniciando partida...");
        ChangeState(GameState.MainMenu);
    }

    // QUERIES
    public bool IsInState(GameState state) => CurrentState == state;
    public bool IsPlaying()                => CurrentState == GameState.Playing;
    public bool IsGameOver()               => CurrentState == GameState.GameOver;
}
*/