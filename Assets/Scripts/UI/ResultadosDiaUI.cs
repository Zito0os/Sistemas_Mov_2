using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ResultadosDiaUI : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private TextMeshProUGUI txt_titulo;
    [SerializeField] private TextMeshProUGUI txt_ganancias;
    [SerializeField] private TextMeshProUGUI txt_balance;

    [Header("Botón")]
    [SerializeField] private Button btn_siguiente;

    [Header("Formato")]
    [SerializeField] private string formatoTitulo    = "Fin del Día {0}";
    [SerializeField] private string formatoGanancias = "Ganancias del día: ${0}";
    [SerializeField] private string formatoBalance   = "Balance total: ${0}";

    private void Awake()
    {
        if (btn_siguiente != null)
            btn_siguiente.onClick.AddListener(SiguienteDia);
    }

    private void OnDestroy()
    {
        if (btn_siguiente != null)
            btn_siguiente.onClick.RemoveListener(SiguienteDia);
    }

    private void OnEnable()
    {
        // Cada vez que el panel se activa (UIManager lo activa al entrar a Results),
        // se leen los datos frescos del frame actual.
        GameManager.OnStateChanged += AlCambiarEstado;
        ActualizarResumen();
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= AlCambiarEstado;
    }

    private void AlCambiarEstado(GameManager.GameState estado)
    {
        if (estado == GameManager.GameState.Results)
            ActualizarResumen();
    }

    private void ActualizarResumen()
    {
        int dia      = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 0;
        int balance  = GestorEconomia.Instancia != null ? GestorEconomia.Instancia.GetBalance() : 0;

        // GestorEconomia no expone ingresosAcumulados públicamente.
        // Para mostrar ganancias del día agrega esta línea a GestorEconomia.cs:
        //   public int IngresosDia => ingresosAcumulados;
        // Por ahora mostramos el balance total en ambas líneas hasta que se exponga.
        int ganancias = GestorEconomia.Instancia != null ? GestorEconomia.Instancia.IngresosDia : 0;;

        if (txt_titulo    != null) txt_titulo.text    = string.Format(formatoTitulo, dia);
        if (txt_ganancias != null) txt_ganancias.text = string.Format(formatoGanancias, ganancias);
        if (txt_balance   != null) txt_balance.text   = string.Format(formatoBalance, balance);
    }

    /// <summary>Llamado por btn_siguiente (OnClick en el Inspector).</summary>
    public void SiguienteDia()
    {
        SoundManager.PlaySound(SoundType.BubblePop);
        GameManager.Instance?.AdvanceToNextState();
    }
}