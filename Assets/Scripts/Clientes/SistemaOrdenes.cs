using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SistemaOrdenes — Puente entre los clientes y la cocina.
///
/// Responsabilidades:
///   - Escuchar cuando un cliente genera una orden
///   - Mantener la lista de órdenes activas
///   - Recibir el taco terminado del Dev A (o simularlo con tecla E)
///   - Evaluar si el taco es correcto
///   - Calcular el pago + propina y notificar al GestorEconomia
///   - Notificar a la UI qué órdenes están activas
/// </summary>
public class SistemaOrdenes : MonoBehaviour
{
    // INSPECTOR

    [Header("Pruebas")]
    [Tooltip("Activa los atajos de teclado para testing sin cocina real")]
    public bool modoDebug = true;

    // ESTADO INTERNO

    /// <summary>
    /// Diccionario de órdenes activas: ClienteIA → Orden.
    /// Permite tener varios clientes esperando al mismo tiempo.
    /// </summary>
    private Dictionary<ClienteIA, Orden> _ordenesActivas = new Dictionary<ClienteIA, Orden>();

    // EVENTOS

    /// <summary>Una nueva orden llegó. La UI escucha esto para mostrarla en pantalla.</summary>
    public static event System.Action<ClienteIA, Orden> alRecibirOrden;

    /// <summary>
    /// Una orden fue completada.
    /// int pagoTotal  = precio base + propina
    /// bool correcto  = si el taco coincidía con la orden
    /// </summary>
    public static event System.Action<Orden, int, bool> alCompletarOrden;

    /// <summary>Una orden fue cancelada por timeout (el cliente se fue sin pagar).</summary>
    public static event System.Action<Orden> alCancelarOrden;

    // SUSCRIPCIONES

    private void OnEnable()
    {
        ClienteIA.alGenerarOrden += AlGenerarOrdenCliente;
        ClienteIA.alIrseCliente += AlIrseCliente;
    }

    private void OnDisable()
    {
        ClienteIA.alGenerarOrden -= AlGenerarOrdenCliente;
        ClienteIA.alIrseCliente -= AlIrseCliente;
    }

    // UPDATE — teclas de prueba

    private void Update()
    {
        if (!modoDebug) return;

        // E → entregar taco perfecto al primer cliente que esté esperando
        if (Input.GetKeyDown(KeyCode.E))
            EntregarTacoSimulado();
    }

    // LISTENERS

    /// <summary>Un ClienteIA llegó al mostrador y generó su orden.</summary>
    private void AlGenerarOrdenCliente(ClienteIA cliente, Orden orden)
    {
        if (_ordenesActivas.ContainsKey(cliente))
        {
            Debug.LogWarning($"[SistemaOrdenes] {cliente.name} ya tiene una orden activa.");
            return;
        }

        _ordenesActivas[cliente] = orden;

        Debug.Log($"[SistemaOrdenes] Nueva orden de {cliente.name}: {orden}");
        Debug.Log($"[SistemaOrdenes] Órdenes activas: {_ordenesActivas.Count} | " +
                  $"Presiona E para entregar el taco correcto.");

        alRecibirOrden?.Invoke(cliente, orden);
    }

    /// <summary>Un cliente se fue — si fue timeout, cancelamos su orden.</summary>
    private void AlIrseCliente(ClienteIA cliente, bool pago)
    {
        if (!_ordenesActivas.ContainsKey(cliente)) return;

        Orden orden = _ordenesActivas[cliente];
        _ordenesActivas.Remove(cliente);

        // Si no pagó fue porque se fue por timeout (ClienteIA ya lo manejó)
        if (!pago)
        {
            Debug.Log($"[SistemaOrdenes] Orden {orden.IDOrden} cancelada por timeout.");
            alCancelarOrden?.Invoke(orden);
        }
    }

    // RECEPCIÓN REAL DEL TACO (Dev A lo llamará aquí)

