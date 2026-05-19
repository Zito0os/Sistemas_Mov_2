using System.Collections;
using UnityEngine;

public class SlotCocina : MonoBehaviour
{
    public enum RestriccionSlot
    {
        AutoPorNumero,
        SoloCarne,
        SoloTortilla,
        CarneYTortilla
    }

    [Header("Configuracion Slot")]
    public int numeroSlot = 1;
    public bool autoDetectarNumeroDesdeNombre = true;
    public RestriccionSlot restriccion = RestriccionSlot.AutoPorNumero;
    public float tiempoCoccion = 10f;

    [Header("Tortilla Dos Etapas")]
    public float tiempoCoccionPorLadoTortilla = 10f;
    public float ventanaDobleTapTortilla = 0.35f;
    public float duracionVolteoTortilla = 0.3f;

    [Header("Visual Tortilla")]
    public GameObject tortillaTacoVisual;
    public string nombreVisualTortilla = "Tortilla_taco";

    [Header("Visual Carne")]
    public GameObject pastorModelVisual;
    public GameObject picadilloModelVisual;
    public GameObject desebradaModelVisual;
    public string nombrePastorModel = "Pastor_model";
    public string nombrePicadilloModel = "Picadillo_model";
    public string nombreDesebradaModel = "desebrada_model";

    [Header("Visual Taco Hecho")]
    public GameObject tacoHechoVisual;
    public string nombreTacoHecho = "taco_hecho";

    [Header("Estado (solo lectura)")]
    [SerializeField] private IngredienteCocina ingredienteActual = IngredienteCocina.Ninguno;
    [SerializeField] private bool estaCocido = false;
    [SerializeField] private int etapaTortilla = 0;
    [SerializeField] private IngredienteCocina carneActualEnTortilla = IngredienteCocina.Ninguno;
    // etapaTortilla: 0=sin tortilla, 1=lado1 cocinandose, 2=lista para voltear,
    //               3=lado2 cocinandose, 4=cocida

    private Coroutine coccionCoroutine;
    private Coroutine volteoCoroutine;
    private float ultimoTapTortillaTiempo = -10f;
    private Vector3 posicionOriginal;

    private void Awake()
    {
        posicionOriginal = transform.position;

        if (autoDetectarNumeroDesdeNombre)
            numeroSlot = ExtraerNumeroSlot(name, numeroSlot);

        if (tortillaTacoVisual == null)
        {
            Transform visual = transform.Find(nombreVisualTortilla);
            if (visual != null) tortillaTacoVisual = visual.gameObject;
        }

        if (pastorModelVisual == null)
        {
            Transform visual = transform.Find(nombrePastorModel);
            if (visual != null) pastorModelVisual = visual.gameObject;
        }

        if (picadilloModelVisual == null)
        {
            Transform visual = transform.Find(nombrePicadilloModel);
            if (visual != null) picadilloModelVisual = visual.gameObject;
        }

        if (desebradaModelVisual == null)
        {
            Transform visual = transform.Find(nombreDesebradaModel);
            if (visual != null) desebradaModelVisual = visual.gameObject;
        }

        if (tacoHechoVisual == null)
        {
            Transform visual = transform.Find(nombreTacoHecho);
            if (visual != null) tacoHechoVisual = visual.gameObject;
        }

        ActualizarVisualesIngredientes();
    }

    // ─── PUNTO DE ENTRADA PRINCIPAL (llamado por Gestos.cs al tocar el slot) ──

    public void InteractuarConSlot()
    {
        CookingStation station = CookingStation.Instance;
        if (station == null) return;

        // Slot vacío → intentar depositar lo que traes en mano
        if (ingredienteActual == IngredienteCocina.Ninguno)
        {
            IntentarColocarIngrediente(station);
            return;
        }

        // Tortilla cocida (etapa 4) → solo drag and drop de carne, no toque directo
        if (ingredienteActual == IngredienteCocina.Tortilla && etapaTortilla == 4)
            return;

        // Carne cocida → retirar al tocarlo (devuelve la mano libre)
        if (estaCocido && ingredienteActual != IngredienteCocina.Tortilla)
        {
            RetirarIngredienteCocido(station);
            return;
        }

        // Tortilla esperando volteo → procesar doble tap
        if (ingredienteActual == IngredienteCocina.Tortilla && carneActualEnTortilla == IngredienteCocina.Ninguno)
            ProcesarTapTortilla();
    }

