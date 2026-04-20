using UnityEngine;

[DisallowMultipleComponent]
public class SlotAcumulativoCarne : MonoBehaviour
{
    public enum TipoCarneCocinada
    {
        Trompo,
        Pastor,
        Picadillo,
        Desebrada
    }

    [Header("Configuracion")]
    public TipoCarneCocinada tipoCarne;
    public GameObject modeloVisual;
    public string nombreModeloVisual = "Modelo";
    public bool buscarModeloSiFalta = true;

    private CookingStation cookingStation;
    private bool isDragging = false;
    private IngredienteCocina ingredienteAsociado;
    private Vector3 posicionOriginal;
    private Quaternion rotacionOriginal;

    private void Awake()
    {
        cookingStation = CookingStation.Instance;

        if (buscarModeloSiFalta && modeloVisual == null)
        {
            Transform modelo = transform.Find(nombreModeloVisual);
            if (modelo != null)
                modeloVisual = modelo.gameObject;
        }

        posicionOriginal = transform.position;
        rotacionOriginal = transform.rotation;

        ingredienteAsociado = tipoCarne switch
        {
            TipoCarneCocinada.Trompo => IngredienteCocina.Trompo,
            TipoCarneCocinada.Pastor => IngredienteCocina.Pastor,
            TipoCarneCocinada.Picadillo => IngredienteCocina.Picadillo,
            TipoCarneCocinada.Desebrada => IngredienteCocina.Desebrada,
            _ => IngredienteCocina.Ninguno
        };

        ActualizarVisual();
    }

    private void Update()
    {
        if (cookingStation == null)
            cookingStation = CookingStation.Instance;

        ActualizarVisual();
    }

    private void ActualizarVisual()
    {
        if (modeloVisual == null)
            return;

        if (cookingStation == null)
        {
            modeloVisual.SetActive(false);
            return;
        }

        modeloVisual.SetActive(ObtenerCantidadCocinada() > 0);
    }

    private int ObtenerCantidadCocinada()
    {
        switch (tipoCarne)
        {
            case TipoCarneCocinada.Trompo:
                return cookingStation.carne_trompo_cocinada;

            case TipoCarneCocinada.Pastor:
                return cookingStation.carne_pastor_cocinada;

            case TipoCarneCocinada.Picadillo:
                return cookingStation.carne_picadillo_cocinada;

            case TipoCarneCocinada.Desebrada:
                return cookingStation.carne_desebrada_cocinada;

            default:
                return 0;
        }
    }

    // Metodos para drag y drop
    public bool PuedeSertomada()
    {
        return ObtenerCantidadCocinada() > 0;
    }

    public void IniciarDrag()
    {
        if (!PuedeSertomada())
            return;

        isDragging = true;
        posicionOriginal = transform.position;
        rotacionOriginal = transform.rotation;
    }

    public void FinalizarDrag()
    {
        isDragging = false;
        transform.position = posicionOriginal;
        transform.rotation = rotacionOriginal;
    }

    public bool EstaDragueando()
    {
        return isDragging;
    }

    public void DevolverCarne()
    {
        isDragging = false;
        transform.position = posicionOriginal;
        transform.rotation = rotacionOriginal;

        // Consume la carne del inventario
        switch (tipoCarne)
        {
            case TipoCarneCocinada.Trompo:
                if (cookingStation.carne_trompo_cocinada > 0)
                    cookingStation.carne_trompo_cocinada--;
                break;
            case TipoCarneCocinada.Pastor:
                if (cookingStation.carne_pastor_cocinada > 0)
                    cookingStation.carne_pastor_cocinada--;
                break;
            case TipoCarneCocinada.Picadillo:
                if (cookingStation.carne_picadillo_cocinada > 0)
                    cookingStation.carne_picadillo_cocinada--;
                break;
            case TipoCarneCocinada.Desebrada:
                if (cookingStation.carne_desebrada_cocinada > 0)
                    cookingStation.carne_desebrada_cocinada--;
                break;
        }

        ActualizarVisual();
    }

    public IngredienteCocina ObtenerIngrediente()
    {
        return ingredienteAsociado;
    }
}
