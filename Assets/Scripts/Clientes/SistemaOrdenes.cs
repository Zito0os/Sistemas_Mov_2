using System.Collections.Generic;
using UnityEngine;

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

    private Dictionary<ClienteIA, Orden> _ordenesActivas = new Dictionary<ClienteIA, Orden>();
    private Dictionary<ClienteIA, int> _cantidadRequeridaPorCliente = new Dictionary<ClienteIA, int>();
    private Dictionary<ClienteIA, int> _cantidadEntregadaPorCliente = new Dictionary<ClienteIA, int>();

    // EVENTOS

    /// <summary>Una nueva orden llegó. La UI escucha esto para mostrarla en pantalla.</summary>
    public static event System.Action<ClienteIA, Orden, int> alRecibirOrden;

    public static event System.Action<Orden, int, bool, int> alCompletarOrden;

    /// <summary>Una orden fue cancelada por timeout (el cliente se fue sin pagar).</summary>
    public static event System.Action<Orden> alCancelarOrden;

    /// <summary>Progreso de la orden activa: entregados/requeridos.</summary>
    public static event System.Action<ClienteIA, Orden, int, int> alActualizarProgresoOrden;

    // SUSCRIPCIONES

    private void OnEnable()
    {
        ClienteIA.alGenerarOrden += AlGenerarOrdenCliente;
        ClienteIA.alIrseCliente  += AlIrseCliente;
    }

    private void OnDisable()
    {
        ClienteIA.alGenerarOrden -= AlGenerarOrdenCliente;
        ClienteIA.alIrseCliente  -= AlIrseCliente;
    }

    // UPDATE — teclas de prueba

    private void Update()
    {
        if (!modoDebug) return;

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
        Debug.Log($"[SistemaOrdenes] Órdenes activas: {_ordenesActivas.Count} | Presiona E para entregar el taco correcto.");

        alRecibirOrden?.Invoke(cliente, orden, cantidadRequerida);
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

        if (!pago)
        {
            Debug.Log($"[SistemaOrdenes] Orden {orden.IDOrden} cancelada por timeout.");
            alCancelarOrden?.Invoke(orden);
        }
    }

    // RECEPCIÓN DEL TACO

    /// <summary>
    /// El jugador entrega tacos al cliente.
    /// tacosEntregadosAhora: cuántos tacos hay en el plato al momento de entregar.
    /// Si es menor a los requeridos → entrega parcial con penalización proporcional.
    /// </summary>
    public void RecibirTacoDelJugador(ClienteIA cliente,
                                      Orden.TipoCarne carneEntregada,
                                      List<Orden.TipoTopping> toppings,
                                      bool tieneTortilla,
                                      int tacosEntregadosAhora = 1)
    {
        if (!_ordenesActivas.ContainsKey(cliente))
        {
            Debug.LogWarning($"[SistemaOrdenes] {cliente.name} no tiene orden activa.");
            return;
        }

        Orden orden = _ordenesActivas[cliente];
        int cantidadRequerida = _cantidadRequeridaPorCliente.TryGetValue(cliente, out int req) ? Mathf.Max(1, req) : 1;
        int cantidadEntregada = _cantidadEntregadaPorCliente.TryGetValue(cliente, out int ent) ? Mathf.Max(0, ent) : 0;

        bool correcto = orden.Coincide(carneEntregada, toppings, tieneTortilla);

        if (!correcto)
        {
            Debug.Log($"[SistemaOrdenes] Taco entregado — ✗ INCORRECTO | Total: $0");
            cliente.RecibirTaco(carneEntregada, toppings, tieneTortilla);
            alCompletarOrden?.Invoke(orden, 0, false, cantidadRequerida);
            _ordenesActivas.Remove(cliente);
            _cantidadRequeridaPorCliente.Remove(cliente);
            _cantidadEntregadaPorCliente.Remove(cliente);
            return;
        }

        // Cuántos tacos se cuentan en esta entrega (no más de los que faltan)
        int faltantesAntes = Mathf.Max(0, cantidadRequerida - cantidadEntregada);
        int tacosAContar   = Mathf.Clamp(tacosEntregadosAhora, 1, faltantesAntes);

        cantidadEntregada += tacosAContar;
        _cantidadEntregadaPorCliente[cliente] = cantidadEntregada;
        alActualizarProgresoOrden?.Invoke(cliente, orden, cantidadEntregada, cantidadRequerida);

        bool esEntregaCompleta = cantidadEntregada >= cantidadRequerida;

        // ── Calcular pago ──────────────────────────────────────────────────────
        int precioBaseTotal = orden.PrecioBase * cantidadRequerida;
        int pagoFinal;

        if (esEntregaCompleta)
        {
            // Entrega completa: precio base total + propina según velocidad
            float proporcionTiempo = cliente.ObtenerProporcionTiempoUsado();
            int propinaPorTaco     = orden.CalcularPropina(proporcionTiempo);
            int propinaTotal       = propinaPorTaco * cantidadRequerida;
            pagoFinal = precioBaseTotal + propinaTotal;

            Debug.Log($"[SistemaOrdenes] Pedido completado — ✓ CORRECTO | " +
                      $"Cantidad: {cantidadRequerida} | Base: ${precioBaseTotal} | " +
                      $"Propina: ${propinaTotal} | Total: ${pagoFinal}");
        }
        else
        {
            // entrega parcial. solo la fracción entregada del precio base, sin propina.
            // ej: pide 3 tacos a $20 c/u = $60. Entrega 2 → paga (2/3) × $60 = $40.
            float fraccion = (float)cantidadEntregada / cantidadRequerida;
            pagoFinal      = Mathf.FloorToInt(precioBaseTotal * fraccion);

            Debug.Log($"[SistemaOrdenes] Entrega PARCIAL — {cantidadEntregada}/{cantidadRequerida} tacos | " +
                      $"Fracción: {fraccion:P0} | Pago: ${pagoFinal} (sin propina) | " +
                      $"Penalización: ${precioBaseTotal - pagoFinal}");
        }

        cliente.RecibirTaco(carneEntregada, toppings, tieneTortilla);
        alCompletarOrden?.Invoke(orden, pagoFinal, esEntregaCompleta, cantidadRequerida);

        _ordenesActivas.Remove(cliente);
        _cantidadRequeridaPorCliente.Remove(cliente);
        _cantidadEntregadaPorCliente.Remove(cliente);
    }

    // SIMULACIÓN DE PRUEBA (tecla E)

    private void EntregarTacoSimulado()
    {
        if (_ordenesActivas.Count == 0)
        {
            Debug.Log("[SistemaOrdenes] No hay órdenes activas para entregar.");
            return;
        }

        ClienteIA clienteObjetivo = null;
        Orden ordenObjetivo = null;

        foreach (var par in _ordenesActivas)
        {
            if (par.Key.Estado == ClienteIA.EstadoCliente.EsperandoComida)
            {
                clienteObjetivo = par.Key;
                ordenObjetivo   = par.Value;
                break;
            }
        }

        if (clienteObjetivo == null)
        {
            Debug.Log("[SistemaOrdenes] Ningún cliente está esperando comida aún.");
            return;
        }

        List<Orden.TipoTopping> toppingsPerfectos = new List<Orden.TipoTopping>(ordenObjetivo.Toppings);
        int cantidadRequerida = _cantidadRequeridaPorCliente.TryGetValue(clienteObjetivo, out int req) ? Mathf.Max(1, req) : 1;
        int cantidadEntregada = _cantidadEntregadaPorCliente.TryGetValue(clienteObjetivo, out int ent) ? Mathf.Max(0, ent) : 0;
        int faltantes = Mathf.Max(1, cantidadRequerida - cantidadEntregada);

        Debug.Log($"[SistemaOrdenes] [DEBUG - E] Entregando taco simulado para {clienteObjetivo.name} " +
                  $"→ {ordenObjetivo.Carne} x{faltantes} + {string.Join(", ", toppingsPerfectos)}");

        // La simulación siempre entrega todo lo que falta (entrega completa)
        RecibirTacoDelJugador(
            clienteObjetivo,
            ordenObjetivo.Carne,
            toppingsPerfectos,
            ordenObjetivo.NecesitaTortilla,
            tacosEntregadosAhora: faltantes
        );
    }

    // QUERIES

    public int CantidadOrdenesActivas => _ordenesActivas.Count;

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

    public bool TryObtenerClienteEsperando(out ClienteIA cliente, out Orden orden,
                                            out int cantidadRequerida, out int cantidadEntregada)
    {
        foreach (var par in _ordenesActivas)
        {
            if (par.Key != null && par.Key.Estado == ClienteIA.EstadoCliente.EsperandoComida)
            {
                cliente           = par.Key;
                orden             = par.Value;
                cantidadRequerida = ObtenerCantidadRequerida(cliente);
                cantidadEntregada = ObtenerCantidadEntregada(cliente);
                return true;
            }
        }

        cliente           = null;
        orden             = null;
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

            
            if (faltantes <= 0 || cantidadEnPlato <= 0)
                continue;

            cliente           = candidato;
            orden             = ordenCandidata;
            cantidadRequerida = requerida;
            cantidadEntregada = entregada;
            return true;
        }

        cliente           = null;
        orden             = null;
        cantidadRequerida = 0;
        cantidadEntregada = 0;
        return false;
    }
}