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

    [Header("Cantidad por orden")]
    [Tooltip("Cantidad minima de tacos que puede pedir un cliente")]
    public int cantidadMinimaPorOrden = 1;

    [Tooltip("Cantidad maxima de tacos que puede pedir un cliente")]
    public int cantidadMaximaPorOrden = 3;

    // ESTADO INTERNO

    /// <summary>
    /// Diccionario de órdenes activas: ClienteIA → Orden.
    /// Permite tener varios clientes esperando al mismo tiempo.
    /// </summary>
    private Dictionary<ClienteIA, Orden> _ordenesActivas = new Dictionary<ClienteIA, Orden>();
    private Dictionary<ClienteIA, int> _cantidadRequeridaPorCliente = new Dictionary<ClienteIA, int>();
    private Dictionary<ClienteIA, int> _cantidadEntregadaPorCliente = new Dictionary<ClienteIA, int>();

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

    /// <summary>Progreso de la orden activa: entregados/requeridos.</summary>
    public static event System.Action<ClienteIA, Orden, int, int> alActualizarProgresoOrden;

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
        int min = Mathf.Max(1, cantidadMinimaPorOrden);
        int max = Mathf.Max(min, cantidadMaximaPorOrden);
        int cantidadRequerida = Random.Range(min, max + 1);
        _cantidadRequeridaPorCliente[cliente] = cantidadRequerida;
        _cantidadEntregadaPorCliente[cliente] = 0;

        Debug.Log($"[SistemaOrdenes] Nueva orden de {cliente.name}: {orden} | Cantidad: {cantidadRequerida}");
        Debug.Log($"[SistemaOrdenes] Órdenes activas: {_ordenesActivas.Count} | " +
                  $"Presiona E para entregar el taco correcto.");

        alRecibirOrden?.Invoke(cliente, orden);
        alActualizarProgresoOrden?.Invoke(cliente, orden, 0, cantidadRequerida);
    }

    /// <summary>Un cliente se fue — si fue timeout, cancelamos su orden.</summary>
    private void AlIrseCliente(ClienteIA cliente, bool pago)
    {
        if (!_ordenesActivas.ContainsKey(cliente)) return;

        Orden orden = _ordenesActivas[cliente];
        _ordenesActivas.Remove(cliente);
        _cantidadRequeridaPorCliente.Remove(cliente);
        _cantidadEntregadaPorCliente.Remove(cliente);

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
        int cantidadRequerida = _cantidadRequeridaPorCliente.TryGetValue(cliente, out int req) ? Mathf.Max(1, req) : 1;
        int cantidadEntregada = _cantidadEntregadaPorCliente.TryGetValue(cliente, out int ent) ? Mathf.Max(0, ent) : 0;

        // Evaluar si el taco es correcto
        bool correcto = orden.Coincide(carneEntregada, toppings, tieneTortilla);

        if (!correcto)
        {
            // Si un taco llega incorrecto, se cierra la orden como fallo.
            Debug.Log($"[SistemaOrdenes] Taco entregado — ✗ INCORRECTO | Total: $0");
            cliente.RecibirTaco(carneEntregada, toppings, tieneTortilla);
            alCompletarOrden?.Invoke(orden, 0, false);
            _ordenesActivas.Remove(cliente);
            _cantidadRequeridaPorCliente.Remove(cliente);
            _cantidadEntregadaPorCliente.Remove(cliente);
            return;
        }

        cantidadEntregada++;
        _cantidadEntregadaPorCliente[cliente] = cantidadEntregada;
        alActualizarProgresoOrden?.Invoke(cliente, orden, cantidadEntregada, cantidadRequerida);

        if (cantidadEntregada < cantidadRequerida)
        {
            Debug.Log($"[SistemaOrdenes] Taco correcto ({cantidadEntregada}/{cantidadRequerida}) para {cliente.name}.");
            return;
        }

        // Calcular pago
        float proporcionTiempo = cliente.ObtenerProporcionTiempoUsado();
        int precioBaseTotal = orden.PrecioBase * cantidadRequerida;
        int propinaPorTaco = orden.CalcularPropina(proporcionTiempo);
        int propinaTotal = propinaPorTaco * cantidadRequerida;
        int pagoTotal = precioBaseTotal + propinaTotal;

        // Log detallado
        Debug.Log($"[SistemaOrdenes] Pedido completado — ✓ CORRECTO | " +
                  $"Cantidad: {cantidadRequerida} | Base: ${precioBaseTotal} | Propina: ${propinaTotal} | Total: ${pagoTotal}");

        // Notificar al cliente (animación de salida satisfecha o no)
        cliente.RecibirTaco(carneEntregada, toppings, tieneTortilla);

        // Notificar a GestorEconomia y UI
        alCompletarOrden?.Invoke(orden, pagoTotal, true);

        // Quitar de órdenes activas
        _ordenesActivas.Remove(cliente);
        _cantidadRequeridaPorCliente.Remove(cliente);
        _cantidadEntregadaPorCliente.Remove(cliente);
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
        int cantidadRequerida = _cantidadRequeridaPorCliente.TryGetValue(clienteObjetivo, out int req) ? Mathf.Max(1, req) : 1;
        int cantidadEntregada = _cantidadEntregadaPorCliente.TryGetValue(clienteObjetivo, out int ent) ? Mathf.Max(0, ent) : 0;
        int faltantes = Mathf.Max(1, cantidadRequerida - cantidadEntregada);

        Debug.Log($"[SistemaOrdenes] [DEBUG - E] Entregando taco simulado para {clienteObjetivo.name} " +
                  $"→ {ordenObjetivo.Carne} x{faltantes} + {string.Join(", ", toppingsPerfectos)}");

        for (int i = 0; i < faltantes; i++)
        {
            RecibirTacoDelJugador(
                clienteObjetivo,
                ordenObjetivo.Carne,
                toppingsPerfectos,
                ordenObjetivo.NecesitaTortilla
            );
        }
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

    public int ObtenerCantidadRequerida(ClienteIA cliente)
    {
        return _cantidadRequeridaPorCliente.TryGetValue(cliente, out int cantidad) ? Mathf.Max(1, cantidad) : 1;
    }

    public int ObtenerCantidadEntregada(ClienteIA cliente)
    {
        return _cantidadEntregadaPorCliente.TryGetValue(cliente, out int cantidad) ? Mathf.Max(0, cantidad) : 0;
    }

    public bool TryObtenerClienteEsperando(out ClienteIA cliente, out Orden orden, out int cantidadRequerida, out int cantidadEntregada)
    {
        foreach (var par in _ordenesActivas)
        {
            if (par.Key != null && par.Key.Estado == ClienteIA.EstadoCliente.EsperandoComida)
            {
                cliente = par.Key;
                orden = par.Value;
                cantidadRequerida = ObtenerCantidadRequerida(cliente);
                cantidadEntregada = ObtenerCantidadEntregada(cliente);
                return true;
            }
        }

        cliente = null;
        orden = null;
        cantidadRequerida = 0;
        cantidadEntregada = 0;
        return false;
    }

    public bool TryObtenerClienteCompatible(Orden.TipoCarne carne,
                                            int cantidadEnPlato,
                                            out ClienteIA cliente,
                                            out Orden orden,
                                            out int cantidadRequerida,
                                            out int cantidadEntregada)
    {
        foreach (var par in _ordenesActivas)
        {
            ClienteIA candidato = par.Key;
            if (candidato == null || candidato.Estado != ClienteIA.EstadoCliente.EsperandoComida)
                continue;

            Orden ordenCandidata = par.Value;
            if (ordenCandidata == null || ordenCandidata.Carne != carne)
                continue;

            int requerida = ObtenerCantidadRequerida(candidato);
            int entregada = ObtenerCantidadEntregada(candidato);
            int faltantes = Mathf.Max(0, requerida - entregada);

            if (faltantes != cantidadEnPlato)
                continue;

            cliente = candidato;
            orden = ordenCandidata;
            cantidadRequerida = requerida;
            cantidadEntregada = entregada;
            return true;
        }

        cliente = null;
        orden = null;
        cantidadRequerida = 0;
        cantidadEntregada = 0;
        return false;
    }
}