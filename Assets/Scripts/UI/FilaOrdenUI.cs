using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class FilaOrdenUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtNumeroOrden;
    [SerializeField] private TextMeshProUGUI txtCantidad;
    [SerializeField] private TextMeshProUGUI txtCarne;
    [SerializeField] private TextMeshProUGUI txtSalsa;
    [SerializeField] private TextMeshProUGUI txtToppings; // opcional

    private string _idOrden;

    public void Inicializar(Orden orden, int cantidadRequerida)
    {
        _idOrden = orden.IDOrden;
        txtNumeroOrden.text = $"#{orden.IDOrden}";
        txtCantidad.text    = $"x{cantidadRequerida}";
        txtCarne.text       = NombreCarne(orden.Carne);

        bool tieneSalsa = orden.Toppings.Contains(Orden.TipoTopping.Salsa);
        txtSalsa.text   = tieneSalsa ? "Con Salsa " : "Sin Salsa";

        // Toppings sin contar la salsa (ya la mostramos separada)
        var otrosToppings = orden.Toppings.FindAll(t => t != Orden.TipoTopping.Salsa);
        if (txtToppings != null)
            txtToppings.text = otrosToppings.Count > 0
                ? string.Join(", ", otrosToppings)
                : "";
    }

    private string NombreCarne(Orden.TipoCarne carne) => carne switch
    {
        Orden.TipoCarne.Pastor    => "Pastor",
        Orden.TipoCarne.Picadillo => "Picadillo",
        Orden.TipoCarne.Trompo    => "Trompo",
        Orden.TipoCarne.Desebrada => "Desebrada",
        _                         => "Taco"
    };

    public bool TieneOrden(string idOrden) => _idOrden == idOrden;
}