    // ─── COLOCAR INGREDIENTE EN SLOT ─────────────────────────────────────────

    private void IntentarColocarIngrediente(CookingStation station)
    {
        // Leer qué trae el jugador en la mano
        IngredienteCocina enMano = station.ObtenerIngredienteSeleccionado();
        if (enMano == IngredienteCocina.Ninguno) return;

        if (!PuedeRecibir(enMano)) return;

        // Soltar el ingrediente de la mano y colocarlo en el slot
        station.SoltarIngrediente();

        ingredienteActual = enMano;
        estaCocido = false;
        etapaTortilla = 0;
        ultimoTapTortillaTiempo = -10f;
        ActualizarVisualesIngredientes();

        if (coccionCoroutine != null)
            StopCoroutine(coccionCoroutine);

        if (ingredienteActual == IngredienteCocina.Tortilla)
        {
            etapaTortilla = 1;
            coccionCoroutine = StartCoroutine(CocinarLadoUnoTortilla());
            return;
        }

        coccionCoroutine = StartCoroutine(CocinarConTiempo());
    }

    // ─── COCCIÓN ─────────────────────────────────────────────────────────────

    private IEnumerator CocinarConTiempo()
    {
        yield return new WaitForSeconds(tiempoCoccion);
        estaCocido = true;
        coccionCoroutine = null;
    }

    private IEnumerator CocinarLadoUnoTortilla()
    {
        yield return new WaitForSeconds(tiempoCoccionPorLadoTortilla);
        etapaTortilla = 2;
        coccionCoroutine = null;
    }

    private IEnumerator CocinarLadoDosTortilla()
    {
        yield return new WaitForSeconds(tiempoCoccionPorLadoTortilla);
        etapaTortilla = 4;
        estaCocido = true;
        coccionCoroutine = null;
    }

    // ─── RETIRAR COCIDO ──────────────────────────────────────────────────────

    private void RetirarIngredienteCocido(CookingStation station)
    {
        station.AgregarIngredienteCocido(ingredienteActual, 1);
        LimpiarSlot();
    }

    // ─── TORTILLA VOLTEO ─────────────────────────────────────────────────────

    private void ProcesarTapTortilla()
    {
        if (estaCocido || etapaTortilla != 2) return;
        if (volteoCoroutine != null) return;

        float ahora = Time.time;
        if (ahora - ultimoTapTortillaTiempo <= ventanaDobleTapTortilla)
        {
            ultimoTapTortillaTiempo = -10f;
            volteoCoroutine = StartCoroutine(VoltearTortillaYContinuar());
            return;
        }
        ultimoTapTortillaTiempo = ahora;
    }

    private IEnumerator VoltearTortillaYContinuar()
    {
        Transform objetivoVolteo = tortillaTacoVisual != null ? tortillaTacoVisual.transform : transform;
        Quaternion rotacionInicial = objetivoVolteo.localRotation;

        float tiempo = 0f;
        while (tiempo < duracionVolteoTortilla)
        {
            tiempo += Time.deltaTime;
            float t = duracionVolteoTortilla <= 0f ? 1f : Mathf.Clamp01(tiempo / duracionVolteoTortilla);
            objetivoVolteo.localRotation = rotacionInicial * Quaternion.Euler(360f * t, 0f, 0f);
            yield return null;
        }

        objetivoVolteo.localRotation = rotacionInicial;
        etapaTortilla = 3;
        volteoCoroutine = null;
        coccionCoroutine = StartCoroutine(CocinarLadoDosTortilla());
    }

    // ─── LIMPIAR ─────────────────────────────────────────────────────────────

