using System.Collections;
using UnityEngine;

/// <summary>
/// ClienteIA — Controla el comportamiento de un cliente individual.
///
/// Estados:
///   Caminando → Esperando → Ordenando → EsperandoComida → Saliendo
///
/// Flujo:
///   1. Aparece en el punto de spawn
///   2. Camina por los waypoints hasta el mostrador
///   3. Genera una orden y espera con un timer de paciencia
///   4. Recibe el taco → evalúa → paga → se va
///   5. Si se acaba el tiempo → se va enojado sin pagar
/// </summary>
public class ClienteIA : MonoBehaviour
{
    // ENUM DE ESTADOS
    public Animator animator;

    public enum EstadoCliente
    {
        Caminando,
        Esperando,
        Ordenando,
        EsperandoComida,
        Saliendo
    }

    // INSPECTOR — Movimiento

    [Header("Movimiento")]
    [Tooltip("Puntos que el cliente sigue en orden hasta llegar al mostrador")]
    public Transform[] puntosDeRuta;

    [Tooltip("Velocidad de caminar del cliente")]
    public float velocidad = 2f;

    [Tooltip("Distancia mínima para considerar que llegó a un waypoint")]
    public float distanciaLlegada = 0.2f;

    // INSPECTOR — Punto de salida

    [Header("Salida")]
    [Tooltip("Waypoints que recorre al salir (enojado o satisfecho)")]
    public Transform[] puntosSalida;

    // INSPECTOR — Día actual (para generar orden)

    [Header("Juego")]
    [Tooltip("Día actual. GestorClientes lo asigna al hacer spawn.")]
    public int diaActual = 1;

    // ESTADO INTERNO

    public EstadoCliente Estado { get; private set; } = EstadoCliente.Caminando;

    /// <summary>La orden generada por este cliente. Pública para que SistemaOrdenes la lea.</summary>
    public Orden OrdenActual { get; private set; }

    private int _indiceWaypoint = 0;
    private float _timerPaciencia = 0f;
    private bool _tacoRecibido = false;

    // EVENTOS

    /// <summary>El cliente llegó al mostrador y generó su orden.</summary>
    public static event System.Action<ClienteIA, Orden> alGenerarOrden;

    /// <summary>El cliente se fue (bool = pagó o no).</summary>
    public static event System.Action<ClienteIA, bool> alIrseCliente;

    /// <summary>Proporción de tiempo restante (0 a 1). Para actualizar la barra de paciencia en UI.</summary>
    public static event System.Action<ClienteIA, float> alActualizarPaciencia;

    // INICIO

    private void Start()
    {
        AsignarWaypointsSiFaltan();
        _indiceWaypoint = 0;
        CambiarEstado(EstadoCliente.Caminando);
    }

