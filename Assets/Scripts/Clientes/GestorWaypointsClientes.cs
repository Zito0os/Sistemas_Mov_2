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

    // EVENTOS

    /// <summary>Se disparó un nuevo cliente. GestorClientes escucha esto.</summary>
    public static event System.Action alSpawnearCliente;

    // CICLO

    private void Start()
    {
        _timerSpawn = intervaloSpawn;
        _spawneados = 0;
    }

    private void Update()
    {
        if (!autoSpawn) return;
        if (prefabCliente == null || puntoSpawn == null) return;

        // ¿Se alcanzó el límite del día?
        if (limiteSpawn > 0 && _spawneados >= limiteSpawn) return;

        // ¿Hay demasiados clientes activos?
        if (ContarClientesActivos() >= maxClientesActivos) return;

        _timerSpawn -= Time.deltaTime;
        if (_timerSpawn > 0f) return;

        _timerSpawn = intervaloSpawn;
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