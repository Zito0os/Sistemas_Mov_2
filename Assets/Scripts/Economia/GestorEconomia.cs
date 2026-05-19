#pragma warning disable 0436
using UnityEngine;

/// <summary>
/// GestorEconomia — Maneja el dinero del jugador y escucha pagos del SistemaOrdenes.
/// </summary>
public class GestorEconomia : MonoBehaviour
{
    public static GestorEconomia Instancia { get; private set; }

    private static bool _estadoInicializado;
    private static int _balancePersistido;
    private static int _ingresosPersistidos;
    private static int _gastosPersistidos;
    private static int _balanceInicioSemanaPersistido;

    [Header("Balance")]
    [SerializeField] private int balanceInicial = 100;
    [SerializeField] private int balanceActual;

    [Header("Estadísticas del día (runtime)")]
    [SerializeField] private int ingresosAcumulados;
    [SerializeField] private int gastosAcumulados;

    [Header("Checkpoint semanal (runtime)")]
    [SerializeField] private int balanceInicioSemana;

    
    public static event System.Action<int> OnMoneyChanged;

    public int IngresosDia => ingresosAcumulados;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;

        if (!_estadoInicializado)
        {
            balanceActual = balanceInicial;
            ingresosAcumulados = 0;
            gastosAcumulados = 0;
            balanceInicioSemana = balanceActual;
            GuardarEstadoPersistente();
            _estadoInicializado = true;
            return;
        }

        balanceActual = _balancePersistido;
        ingresosAcumulados = _ingresosPersistidos;
        gastosAcumulados = _gastosPersistidos;
        balanceInicioSemana = _balanceInicioSemanaPersistido;
    }

    private void OnEnable()
    {
        SistemaOrdenes.alCompletarOrden += AlCompletarOrden;
        GameManager.OnDayChanged += AlCambiarDia;
        GameManager.OnWeeklyProgressReset += AlReiniciarProgresoSemanal;
    }

    private void OnDisable()
    {
        SistemaOrdenes.alCompletarOrden -= AlCompletarOrden;
        GameManager.OnDayChanged -= AlCambiarDia;
        GameManager.OnWeeklyProgressReset -= AlReiniciarProgresoSemanal;
    }

    private void Start()
    {
        NotificarCambioBalance();
    }

    private void AlCompletarOrden(Orden orden, int pagoTotal, bool correcto, int cantidadRequerida)
    {
        if (pagoTotal <= 0) return;
        AddMoney(pagoTotal);
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;

        balanceActual += amount;
        ingresosAcumulados += amount;
        GuardarEstadoPersistente();
        NotificarCambioBalance();
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return true;
        if (balanceActual < amount) return false;

        balanceActual -= amount;
        gastosAcumulados += amount;
        GuardarEstadoPersistente();
        NotificarCambioBalance();
        return true;
    }

    public int GetBalance()
    {
        return balanceActual;
    }

    public void AgregarDinero(int monto)
    {
        AddMoney(monto);
    }

    public bool GastarDinero(int monto)
    {
        return SpendMoney(monto);
    }

    public int ObtenerBalance()
    {
        return GetBalance();
    }

    private void NotificarCambioBalance()
    {
        OnMoneyChanged?.Invoke(balanceActual);
    }

    private void AlCambiarDia(int nuevoDia)
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.DayInWeek != 1) return;

        balanceInicioSemana = balanceActual;
        GuardarEstadoPersistente();
        Debug.Log($"[GestorEconomia] Checkpoint semanal guardado. Semana: {GameManager.Instance.CurrentWeek} | Día: {nuevoDia} | Balance inicial de semana: ${balanceInicioSemana}");
    }

    private void AlReiniciarProgresoSemanal(int semana, int diaAlQueRegreso)
    {
        int balanceAntes = balanceActual;
        balanceActual = balanceInicioSemana;
        GuardarEstadoPersistente();
        NotificarCambioBalance();

        Debug.LogWarning($"[GestorEconomia] Rollback semanal aplicado. Semana: {semana} | Día: {diaAlQueRegreso} | Balance: ${balanceAntes} -> ${balanceActual}");
    }

    private void GuardarEstadoPersistente()
    {
        _balancePersistido = balanceActual;
        _ingresosPersistidos = ingresosAcumulados;
        _gastosPersistidos = gastosAcumulados;
        _balanceInicioSemanaPersistido = balanceInicioSemana;
    }
}
