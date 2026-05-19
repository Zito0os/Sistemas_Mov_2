using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FilaOrdenCompletadaUI : MonoBehaviour
{
    [Header("Estado visual")]
    [SerializeField] private Image  fondoPanel;
    [SerializeField] private Image  imgEstado;       // sprite ✓ o ✗
    [SerializeField] private Sprite spriteCorrect;
    [SerializeField] private Sprite spriteIncorrect;
    [SerializeField] private Color  colorCorrecto   = new Color(0.1f, 0.55f, 0.1f, 0.85f);
    [SerializeField] private Color  colorIncorrecto = new Color(0.55f, 0.1f, 0.1f, 0.85f);
    [SerializeField] private Color  colorCancelada  = new Color(0.35f, 0.35f, 0.1f, 0.85f);

    [Header("Textos")]
    [SerializeField] private TextMeshProUGUI txtNumeroOrden;
    [SerializeField] private TextMeshProUGUI txtCantidad;
    [SerializeField] private TextMeshProUGUI txtCarne;
    [SerializeField] private TextMeshProUGUI txtSalsa;
    [SerializeField] private TextMeshProUGUI txtToppings;   // puede ser null
    [SerializeField] private TextMeshProUGUI txtCobro;

    // ── Llamado cuando se completó (correcto o incorrecto) ──────────────────
    public void InicializarCompletada(Orden orden, int cantidadRequerida, int pagoTotal, bool correcto)
    {
        // Fondo e ícono
        if (fondoPanel != null)
            fondoPanel.color = correcto ? colorCorrecto : colorIncorrecto;

        if (imgEstado != null)
            imgEstado.sprite = correcto ? spriteCorrect : spriteIncorrect;

        // Datos del pedido
        txtNumeroOrden.text = $"#{orden.IDOrden}";
        txtCantidad.text    = $"x{cantidadRequerida}";
        txtCarne.text       = NombreCarne(orden.Carne);

        bool tieneSalsa   = orden.Toppings.Contains(Orden.TipoTopping.Salsa);
        txtSalsa.text     = tieneSalsa ? "Con Salsa " : "Sin Salsa";

        var otrosToppings = orden.Toppings.FindAll(t => t != Orden.TipoTopping.Salsa);
        if (txtToppings != null)
            txtToppings.text = otrosToppings.Count > 0 ? string.Join(", ", otrosToppings) : "";

        // Cobro
        if (correcto)
            txtCobro.text = $"${pagoTotal} cobrado";
        else
            txtCobro.text = "$0 — taco incorrecto";

        txtCobro.color = correcto ? Color.green : Color.red;
    }

    // ── Llamado cuando el cliente se fue sin que le entregaran ─────────────
    public void InicializarCancelada(Orden orden)
    {
        if (fondoPanel != null)
            fondoPanel.color = colorCancelada;

        if (imgEstado != null)
            imgEstado.sprite = spriteIncorrect;

        txtNumeroOrden.text = $"#{orden.IDOrden}";
        txtCantidad.text    = "";
        txtCarne.text       = NombreCarne(orden.Carne);

        bool tieneSalsa = orden.Toppings.Contains(Orden.TipoTopping.Salsa);
        txtSalsa.text   = tieneSalsa ? "Con Salsa ✓" : "Sin Salsa";

        if (txtToppings != null)
        {
            var otrosToppings = orden.Toppings.FindAll(t => t != Orden.TipoTopping.Salsa);
            txtToppings.text = otrosToppings.Count > 0 ? string.Join(", ", otrosToppings) : "";
        }

        txtCobro.text  = "$0";
        txtCobro.color = Color.yellow;
    }

    private string NombreCarne(Orden.TipoCarne carne) => carne switch
    {
        Orden.TipoCarne.Pastor    => "Pastor",
        Orden.TipoCarne.Picadillo => "Picadillo",
        Orden.TipoCarne.Trompo    => "Trompo",
        Orden.TipoCarne.Desebrada => "Desebrada",
        _                         => "Taco"
    };
}