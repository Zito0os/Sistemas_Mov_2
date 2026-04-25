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
    [Tooltip("Prefab simple: un GameObject con TextMeshProUGUI")]
    [SerializeField] private GameObject prefabFilaActiva;
    [SerializeField] private GameObject prefabFilaCompletada;

    [Header("Texto vacío")]
    [SerializeField] private string textoSinActivas    = "Sin órdenes activas";
    [SerializeField] private string textoSinCompletadas = "Sin órdenes completadas";

    // Mapa clienteIA → fila activa instanciada (para moverla al completarse)
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
        SistemaOrdenes.alCancelarOrden += AlCancelarOrden;

    }

    private void OnDisable()
    {
        SistemaOrdenes.alRecibirOrden   -= AlRecibirOrden;
        SistemaOrdenes.alCompletarOrden -= AlCompletarOrden;
        SistemaOrdenes.alCancelarOrden -= AlCancelarOrden;

    }

    // ── Eventos de órdenes ────────────────────────────────────────────────────

    private void AlRecibirOrden(ClienteIA cliente, Orden orden)
    {
        if (orden == null || contentActivas == null) return;

        // Limpiar placeholder de "sin órdenes" si existe
        LimpiarPlaceholder(contentActivas);

        GameObject fila = prefabFilaActiva != null
            ? Instantiate(prefabFilaActiva, contentActivas)
            : CrearFilaDefecto(contentActivas);

        TextMeshProUGUI tmp = fila.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = FormatearOrdenActiva(orden);

        // Guardar referencia para moverla cuando se complete
        _filasActivas[cliente] = fila;

        ActualizarPlaceholder(contentActivas, textoSinActivas);
    }

    private void AlCompletarOrden(Orden orden, int pagoTotal, bool correcto)
    {
        // Buscar la fila activa correspondiente a esta orden
        ClienteIA clienteKey = null;
        GameObject filaActiva = null;

        foreach (var kv in _filasActivas)
        {
            // La orden es la misma si el IDOrden coincide con alguna activa
            // Comparamos por referencia directa (SistemaOrdenes nos pasa la misma instancia)
            if (kv.Value != null)
            {
                TextMeshProUGUI tmp = kv.Value.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null && tmp.text.Contains(orden.IDOrden))
                {
                    clienteKey = kv.Key;
                    filaActiva = kv.Value;
                    break;
                }
            }
        }


        // Quitar de activas
        if (filaActiva != null)
        {
            Destroy(filaActiva);
            if (clienteKey != null)
                _filasActivas.Remove(clienteKey);
        }

        // Agregar a completadas
        if (contentCompletadas != null)
        {
            LimpiarPlaceholder(contentCompletadas);

            GameObject fila = prefabFilaCompletada != null
                ? Instantiate(prefabFilaCompletada, contentCompletadas)
                : CrearFilaDefecto(contentCompletadas);

            TextMeshProUGUI tmp = fila.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = FormatearOrdenCompletada(orden, pagoTotal, correcto);
        }

        ActualizarPlaceholder(contentActivas, textoSinActivas);
        ActualizarPlaceholder(contentCompletadas, textoSinCompletadas);
    }

   private void AlCancelarOrden(Orden orden)
    {
        // Quitar de activas
        foreach (var kv in _filasActivas)
        {
            if (kv.Value != null)
            {
                TextMeshProUGUI tmp = kv.Value.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null && tmp.text.Contains(orden.IDOrden))
                {
                    Destroy(kv.Value);
                    _filasActivas.Remove(kv.Key);
                    break;
                }
            }
        }

        // Agregar a completadas como no entregada
        if (contentCompletadas != null)
        {
            GameObject fila = prefabFilaCompletada != null
                ? Instantiate(prefabFilaCompletada, contentCompletadas)
                : CrearFilaDefecto(contentCompletadas);

            TextMeshProUGUI tmp = fila.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = $"[{orden.IDOrden}] {NombreCarne(orden.Carne)}\n✗ No entregada  |  $0";
        }

        ActualizarPlaceholder(contentActivas, textoSinActivas);
        ActualizarPlaceholder(contentCompletadas, textoSinCompletadas);
    }
   
    // ── Formato de texto ──────────────────────────────────────────────────────

    private string FormatearOrdenActiva(Orden orden)
    {
        string toppings = orden.Toppings.Count > 0
            ? string.Join(", ", orden.Toppings)
            : "sin toppings";

        return $"[{orden.IDOrden}] {NombreCarne(orden.Carne)}\n" +
               $"{toppings}\n" +
               $"Precio base: ${orden.PrecioBase}";
    }

    private string FormatearOrdenCompletada(Orden orden, int pagoTotal, bool correcto)
    {
        string estado = correcto ? "✓ Correcto" : "✗ Incorrecto";
        return $"[{orden.IDOrden}] {NombreCarne(orden.Carne)}\n" +
               $"{estado}  |  Cobrado: ${pagoTotal}";
    }

    private string NombreCarne(Orden.TipoCarne carne)
    {
        switch (carne)
        {
            case Orden.TipoCarne.Pastor:    return "Pastor";
            case Orden.TipoCarne.Picadillo: return "Picadillo";
            case Orden.TipoCarne.Trompo:    return "Trompo";
            case Orden.TipoCarne.Desebrada: return "Desebrada";
            default:                        return "Taco";
        }
    }

    // ── Helpers de layout ─────────────────────────────────────────────────────

    /// <summary>Crea una fila mínima si no hay prefab asignado.</summary>
    private GameObject CrearFilaDefecto(Transform parent)
    {
        GameObject go = new GameObject("Fila");
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 24;
        tmp.color = Color.white;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 80);

        return go;
    }

    /// <summary>
    /// Si el Content no tiene hijos reales (solo el placeholder), muestra el texto vacío.
    /// Si tiene hijos reales, oculta el placeholder.
    /// </summary>
    private void ActualizarPlaceholder(Transform content, string textoVacio)
    {
        if (content == null) return;

        // Contamos hijos que NO sean el placeholder
        int hijosReales = 0;
        Transform placeholder = content.Find("Placeholder");

        foreach (Transform hijo in content)
        {
            if (hijo != placeholder) hijosReales++;
        }

        if (placeholder == null) return;

        placeholder.gameObject.SetActive(hijosReales == 0);
    }

    /// <summary>Quita el placeholder si existe al agregar el primer elemento real.</summary>
    private void LimpiarPlaceholder(Transform content)
    {
        // No lo destruimos, solo lo ocultamos — ActualizarPlaceholder lo maneja
    }
}