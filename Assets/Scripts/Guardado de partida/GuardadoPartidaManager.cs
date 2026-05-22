using System;
using System.IO;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class GuardadoPartidaManager : MonoBehaviour
{
    public static GuardadoPartidaManager Instance { get; private set; }

    public const int MaxSlots = 8;
    public const string EscenaJuego = "TestClientes";

    private const string CarpetaGuardado = "GuardadoDePartida";
    private const string PrefijoArchivo = "Partida_";
    private const string ExtensionArchivo = ".json";

    [Serializable]
    public class DatosPartida
    {
        public int version = 1;
        public int slot = -1;
        public int balanceTotal = 300;
        public int diaActual = 1;
        public int stockPastor = 0;
        public int stockPicadillo = 0;
        public int stockDesebrada = 0;
        public int stockTortillas = 0;
        public int stockCebolla = 0;
        public int stockSalsa = 0;
        public string fechaUtc = string.Empty;
    }

    private enum ModoPendiente
    {
        Ninguno,
        NuevaPartida,
        CargarPartida
    }

    [SerializeField] private int slotActual = -1;
    [SerializeField] private DatosPartida datosPendientes;
    [SerializeField] private ModoPendiente modoPendiente = ModoPendiente.Ninguno;

    private bool _escenaRegistrada;

    private static readonly BindingFlags FlagsPrivados = BindingFlags.Instance | BindingFlags.NonPublic;

    private string RutaBase => Path.Combine(Application.persistentDataPath, CarpetaGuardado);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearInstanciaSiFalta()
    {
        if (Instance != null)
            return;

        GameObject root = new GameObject(nameof(GuardadoPartidaManager));
        root.AddComponent<GuardadoPartidaManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        AsegurarCarpeta();
    }

    private void OnEnable()
    {
        if (_escenaRegistrada)
            return;

        SceneManager.sceneLoaded += AlCargarEscena;
        _escenaRegistrada = true;
    }

    private void OnDisable()
    {
        if (!_escenaRegistrada)
            return;

        SceneManager.sceneLoaded -= AlCargarEscena;
        _escenaRegistrada = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool HaySlotValidoSeleccionado => slotActual >= 1 && slotActual <= MaxSlots;

    public bool SlotTieneDatos(int slot)
    {
        return EsSlotValido(slot) && File.Exists(ObtenerRutaSlot(slot));
    }

    public DatosPartida ObtenerDatosSlot(int slot)
    {
        if (!EsSlotValido(slot))
            return null;

        string ruta = ObtenerRutaSlot(slot);
        if (!File.Exists(ruta))
            return null;

        try
        {
            string json = File.ReadAllText(ruta);
            DatosPartida datos = JsonUtility.FromJson<DatosPartida>(json);
            if (datos == null)
                return null;

            datos.slot = slot;
            return datos;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GuardadoPartidaManager] Error leyendo slot {slot}: {ex.Message}");
            return null;
        }
    }

    public bool PrepararNuevaPartida(int slot)
    {
        if (!EsSlotValido(slot))
            return false;

        slotActual = slot;
        datosPendientes = CrearDatosIniciales(slot);
        modoPendiente = ModoPendiente.NuevaPartida;
        return true;
    }

    public bool CargarPartidaEnSlot(int slot)
    {
        DatosPartida datos = ObtenerDatosSlot(slot);
        if (datos == null)
            return false;

        slotActual = slot;
        datosPendientes = datos;
        modoPendiente = ModoPendiente.CargarPartida;
        return true;
    }

    public bool GuardarPartidaActual()
    {
        if (!HaySlotValidoSeleccionado)
            return false;

        DatosPartida captura = CapturarEstadoActual();
        if (captura == null)
            return false;

        captura.slot = slotActual;
        captura.fechaUtc = DateTime.UtcNow.ToString("o");

        try
        {
            AsegurarCarpeta();
            File.WriteAllText(ObtenerRutaSlot(slotActual), JsonUtility.ToJson(captura, true));
            datosPendientes = captura;
            modoPendiente = ModoPendiente.Ninguno;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GuardadoPartidaManager] No se pudo guardar el slot {slotActual}: {ex.Message}");
            return false;
        }
    }

    public bool GuardarDatosPendientes()
    {
        if (!HaySlotValidoSeleccionado || datosPendientes == null)
            return false;

        datosPendientes.slot = slotActual;
        datosPendientes.fechaUtc = DateTime.UtcNow.ToString("o");

        try
        {
            AsegurarCarpeta();
            File.WriteAllText(ObtenerRutaSlot(slotActual), JsonUtility.ToJson(datosPendientes, true));
            modoPendiente = ModoPendiente.Ninguno;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GuardadoPartidaManager] No se pudieron guardar los datos pendientes del slot {slotActual}: {ex.Message}");
            return false;
        }
    }

    public bool EliminarSlot(int slot)
    {
        if (!EsSlotValido(slot))
            return false;

        try
        {
            string ruta = ObtenerRutaSlot(slot);
            if (File.Exists(ruta))
                File.Delete(ruta);

            if (slotActual == slot)
            {
                datosPendientes = null;
                modoPendiente = ModoPendiente.Ninguno;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GuardadoPartidaManager] No se pudo borrar el slot {slot}: {ex.Message}");
            return false;
        }
    }

    public string ObtenerResumenSlot(int slot)
    {
        DatosPartida datos = ObtenerDatosSlot(slot);
        if (datos == null)
            return "Sin informacion de partida";

        string fechaFormateada = FormatearFechaGuardado(datos.fechaUtc);
        if (!string.IsNullOrEmpty(fechaFormateada))
            return $"Dia {datos.diaActual} | Balance ${datos.balanceTotal}\n{fechaFormateada}";

        return $"Dia {datos.diaActual} | Balance ${datos.balanceTotal}";
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != EscenaJuego)
            return;

        if (modoPendiente == ModoPendiente.Ninguno || datosPendientes == null)
            return;

        AplicarDatosPendientes();
        modoPendiente = ModoPendiente.Ninguno;
        datosPendientes = null;
    }

    private void AplicarDatosPendientes()
    {
        AplicarGameManager(datosPendientes);
        AplicarEconomia(datosPendientes);
        AplicarCocina(datosPendientes);
    }

    private void AplicarGameManager(DatosPartida datos)
    {
        if (GameManager.Instance == null)
            return;

        int dia = Mathf.Max(1, datos.diaActual);
        SetPrivateField(GameManager.Instance, "<CurrentDay>k__BackingField", dia);
        SetPrivateField(GameManager.Instance, "inicioSemanaActual", ObtenerInicioSemana(dia, GameManager.Instance.DaysPerWeek));

        InvocarEventoEstatico(typeof(GameManager), "OnDayChanged", dia);

        if (GameManager.Instance.CurrentState != GameManager.GameState.StartDay)
            GameManager.Instance.ChangeState(GameManager.GameState.StartDay);
    }

    private void AplicarEconomia(DatosPartida datos)
    {
        if (GestorEconomia.Instancia == null)
            return;

        SetPrivateField(GestorEconomia.Instancia, "balanceActual", datos.balanceTotal);
        SetPrivateField(GestorEconomia.Instancia, "ingresosAcumulados", 0);
        SetPrivateField(GestorEconomia.Instancia, "gastosAcumulados", 0);
        SetPrivateField(GestorEconomia.Instancia, "balanceInicioSemana", datos.balanceTotal);

        InvocarEventoEstatico(typeof(GestorEconomia), "OnMoneyChanged", datos.balanceTotal);
    }

    private void AplicarCocina(DatosPartida datos)
    {
        CookingStation cocina = CookingStation.Instance;
        if (cocina == null)
            return;

        cocina.stock_pastor = datos.stockPastor;
        cocina.stock_picadillo = datos.stockPicadillo;
        cocina.stock_desebrada = datos.stockDesebrada;
        cocina.stock_tortillas = datos.stockTortillas;
        cocina.stock_cebolla = datos.stockCebolla;
        cocina.stock_salsa = datos.stockSalsa;

        cocina.carne_pastor_cocinada = 0;
        cocina.carne_picadillo_cocinada = 0;
        cocina.carne_desebrada_cocinada = 0;
        cocina.carne_trompo_cocinada = 0;
        cocina.tortilla_cocinada = 0;
        cocina.tacos_pastor = 0;
        cocina.tacos_trompo = 0;
        cocina.tacos_picadillo = 0;
        cocina.tacos_desebrada = 0;

        SetPrivateField(cocina, "enMano", IngredienteCocina.Ninguno);
    }

    private DatosPartida CapturarEstadoActual()
    {
        int diaGuardado = ObtenerDiaParaGuardar();

        DatosPartida datos = new DatosPartida
        {
            slot = slotActual,
            balanceTotal = GestorEconomia.Instancia != null ? GestorEconomia.Instancia.GetBalance() : 0,
            diaActual = diaGuardado,
            stockPastor = CookingStation.Instance != null ? CookingStation.Instance.stock_pastor : 0,
            stockPicadillo = CookingStation.Instance != null ? CookingStation.Instance.stock_picadillo : 0,
            stockDesebrada = CookingStation.Instance != null ? CookingStation.Instance.stock_desebrada : 0,
            stockTortillas = CookingStation.Instance != null ? CookingStation.Instance.stock_tortillas : 0,
            stockCebolla = CookingStation.Instance != null ? CookingStation.Instance.stock_cebolla : 0,
            stockSalsa = CookingStation.Instance != null ? CookingStation.Instance.stock_salsa : 0,
            fechaUtc = DateTime.UtcNow.ToString("o")
        };

        return datos;
    }

    private int ObtenerDiaParaGuardar()
    {
        if (GameManager.Instance == null)
            return 1;

        int diaActual = GameManager.Instance.CurrentDay;
        if (GameManager.Instance.CurrentState == GameManager.GameState.Results)
            diaActual += 1;

        return Mathf.Max(1, diaActual);
    }

    private DatosPartida CrearDatosIniciales(int slot)
    {
        return new DatosPartida
        {
            slot = slot,
            balanceTotal = 300,
            diaActual = 1,
            stockPastor = 0,
            stockPicadillo = 0,
            stockDesebrada = 0,
            stockTortillas = 0,
            stockCebolla = 0,
            stockSalsa = 0,
            fechaUtc = DateTime.UtcNow.ToString("o")
        };
    }

    private void AsegurarCarpeta()
    {
        if (!Directory.Exists(RutaBase))
            Directory.CreateDirectory(RutaBase);
    }

    private string ObtenerRutaSlot(int slot)
    {
        return Path.Combine(RutaBase, $"{PrefijoArchivo}{slot}{ExtensionArchivo}");
    }

    private static bool EsSlotValido(int slot)
    {
        return slot >= 1 && slot <= MaxSlots;
    }

    private static int ObtenerInicioSemana(int dia, int diasPorSemana)
    {
        int dias = Mathf.Max(1, diasPorSemana);
        int diaSeguro = Mathf.Max(1, dia);
        int semanaBaseCero = (diaSeguro - 1) / dias;
        return (semanaBaseCero * dias) + 1;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null)
            return;

        FieldInfo field = target.GetType().GetField(fieldName, FlagsPrivados | BindingFlags.Public);
        if (field == null)
            return;

        field.SetValue(target, value);
    }

    private static void InvocarEventoEstatico(Type tipo, string nombreEvento, params object[] argumentos)
    {
        if (tipo == null || string.IsNullOrEmpty(nombreEvento))
            return;

        try
        {
            FieldInfo field = tipo.GetField(nombreEvento, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                return;

            Delegate delegado = field.GetValue(null) as Delegate;
            delegado?.DynamicInvoke(argumentos);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GuardadoPartidaManager] No se pudo invocar el evento '{nombreEvento}' de '{tipo.Name}': {ex.Message}");
        }
    }

    private static string FormatearFechaGuardado(string fechaUtc)
    {
        if (string.IsNullOrWhiteSpace(fechaUtc))
            return string.Empty;

        if (DateTimeOffset.TryParse(fechaUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset fechaGuardado))
        {
            DateTime local = fechaGuardado.LocalDateTime;
            return local.ToString("dd/MM/yyyy HH:mm");
        }

        return string.Empty;
    }
}
