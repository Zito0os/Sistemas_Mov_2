using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class OrdenLista : MonoBehaviour
{
    [Header("Configuracion")]
    public int capacidadMaximaTacos = 4;
    public bool entregarAutomaticamente = true;

    [Header("Estado (solo lectura)")]
    [SerializeField] private List<IngredienteCocina> tacosCargados = new List<IngredienteCocina>();
    [SerializeField] private IngredienteCocina ultimoTacoEntregado = IngredienteCocina.Ninguno;
    [SerializeField] private List<IngredienteCocina> historialTacosEntregados = new List<IngredienteCocina>();

    private CookingStation cookingStation;
    private SistemaOrdenes sistemaOrdenes;

    private void Awake()
    {
        cookingStation = CookingStation.Instance;
        sistemaOrdenes = FindFirstObjectByType<SistemaOrdenes>();
    }

    public bool PuedeLlevarTaco()
    {
        return tacosCargados.Count < capacidadMaximaTacos;
    }

    public void RecibirTaco(SlotCocina slotTaco)
    {
        if (slotTaco == null || !slotTaco.TieneTaco() || !PuedeLlevarTaco())
            return;

        IngredienteCocina carneTaco = slotTaco.ObtenerCarneEnTortilla();
        ultimoTacoEntregado = carneTaco;
        historialTacosEntregados.Add(carneTaco);

        // Agregar a inventario de tacos
        switch (carneTaco)
        {
            case IngredienteCocina.Trompo:
                cookingStation.tacos_trompo++;
                break;
            case IngredienteCocina.Pastor:
                cookingStation.tacos_pastor++;
                break;
            case IngredienteCocina.Picadillo:
                cookingStation.tacos_picadillo++;
                break;
            case IngredienteCocina.Desebrada:
                cookingStation.tacos_desebrada++;
                break;
        }

        tacosCargados.Add(carneTaco);
        slotTaco.EliminarTaco();

        if (entregarAutomaticamente)
            IntentarEntregaAutomaticaSegunOrden();
    }

    public void EntregarOrden()
    {
        tacosCargados.Clear();
    }

    public int ObtenerCantidadTacos()
    {
        return tacosCargados.Count;
    }

    public bool EstiLlena()
    {
        return tacosCargados.Count >= capacidadMaximaTacos;
    }

    public IngredienteCocina ObtenerUltimoTacoEntregado()
    {
        return ultimoTacoEntregado;
    }

    public IReadOnlyList<IngredienteCocina> ObtenerHistorialTacosEntregados()
    {
        return historialTacosEntregados;
    }

    private void IntentarEntregaAutomaticaSegunOrden()
    {
        if (sistemaOrdenes == null)
            sistemaOrdenes = FindFirstObjectByType<SistemaOrdenes>();

        if (sistemaOrdenes == null)
            return;

        if (tacosCargados.Count <= 0)
            return;

        IngredienteCocina carneEnPlato = tacosCargados[0];
        for (int i = 1; i < tacosCargados.Count; i++)
        {
            if (tacosCargados[i] != carneEnPlato)
                return;
        }

        if (!TryConvertirIngredienteAOrdenCarne(carneEnPlato, out Orden.TipoCarne carneObjetivo))
            return;

        if (!sistemaOrdenes.TryObtenerClienteCompatible(carneObjetivo,
                                                        tacosCargados.Count,
                                                        out ClienteIA clienteObjetivo,
                                                        out Orden ordenObjetivo,
                                                        out int cantidadRequerida,
                                                        out int cantidadEntregadaActual))
            return;

        int faltantes = Mathf.Max(0, cantidadRequerida - cantidadEntregadaActual);
        if (faltantes <= 0)
            return;

        List<Orden.TipoTopping> toppingsOrden = new List<Orden.TipoTopping>(ordenObjetivo.Toppings);

        for (int i = 0; i < faltantes; i++)
        {
            sistemaOrdenes.RecibirTacoDelJugador(
                clienteObjetivo,
                ordenObjetivo.Carne,
                toppingsOrden,
                ordenObjetivo.NecesitaTortilla
            );
        }

        EntregarOrden();
    }

    private bool TryConvertirIngredienteAOrdenCarne(IngredienteCocina ingrediente, out Orden.TipoCarne carne)
    {
        switch (ingrediente)
        {
            case IngredienteCocina.Trompo:
                carne = Orden.TipoCarne.Trompo;
                return true;
            case IngredienteCocina.Pastor:
                carne = Orden.TipoCarne.Pastor;
                return true;
            case IngredienteCocina.Picadillo:
                carne = Orden.TipoCarne.Picadillo;
                return true;
            case IngredienteCocina.Desebrada:
                carne = Orden.TipoCarne.Desebrada;
                return true;
            default:
                carne = default;
                return false;
        }
    }

    private IngredienteCocina ConvertirCarneOrdenAIngrediente(Orden.TipoCarne carne)
    {
        switch (carne)
        {
            case Orden.TipoCarne.Trompo:
                return IngredienteCocina.Trompo;
            case Orden.TipoCarne.Pastor:
                return IngredienteCocina.Pastor;
            case Orden.TipoCarne.Picadillo:
                return IngredienteCocina.Picadillo;
            case Orden.TipoCarne.Desebrada:
                return IngredienteCocina.Desebrada;
            default:
                return IngredienteCocina.Ninguno;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
}
