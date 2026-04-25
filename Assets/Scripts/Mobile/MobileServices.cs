using UnityEngine;

/// <summary>
/// MobileServices — Singleton fachada que orquesta todos los servicios del dispositivo móvil.
///
/// Responsabilidades:
///   - Punto único de acceso a vibración / haptics (y en el futuro: giroscopio, cámara, notificaciones)
///   - Persistencia de toggles on/off vía PlayerPrefs
///   - Sobrevive entre cargas de escena (DontDestroyOnLoad)
///
/// Uso desde otros scripts:
///   MobileServices.Instance.Haptics.VibrarCorto();
///   MobileServices.Instance.VibracionActivada = false;
///
/// Cómo se conecta:
///   - Coloca un GameObject vacío en la escena TestClientes con este componente
///   - El HapticsManager se suscribe automáticamente a los eventos del juego en Awake/OnEnable
///   - Los toggles se guardan en PlayerPrefs y persisten entre sesiones (cuenta como
///     1 de las 3 variables de persistencia que pide la rúbrica)
/// </summary>
public class MobileServices : MonoBehaviour
{
    // SINGLETON

    public static MobileServices Instance { get; private set; }

    // KEYS DE PLAYERPREFS (centralizadas para evitar typos repartidos por el código)

    private const string KEY_VIBRACION = "taquero_vibracion_on";
    // Para futuros módulos:
    // private const string KEY_GIROSCOPIO = "taquero_gyro_on";
    // private const string KEY_NOTIFICACIONES = "taquero_notif_on";

    // SERVICIOS

    /// <summary>Manager de vibración / haptics. Siempre disponible.</summary>
    public HapticsManager Haptics { get; private set; }

    // Para futuros módulos (entrega final):
    // public GyroscopeManager Gyro { get; private set; }
    // public CameraManager Camera { get; private set; }
    // public NotificacionesManager Notificaciones { get; private set; }

    // INSPECTOR — Configuración inicial (solo si NO hay valor previo en PlayerPrefs)

    [Header("Configuración por defecto (primera vez)")]
    [Tooltip("Si nunca se ha jugado antes, ¿la vibración arranca activada?")]
    [SerializeField] private bool vibracionPorDefecto = true;

    [Header("Debug")]
    [SerializeField] private bool logsActivos = true;

    // TOGGLES PÚBLICOS

    private bool _vibracionActivada;

    /// <summary>
    /// ¿La vibración está activada? Se persiste automáticamente en PlayerPrefs.
    /// La UI de opciones debe leer y escribir esta propiedad.
    /// </summary>
    public bool VibracionActivada
    {
        get => _vibracionActivada;
        set
        {
            if (_vibracionActivada == value) return;
            _vibracionActivada = value;
            PlayerPrefs.SetInt(KEY_VIBRACION, value ? 1 : 0);
            PlayerPrefs.Save();

            if (logsActivos)
                Debug.Log($"[MobileServices] Vibración {(value ? "ACTIVADA" : "DESACTIVADA")} (guardado en PlayerPrefs).");
        }
    }

    // CICLO DE VIDA

    private void Awake()
    {
        // Patrón singleton estándar (mismo que GameManager)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CargarConfiguracionPersistida();
        InicializarServicios();
    }

    private void OnDestroy()
    {
        // Si esta es la instancia activa, limpiamos referencias
        if (Instance != this) return;

        // Importante: liberar las suscripciones a eventos para evitar leaks
        Haptics?.LiberarSuscripciones();
    }

    // INICIALIZACIÓN

    private void CargarConfiguracionPersistida()
    {
        // Si nunca se ha jugado, usar el valor por defecto del Inspector
        if (!PlayerPrefs.HasKey(KEY_VIBRACION))
        {
            _vibracionActivada = vibracionPorDefecto;
            PlayerPrefs.SetInt(KEY_VIBRACION, vibracionPorDefecto ? 1 : 0);
            PlayerPrefs.Save();
        }
        else
        {
            _vibracionActivada = PlayerPrefs.GetInt(KEY_VIBRACION) == 1;
        }

        if (logsActivos)
            Debug.Log($"[MobileServices] Config cargada — Vibración: {_vibracionActivada}");
    }

    private void InicializarServicios()
    {
        // Crear y arrancar el HapticsManager
        Haptics = new HapticsManager(this, logsActivos);
        Haptics.RegistrarSuscripciones();

        if (logsActivos)
            Debug.Log("[MobileServices] Servicios inicializados.");
    }

    // API PÚBLICA AUXILIAR

    /// <summary>
    /// Activa o desactiva todas las vibraciones de un solo golpe.
    /// La UI de opciones puede llamar esto directamente desde un Toggle.
    /// </summary>
    public void ToggleVibracion(bool activar)
    {
        VibracionActivada = activar;
    }

    /// <summary>Restablece toda la configuración a los valores por defecto.</summary>
    public void RestablecerConfiguracion()
    {
        PlayerPrefs.DeleteKey(KEY_VIBRACION);
        PlayerPrefs.Save();
        CargarConfiguracionPersistida();

        if (logsActivos)
            Debug.Log("[MobileServices] Configuración restablecida a valores por defecto.");
    }
}