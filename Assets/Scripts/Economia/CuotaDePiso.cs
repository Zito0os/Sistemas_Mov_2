#pragma warning disable 0436
using UnityEngine;

/// <summary>
/// CuotaDePiso — Gestiona el cobro al final de cada semana (cada 3 días por defecto).
/// Se activa al entrar al estado GameState.CuotaDePiso en GameManager.
/// </summary>
public class CuotaDePiso : MonoBehaviour
{
    [Header("Configuración de cuota")]
    [SerializeField, Min(1)] private int cuotaBase = 120;
    [SerializeField, Min(0)] private int incrementoPorSemana = 40;

    [Header("Debug")]
    [SerializeField] private bool resolverAutomaticamente = true;
    [SerializeField] private KeyCode teclaForzarPago = KeyCode.P;
    [SerializeField] private KeyCode teclaForzarNoPago = KeyCode.O;

    [Header("Runtime")]
    [SerializeField] private int cuotaActual;
    [SerializeField] private bool cuotaPendiente;
    [SerializeField] private bool mostrarMenuResultado;
    [SerializeField] private bool cuotaCumplida;
    [SerializeField] private string mensajeResultado = string.Empty;

    public static event System.Action<int, int, int> OnCuotaCalculada;
    public static event System.Action<bool, int, int, int, int> OnResultadoCuota;
    // (pagada, cuota, balanceAntes, balanceDespues, semana)

    private Rect _rectMenu = new Rect(20, 20, 420, 220);

    private void OnEnable()
    {
        GameManager.OnStateChanged += AlCambiarEstado;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= AlCambiarEstado;
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

    private void OnGUI()
    {
        if (!mostrarMenuResultado) return;
        _rectMenu = GUI.Window(9381, _rectMenu, DibujarVentanaResultado, "CUOTA DE PISO");
    }

    private void AlCambiarEstado(GameManager.GameState estado)
    {
        if (estado != GameManager.GameState.CuotaDePiso) return;

        PrepararCobro();

        if (resolverAutomaticamente)
            ResolverCuota(forzarPago: false);
        else
            Debug.Log($"[CuotaDePiso][DEBUG] Presiona {teclaForzarPago} para pagar o {teclaForzarNoPago} para no pagar.");
    }

    private void DibujarVentanaResultado(int windowId)
    {
        GUILayout.BeginVertical();
        GUILayout.Space(8);
        GUILayout.Label(mensajeResultado);
        GUILayout.Space(8);

        if (GUILayout.Button("Regresar", GUILayout.Height(34)))
            ClickRegresar();

        if (GUILayout.Button("Salir", GUILayout.Height(34)))
            ClickSalir();

        if (cuotaCumplida)
        {
            if (GUILayout.Button("Siguiente semana", GUILayout.Height(34)))
                ClickSiguienteSemana();
        }

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    private void PrepararCobro()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[CuotaDePiso] No hay GameManager activo.");
            cuotaPendiente = false;
            return;
        }

        int semana = gm.CurrentWeek;
        cuotaActual = CalcularCuota(semana);
        cuotaPendiente = true;

        int balance = GestorEconomia.Instancia != null ? GestorEconomia.Instancia.GetBalance() : 0;
        Debug.Log($"[CuotaDePiso] INICIO COBRO | Semana: {semana} | Día global: {gm.CurrentDay} | Día en semana: {gm.DayInWeek}/{gm.DaysPerWeek} | Cuota: ${cuotaActual} | Balance actual: ${balance}");
        OnCuotaCalculada?.Invoke(cuotaActual, balance, semana);
    }

    private int CalcularCuota(int semana)
    {
        int semanaIndex = Mathf.Max(0, semana - 1);
        return cuotaBase + (incrementoPorSemana * semanaIndex);
    }

    private void ResolverCuota(bool forzarPago)
    {
        if (!cuotaPendiente) return;

        GameManager gm = GameManager.Instance;
        GestorEconomia economia = GestorEconomia.Instancia;

        if (gm == null || economia == null)
        {
            Debug.LogError("[CuotaDePiso] Falta GameManager o GestorEconomia para resolver la cuota.");
            cuotaPendiente = false;
            return;
        }

        int semana = gm.CurrentWeek;
        int balanceAntes = economia.GetBalance();

        bool pagada;
        if (forzarPago)
        {
            pagada = economia.SpendMoney(cuotaActual);
            if (!pagada)
                Debug.LogWarning("[CuotaDePiso][DEBUG] Se forzó pago, pero no alcanzó el dinero. Se tomará como no pagada.");
        }
        else
        {
            pagada = economia.SpendMoney(cuotaActual);
        }

        int balanceDespues = economia.GetBalance();
        cuotaPendiente = false;
        cuotaCumplida = pagada;
        mostrarMenuResultado = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        mensajeResultado = pagada
            ? "¡Cumpliste la cuota de piso!"
            : "No cumpliste la cuota de piso.";

        if (pagada)
            Debug.Log($"[CuotaDePiso] CUOTA CUMPLIDA | Semana: {semana} | Balance: ${balanceAntes} -> ${balanceDespues}");
        else
            Debug.LogWarning($"[CuotaDePiso] CUOTA NO CUMPLIDA | Semana: {semana} | Balance: ${balanceAntes} -> ${balanceDespues} | Se regresará al inicio de semana.");

        OnResultadoCuota?.Invoke(pagada, cuotaActual, balanceAntes, balanceDespues, semana);
    }

    private void ClickRegresar()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[CuotaDePiso] No hay GameManager activo al presionar Regresar.");
            return;
        }

        if (!cuotaCumplida)
        {
            Debug.Log("[CuotaDePiso] Regresar seleccionado con cuota NO cumplida. Volviendo al inicio de la semana.");
            mostrarMenuResultado = false;
            gm.RegistrarResultadoCuota(false);
            return;
        }

        var gestorClientes = FindFirstObjectByType<GestorClientes>();
        if (gestorClientes != null)
            gestorClientes.DetenerTurno();

        Debug.Log("[CuotaDePiso] Regresar seleccionado con cuota cumplida.");
        mostrarMenuResultado = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void ClickSiguienteSemana()
    {
        if (!cuotaCumplida) return;

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[CuotaDePiso] No hay GameManager activo al presionar Siguiente semana.");
            return;
        }

        Debug.Log("[CuotaDePiso] Siguiente semana seleccionado. Avanzando al siguiente día de la siguiente semana.");
        mostrarMenuResultado = false;
        gm.RegistrarResultadoCuota(true);
    }

    private void ClickSalir()
    {
        Debug.Log("[CuotaDePiso] Salir seleccionado. Cerrando juego...");
        Application.Quit();
    }
}
