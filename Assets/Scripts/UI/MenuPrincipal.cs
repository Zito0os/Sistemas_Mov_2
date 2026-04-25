using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuPrincipal : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelOpciones;

    [Header("Botones del menú")]
    [SerializeField] private Button btn_jugar;
    [SerializeField] private Button btn_opciones;
    [SerializeField] private Button btn_salir;

    [Header("Opciones")]
    [SerializeField] private Toggle  toggle_vibracion;
    [SerializeField] private Button  btn_cerrar_opciones;

    [Header("Nombre de la escena del juego")]
    [SerializeField] private string nombreEscenaJuego = "SampleScene";

    private const string KEY_VIBRACION = "vibracion_activa";

    private void Awake()
    {
        btn_jugar?.onClick.AddListener(Jugar);
        btn_opciones?.onClick.AddListener(AbrirOpciones);
        btn_salir?.onClick.AddListener(Salir);
        btn_cerrar_opciones?.onClick.AddListener(CerrarOpciones);
        toggle_vibracion?.onValueChanged.AddListener(AlCambiarVibracion);
    }

    private void OnDestroy()
    {
        btn_jugar?.onClick.RemoveAllListeners();
        btn_opciones?.onClick.RemoveAllListeners();
        btn_salir?.onClick.RemoveAllListeners();
        btn_cerrar_opciones?.onClick.RemoveAllListeners();
        toggle_vibracion?.onValueChanged.RemoveAllListeners();
    }

    private void Start()
    {
        Mostrar(panelMenu);
        Ocultar(panelOpciones);

        // Sincronizar toggle con preferencia guardada
        if (toggle_vibracion != null)
            toggle_vibracion.isOn = PlayerPrefs.GetInt(KEY_VIBRACION, 1) == 1;
    }

    // ── Botones ───────────────────────────────────────────────────────────────

    public void Jugar()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void AbrirOpciones()
    {
        Ocultar(panelMenu);
        Mostrar(panelOpciones);

        if (toggle_vibracion != null)
            toggle_vibracion.isOn = PlayerPrefs.GetInt(KEY_VIBRACION, 1) == 1;
    }

    public void CerrarOpciones()
    {
        Mostrar(panelMenu);
        Ocultar(panelOpciones);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void AlCambiarVibracion(bool activa)
    {
        PlayerPrefs.SetInt(KEY_VIBRACION, activa ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[MenuPrincipal] Vibración: {(activa ? "ON" : "OFF")}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void Mostrar(GameObject p) { if (p != null) p.SetActive(true); }
    private static void Ocultar(GameObject p)  { if (p != null) p.SetActive(false); }
}