    private void LimpiarSlot()
    {
        if (coccionCoroutine != null) { StopCoroutine(coccionCoroutine); coccionCoroutine = null; }
        if (volteoCoroutine != null)  { StopCoroutine(volteoCoroutine);  volteoCoroutine  = null; }

        ingredienteActual       = IngredienteCocina.Ninguno;
        carneActualEnTortilla   = IngredienteCocina.Ninguno;
        estaCocido              = false;
        etapaTortilla           = 0;
        ultimoTapTortillaTiempo = -10f;
        ActualizarVisualesIngredientes();
    }

    // ─── VISUALES ─────────────────────────────────────────────────────────────

    private void ActualizarVisualesIngredientes()
    {
        bool esTortilla      = ingredienteActual == IngredienteCocina.Tortilla;
        bool tieneTacoHecho  = esTortilla && carneActualEnTortilla != IngredienteCocina.Ninguno;

        if (tortillaTacoVisual != null)
            tortillaTacoVisual.SetActive(esTortilla && !tieneTacoHecho);

        if (tacoHechoVisual != null)
            tacoHechoVisual.SetActive(tieneTacoHecho);

        // Nunca mostrar tortilla base si el taco hecho está activo
        if (tortillaTacoVisual != null && tacoHechoVisual != null && tacoHechoVisual.activeSelf)
            tortillaTacoVisual.SetActive(false);

        if (pastorModelVisual   != null) pastorModelVisual.SetActive(ingredienteActual == IngredienteCocina.Pastor);
        if (picadilloModelVisual!= null) picadilloModelVisual.SetActive(ingredienteActual == IngredienteCocina.Picadillo);
        if (desebradaModelVisual!= null) desebradaModelVisual.SetActive(ingredienteActual == IngredienteCocina.Desebrada);
    }

    // ─── RESTRICCIONES ───────────────────────────────────────────────────────

    private bool PuedeRecibir(IngredienteCocina ingrediente)
    {
        switch (restriccion)
        {
            case RestriccionSlot.SoloCarne:
                return ingrediente != IngredienteCocina.Tortilla;
            case RestriccionSlot.SoloTortilla:
                return ingrediente == IngredienteCocina.Tortilla;
            case RestriccionSlot.CarneYTortilla:
                return ingrediente != IngredienteCocina.Ninguno;
            case RestriccionSlot.AutoPorNumero:
            default:
                if (numeroSlot >= 13 && numeroSlot <= 16)
                    return ingrediente == IngredienteCocina.Tortilla;
                return ingrediente != IngredienteCocina.Tortilla && ingrediente != IngredienteCocina.Ninguno;
        }
    }

    private int ExtraerNumeroSlot(string nombre, int valorPorDefecto)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return valorPorDefecto;
        int ultimoGuion = nombre.LastIndexOf('_');
        if (ultimoGuion < 0 || ultimoGuion >= nombre.Length - 1) return valorPorDefecto;
        string posibleNumero = nombre.Substring(ultimoGuion + 1);
        return int.TryParse(posibleNumero, out int numero) ? numero : valorPorDefecto;
    }

    // ─── API PÚBLICA PARA DRAG AND DROP ────────────────────────

    public bool PuedeLlevarCarne()
    {
        return ingredienteActual == IngredienteCocina.Tortilla
            && etapaTortilla == 4
            && carneActualEnTortilla == IngredienteCocina.Ninguno;
    }

    public void RecibirCarne(IngredienteCocina tipoCarne)
    {
        if (!PuedeLlevarCarne() || tipoCarne == IngredienteCocina.Ninguno || tipoCarne == IngredienteCocina.Tortilla)
            return;

        carneActualEnTortilla = tipoCarne;
        if (tortillaTacoVisual != null) tortillaTacoVisual.SetActive(false);
        ActualizarVisualesIngredientes();
    }

    public IngredienteCocina ObtenerCarneEnTortilla() => carneActualEnTortilla;

    public bool TieneTaco()
    {
        return ingredienteActual == IngredienteCocina.Tortilla
            && carneActualEnTortilla != IngredienteCocina.Ninguno;
    }

    public void EliminarTaco()
    {
        if (TieneTaco())
        {
            transform.position = posicionOriginal;
            LimpiarSlot();
        }
    }

    public void ReestablecerPosicion()
    {
        transform.position = posicionOriginal;
    }
}