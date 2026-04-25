using UnityEngine;

/// <summary>
/// GestorClientes — Controla la lógica del flujo de clientes por día.
///
/// Responsabilidades:
///   - Definir cuántos clientes llegan cada día y a qué velocidad
///   - Activar / desactivar el spawn de GestorWaypointsClientes
///   - Llevar conteo de clientes atendidos, pagos y timeouts del día
///   - Escalar la dificultad automáticamente según el día
///   - Notificar cuando terminó el turno del día
///
/// NO hace spawn directamente: delega eso en GestorWaypointsClientes.
/// </summary>
public class GestorClientes : MonoBehaviour
{
    // REFERENCIAS

    [Header("Referencias")]
    [Tooltip("El GestorWaypointsClientes de la escena (maneja el spawn físico)")]
    public GestorWaypointsClientes gestorWaypoints;

    // INSPECTOR — Configuración del día

    [Header("Configuración del día")]
    [Tooltip("Día actual de la partida")]
    public int diaActual = 1;

    [Tooltip("¿Iniciar el turno automáticamente al hacer Play? (útil para TestClientes)")]
    public bool iniciarAlArrancar = true;

    // INSPECTOR — Escalado de dificultad (solo lectura en runtime)

    [Header("Dificultad actual (se calcula solo)")]
    [SerializeField] private int _clientesDelDia = 0;
    [SerializeField] private int _clientesRestantes = 0;
    [SerializeField] private float _intervaloActual = 0f;

    // CONTEO INTERNO DEL DÍA

    private int _clientesSpawneados = 0;
    private int _clientesAtendidos = 0;   // pagaron o se fueron (cualquier resultado)
    private int _pagosRecibidos = 0;
    private int _timeouts = 0;
    private bool _turnoActivo = false;

    // EVENTOS

    /// <summary>Se disparó un nuevo spawn (int = número de cliente del día).</summary>
    public static event System.Action<int> alSpawnearCliente;

    /// <summary>Todos los clientes del día fueron atendidos.</summary>
    public static event System.Action<int, int, int> alTerminarTurno;
    // Parámetros: (pagosRecibidos, timeouts, diaActual)

    // INICIO

    private void OnEnable()
    {
        ClienteIA.alIrseCliente += AlIrseUnCliente;
        GestorWaypointsClientes.alSpawnearCliente += AlSpawnearUnCliente;

        GameManager.OnDayChanged += AlCambiarDia;
        GameManager.OnStateChanged += AlCambiarEstadoJuego;
    }

    private void OnDisable()
    {
        ClienteIA.alIrseCliente -= AlIrseUnCliente;
        GestorWaypointsClientes.alSpawnearCliente -= AlSpawnearUnCliente;

        GameManager.OnDayChanged -= AlCambiarDia;
        GameManager.OnStateChanged -= AlCambiarEstadoJuego;
    }

    private void Start()
    {
        if (gestorWaypoints == null)
            gestorWaypoints = FindFirstObjectByType<GestorWaypointsClientes>();

        if (gestorWaypoints == null)
        {
            Debug.LogError("[GestorClientes] No se encontró GestorWaypointsClientes en la escena.");
            return;
        }

        // Desactivar spawn hasta que empiece el turno
        //gestorWaypoints.autoSpawn = false; linea comentada por que el gamemaneger corre primero e incia el turno automaticamente, pero despues el gestor clinetes apaga el spawn

        if (GameManager.Instance != null)
        {
            diaActual = GameManager.Instance.CurrentDay;
            Debug.Log($"[GestorClientes] Día sincronizado desde GameManager: {diaActual}");
        }
        else
        {
            Debug.LogWarning($"[GestorClientes] No hay GameManager activo. Se usará diaActual local: {diaActual}");
        }

        if (iniciarAlArrancar)
            IniciarTurno(diaActual);
    }

    // API PÚBLICA

    /// <summary>
    /// Inicia el turno del día. Llámalo cuando el GameManager entre en estado Playing.
    /// Por ahora también se puede llamar con iniciarAlArrancar = true para pruebas.
    /// </summary>
    public void IniciarTurno(int dia)
    {
        diaActual = dia;

        // Calcular parámetros del día
        DificultadDelDia config = CalcularDificultad(dia);
        _clientesDelDia = config.totalClientes;
        _clientesRestantes = config.totalClientes;
        _intervaloActual = config.intervaloSpawn;

        // Resetear conteos
        _clientesSpawneados = 0;
        _clientesAtendidos = 0;
        _pagosRecibidos = 0;
        _timeouts = 0;
        _turnoActivo = true;

        // Configurar y activar el gestor de waypoints
        gestorWaypoints.diaActual = dia;
        gestorWaypoints.intervaloSpawn = _intervaloActual;
        gestorWaypoints.maxClientesActivos = config.maxSimultaneos;
        gestorWaypoints.limiteSpawn = _clientesDelDia;
        gestorWaypoints.autoSpawn = true;

        Debug.Log($"[GestorClientes] Día {dia} iniciado — {_clientesDelDia} clientes, " +
                  $"cada {_intervaloActual}s, máx {config.maxSimultaneos} simultáneos.");
    }

