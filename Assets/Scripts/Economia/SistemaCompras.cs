using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SistemaCompras : MonoBehaviour
{
    // ── Catálogo ───────────────────────────────────────────────────────────────

    [Serializable]
    public class ItemTienda
    {
        public string nombre      = "Item";
        public string descripcion = "";
        public int    precio      = 10;
        public int    stock       = 10;
        [HideInInspector] public int _cantidadComprada = 0;
    }

    [Header("Catálogo de ítems")]
    [SerializeField] private ItemTienda[] catalogo =
    {
        new ItemTienda { nombre = "Carne al Pastor",    descripcion = "Paquete 500g", precio = 15, stock = 20  },
        new ItemTienda { nombre = "Picadillo",          descripcion = "Paquete 400g", precio = 25, stock = 20  },
        new ItemTienda { nombre = "Desebrada",          descripcion = "Paquete 400g", precio = 25, stock = 20  },
        new ItemTienda { nombre = "Tortillas",          descripcion = "Paquete x20",  precio = 5, stock = 20 },
        new ItemTienda { nombre = "Cebolla + Cilantro", descripcion = "Para tacos",   precio = 10, stock = 20 },
        new ItemTienda { nombre = "Salsa",              descripcion = "Frasco 350ml", precio = 7, stock = 20  },
    };

    // ── Referencias UI ─────────────────────────────────────────────────────────

    [Header("Panel principal")]
    [SerializeField] private GameObject panelTienda;

    [Header("Textos del panel")]
    [SerializeField] private TextMeshProUGUI txt_balance;
    [SerializeField] private TextMeshProUGUI txt_mensaje;

    [Header("Contenedor de filas (ScrollView → Content)")]
    [SerializeField] private Transform contenedorItems;

    [Header("Prefab de fila de ítem")]
    [Tooltip("Prefab con hijos: txt_nombre, txt_descripcion, txt_precio, txt_stock, txt_comprados, btn_comprar")]
    [SerializeField] private GameObject prefabItemTienda;

    [Header("Botón cerrar")]
    [SerializeField] private Button btn_cerrar;

    [Header("Debug")]
    [SerializeField] private bool logsActivos = true;

    // ── Estado interno ─────────────────────────────────────────────────────────

    private bool _habilitada = false;
    private bool _abierta    = false;
    private readonly List<GameObject> _filas = new List<GameObject>();

    // ── Evento ────────────────────────────────────────────────────────────────

    public static event Action<string, int, bool> OnCompraIntentada;

    // ── Ciclo de vida ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (panelTienda != null)
            panelTienda.SetActive(false);

        if (btn_cerrar != null)
            btn_cerrar.onClick.AddListener(CerrarTienda);
    }

    private void Start()
    {
        
        if (GameManager.Instance != null)
            AlCambiarEstado(GameManager.Instance.CurrentState);
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged    += AlCambiarEstado;
        GestorEconomia.OnMoneyChanged += AlCambiarBalance;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged    -= AlCambiarEstado;
        GestorEconomia.OnMoneyChanged -= AlCambiarBalance;
    }

    private void OnDestroy()
    {
        if (btn_cerrar != null)
            btn_cerrar.onClick.RemoveListener(CerrarTienda);
    }

    // ── Listeners de estado ────────────────────────────────────────────────────

    private void AlCambiarEstado(GameManager.GameState estado)
    {
        if (estado == GameManager.GameState.StartDay)
        {
            _habilitada = true;
            ResetearCompras();
            if (logsActivos)
                Debug.Log("[SistemaCompras] Habilitado — fase de compras activa.");
            return;
        }

        _habilitada = false;
        CerrarTienda();

        if (logsActivos)
            Debug.Log($"[SistemaCompras] Deshabilitado — estado: {estado}.");
    }

    private void AlCambiarBalance(int nuevoBalance)
    {
        if (!_abierta) return;
        ActualizarTextoBalance(nuevoBalance);
        RefrescarBotones();
    }

    // ── API pública ────────────────────────────────────────────────────────────

    public void AbrirTienda()
    {
        if (!_habilitada)
        {
            if (logsActivos)
                Debug.Log("[SistemaCompras] Tienda no disponible fuera de StartDay.");
            return;
        }

        if (_abierta) return;

        _abierta = true;
        if (panelTienda != null)
            panelTienda.SetActive(true);

        ConstruirFilas();
        ActualizarTextoBalance(ObtenerBalance());
        if (txt_mensaje != null) txt_mensaje.text = "";

        if (logsActivos)
            Debug.Log("[SistemaCompras] Panel abierto.");
    }

    public void CerrarTienda()
    {
        if (!_abierta) return;

        _abierta = false;
        if (panelTienda != null)
            panelTienda.SetActive(false);

        if (logsActivos)
            Debug.Log("[SistemaCompras] Panel cerrado.");
    }

    // ── Construcción de filas ──────────────────────────────────────────────────

    private void ConstruirFilas()
    {
        if (contenedorItems == null || prefabItemTienda == null)
        {
            Debug.LogWarning("[SistemaCompras] Falta contenedorItems o prefabItemTienda en el Inspector.");
            return;
        }

        foreach (var fila in _filas)
            if (fila != null) Destroy(fila);
        _filas.Clear();

        int balance = ObtenerBalance();

        for (int i = 0; i < catalogo.Length; i++)
        {
            ItemTienda item = catalogo[i];
            if (item == null) continue;

            GameObject filaGO = Instantiate(prefabItemTienda, contenedorItems);
            _filas.Add(filaGO);

            SetTexto(filaGO, "txt_nombre",      item.nombre);
            SetTexto(filaGO, "txt_descripcion", item.descripcion);
            SetTexto(filaGO, "txt_precio",      $"${item.precio}");
            SetTexto(filaGO, "txt_stock",       $"x{item.stock - item._cantidadComprada}");
            SetTexto(filaGO, "txt_comprados",   $"Comprados: {item._cantidadComprada}");

            int idx = i;
            Button btn = BuscarHijo<Button>(filaGO, "btn_comprar");
            if (btn != null)
            {
                int stockRestante = item.stock - item._cantidadComprada;
                btn.interactable = stockRestante > 0 && balance >= item.precio;
                btn.onClick.AddListener(() => IntentarCompra(idx));
            }
        }
    }

    private void RefrescarBotones()
    {
        int balance = ObtenerBalance();

        for (int i = 0; i < _filas.Count && i < catalogo.Length; i++)
        {
            GameObject filaGO = _filas[i];
            if (filaGO == null) continue;

            ItemTienda item = catalogo[i];
            int stockRestante = item.stock - item._cantidadComprada;

            SetTexto(filaGO, "txt_stock",     $"x{stockRestante}");
            SetTexto(filaGO, "txt_comprados", $"Comprados: {item._cantidadComprada}");

            Button btn = BuscarHijo<Button>(filaGO, "btn_comprar");
            if (btn != null)
                btn.interactable = stockRestante > 0 && balance >= item.precio;
        }
    }

    // ── Compra ─────────────────────────────────────────────────────────────────

    private void IntentarCompra(int idx)
    {
        if (idx < 0 || idx >= catalogo.Length) return;
        ItemTienda item = catalogo[idx];

        GestorEconomia economia = GestorEconomia.Instancia;
        if (economia == null) { MostrarMensaje("Error: no hay economía."); return; }

        int stockRestante = item.stock - item._cantidadComprada;
        if (stockRestante <= 0)
        {
            MostrarMensaje($"Sin stock de {item.nombre}.");
            OnCompraIntentada?.Invoke(item.nombre, item.precio, false);
            return;
        }

        bool exito = economia.SpendMoney(item.precio);
        if (!exito)
        {
            MostrarMensaje($"Dinero insuficiente para {item.nombre}.");
            OnCompraIntentada?.Invoke(item.nombre, item.precio, false);
            if (logsActivos)
                Debug.Log($"[SistemaCompras] Fallida — {item.nombre} | Balance: {economia.GetBalance()}");
            return;
        }

        item._cantidadComprada++;
        AgregarStockACooking(item.nombre);

        MostrarMensaje($"Compraste {item.nombre}!");
        OnCompraIntentada?.Invoke(item.nombre, item.precio, true);
        ActualizarTextoBalance(economia.GetBalance());
        RefrescarBotones();

        if (logsActivos)
            Debug.Log($"[SistemaCompras] OK — {item.nombre} | Total hoy: {item._cantidadComprada}");
    }

    // ── Conexión con CookingStation ────────────────────────────────────────────

    private void AgregarStockACooking(string nombreItem)
    {
        if (CookingStation.Instance == null) return;

        switch (nombreItem)
        {
            case "Carne al Pastor":
                CookingStation.Instance.AgregarStockCrudo(IngredienteCocina.Pastor);
                break;
            case "Picadillo":
                CookingStation.Instance.AgregarStockCrudo(IngredienteCocina.Picadillo);
                break;
            case "Desebrada":
                CookingStation.Instance.AgregarStockCrudo(IngredienteCocina.Desebrada);
                break;
            case "Tortillas":
                CookingStation.Instance.AgregarStockCrudo(IngredienteCocina.Tortilla);
                break;
            case "Cebolla + Cilantro":
                CookingStation.Instance.AgregarStockExtra("cebolla");
                break;
            case "Salsa":
                CookingStation.Instance.AgregarStockExtra("salsa");
                break;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void ResetearCompras()
    {
        foreach (var item in catalogo)
            if (item != null) item._cantidadComprada = 0;
    }

    private void ActualizarTextoBalance(int balance)
    {
        if (txt_balance != null) txt_balance.text = $"Balance: ${balance}";
    }

    private void MostrarMensaje(string msg)
    {
        if (txt_mensaje != null) txt_mensaje.text = msg;
    }

    private int ObtenerBalance() =>
        GestorEconomia.Instancia != null ? GestorEconomia.Instancia.GetBalance() : 0;

    private static void SetTexto(GameObject raiz, string nombreHijo, string valor)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != nombreHijo) continue;
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = valor;
                return;
            }
        }
    }

    private static T BuscarHijo<T>(GameObject raiz, string nombre) where T : Component
    {
        return raiz.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == nombre)
            ?.GetComponent<T>();
    }

    // ── Queries ────────────────────────────────────────────────────────────────

    public int CantidadComprada(string nombreItem)
    {
        foreach (var item in catalogo)
            if (item != null && item.nombre == nombreItem)
                return item._cantidadComprada;
        return 0;
    }

    public bool TiendaAbierta    => _abierta;
    public bool TiendaHabilitada => _habilitada;
}