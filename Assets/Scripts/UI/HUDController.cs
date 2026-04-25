using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private TextMeshProUGUI txt_dinero;
    [SerializeField] private TextMeshProUGUI txt_dia;

    [Header("Formato (usa {0} para el valor)")]
    [SerializeField] private string formatoDinero = "${0}";
    [SerializeField] private string formatoDia    = "Dia {0}";

    private void OnEnable()
    {
        GestorEconomia.OnMoneyChanged += AlCambiarDinero;
        GameManager.OnDayChanged      += AlCambiarDia;
    }

    private void OnDisable()
    {
        GestorEconomia.OnMoneyChanged -= AlCambiarDinero;
        GameManager.OnDayChanged      -= AlCambiarDia;
    }

    private void Start()
    {
        // Sincronizar con el estado actual al activarse
        if (GestorEconomia.Instancia != null)
            AlCambiarDinero(GestorEconomia.Instancia.GetBalance());

        if (GameManager.Instance != null)
            AlCambiarDia(GameManager.Instance.CurrentDay);
    }

    private void AlCambiarDinero(int cantidad)
    {
        if (txt_dinero != null)
            txt_dinero.text = string.Format(formatoDinero, cantidad);
    }

    private void AlCambiarDia(int dia)
    {
        if (txt_dia != null)
            txt_dia.text = string.Format(formatoDia, dia);
    }

    // Llamado por btn_pausa desde el Inspector (OnClick)
    public void OnBotonPausa() => UIManager.Instance?.TogglePausa();


}