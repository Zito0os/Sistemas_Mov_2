#pragma warning disable 0436
using UnityEngine;
using UnityEngine.UI;
public class TutorialCocina : MonoBehaviour
{
    private const string PREFS_KEY = "TutorialCocinaVisto";

    [Header("Referencias UI")]
    [SerializeField] private GameObject panelRaiz;

    [Header("Debug")]
    [SerializeField] private bool resetearEnEditor = false;

    private bool _visible = false;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
#if UNITY_EDITOR
        if (resetearEnEditor)
            PlayerPrefs.DeleteKey(PREFS_KEY);
#endif
        if (panelRaiz != null) panelRaiz.SetActive(false);
    }

    private void OnEnable()  => GameManager.OnStateChanged += AlCambiarEstado;
    private void OnDisable() => GameManager.OnStateChanged -= AlCambiarEstado;

    private void Update()
    {
        if (!_visible) return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) CerrarPanel();
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            CerrarPanel();
#endif
    }

    // ── Lógica ───────────────────────────────────────────────────────────────

    private void AlCambiarEstado(GameManager.GameState estado)
    {
        if (estado != GameManager.GameState.Playing) return;

        bool yaSeVio = PlayerPrefs.GetInt(PREFS_KEY, 0) == 1;
        if (yaSeVio) return;

        MostrarPanel();
    }

    private void MostrarPanel()
    {
        if (panelRaiz != null) panelRaiz.SetActive(true);
        _visible = true;

        // Pausar el juego mientras el jugador lee los controles
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        Debug.Log("[TutorialCocina] Panel mostrado. Juego pausado.");
    }

    private void CerrarPanel()
    {
        if (!_visible) return;
        _visible = false;
        if (panelRaiz != null) panelRaiz.SetActive(false);

        // Reanudar el juego
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        PlayerPrefs.SetInt(PREFS_KEY, 1);
        PlayerPrefs.Save();

        Debug.Log("[TutorialCocina] Cerrado. Juego reanudado.");
    }
}