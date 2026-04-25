using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuPausa : MonoBehaviour
{
    [Header("Sub-paneles internos")]
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject panelOpciones;

    [Header("Botones de pausa")]
    [SerializeField] private Button btn_reanudar;
    [SerializeField] private Button btn_opciones;
    [SerializeField] private Button btn_menu;

    [Header("Opciones")]
    [SerializeField] private Toggle toggle_vibracion;
    [SerializeField] private Button btn_cerrar_opciones;

    // Clave de PlayerPrefs para persistir la preferencia
    private const string KEY_VIBRACION = "vibracion_activa";

    private void Awake()
    {
        // Botones de pausa
        btn_reanudar?.onClick.AddListener(Reanudar);
        btn_opciones?.onClick.AddListener(AbrirOpciones);
        btn_menu?.onClick.AddListener(IrAlMenu);

        // Opciones
        btn_cerrar_opciones?.onClick.AddListener(CerrarOpciones);
        toggle_vibracion?.onValueChanged.AddListener(AlCambiarVibracion);
    }

    private void OnDestroy()
    {
        btn_reanudar?.onClick.RemoveAllListeners();
        btn_opciones?.onClick.RemoveAllListeners();
        btn_menu?.onClick.RemoveAllListeners();
        btn_cerrar_opciones?.onClick.RemoveAllListeners();
        toggle_vibracion?.onValueChanged.RemoveAllListeners();
    }

    private void OnEnable()
    {
        // Al abrirse el panel de pausa, mostrar pausa y ocultar opciones
        Mostrar(panelPausa);
        Ocultar(panelOpciones);

        // Sincronizar el toggle con el valor guardado
        if (toggle_vibracion != null)
            toggle_vibracion.isOn = PlayerPrefs.GetInt(KEY_VIBRACION, 1) == 1;
    }

    // ── Botones de pausa ──────────────────────────────────────────────────────

    public void Reanudar()
    {
        UIManager.Instance?.CerrarPausa();
    }

    public void AbrirOpciones()
    {
        Ocultar(panelPausa);
        Mostrar(panelOpciones);

        // Sincronizar toggle al abrir opciones
        if (toggle_vibracion != null)
            toggle_vibracion.isOn = PlayerPrefs.GetInt(KEY_VIBRACION, 1) == 1;
    }

    public void IrAlMenu()
    {
        UIManager.Instance?.IrAlMenuPrincipal();
    }

    // ── Opciones ──────────────────────────────────────────────────────────────

    public void CerrarOpciones()
    {
        Mostrar(panelPausa);
        Ocultar(panelOpciones);
    }

    private void AlCambiarVibracion(bool activa)
    {
        // Guardar preferencia
        PlayerPrefs.SetInt(KEY_VIBRACION, activa ? 1 : 0);
        PlayerPrefs.Save();

        // Si existe el servicio de vibración, notificarlo
        // ServiciosMovil.Instance?.SetVibracionActiva(activa);
        Debug.Log($"[MenuPausa] Vibración: {(activa ? "ON" : "OFF")}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Consulta pública para que otros sistemas sepan si la vibración está activa.
    /// Uso: MenuPausa.VibracionActiva
    /// </summary>
    public static bool VibracionActiva =>
        PlayerPrefs.GetInt(KEY_VIBRACION, 1) == 1;

    private static void Mostrar(GameObject p) { if (p != null) p.SetActive(true); }
    private static void Ocultar(GameObject p)  { if (p != null) p.SetActive(false); }
}