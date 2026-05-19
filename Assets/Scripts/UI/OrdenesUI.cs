using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrdenesUI : MonoBehaviour
{
    public static OrdenesUI Instance { get; private set; }

    [Header("Contenedores de listas")]
    [SerializeField] private Transform contentActivas;
    [SerializeField] private Transform contentCompletadas;

    [Header("Prefabs de fila")]
    [SerializeField] private GameObject prefabFilaActiva;
    [SerializeField] private GameObject prefabFilaCompletada;

    [Header("Texto vacío")]
    [SerializeField] private string textoSinActivas     = "Sin órdenes activas";
    [SerializeField] private string textoSinCompletadas = "Sin órdenes completadas";

    // Mapa clienteIA → fila activa instanciada (para destruirla cuando se complete)
    private Dictionary<ClienteIA, GameObject> _filasActivas = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        SistemaOrdenes.alRecibirOrden   += AlRecibirOrden;
        SistemaOrdenes.alCompletarOrden += AlCompletarOrden;
        SistemaOrdenes.alCancelarOrden  += AlCancelarOrden;
    }

    private void OnDisable()
    {
        SistemaOrdenes.alRecibirOrden   -= AlRecibirOrden;
        SistemaOrdenes.alCompletarOrden -= AlCompletarOrden;
        SistemaOrdenes.alCancelarOrden  -= AlCancelarOrden;
    }

    // ── Eventos de órdenes ────────────────────────────────────────────────────

    private void AlRecibirOrden(ClienteIA cliente, Orden orden, int cantidadRequerida)
    {
        if (orden == null || contentActivas == null) return;
        LimpiarPlaceholder(contentActivas);

        GameObject fila = prefabFilaActiva != null
            ? Instantiate(prefabFilaActiva, contentActivas)
            : CrearFilaDefecto(contentActivas);

        FilaOrdenUI filaUI = fila.GetComponent<FilaOrdenUI>();
        if (filaUI != null)
            filaUI.Inicializar(orden, cantidadRequerida);
        else
        {
            // Fallback texto plano si no hay prefab con FilaOrdenUI
            TextMeshProUGUI tmp = fila.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = FormatearOrdenActiva(orden, cantidadRequerida);
        }

        _filasActivas[cliente] = fila;
        ActualizarPlaceholder(contentActivas, textoSinActivas);
    }

    private void AlCompletarOrden(Orden orden, int pagoTotal, bool correcto, int cantidadRequerida)
    {
        // — Buscar y destruir la fila activa correspondiente —
        ClienteIA clienteKey = null;
        GameObject filaActiva = null;

        foreach (var kv in _filasActivas)
        {
            if (kv.Value == null) continue;

            // Buscar por IDOrden dentro del FilaOrdenUI o del TMP de fallback
            FilaOrdenUI filaUI = kv.Value.GetComponent<FilaOrdenUI>();
            bool coincide = filaUI != null
                ? filaUI.TieneOrden(orden.IDOrden)
                : kv.Value.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains(orden.IDOrden) ?? false;

            if (coincide)
            {
                clienteKey = kv.Key;
                filaActiva = kv.Value;
                break;
            }
        }

        if (filaActiva != null)
        {
            Destroy(filaActiva);
            if (clienteKey != null) _filasActivas.Remove(clienteKey);
        }

        // — Agregar a completadas —
        if (contentCompletadas != null)
        {
            LimpiarPlaceholder(contentCompletadas);

            GameObject fila = prefabFilaCompletada != null
                ? Instantiate(prefabFilaCompletada, contentCompletadas)
                : CrearFilaDefecto(contentCompletadas);

            FilaOrdenCompletadaUI filaCompletadaUI = fila.GetComponent<FilaOrdenCompletadaUI>();
            if (filaCompletadaUI != null)
                filaCompletadaUI.InicializarCompletada(orden, cantidadRequerida, pagoTotal, correcto);
            else
            {
                // Fallback texto plano
                TextMeshProUGUI tmp = fila.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = FormatearOrdenCompletada(orden, cantidadRequerida, pagoTotal, correcto);
            }
        }

        ActualizarPlaceholder(contentActivas,     textoSinActivas);
        ActualizarPlaceholder(contentCompletadas, textoSinCompletadas);
    }

    private void AlCancelarOrden(Orden orden)
    {
        // — Buscar y destruir la fila activa —
        foreach (var kv in _filasActivas)
        {
            if (kv.Value == null) continue;

            FilaOrdenUI filaUI = kv.Value.GetComponent<FilaOrdenUI>();
            bool coincide = filaUI != null
                ? filaUI.TieneOrden(orden.IDOrden)
                : kv.Value.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains(orden.IDOrden) ?? false;

            if (coincide)
            {
                Destroy(kv.Value);
                _filasActivas.Remove(kv.Key);
                break;
            }
        }

        // — Agregar a completadas como cancelada —
        if (contentCompletadas != null)
        {
            LimpiarPlaceholder(contentCompletadas);

            GameObject fila = prefabFilaCompletada != null
                ? Instantiate(prefabFilaCompletada, contentCompletadas)
                : CrearFilaDefecto(contentCompletadas);

            FilaOrdenCompletadaUI filaCompletadaUI = fila.GetComponent<FilaOrdenCompletadaUI>();
            if (filaCompletadaUI != null)
                filaCompletadaUI.InicializarCancelada(orden);
            else
            {
                // Fallback texto plano
                TextMeshProUGUI tmp = fila.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                    tmp.text = $"#{orden.IDOrden} — {NombreCarne(orden.Carne)}\n✗ Se fue el cliente | $0";
            }
        }

        ActualizarPlaceholder(contentActivas,     textoSinActivas);
        ActualizarPlaceholder(contentCompletadas, textoSinCompletadas);
    }

    // ── Formato de texto (fallback sin prefab) ────────────────────────────────

    private string FormatearOrdenActiva(Orden orden, int cantidad)
    {
        string toppings = orden.Toppings.Count > 0
            ? string.Join(", ", orden.Toppings)
            : "sin toppings";

        return $"#{orden.IDOrden}  x{cantidad}\n" +
               $"{NombreCarne(orden.Carne)}\n" +
               $"{toppings}";
    }

    private string FormatearOrdenCompletada(Orden orden, int cantidad, int pagoTotal, bool correcto)
    {
        string estado = correcto ? "✓ Correcto" : "✗ Incorrecto";
        return $"#{orden.IDOrden}  x{cantidad}\n" +
               $"{NombreCarne(orden.Carne)}\n" +
               $"{estado} | ${pagoTotal}";
    }

    private string NombreCarne(Orden.TipoCarne carne) => carne switch
    {
        Orden.TipoCarne.Pastor    => "Pastor",
        Orden.TipoCarne.Picadillo => "Picadillo",
        Orden.TipoCarne.Trompo    => "Trompo",
        Orden.TipoCarne.Desebrada => "Desebrada",
        _                         => "Taco"
    };

    // ── Helpers de layout ─────────────────────────────────────────────────────

    private GameObject CrearFilaDefecto(Transform parent)
    {
        GameObject go = new GameObject("Fila");
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 24;
        tmp.color    = Color.white;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 80);

        return go;
    }

    private void ActualizarPlaceholder(Transform content, string textoVacio)
    {
        if (content == null) return;

        Transform placeholder = content.Find("Placeholder");
        int hijosReales = 0;

        foreach (Transform hijo in content)
        {
            if (hijo != placeholder) hijosReales++;
        }

        if (placeholder == null) return;
        placeholder.gameObject.SetActive(hijosReales == 0);
    }

    private void LimpiarPlaceholder(Transform content)
    {
        // No destruimos el placeholder, ActualizarPlaceholder lo maneja
    }
}