    /// <summary>Detiene el spawn manualmente (por pausa o cambio de estado).</summary>
    public void DetenerTurno()
    {
        _turnoActivo = false;
        if (gestorWaypoints != null)
            gestorWaypoints.autoSpawn = false;

        Debug.Log("[GestorClientes] Turno detenido.");
    }

    // LISTENERS DE EVENTOS

    private void AlSpawnearUnCliente()
    {
        _clientesSpawneados++;
        _clientesRestantes = _clientesDelDia - _clientesSpawneados;
        alSpawnearCliente?.Invoke(_clientesSpawneados);

        Debug.Log($"[GestorClientes] Cliente {_clientesSpawneados}/{_clientesDelDia} spawneado.");

        // Si ya se spawnearon todos, desactivar spawn
        if (_clientesSpawneados >= _clientesDelDia)
        {
            gestorWaypoints.autoSpawn = false;
            Debug.Log("[GestorClientes] Todos los clientes del día fueron spawneados.");
        }
    }

    private void AlIrseUnCliente(ClienteIA cliente, bool pago)
    {
        if (!_turnoActivo) return;

        _clientesAtendidos++;

        if (pago) _pagosRecibidos++;
        else _timeouts++;

        Debug.Log($"[GestorClientes] Cliente se fue. Pago: {pago} | " +
                  $"Atendidos: {_clientesAtendidos}/{_clientesDelDia}");

        // ¿Ya se atendieron todos los clientes del día?
        if (_clientesAtendidos >= _clientesDelDia)
            TerminarTurno();
    }

    // FIN DEL TURNO

    private void TerminarTurno()
    {
        _turnoActivo = false;
        gestorWaypoints.autoSpawn = false;

        Debug.Log($"[GestorClientes] Turno terminado — " +
                  $"Pagos: {_pagosRecibidos} | Timeouts: {_timeouts}");

        alTerminarTurno?.Invoke(_pagosRecibidos, _timeouts, diaActual);

        // TODO: cuando exista GameManager →
        // GameManager.Instance.AdvanceToNextState();
    }

    private void AlCambiarDia(int nuevoDia)
    {
        diaActual = nuevoDia;
        Debug.Log($"[GestorClientes] Día actualizado por evento: {diaActual}");
    }

    private void AlCambiarEstadoJuego(GameManager.GameState nuevoEstado)
    {
        if (nuevoEstado == GameManager.GameState.StartDay)
        {
            if (GameManager.Instance != null)
                diaActual = GameManager.Instance.CurrentDay;

            Debug.Log($"[GestorClientes] StartDay detectado. Preparando turno del día {diaActual}.");
            IniciarTurno(diaActual);
            return;
        }

        if (nuevoEstado == GameManager.GameState.Playing && !_turnoActivo)
        {
            Debug.Log($"[GestorClientes] Playing detectado sin turno activo. Iniciando día {diaActual}.");
            IniciarTurno(diaActual);
            return;
        }

        if (nuevoEstado == GameManager.GameState.Results ||
            nuevoEstado == GameManager.GameState.CuotaDePiso ||
            nuevoEstado == GameManager.GameState.GameOver)
        {
            DetenerTurno();
        }
    }

    // DIFICULTAD POR DÍA

    private struct DificultadDelDia
    {
        public int totalClientes;
        public int maxSimultaneos;
        public float intervaloSpawn;
    }

    /// <summary>
    /// Define cuántos clientes llegan y qué tan seguido según el día.
    ///
    ///   Días 1-3:  2-3 clientes, 1 a la vez, cada 3s    → fácil
    ///   Días 4-7:  4-5 clientes, 2 a la vez, cada 2.5s  → medio
    ///   Días 8+:   6-8 clientes, 3 a la vez, cada 2s    → difícil
    /// </summary>
    private DificultadDelDia CalcularDificultad(int dia)
    {
        if (dia >= 8)
            return new DificultadDelDia
            {
                totalClientes = Random.Range(6, 9),
                maxSimultaneos = 3,
                intervaloSpawn = 2f
            };

        if (dia >= 4)
            return new DificultadDelDia
            {
                totalClientes = Random.Range(4, 6),
                maxSimultaneos = 2,
                intervaloSpawn = 2.5f
            };

        return new DificultadDelDia
        {
            totalClientes = Random.Range(2, 4),
            maxSimultaneos = 1,
            intervaloSpawn = 3f
        };
    }

    // QUERIES

    public bool TurnoActivo => _turnoActivo;
    public int PagosDelDia => _pagosRecibidos;
    public int TimeoutsDelDia => _timeouts;
    public int ClientesAtendidos => _clientesAtendidos;
}