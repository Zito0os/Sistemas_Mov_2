using UnityEngine;

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

    [Tooltip("¿Iniciar el turno automáticamente al hacer Play? (útil para escenas de prueba)")]
    public bool iniciarAlArrancar = false;

    // INSPECTOR — Escalado de dificultad (solo lectura en runtime)

    [Header("Dificultad actual (se calcula solo)")]
    [SerializeField] private int _clientesDelDia = 0;
    [SerializeField] private int _clientesRestantes = 0;
    [SerializeField] private float _intervaloActual = 0f;

    // CONTEO INTERNO DEL DÍA

    private int _clientesSpawneados = 0;
    private int _clientesAtendidos = 0;
    private int _pagosRecibidos = 0;
    private int _timeouts = 0;
    private bool _turnoActivo = false;

    // EVENTOS

    public static event System.Action<int> alSpawnearCliente;

    public static event System.Action<int, int, int> alTerminarTurno;
    // Parámetros: (pagosRecibidos, timeouts, diaActual)

    // INICIO

    private void OnEnable()
    {
        ClienteIA.alIrseCliente += AlIrseUnCliente;
        GestorWaypointsClientes.alSpawnearCliente += AlSpawnearUnCliente;

        GameManager.OnDayChanged   += AlCambiarDia;
        GameManager.OnStateChanged += AlCambiarEstadoJuego;
    }

    private void OnDisable()
    {
        ClienteIA.alIrseCliente -= AlIrseUnCliente;
        GestorWaypointsClientes.alSpawnearCliente -= AlSpawnearUnCliente;

        GameManager.OnDayChanged   -= AlCambiarDia;
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

        // Asegurar que el autoSpawn empieza apagado — el cartel lo activará vía Playing
        gestorWaypoints.autoSpawn = false;

        if (GameManager.Instance != null)
        {
            diaActual = GameManager.Instance.CurrentDay;
            Debug.Log($"[GestorClientes] Día sincronizado desde GameManager: {diaActual}");
        }
        else
        {
            Debug.LogWarning($"[GestorClientes] No hay GameManager activo. Se usará diaActual local: {diaActual}");
        }

        // Solo para escenas de prueba sin GameManager
        if (iniciarAlArrancar)
        {
            Debug.Log("[GestorClientes] iniciarAlArrancar = true → iniciando turno directo.");
            IniciarTurno(diaActual);
        }
    }

    // API PÚBLICA
    public void IniciarTurno(int dia)
    {
        diaActual = dia;

        DificultadDelDia config = CalcularDificultad(dia);
        _clientesDelDia    = config.totalClientes;
        _clientesRestantes = config.totalClientes;
        _intervaloActual   = config.intervaloSpawn;

        _clientesSpawneados = 0;
        _clientesAtendidos  = 0;
        _pagosRecibidos     = 0;
        _timeouts           = 0;
        _turnoActivo        = true;

        gestorWaypoints.diaActual         = dia;
        gestorWaypoints.intervaloSpawn    = _intervaloActual;
        gestorWaypoints.maxClientesActivos = config.maxSimultaneos;
        gestorWaypoints.limiteSpawn       = _clientesDelDia;
        gestorWaypoints.autoSpawn         = true;

        Debug.Log($"[GestorClientes] Turno iniciado — Día {dia} | {_clientesDelDia} clientes | " +
                  $"cada {_intervaloActual}s | máx {config.maxSimultaneos} simultáneos.");
    }

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
    }

    private void AlCambiarDia(int nuevoDia)
    {
        diaActual = nuevoDia;
        Debug.Log($"[GestorClientes] Día actualizado por evento: {diaActual}");
    }

    private void AlCambiarEstadoJuego(GameManager.GameState nuevoEstado)
    {
        // ── PLAYING aquí arranca el turno ( ──
        if (nuevoEstado == GameManager.GameState.Playing)
        {
            if (GameManager.Instance != null)
                diaActual = GameManager.Instance.CurrentDay;

            if (!_turnoActivo)
            {
                Debug.Log($"[GestorClientes] Estado Playing detectado → iniciando turno día {diaActual}.");
                IniciarTurno(diaActual);
            }
            else
            {
                Debug.Log("[GestorClientes] Playing detectado pero el turno ya estaba activo. No se reinicia.");
            }
            return;
        }

        // ── STARTDAY preparar el día sin iniciar spawn ──
        if (nuevoEstado == GameManager.GameState.StartDay)
        {
            if (GameManager.Instance != null)
                diaActual = GameManager.Instance.CurrentDay;

            // Asegurar que el spawn esté apagado durante las compras
            if (gestorWaypoints != null)
                gestorWaypoints.autoSpawn = false;

            _turnoActivo = false;

            Debug.Log($"[GestorClientes] StartDay — esperando al jugador en el cartel. Día: {diaActual}");
            return;
        }

        // ── Cualquier otro estado → detener turno ──
        if (nuevoEstado == GameManager.GameState.Results    ||
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

    private DificultadDelDia CalcularDificultad(int dia)
    {
        if (dia >= 8)
            return new DificultadDelDia
            {
                totalClientes  = Random.Range(6, 9),
                maxSimultaneos = 3,
                intervaloSpawn = 2f
            };

        if (dia >= 4)
            return new DificultadDelDia
            {
                totalClientes  = Random.Range(4, 6),
                maxSimultaneos = 2,
                intervaloSpawn = 2.5f
            };

        return new DificultadDelDia
        {
            totalClientes  = Random.Range(2, 4),
            maxSimultaneos = 1,
            intervaloSpawn = 3f
        };
    }

    // QUERIES

    public bool TurnoActivo      => _turnoActivo;
    public int PagosDelDia       => _pagosRecibidos;
    public int TimeoutsDelDia    => _timeouts;
    public int ClientesAtendidos => _clientesAtendidos;
}