    // UPDATE

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && Estado != EstadoCliente.Saliendo)
            IrseDelPuesto(pago: false);

        switch (Estado)
        {
            case EstadoCliente.Caminando:
                Caminar();
                break;

            case EstadoCliente.EsperandoComida:
                
                ActualizarPaciencia();
                animator.SetBool("Esperando", true);
                break;
        }
    }

    // LÓGICA DE ESTADOS

    private void Caminar()
    {
        if (puntosDeRuta == null || puntosDeRuta.Length == 0) return;
        if (_indiceWaypoint < 0 || _indiceWaypoint >= puntosDeRuta.Length) return;

        Transform destino = puntosDeRuta[_indiceWaypoint];
        if (destino == null)
        {
            Debug.LogWarning($"[ClienteIA] Waypoint de ruta nulo en índice {_indiceWaypoint} para {gameObject.name}.");
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            destino.position,
            velocidad * Time.deltaTime
        );

        // Rotar hacia el destino
        Vector3 direccion = (destino.position - transform.position).normalized;
        if (direccion != Vector3.zero)
            transform.forward = Vector3.Lerp(transform.forward, direccion, 10f * Time.deltaTime);

        // ¿Llegó al waypoint?
        if (Vector3.Distance(transform.position, destino.position) <= distanciaLlegada)
        {
            transform.position = destino.position;
            _indiceWaypoint++;

            // ¿Era el último waypoint? → llegó al mostrador
            if (_indiceWaypoint >= puntosDeRuta.Length)
                LlegarAlMostrador();
        }
    }

    private void LlegarAlMostrador()
    {
        CambiarEstado(EstadoCliente.Ordenando);

        // Genera la orden y notifica al SistemaOrdenes y a la UI
        OrdenActual = Orden.GenerarAleatoria(diaActual);
        _timerPaciencia = OrdenActual.TiempoDePaciencia;

        Debug.Log($"[ClienteIA] {gameObject.name} ordenó: {OrdenActual}");
        alGenerarOrden?.Invoke(this, OrdenActual);

        CambiarEstado(EstadoCliente.EsperandoComida);
    }

    private void ActualizarPaciencia()
    {
        _timerPaciencia -= Time.deltaTime;

        float proporcion = Mathf.Clamp01(_timerPaciencia / OrdenActual.TiempoDePaciencia);
        alActualizarPaciencia?.Invoke(this, proporcion);

        // Se acabó la paciencia → se va sin pagar
        if (_timerPaciencia <= 0f)
        {
            Debug.Log($"[ClienteIA] {gameObject.name} se fue enojado (timeout).");
            IrseDelPuesto(pago: false);
        }
    }

    // API PÚBLICA

    /// <summary>
    /// Llamado por SistemaOrdenes cuando el jugador entrega un taco.
    /// </summary>
    public void RecibirTaco(Orden.TipoCarne carneEntregada,
                            System.Collections.Generic.List<Orden.TipoTopping> toppingsEntregados,
                            bool tieneTortilla)
    {
        if (Estado != EstadoCliente.EsperandoComida) return;
        if (_tacoRecibido) return;

        _tacoRecibido = true;

        bool correcto = OrdenActual.Coincide(carneEntregada, toppingsEntregados, tieneTortilla);
        Debug.Log($"[ClienteIA] Taco recibido — correcto: {correcto}");

        IrseDelPuesto(pago: correcto);
    }

    /// <summary>
    /// Devuelve qué proporción del tiempo de paciencia ya se usó (0 = recién ordenó, 1 = timeout).
    /// Usado por SistemaOrdenes para calcular la propina.
    /// </summary>
    public float ObtenerProporcionTiempoUsado()
    {
        if (OrdenActual == null) return 1f;
        float tiempoUsado = OrdenActual.TiempoDePaciencia - _timerPaciencia;
        return Mathf.Clamp01(tiempoUsado / OrdenActual.TiempoDePaciencia);
    }

    // SALIDA

    private void IrseDelPuesto(bool pago)
    {
        CambiarEstado(EstadoCliente.Saliendo);
        alIrseCliente?.Invoke(this, pago);
        StartCoroutine(CaminarHastaSalida());
    }

    private IEnumerator CaminarHastaSalida()
    {
        if (puntosSalida == null || puntosSalida.Length == 0)
        {
            Destroy(gameObject);
            yield break;
        }

        for (int i = 0; i < puntosSalida.Length; i++)
        {
            Transform punto = puntosSalida[i];
            if (punto == null)
            {
                Debug.LogWarning($"[ClienteIA] Waypoint de salida nulo en índice {i} para {gameObject.name}.");
                yield break;
            }

            while (Vector3.Distance(transform.position, punto.position) > distanciaLlegada)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    punto.position,
                    velocidad * Time.deltaTime
                );

                Vector3 dir = (punto.position - transform.position).normalized;
                if (dir != Vector3.zero)
                    transform.forward = Vector3.Lerp(transform.forward, dir, 10f * Time.deltaTime);

                yield return null;
            }

            transform.position = punto.position;
        }

        Destroy(gameObject);
    }

    // HELPER

    private void CambiarEstado(EstadoCliente nuevoEstado)
    {
        Estado = nuevoEstado;
        Debug.Log($"[ClienteIA] {gameObject.name} → {nuevoEstado}");
    }

    private void AsignarWaypointsSiFaltan()
    {
        bool tieneRuta = puntosDeRuta != null && puntosDeRuta.Length > 0;
        bool tieneSalida = puntosSalida != null && puntosSalida.Length > 0;
        if (tieneRuta && tieneSalida) return;

        var gestor = FindFirstObjectByType<GestorWaypointsClientes>();
        if (gestor == null) return;

        if (!tieneRuta)
            puntosDeRuta = gestor.puntosDeRuta;

        if (!tieneSalida)
            puntosSalida = gestor.puntosSalida;
    }
}