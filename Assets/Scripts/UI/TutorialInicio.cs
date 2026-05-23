#pragma warning disable 0436
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialInicio : MonoBehaviour
{
    private const string PREFS_KEY = "TutorialInicioVisto";

    [Header("Referencias UI")]
    [SerializeField] private GameObject panelRaiz;

    [Header("Timing")]
    [Tooltip("Segundos a esperar antes de mostrar el tutorial. Debe ser >= segundosAutoOcultar del StartDayPanel (default 4).")]
    [SerializeField] private float delayTrasStartDay = 4.2f;

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
        if (estado != GameManager.GameState.StartDay) return;

        bool esPrimerDia = GameManager.Instance != null && GameManager.Instance.CurrentDay == 1;
        bool yaSeVio     = PlayerPrefs.GetInt(PREFS_KEY, 0) == 1;

        if (!esPrimerDia || yaSeVio) return;

        StartCoroutine(MostrarTrasDelay());
    }

    private IEnumerator MostrarTrasDelay()
    {
        yield return new WaitForSeconds(delayTrasStartDay);
        MostrarPanel();
    }

    private void MostrarPanel()
    {
        if (panelRaiz != null) panelRaiz.SetActive(true);
        _visible = true;
        Debug.Log("[TutorialInicio] Panel mostrado.");
    }

    private void CerrarPanel()
    {
        if (!_visible) return;
        _visible = false;
        if (panelRaiz != null) panelRaiz.SetActive(false);
        PlayerPrefs.SetInt(PREFS_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("[TutorialInicio] Cerrado y marcado como visto.");
    }
}