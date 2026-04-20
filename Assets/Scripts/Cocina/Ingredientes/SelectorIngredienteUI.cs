using UnityEngine;

public class SelectorIngredienteUI : MonoBehaviour
{
    [Header("Referencia opcional")]
    public CookingStation cookingStation;

    private void Awake()
    {
        if (cookingStation == null)
            cookingStation = CookingStation.Instance;
    }

    public void SeleccionarIngredientePorIndice(int indice)
    {
        if (cookingStation == null)
            cookingStation = CookingStation.Instance;

        if (cookingStation == null)
        {
            Debug.LogWarning("No se encontro CookingStation para seleccionar ingrediente.");
            return;
        }

        if (indice < 0 || indice > 4)
        {
            Debug.LogWarning($"Indice de ingrediente invalido: {indice}. Debe estar entre 0 y 4.");
            return;
        }

        cookingStation.ingrediente_seleccionado = indice.ToString();
    }

    public void SeleccionarTrompo()
    {
        SeleccionarIngredientePorIndice(0);
    }

    public void SeleccionarPastor()
    {
        SeleccionarIngredientePorIndice(1);
    }

    public void SeleccionarPicadillo()
    {
        SeleccionarIngredientePorIndice(2);
    }

    public void SeleccionarDesebrada()
    {
        SeleccionarIngredientePorIndice(3);
    }

    public void SeleccionarTortilla()
    {
        SeleccionarIngredientePorIndice(4);
    }
}
