using System.Collections;
using UnityEngine;

public class ClienteIA : MonoBehaviour
{
    public Animator animator;

    public enum EstadoCliente
    {
        Caminando,
        Esperando,
        Ordenando,
        EsperandoComida,
        Saliendo
    }

    [Header("Movimiento")]
    [Tooltip("Puntos que el cliente sigue en orden hasta llegar al mostrador")]
    public Transform[] puntosDeRuta;

    [Tooltip("Velocidad de caminar del cliente")]
    public float velocidad = 2f;

    [Tooltip("Distancia mínima para considerar que llegó a un waypoint")]
    public float distanciaLlegada = 0.2f;

    [Header("Salida")]
    [Tooltip("Waypoints que recorre al salir (enojado o satisfecho)")]
    public Transform[] puntosSalida;

    [Header("Juego")]
    [Tooltip("Día actual. GestorClientes lo asigna al hacer spawn.")]
    public int diaActual = 1;

    // ESTADO INTERNO

    public EstadoCliente Estado { get; private set; } = EstadoCliente.Caminando;

    public Orden OrdenActual { get; private set; }

    private int _indiceWaypoint = 0;
    private float _timerPaciencia = 0f;
    private bool _tacoRecibido = false;

    // EVENTOS

    public static event System.Action<ClienteIA, Orden> alGenerarOrden;
    public static event System.Action<ClienteIA, bool> alIrseCliente;
    public static event System.Action<ClienteIA, float> alActualizarPaciencia;

    // INICIO

    private void Start()
    {
        AsignarWaypointsSiFaltan();
        _indiceWaypoint = 0;
        CambiarEstado(EstadoCliente.Caminando);

        // Asegurarse de arrancar en walking
        ActualizarAnimacion(false);
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

        Vector3 direccion = (destino.position - transform.position).normalized;
        if (direccion != Vector3.zero)
            transform.forward = Vector3.Lerp(transform.forward, direccion, 10f * Time.deltaTime);

        if (Vector3.Distance(transform.position, destino.position) <= distanciaLlegada)
        {
            transform.position = destino.position;
            _indiceWaypoint++;

            if (_indiceWaypoint >= puntosDeRuta.Length)
                LlegarAlMostrador();
        }
    }

    private void LlegarAlMostrador()
    {
        CambiarEstado(EstadoCliente.Ordenando);

        OrdenActual = Orden.GenerarAleatoria(diaActual);
        _timerPaciencia = OrdenActual.TiempoDePaciencia;

        Debug.Log($"[ClienteIA] {gameObject.name} ordenó: {OrdenActual}");
        alGenerarOrden?.Invoke(this, OrdenActual);

        CambiarEstado(EstadoCliente.EsperandoComida);

        // Activar idle al llegar al mostrador
        ActualizarAnimacion(true);
    }

    private void ActualizarPaciencia()
    {
        _timerPaciencia -= Time.deltaTime;

        float proporcion = Mathf.Clamp01(_timerPaciencia / OrdenActual.TiempoDePaciencia);
        alActualizarPaciencia?.Invoke(this, proporcion);

        if (_timerPaciencia <= 0f)
        {
            Debug.Log($"[ClienteIA] {gameObject.name} se fue enojado (timeout).");
            IrseDelPuesto(pago: false);
        }
    }

    // ANIMACIÓN

    /// <summary>
    /// Centraliza todos los cambios de animación.
    /// esperando = true  → Idle (parado en el mostrador)
    /// esperando = false → Walking (caminando hacia o desde el puesto)
    /// </summary>
    private void ActualizarAnimacion(bool esperando)
    {
        if (animator == null) return;
        animator.SetBool("Esperando", esperando);
    }

    // API PÚBLICA

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

        // Volver a walking para la animación de salida
        ActualizarAnimacion(false);

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

    // HELPERS

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