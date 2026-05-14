using UnityEngine;

/// <summary>
/// GestorWaypointsClientes — Centraliza rutas y hace spawn físico de clientes.
/// Soporta múltiples prefabs de cliente que se eligen aleatoriamente sin repetir.
/// </summary>
public class GestorWaypointsClientes : MonoBehaviour
{
    [Header("Rutas")]
    public Transform[] puntosDeRuta;
    public Transform[] puntosSalida;

    [Header("Spawn")]
    [Tooltip("Arrastra aquí los 7 prefabs de cliente (uno por modelo). Se eligen aleatoriamente.")]
    public GameObject[] prefabsClientes;

    [Tooltip("Punto donde aparecerán los clientes")]
    public Transform puntoSpawn;

    [Tooltip("Si está activo, crea clientes automáticamente")]
    public bool autoSpawn = false;

    [Tooltip("Tiempo entre spawns (GestorClientes lo sobreescribe)")]
    public float intervaloSpawn = 6f;

    [Tooltip("Máximo de clientes activos al mismo tiempo (GestorClientes lo sobreescribe)")]
    public int maxClientesActivos = 1;

    [Tooltip("Total de clientes a spawnear en el día. 0 = sin límite")]
    public int limiteSpawn = 0;

    [Tooltip("Día actual (GestorClientes lo asigna)")]
    public int diaActual = 1;

    // ESTADO INTERNO

    private float _timerSpawn = 0f;
    private int _spawneados = 0;
    private bool _ultimoEstadoEsperando = false;
    private int _ultimoPrefabIndex = -1;

    // EVENTOS

    public static event System.Action alSpawnearCliente;

    // CICLO

    private void Start()
    {
        _timerSpawn = intervaloSpawn;
        _spawneados = 0;

        if (prefabsClientes == null || prefabsClientes.Length == 0)
            Debug.LogError("[GestorWaypoints] No hay prefabs de cliente asignados en el Inspector.");
    }

    private void Update()
    {
        if (!autoSpawn)
        {
            _ultimoEstadoEsperando = false;
            return;
        }

        if (prefabsClientes == null || prefabsClientes.Length == 0) return;
        if (puntoSpawn == null) return;
        if (limiteSpawn > 0 && _spawneados >= limiteSpawn) return;

        int activos = ContarClientesActivos();
        if (activos >= maxClientesActivos)
        {
            if (!_ultimoEstadoEsperando)
            {
                Debug.Log($"[GestorWaypoints] Cupo lleno ({activos}/{maxClientesActivos}). Esperando...");
                _ultimoEstadoEsperando = true;
            }
            return;
        }

        if (_ultimoEstadoEsperando)
        {
            Debug.Log("[GestorWaypoints] Cupo libre. Preparando siguiente spawn.");
            _ultimoEstadoEsperando = false;
        }

        _timerSpawn -= Time.deltaTime;
        if (_timerSpawn > 0f) return;

        _timerSpawn = intervaloSpawn;
        SpawnCliente();
    }

    // SPAWN

    private void SpawnCliente()
    {
        GameObject prefabElegido = ElegirPrefabAleatorio();
        if (prefabElegido == null) return;

        GameObject nuevoCliente = Instantiate(prefabElegido, puntoSpawn.position, puntoSpawn.rotation);

        ClienteIA clienteIA = nuevoCliente.GetComponent<ClienteIA>();
        if (clienteIA != null)
        {
            clienteIA.diaActual = diaActual;
            clienteIA.puntosDeRuta = puntosDeRuta;
            clienteIA.puntosSalida = puntosSalida;
        }
        else
        {
            Debug.LogWarning($"[GestorWaypoints] El prefab '{prefabElegido.name}' no tiene ClienteIA.");
        }

        _spawneados++;
        Debug.Log($"[GestorWaypoints] Spawneado: {prefabElegido.name} ({_spawneados}/{limiteSpawn})");
        alSpawnearCliente?.Invoke();
    }

    private GameObject ElegirPrefabAleatorio()
    {
        if (prefabsClientes.Length == 1)
            return prefabsClientes[0];

        int elegido;
        int intentos = 0;

        do
        {
            elegido = Random.Range(0, prefabsClientes.Length);
            intentos++;
        }
        while (elegido == _ultimoPrefabIndex && intentos < 20);

        _ultimoPrefabIndex = elegido;
        return prefabsClientes[elegido];
    }

    private int ContarClientesActivos()
    {
        return FindObjectsByType<ClienteIA>(FindObjectsSortMode.None).Length;
    }

    public void ResetearConteo()
    {
        _spawneados = 0;
        _timerSpawn = intervaloSpawn;
        _ultimoPrefabIndex = -1;
    }
}