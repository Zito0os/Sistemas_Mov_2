#pragma warning disable 0436
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CuotaDePiso : MonoBehaviour
{
    [Header("Configuración de cuota")]
    [SerializeField, Min(1)] private int cuotaBase = 120;
    [SerializeField, Min(0)] private int incrementoPorSemana = 40;

    [Header("Debug")]
    [SerializeField] private bool resolverAutomaticamente = true;
    [SerializeField] private KeyCode teclaForzarPago    = KeyCode.P;
    [SerializeField] private KeyCode teclaForzarNoPago  = KeyCode.O;

    [Header("Referencias UI — Panel raíz")]
    [SerializeField] private GameObject panelRaiz;

    [Header("Referencias UI — Textos")]
    [SerializeField] private TextMeshProUGUI txtTitulo;
    [SerializeField] private TextMeshProUGUI txtSemana;
    [SerializeField] private TextMeshProUGUI txtMonto;
    [SerializeField] private TextMeshProUGUI txtBalance;
    [SerializeField] private TextMeshProUGUI txtResultado;

    [Header("Referencias UI — Botones")]
    [SerializeField] private Button btnPagar;       // Solo en modo debug (resolverAutomaticamente = false)
    [SerializeField] private Button btnRegresar;    // Solo si cuota NO cumplida
    [SerializeField] private Button btnSiguiente;   // Solo si cuota cumplida
    [SerializeField] private Button btnSalir;

    [Header("Mensajes editables")]
    [SerializeField] private string mensajeTitulo      = "CUOTA DE PISO";
    [SerializeField] private string mensajeCumplida    = "¡Cumpliste la cuota de piso!";
    [SerializeField] private string mensajeNoCumplida  = "No cumpliste la cuota de piso.";
    [SerializeField] private string mensajeNoCumplida2 = "Regresarás al inicio de la semana.";

    // ── Eventos públicos ──────────────────────────────────────────────────────
    public static event System.Action<int, int, int>             OnCuotaCalculada;
    public static event System.Action<bool, int, int, int, int>  OnResultadoCuota;
    // OnResultadoCuota: (pagada, cuota, balanceAntes, balanceDespues, semana)

    // ── Estado interno ────────────────────────────────────────────────────────
    [Header("Runtime (solo lectura)")]
    [SerializeField] private int  cuotaActual;
    [SerializeField] private bool cuotaPendiente;
    [SerializeField] private bool cuotaCumplida;
    [SerializeField] private bool mostrandoResultado;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (btnPagar     != null) btnPagar    .onClick.AddListener(() => ResolverCuota(forzarPago: true));
        if (btnRegresar  != null) btnRegresar .onClick.AddListener(ClickRegresar);
        if (btnSiguiente != null) btnSiguiente.onClick.AddListener(ClickSiguienteSemana);
        if (btnSalir     != null) btnSalir    .onClick.AddListener(ClickSalir);
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
        if (btnPagar     != null) btnPagar    .onClick.RemoveAllListeners();
        if (btnRegresar  != null) btnRegresar .onClick.RemoveAllListeners();
        if (btnSiguiente != null) btnSiguiente.onClick.RemoveAllListeners();
        if (btnSalir     != null) btnSalir    .onClick.RemoveAllListeners();
    }

    private void Update()
    {
        if (!cuotaPendiente || resolverAutomaticamente) return;

        if (Input.GetKeyDown(teclaForzarPago))
        {
            Debug.Log("[CuotaDePiso][DEBUG] Forzando pago de cuota.");
            ResolverCuota(forzarPago: true);
        }
        else if (Input.GetKeyDown(teclaForzarNoPago))
        {
            Debug.Log("[CuotaDePiso][DEBUG] Forzando no pago de cuota.");
            ResolverCuota(forzarPago: false);
        }
    }

    // ── Listener de estado ────────────────────────────────────────────────────

    private void AlCambiarEstado(GameManager.GameState estado)
    {
        if (estado != GameManager.GameState.CuotaDePiso) return;

        PrepararCobro();

        if (resolverAutomaticamente)
            ResolverCuota(forzarPago: false);
        else
            Debug.Log($"[CuotaDePiso][DEBUG] Presiona {teclaForzarPago} para pagar o {teclaForzarNoPago} para no pagar.");
    }

    // ── Lógica principal ──────────────────────────────────────────────────────

    private void PrepararCobro()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[CuotaDePiso] No hay GameManager activo.");
            cuotaPendiente = false;
            return;
        }

        int semana  = gm.CurrentWeek;
        cuotaActual = CalcularCuota(semana);
        cuotaPendiente     = true;
        mostrandoResultado = false;

        int balance = GestorEconomia.Instancia != null ? GestorEconomia.Instancia.GetBalance() : 0;

        Debug.Log($"[CuotaDePiso] INICIO COBRO | Semana: {semana} | Día global: {gm.CurrentDay} " +
                  $"| Día en semana: {gm.DayInWeek}/{gm.DaysPerWeek} " +
                  $"| Cuota: ${cuotaActual} | Balance actual: ${balance}");

        OnCuotaCalculada?.Invoke(cuotaActual, balance, semana);
        MostrarPanelCobro(semana, cuotaActual, balance);
    }

    private int CalcularCuota(int semana)
    {
        int semanaIndex = Mathf.Max(0, semana - 1);
        return cuotaBase + (incrementoPorSemana * semanaIndex);
    }

    private void ResolverCuota(bool forzarPago)
    {
        if (!cuotaPendiente) return;

        GameManager    gm       = GameManager.Instance;
        GestorEconomia economia = GestorEconomia.Instancia;

        if (gm == null || economia == null)
        {
            Debug.LogError("[CuotaDePiso] Falta GameManager o GestorEconomia para resolver la cuota.");
            cuotaPendiente = false;
            return;
        }

        int  semana       = gm.CurrentWeek;
        int  balanceAntes = economia.GetBalance();
        bool pagada       = economia.SpendMoney(cuotaActual);

        if (forzarPago && !pagada)
            Debug.LogWarning("[CuotaDePiso][DEBUG] Se forzó pago, pero no alcanzó el dinero. Se tomará como no pagada.");

        int balanceDespues = economia.GetBalance();
        cuotaPendiente     = false;
        cuotaCumplida      = pagada;
        mostrandoResultado = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (pagada)
            Debug.Log($"[CuotaDePiso] CUOTA CUMPLIDA | Semana: {semana} | Balance: ${balanceAntes} → ${balanceDespues}");
        else
            Debug.LogWarning($"[CuotaDePiso] CUOTA NO CUMPLIDA | Semana: {semana} | Balance: ${balanceAntes} → ${balanceDespues} | Se regresará al inicio de semana.");

        OnResultadoCuota?.Invoke(pagada, cuotaActual, balanceAntes, balanceDespues, semana);
        MostrarResultado(pagada, balanceDespues);
    }

    // ── Métodos de UI ─────────────────────────────────────────────────────────

    private void MostrarPanelCobro(int semana, int cuota, int balance)
    {
        Debug.Log($"[CuotaDePiso] MostrarPanelCobro — panelRaiz={panelRaiz}");

        if (panelRaiz != null)
            panelRaiz.SetActive(true);

        SetTexto(txtTitulo,    mensajeTitulo);
        SetTexto(txtSemana,    $"Semana {semana}");
        SetTexto(txtMonto,     $"Cuota: ${cuota}");
        SetTexto(txtBalance,   $"Tu dinero: ${balance}");
        SetTexto(txtResultado, string.Empty);

        // Todos los botones de resultado ocultos hasta resolver
        SetActivo(btnPagar,     !resolverAutomaticamente);
        SetActivo(btnRegresar,  false);
        SetActivo(btnSiguiente, false);
        SetActivo(btnSalir,     false);
    }

    private void MostrarResultado(bool pagada, int balanceFinal)
    {
        SetTexto(txtBalance, $"Tu dinero: ${balanceFinal}");

        if (pagada)
            SetTexto(txtResultado, mensajeCumplida);
        else
            SetTexto(txtResultado, $"{mensajeNoCumplida}\n{mensajeNoCumplida2}");

        // Cuota cumplida   → Siguiente semana + Salir (sin Regresar)
        // Cuota NO cumplida → Regresar + Salir (sin Siguiente)
        SetActivo(btnPagar,     false);
        SetActivo(btnSiguiente, pagada);
        SetActivo(btnRegresar,  !pagada);
        SetActivo(btnSalir,     true);
    }

    // ── Handlers de botones ───────────────────────────────────────────────────

    private void ClickRegresar()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[CuotaDePiso] No hay GameManager activo al presionar Regresar.");
            return;
        }

        // Regresar solo aparece cuando la cuota NO fue cumplida
        Debug.Log("[CuotaDePiso] Regresar — cuota NO cumplida. Volviendo al inicio de la semana.");
        OcultarPanel();
        gm.RegistrarResultadoCuota(false);
    }

    private void ClickSiguienteSemana()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[CuotaDePiso] No hay GameManager activo al presionar Siguiente semana.");
            return;
        }

        // Siguiente solo aparece cuando la cuota SÍ fue cumplida
        Debug.Log("[CuotaDePiso] Siguiente semana — avanzando al siguiente ciclo.");
        OcultarPanel();
        gm.RegistrarResultadoCuota(true);
    }

     private void ClickSalir()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("[CuotaDePiso] No hay GameManager activo al presionar Salir.");
                return;
            }
    
            Debug.Log("[CuotaDePiso] Salir seleccionado. Regresando al menú principal.");
            OcultarPanel();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void OcultarPanel()
    {
        mostrandoResultado = false;
        if (panelRaiz != null)
            panelRaiz.SetActive(false);
    }

    private static void SetTexto(TextMeshProUGUI label, string texto)
    {
        if (label != null)
            label.text = texto;
    }

    private static void SetActivo(Button boton, bool activo)
    {
        if (boton != null)
            boton.gameObject.SetActive(activo);
    }
}