    /// <summary>
    /// El Dev A llama este método cuando el jugador termina de armar un taco.
    /// Por ahora también se llama internamente con la tecla E para simular la entrega.
    ///
    /// cliente         → el ClienteIA al que se le entrega
    /// carneEntregada  → qué tipo de carne lleva el taco
    /// toppings        → lista de toppings que se pusieron
    /// tieneTortilla   → si se calentó la tortilla
    /// </summary>
    public void RecibirTacoDelJugador(ClienteIA cliente,
                                      Orden.TipoCarne carneEntregada,
                                      List<Orden.TipoTopping> toppings,
                                      bool tieneTortilla)
    {
        if (!_ordenesActivas.ContainsKey(cliente))
        {
            Debug.LogWarning($"[SistemaOrdenes] {cliente.name} no tiene orden activa.");
            return;
        }

        Orden orden = _ordenesActivas[cliente];

        // Evaluar si el taco es correcto
        bool correcto = orden.Coincide(carneEntregada, toppings, tieneTortilla);

        // Calcular pago
        float proporcionTiempo = cliente.ObtenerProporcionTiempoUsado();
        int propina = correcto ? orden.CalcularPropina(proporcionTiempo) : 0;
        int pagoTotal = correcto ? orden.PrecioBase + propina : 0;

        // Log detallado
        string resultadoStr = correcto ? "✓ CORRECTO" : "✗ INCORRECTO";
        Debug.Log($"[SistemaOrdenes] Taco entregado — {resultadoStr} | " +
                  $"Base: ${orden.PrecioBase} | Propina: ${propina} | Total: ${pagoTotal}");

        // Notificar al cliente (animación de salida satisfecha o no)
        cliente.RecibirTaco(carneEntregada, toppings, tieneTortilla);

        // Notificar a GestorEconomia y UI
        alCompletarOrden?.Invoke(orden, pagoTotal, correcto);

        // Quitar de órdenes activas
        _ordenesActivas.Remove(cliente);
    }

    // SIMULACIÓN DE PRUEBA (tecla E)

    /// <summary>
    /// Simula entregar el taco perfecto al primer cliente que esté esperando.
    /// Construye el taco exactamente igual a la orden del cliente.
    /// Solo activo cuando modoDebug = true.
    /// </summary>
    private void EntregarTacoSimulado()
    {
        if (_ordenesActivas.Count == 0)
        {
            Debug.Log("[SistemaOrdenes] No hay órdenes activas para entregar.");
            return;
        }

        // Toma el primer cliente de la lista
        ClienteIA clienteObjetivo = null;
        Orden ordenObjetivo = null;

        foreach (var par in _ordenesActivas)
        {
            if (par.Key.Estado == ClienteIA.EstadoCliente.EsperandoComida)
            {
                clienteObjetivo = par.Key;
                ordenObjetivo = par.Value;
                break;
            }
        }

        if (clienteObjetivo == null)
        {
            Debug.Log("[SistemaOrdenes] Ningún cliente está esperando comida aún.");
            return;
        }

        // Construir taco idéntico a la orden
        List<Orden.TipoTopping> toppingsPerfectos = new List<Orden.TipoTopping>(ordenObjetivo.Toppings);

        Debug.Log($"[SistemaOrdenes] [DEBUG - E] Entregando taco simulado para {clienteObjetivo.name} " +
                  $"→ {ordenObjetivo.Carne} + {string.Join(", ", toppingsPerfectos)}");

        RecibirTacoDelJugador(
            clienteObjetivo,
            ordenObjetivo.Carne,
            toppingsPerfectos,
            ordenObjetivo.NecesitaTortilla
        );
    }

    // QUERIES

    /// <summary>Cuántas órdenes están activas en este momento.</summary>
    public int CantidadOrdenesActivas => _ordenesActivas.Count;

    /// <summary>Devuelve la orden de un cliente específico, o null si no tiene.</summary>
    public Orden ObtenerOrden(ClienteIA cliente)
    {
        _ordenesActivas.TryGetValue(cliente, out Orden orden);
        return orden;
    }
}