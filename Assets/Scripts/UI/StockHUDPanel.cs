using UnityEngine;
using TMPro;


public class StockHUDPanel : MonoBehaviour
{
    [Header("Panel raíz (para mostrar/ocultar)")]
    [SerializeField] private GameObject panelStock;

    [Header("Textos de stock")]
    [SerializeField] private TextMeshProUGUI txt_stock_pastor;
    [SerializeField] private TextMeshProUGUI txt_stock_picadillo;
    [SerializeField] private TextMeshProUGUI txt_stock_desebrada;
    [SerializeField] private TextMeshProUGUI txt_stock_tortillas;
    [SerializeField] private TextMeshProUGUI txt_stock_cebolla;
    [SerializeField] private TextMeshProUGUI txt_stock_salsa;

    [Header("Formato del texto")]
    [SerializeField] private string formato = "{0}: {1}";

    [Header("Mostrar siempre durante Playing (false = solo con botón toggle)")]
    [SerializeField] private bool siempreVisible = true;

    // ── Ciclo de vida ──────────────────────────────────────────────────────────

    private void OnEnable()
    {
        GameManager.OnStateChanged        += AlCambiarEstado;
        SistemaCompras.OnCompraIntentada  += AlComprar;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged        -= AlCambiarEstado;
        SistemaCompras.OnCompraIntentada  -= AlComprar;
    }

    private void Start()
    {
        if (panelStock != null)
            panelStock.SetActive(false);
    }

    // ── Listeners ─────────────────────────────────────────────────────────────

    private void AlCambiarEstado(GameManager.GameState estado)
    {
        if (estado == GameManager.GameState.Playing && siempreVisible)
        {
            MostrarPanel(true);
            Refrescar();
        }
        else if (estado != GameManager.GameState.Playing)
        {
            MostrarPanel(false);
        }
    }

    private void AlComprar(string nombre, int precio, bool exito)
    {
        if (exito) Refrescar();
    }

    // ── Toggle manual (asignar al botón del HUD si siempreVisible = false) ────

    public void TogglePanel()
    {
        if (panelStock == null) return;
        panelStock.SetActive(!panelStock.activeSelf);
        if (panelStock.activeSelf) Refrescar();
    }

    // ── Refresco ──────────────────────────────────────────────────────────────

    private void Refrescar()
    {
        CookingStation cs = CookingStation.Instance;
        if (cs == null) return;

        SetTexto(txt_stock_pastor,    "Pastor",    cs.stock_pastor);
        SetTexto(txt_stock_picadillo, "Picadillo", cs.stock_picadillo);
        SetTexto(txt_stock_desebrada, "Desebrada", cs.stock_desebrada);
        SetTexto(txt_stock_tortillas, "Tortillas", cs.stock_tortillas);
        SetTexto(txt_stock_cebolla,   "Cebolla",   cs.stock_cebolla);
        SetTexto(txt_stock_salsa,     "Salsa",     cs.stock_salsa);
    }

    private void MostrarPanel(bool mostrar)
    {
        if (panelStock != null)
            panelStock.SetActive(mostrar);
    }

    private void SetTexto(TextMeshProUGUI tmp, string nombre, int cantidad)
    {
        if (tmp == null) return;
        tmp.text = string.Format(formato, nombre, cantidad);

        // Color rojo si no hay stock
        tmp.color = cantidad <= 0 ? new Color(0.9f, 0.3f, 0.3f) : Color.white;
    }
}