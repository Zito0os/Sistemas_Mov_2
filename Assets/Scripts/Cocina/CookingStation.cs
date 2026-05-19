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

    // ── EN MANO ───────────────────────────────────────────────────────────────

    [Header("En Mano (solo lectura en runtime)")]
    [SerializeField] private IngredienteCocina enMano = IngredienteCocina.Ninguno;

    public bool TieneAlgoEnMano => enMano != IngredienteCocina.Ninguno;

    public bool AgarrarIngrediente(IngredienteCocina ingrediente)
    {
        if (TieneAlgoEnMano)
        {
            Debug.Log($"[CookingStation] Manos llenas ({enMano}). Suelta el ingrediente antes.");
            return false;
        }

        if (ingrediente == IngredienteCocina.Ninguno)
            return false;

        enMano = ingrediente;
        Debug.Log($"[CookingStation] Agarraste: {enMano}");
        return true;
    }

    public IngredienteCocina SoltarIngrediente()
    {
        IngredienteCocina temp = enMano;
        enMano = IngredienteCocina.Ninguno;
        Debug.Log($"[CookingStation] Soltaste: {temp}");
        return temp;
    }

    public IngredienteCocina ObtenerIngredienteSeleccionado() => enMano;

    // ── STOCK CRUDO (llenado por SistemaCompras al inicio del día) ────────────

    [Header("Stock crudo (comprado en tienda)")]
    public int stock_pastor     = 0;
    public int stock_picadillo  = 0;
    public int stock_desebrada  = 0;
    public int stock_tortillas  = 0;
    public int stock_cebolla    = 0;
    public int stock_salsa      = 0;

    /// <summary>Agrega stock crudo al comprar en la tienda.</summary>
    public void AgregarStockCrudo(IngredienteCocina ingrediente, int cantidad = 1)
    {
        switch (ingrediente)
        {
            case IngredienteCocina.Pastor:    stock_pastor    += cantidad; break;
            case IngredienteCocina.Picadillo: stock_picadillo += cantidad; break;
            case IngredienteCocina.Desebrada: stock_desebrada += cantidad; break;
            case IngredienteCocina.Tortilla:  stock_tortillas += cantidad; break;
        }
        Debug.Log($"[CookingStation] Stock crudo +{cantidad}: {ingrediente}");
    }

    /// <summary>Agrega stock de ingredientes sin enum (cebolla, salsa).</summary>
    public void AgregarStockExtra(string nombre, int cantidad = 1)
    {
        switch (nombre)
        {
            case "cebolla": stock_cebolla += cantidad; break;
            case "salsa":   stock_salsa   += cantidad; break;
        }
        Debug.Log($"[CookingStation] Stock extra +{cantidad}: {nombre}");
    }

    /// <summary>¿Hay suficiente stock crudo para cocinar?</summary>
    public bool TieneStockCrudo(IngredienteCocina ingrediente, int cantidad = 1)
    {
        return ingrediente switch
        {
            IngredienteCocina.Pastor    => stock_pastor    >= cantidad,
            IngredienteCocina.Picadillo => stock_picadillo >= cantidad,
            IngredienteCocina.Desebrada => stock_desebrada >= cantidad,
            IngredienteCocina.Tortilla  => stock_tortillas >= cantidad,
            _                           => false
        };
    }

    /// <summary>Consume stock crudo al agarrar para cocinar.</summary>
    public bool ConsumirStockCrudo(IngredienteCocina ingrediente, int cantidad = 1)
    {
        if (!TieneStockCrudo(ingrediente, cantidad))
        {
            Debug.Log($"[CookingStation] Sin stock crudo de {ingrediente}.");
            return false;
        }

        switch (ingrediente)
        {
            case IngredienteCocina.Pastor:    stock_pastor    -= cantidad; break;
            case IngredienteCocina.Picadillo: stock_picadillo -= cantidad; break;
            case IngredienteCocina.Desebrada: stock_desebrada -= cantidad; break;
            case IngredienteCocina.Tortilla:  stock_tortillas -= cantidad; break;
        }
        return true;
    }

    /// <summary>Resetea todo el stock crudo al inicio de un nuevo día.</summary>
    public void ResetearStockCrudo()
    {
        stock_pastor = stock_picadillo = stock_desebrada = 0;
        stock_tortillas = stock_cebolla = stock_salsa = 0;
        Debug.Log("[CookingStation] Stock crudo reseteado para nuevo día.");
    }

    // ── CARNE COCINADA ────────────────────────────────────────────────────────

    [Header("Carne Cocinada")]
    public int carne_pastor_cocinada;
    public int carne_picadillo_cocinada;
    public int carne_desebrada_cocinada;
    public int carne_trompo_cocinada;

    [Header("Tortillas")]
    public int tortilla_cocinada;

    // ── TACOS LISTOS ──────────────────────────────────────────────────────────

    [Header("Tacos Listos")]
    public int tacos_pastor;
    public int tacos_trompo;
    public int tacos_picadillo;
    public int tacos_desebrada;

    // ── CICLO ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged += AlCambiarEstado;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= AlCambiarEstado;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void AlCambiarEstado(GameManager.GameState estado)
    {
        if (estado == GameManager.GameState.StartDay)
            ResetearStockCrudo();
    }

    // ── AGREGAR INGREDIENTE COCIDO ────────────────────────────────────────────

    public void AgregarIngredienteCocido(IngredienteCocina ingrediente, int cantidad = 1)
    {
        int valor = Mathf.Max(0, cantidad);
        if (valor <= 0) return;

        switch (ingrediente)
        {
            case IngredienteCocina.Trompo:    carne_trompo_cocinada    += valor; break;
            case IngredienteCocina.Pastor:    carne_pastor_cocinada    += valor; break;
            case IngredienteCocina.Picadillo: carne_picadillo_cocinada += valor; break;
            case IngredienteCocina.Desebrada: carne_desebrada_cocinada += valor; break;
            case IngredienteCocina.Tortilla:  tortilla_cocinada        += valor; break;
        }
    }

    // ── CONSUMIR CARNE COCINADA ───────────────────────────────────────────────

    public bool ConsumirCarneCocinada(IngredienteCocina ingrediente, int cantidad = 1)
    {
        int valor = Mathf.Max(0, cantidad);
        if (valor <= 0) return true;

        switch (ingrediente)
        {
            case IngredienteCocina.Trompo:
                if (carne_trompo_cocinada < valor) return false;
                carne_trompo_cocinada -= valor;
                return true;
            case IngredienteCocina.Pastor:
                if (carne_pastor_cocinada < valor) return false;
                carne_pastor_cocinada -= valor;
                return true;
            case IngredienteCocina.Picadillo:
                if (carne_picadillo_cocinada < valor) return false;
                carne_picadillo_cocinada -= valor;
                return true;
            case IngredienteCocina.Desebrada:
                if (carne_desebrada_cocinada < valor) return false;
                carne_desebrada_cocinada -= valor;
                return true;
            default:
                return false;
        }
    }
}