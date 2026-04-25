using UnityEngine;

/// <summary>
/// GestorWaypointsClientes — Centraliza rutas y hace spawn físico de clientes.
/// GestorClientes controla cuándo y cuántos. Este script solo instancia y asigna rutas.
/// </summary>
public class GestorWaypointsClientes : MonoBehaviour
{
    // INSPECTOR — Rutas

    [Header("Rutas")]
    [Tooltip("Puntos que el cliente sigue en orden hasta llegar al mostrador")]
    public Transform[] puntosDeRuta;

    [Tooltip("Waypoints que recorre el cliente al salir")]
    public Transform[] puntosSalida;

    // INSPECTOR — Spawn

    [Header("Spawn")]
    [Tooltip("Prefab del cliente")]
    public GameObject prefabCliente;

    [Tooltip("Punto donde aparecerán los clientes")]
    public Transform puntoSpawn;

    [Tooltip("Si está activo, crea clientes automáticamente")]
    public bool autoSpawn = false;

    [Tooltip("Tiempo entre spawns automáticos (GestorClientes lo sobreescribe)")]
    public float intervaloSpawn = 6f;

    [Tooltip("Máximo de clientes activos al mismo tiempo (GestorClientes lo sobreescribe)")]
    public int maxClientesActivos = 1;

    [Tooltip("Total de clientes a spawnear en el día. 0 = sin límite (GestorClientes lo asigna)")]
    public int limiteSpawn = 0;

    [Tooltip("Día actual (GestorClientes lo asigna antes de activar autoSpawn)")]
    public int diaActual = 1;

    // ESTADO INTERNO

    private float _timerSpawn = 0f;
    private int _spawneados = 0;

    private bool _ultimoEstadoEsperando = false;

    // EVENTOS

    /// <summary>Se disparó un nuevo cliente. GestorClientes escucha esto.</summary>
    public static event System.Action alSpawnearCliente;

    // CICLO

    private void Start()
    {
        _timerSpawn = intervaloSpawn;
        _spawneados = 0;
        Debug.Log($"[GestorWaypoints] Start() ejecutado. GameObject: {gameObject.name}, activo: {gameObject.activeInHierarchy}, prefab: {(prefabCliente != null ? prefabCliente.name : "NULL")}, puntoSpawn: {(puntoSpawn != null ? puntoSpawn.name : "NULL")}");
    }

    private void Update()
    {
        if (!autoSpawn)
        {
            // Si estábamos esperando y se apagó el spawn, resetear el flag
            _ultimoEstadoEsperando = false;
            return;
        }

        if (prefabCliente == null)
        {
            Debug.LogWarning("[GestorWaypoints] prefabCliente no asignado en Inspector.");
            return;
        }
        if (puntoSpawn == null)
        {
            Debug.LogWarning("[GestorWaypoints] puntoSpawn no asignado en Inspector.");
            return;
        }

        // ¿Se alcanzó el límite del día?
        if (limiteSpawn > 0 && _spawneados >= limiteSpawn) return;

        // ¿Hay demasiados clientes activos?
        int activos = ContarClientesActivos();
        if (activos >= maxClientesActivos)
        {
            // Solo logear cuando ENTRAMOS al estado "esperando" (flanco), no cada frame
            if (!_ultimoEstadoEsperando)
            {
                Debug.Log($"[GestorWaypoints] Cupo lleno. Esperando a que se vaya un cliente. ({activos}/{maxClientesActivos})");
                _ultimoEstadoEsperando = true;
            }
            return;
        }

        // Si salimos del estado "esperando" (se fue un cliente, hay cupo otra vez), logear la transición
        if (_ultimoEstadoEsperando)
        {
            Debug.Log($"[GestorWaypoints] Cupo libre nuevamente. Preparando siguiente spawn.");
            _ultimoEstadoEsperando = false;
        }

        _timerSpawn -= Time.deltaTime;
        if (_timerSpawn > 0f) return;

        _timerSpawn = intervaloSpawn;
        Debug.Log($"[GestorWaypoints] Spawn disparado. Total spawneados: {_spawneados + 1}/{limiteSpawn}");
        SpawnCliente();
    }

    // SPAWN

    private void SpawnCliente()
    {
        GameObject nuevoCliente = Instantiate(prefabCliente, puntoSpawn.position, puntoSpawn.rotation);

        // Asignar día y rutas al ClienteIA recién creado
        ClienteIA clienteIA = nuevoCliente.GetComponent<ClienteIA>();
        if (clienteIA != null)
        {
            clienteIA.diaActual = diaActual;
            clienteIA.puntosDeRuta = puntosDeRuta;
            clienteIA.puntosSalida = puntosSalida;
        }

        _spawneados++;
        alSpawnearCliente?.Invoke();
    }

    // HELPERS

    private int ContarClientesActivos()
    {
        return FindObjectsByType<ClienteIA>(FindObjectsSortMode.None).Length;
    }

    /// <summary>Reinicia el conteo de spawns (llamar al inicio de cada día).</summary>
    public void ResetearConteo()
    {
        _spawneados = 0;
        _timerSpawn = intervaloSpawn;
    }
}