using TMPro;
using UnityEngine;


public class StartDayPanel : MonoBehaviour
{
    [Header("Textos informativos")]
    [SerializeField] private TextMeshProUGUI txt_dia;
    [SerializeField] private TextMeshProUGUI txt_balance;
    [SerializeField] private TextMeshProUGUI txt_instruccion;

    [Header("Formato")]
    [SerializeField] private string formatoDia     = "DIA {0}";
    [SerializeField] private string textoInstruccion =
        "Ve a la tienda a comprar ingredientes. Cuando termines toca el cartel para iniciar el turno.";

    [Header("Auto-ocultar tras N segundos (0 = nunca)")]
    [SerializeField] private float segundosAutoOcultar = 4f;

    private float _timer;
    private bool  _ocultado = false;

    // ── Ciclo de vida ──────────────────────────────────────────────────────────

    private void OnEnable()
    {
        _timer   = segundosAutoOcultar;
        _ocultado = false;

        // Sincronizar textos con el estado actual
        if (GameManager.Instance != null)
        {
            if (txt_dia != null)
                txt_dia.text = string.Format(formatoDia, GameManager.Instance.CurrentDay);
        }

        if (txt_balance != null && GestorEconomia.Instancia != null)
            txt_balance.text = $"Balance: ${GestorEconomia.Instancia.GetBalance()}";

        if (txt_instruccion != null)
            txt_instruccion.text = textoInstruccion;

        Debug.Log("[StartDayPanel] Fase de compras iniciada. El jugador debe ir al cartel para empezar.");
    }

    private void Update()
    {
        if (_ocultado || segundosAutoOcultar <= 0f) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            CerrarPanel();
            Debug.Log("[StartDayPanel] Panel auto-ocultado. StartDay sigue activo — esperando cartel.");
            SoundManager.PlayMusicLoop();
        }
    }

    // ── API pública  ───────────────

    public void CerrarPanel()
    {
        
        _ocultado = true;
        gameObject.SetActive(false);
        //SoundManager.PlayMusicLoop();
    }
}