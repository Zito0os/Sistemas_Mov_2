#pragma warning disable 0436
using System;
using UnityEngine;

/// <summary>
/// SistemaCompras — Tienda básica de prueba con UI placeholder en OnGUI.
/// Presiona T para abrir/cerrar y comprar ítems usando GestorEconomia.
/// </summary>
public class SistemaCompras : MonoBehaviour
{
    [Serializable]
    private class ItemTiendaPlaceholder
    {
        public string nombre = "Item";
        public int precio = 10;
        public int stock = 99;
    }
    [Header("Debug tienda")]
    [SerializeField] private KeyCode teclaTienda = KeyCode.T;
    [SerializeField] private bool tiendaAbierta;

    [Header("Items placeholder")]
    [SerializeField] private ItemTiendaPlaceholder[] catalogo =
    {
        new ItemTiendaPlaceholder { nombre = "Carne (placeholder)", precio = 20, stock = 10 },
        new ItemTiendaPlaceholder { nombre = "Tortillas (placeholder)", precio = 8, stock = 30 },
        new ItemTiendaPlaceholder { nombre = "Verduras (placeholder)", precio = 12, stock = 20 },
        new ItemTiendaPlaceholder { nombre = "Ingrediente especial (placeholder)", precio = 35, stock = 5 }
    };

    [Header("Estado compra (runtime)")]
    [SerializeField] private string ultimoMensaje = "Sin compras aún.";

    /// <summary>Evento para UI futura o telemetría: (nombreItem, precio, compraExitosa).</summary>
    public static event Action<string, int, bool> OnCompraIntentada;

    private Rect _ventanaRect = new Rect(20, 20, 460, 320);

    private void Update()
    {
        if (Input.GetKeyDown(teclaTienda))
        {
            tiendaAbierta = !tiendaAbierta;
            ultimoMensaje = tiendaAbierta
                ? "Tienda abierta (placeholder)."
                : "Tienda cerrada.";
        }
    }

    private void OnGUI()
    {
        if (!tiendaAbierta) return;

        _ventanaRect = GUI.Window(3317, _ventanaRect, DibujarVentanaTienda, "TIENDA DE PRUEBA");
    }

    private void DibujarVentanaTienda(int windowId)
    {
        GestorEconomia economia = GestorEconomia.Instancia;
        int balance = economia != null ? economia.GetBalance() : 0;

        GUILayout.BeginVertical();
        GUILayout.Label("Presiona T para cerrar");
        GUILayout.Label($"Balance actual: ${balance}");
        GUILayout.Space(8);

        if (economia == null)
        {
            GUILayout.Label("No se encontró GestorEconomia en la escena.");
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
            return;
        }

        if (catalogo == null || catalogo.Length == 0)
        {
            GUILayout.Label("No hay ítems configurados en el catálogo.");
        }
        else
        {
            for (int i = 0; i < catalogo.Length; i++)
            {
                ItemTiendaPlaceholder item = catalogo[i];
                if (item == null) continue;

                GUILayout.BeginHorizontal("box");
                GUILayout.Label($"{item.nombre} | ${item.precio} | Stock: {item.stock}");

                bool sinStock = item.stock <= 0;
                bool sinDinero = balance < item.precio;

                GUI.enabled = !sinStock && !sinDinero;
                if (GUILayout.Button("Comprar", GUILayout.Width(90)))
                {
                    IntentarCompra(item, economia);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(8);
        GUILayout.Label($"Último mensaje: {ultimoMensaje}");

        if (GUILayout.Button("Cerrar tienda"))
            tiendaAbierta = false;

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    private void IntentarCompra(ItemTiendaPlaceholder item, GestorEconomia economia)
    {
        if (item.stock <= 0)
        {
            ultimoMensaje = $"Sin stock de {item.nombre}.";
            OnCompraIntentada?.Invoke(item.nombre, item.precio, false);
            return;
        }

        bool compraExitosa = economia.SpendMoney(item.precio);
        if (!compraExitosa)
        {
            ultimoMensaje = $"Dinero insuficiente para {item.nombre}.";
            OnCompraIntentada?.Invoke(item.nombre, item.precio, false);
            return;
        }

        item.stock--;
        ultimoMensaje = $"Compraste {item.nombre} por ${item.precio}.";
        OnCompraIntentada?.Invoke(item.nombre, item.precio, true);
    }
}
