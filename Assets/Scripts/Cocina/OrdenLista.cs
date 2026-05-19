using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class OrdenLista : MonoBehaviour
{
    [Header("Configuracion")]
    public int capacidadMaximaTacos = 4;

    [Tooltip("Si está en FALSE el jugador debe hacer HOLD sobre la OrdenLista para entregar.")]
    public bool entregarAutomaticamente = true;

    [Header("Modelos visuales de tacos en el plato")]
    [Tooltip("Arrastra aquí los GameObjects hijo que representan cada taco en el plato (máx 4). " +
             "El índice 0 es el primer taco visible, el 3 el último.")]
    public GameObject[] modelosTacos;

    [Header("Estado (solo lectura)")]
    [SerializeField] private List<IngredienteCocina> tacosCargados = new List<IngredienteCocina>();
    [SerializeField] private IngredienteCocina ultimoTacoEntregado = IngredienteCocina.Ninguno;
    [SerializeField] private List<IngredienteCocina> historialTacosEntregados = new List<IngredienteCocina>();
    [SerializeField] private bool _tieneSalsa = false;

    private CookingStation cookingStation;
    private SistemaOrdenes sistemaOrdenes;

    // ── EVENTOS ───────────────────────────────────────────────────────────────

    public static event System.Action<OrdenLista> alListaActualizada;
    public static event System.Action<OrdenLista> alOrdenEntregada;

    // ── CICLO ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        cookingStation = CookingStation.Instance;
        sistemaOrdenes = FindFirstObjectByType<SistemaOrdenes>();

        // Empezar con todos los modelos ocultos
        ActualizarModelosTacos();
    }

    // ── API PÚBLICA ───────────────────────────────────────────────────────────

    public bool PuedeLlevarTaco() => tacosCargados.Count < capacidadMaximaTacos;

    public void RecibirTaco(SlotCocina slotTaco)
    {
        if (slotTaco == null || !slotTaco.TieneTaco() || !PuedeLlevarTaco())
            return;

        IngredienteCocina carneTaco = slotTaco.ObtenerCarneEnTortilla();
        ultimoTacoEntregado = carneTaco;
        historialTacosEntregados.Add(carneTaco);

        switch (carneTaco)
        {
            case IngredienteCocina.Trompo:    cookingStation.tacos_trompo++;    break;
            case IngredienteCocina.Pastor:    cookingStation.tacos_pastor++;    break;
            case IngredienteCocina.Picadillo: cookingStation.tacos_picadillo++; break;
            case IngredienteCocina.Desebrada: cookingStation.tacos_desebrada++; break;
        }

        tacosCargados.Add(carneTaco);
        slotTaco.EliminarTaco();

        ActualizarModelosTacos();   // ← mostrar el taco recién agregado
        alListaActualizada?.Invoke(this);

        if (entregarAutomaticamente)
            IntentarEntregaSegunOrden(esManual: false);
    }

    public void AplicarSalsa()
    {
        _tieneSalsa = true;
        Debug.Log("[OrdenLista] Salsa aplicada.");
        alListaActualizada?.Invoke(this);
    }

    public bool TieneSalsa() => _tieneSalsa;

    public void EntregarOrdenManual()
    {
        if (tacosCargados.Count <= 0)
        {
            Debug.Log("[OrdenLista] No hay tacos cargados para entregar.");
            return;
        }
        IntentarEntregaSegunOrden(esManual: true);
    }

    /// <summary>
    /// Limpia el plato (llamado internamente tras una entrega exitosa).
    /// </summary>
    public void EntregarOrden()
    {
        tacosCargados.Clear();
        _tieneSalsa = false;
        ActualizarModelosTacos();   // ← ocultar todos los tacos
        alOrdenEntregada?.Invoke(this);
    }

    public int  ObtenerCantidadTacos()  => tacosCargados.Count;
    public bool EstiLlena()             => tacosCargados.Count >= capacidadMaximaTacos;

    public IngredienteCocina ObtenerUltimoTacoEntregado() => ultimoTacoEntregado;

    public IReadOnlyList<IngredienteCocina> ObtenerHistorialTacosEntregados()
        => historialTacosEntregados;

    // ── VISUAL TACOS ──────────────────────────────────────────────────────────

    /// <summary>
    /// Activa/desactiva los GameObjects hijo de tacos según cuántos hay cargados.
    /// modelosTacos[0] = primer taco, modelosTacos[3] = cuarto taco.
    /// </summary>
    private void ActualizarModelosTacos()
    {
        if (modelosTacos == null || modelosTacos.Length == 0) return;

        for (int i = 0; i < modelosTacos.Length; i++)
        {
            if (modelosTacos[i] == null) continue;
            modelosTacos[i].SetActive(i < tacosCargados.Count);
        }
    }

    // ── ENTREGA ───────────────────────────────────────────────────────────────

    private void IntentarEntregaSegunOrden(bool esManual)
    {
        if (sistemaOrdenes == null)
            sistemaOrdenes = FindFirstObjectByType<SistemaOrdenes>();
        if (sistemaOrdenes == null) return;
        if (tacosCargados.Count <= 0) return;

        // Todos los tacos del plato deben ser del mismo tipo
        IngredienteCocina carneEnPlato = tacosCargados[0];
        for (int i = 1; i < tacosCargados.Count; i++)
        {
            if (tacosCargados[i] != carneEnPlato)
            {
                if (esManual)
                    Debug.Log("[OrdenLista] Los tacos cargados no son del mismo tipo.");
                return;
            }
        }

        if (!TryConvertirIngredienteAOrdenCarne(carneEnPlato, out Orden.TipoCarne carneObjetivo))
            return;

        if (!sistemaOrdenes.TryObtenerClienteCompatible(carneObjetivo,
                                                        tacosCargados.Count,
                                                        out ClienteIA clienteObjetivo,
                                                        out Orden ordenObjetivo,
                                                        out int cantidadRequerida,
                                                        out int cantidadEntregadaActual))
        {
            if (esManual)
                Debug.Log("[OrdenLista] No hay cliente que quiera este taco.");
            return;
        }

        // Incluir la salsa como topping si el jugador la aplicó
        List<Orden.TipoTopping> toppings = new List<Orden.TipoTopping>(ordenObjetivo.Toppings);
        if (_tieneSalsa && !toppings.Contains(Orden.TipoTopping.Salsa))
            toppings.Add(Orden.TipoTopping.Salsa);

        int tacosEnPlato = tacosCargados.Count;

        // Pasar cuántos tacos entrega el jugador ahora para que SistemaOrdenes
        // calcule la penalización si es entrega parcial.
        sistemaOrdenes.RecibirTacoDelJugador(
            clienteObjetivo,
            ordenObjetivo.Carne,
            toppings,
            ordenObjetivo.NecesitaTortilla,
            tacosEntregadosAhora: tacosEnPlato   // ← cantidad real en el plato
        );

        EntregarOrden();
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────

    private bool TryConvertirIngredienteAOrdenCarne(IngredienteCocina ingrediente, out Orden.TipoCarne carne)
    {
        switch (ingrediente)
        {
            case IngredienteCocina.Trompo:    carne = Orden.TipoCarne.Trompo;    return true;
            case IngredienteCocina.Pastor:    carne = Orden.TipoCarne.Pastor;    return true;
            case IngredienteCocina.Picadillo: carne = Orden.TipoCarne.Picadillo; return true;
            case IngredienteCocina.Desebrada: carne = Orden.TipoCarne.Desebrada; return true;
            default: carne = default; return false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
}