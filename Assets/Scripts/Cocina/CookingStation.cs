using UnityEngine;

[DisallowMultipleComponent]
public class CookingStation : MonoBehaviour
{
    private static CookingStation instance;
    public static CookingStation Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<CookingStation>();

            return instance;
        }
    }

    public int cantidad_tortillas;

    public int carne_pastor;
    public int carne_picadillo;
    public int carne_desebrada;
    public int carne_trompo;

    public int carne_pastor_cocinada;
    public int carne_picadillo_cocinada;
    public int carne_desebrada_cocinada;
    public int carne_trompo_cocinada;

    public int tacos_pastor;
    public int tacos_trompo;
    public int tacos_picadillo;
    public int tacos_desebrada;


    public int tortilla_cruda;
    public int tortilla_cocinada;

    //trompo = 0
    //pastor = 1
    //picadillo = 2
    //desebrada = 3
    

    //tortillas = 4

    public string ingrediente_seleccionado;


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void AgregarTortilla(int cantidad = 1)
    {
        int valor = Mathf.Max(0, cantidad);
        cantidad_tortillas += valor;
        tortilla_cruda += valor;
    }

    public void AgregarCarnePastor(int cantidad = 1)
    {
        carne_pastor += Mathf.Max(0, cantidad);
    }

    public void AgregarCarnePicadillo(int cantidad = 1)
    {
        carne_picadillo += Mathf.Max(0, cantidad);
    }

    public void AgregarCarneDesebrada(int cantidad = 1)
    {
        carne_desebrada += Mathf.Max(0, cantidad);
    }

    public void AgregarCarneTrompo(int cantidad = 1)
    {
        carne_trompo += Mathf.Max(0, cantidad);
    }

    public IngredienteCocina ObtenerIngredienteSeleccionado()
    {
        if (string.IsNullOrWhiteSpace(ingrediente_seleccionado))
            return IngredienteCocina.Ninguno;

        string valor = ingrediente_seleccionado.Trim().ToLowerInvariant();
        switch (valor)
        {
            case "0":
            case "trompo":
            case "carne_trompo":
                return IngredienteCocina.Trompo;

            case "1":
            case "pastor":
            case "carne_pastor":
                return IngredienteCocina.Pastor;

            case "2":
            case "picadillo":
            case "carne_picadillo":
                return IngredienteCocina.Picadillo;

            case "3":
            case "desebrada":
            case "carne_desebrada":
                return IngredienteCocina.Desebrada;

            case "4":
            case "tortilla":
            case "tortillas":
                return IngredienteCocina.Tortilla;

            default:
                return IngredienteCocina.Ninguno;
        }
    }

    public bool ConsumirIngredienteCrudo(IngredienteCocina ingrediente, int cantidad = 1)
    {
        int valor = Mathf.Max(0, cantidad);
        if (valor <= 0)
            return true;

        switch (ingrediente)
        {
            case IngredienteCocina.Trompo:
                if (carne_trompo < valor)
                    return false;
                carne_trompo -= valor;
                return true;

            case IngredienteCocina.Pastor:
                if (carne_pastor < valor)
                    return false;
                carne_pastor -= valor;
                return true;

            case IngredienteCocina.Picadillo:
                if (carne_picadillo < valor)
                    return false;
                carne_picadillo -= valor;
                return true;

            case IngredienteCocina.Desebrada:
                if (carne_desebrada < valor)
                    return false;
                carne_desebrada -= valor;
                return true;

            case IngredienteCocina.Tortilla:
            {
                int disponibles = Mathf.Max(cantidad_tortillas, tortilla_cruda);
                if (disponibles < valor)
                    return false;

                cantidad_tortillas = Mathf.Max(0, cantidad_tortillas - valor);
                tortilla_cruda = Mathf.Max(0, tortilla_cruda - valor);
                return true;
            }

            default:
                return false;
        }
    }

    public void AgregarIngredienteCocido(IngredienteCocina ingrediente, int cantidad = 1)
    {
        int valor = Mathf.Max(0, cantidad);
        if (valor <= 0)
            return;

        switch (ingrediente)
        {
            case IngredienteCocina.Trompo:
                carne_trompo_cocinada += valor;
                break;

            case IngredienteCocina.Pastor:
                carne_pastor_cocinada += valor;
                break;

            case IngredienteCocina.Picadillo:
                carne_picadillo_cocinada += valor;
                break;

            case IngredienteCocina.Desebrada:
                carne_desebrada_cocinada += valor;
                break;

            case IngredienteCocina.Tortilla:
                tortilla_cocinada += valor;
                break;
        }
    }

}
