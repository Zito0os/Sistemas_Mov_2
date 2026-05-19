using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SalsaGiroscopio : MonoBehaviour
{
    // ── INSPECTOR ─────────────────────────────────────────────────────────────

    [Header("Panel de salsa")]
    public GameObject panelSalsa;
    public Image      barraProgreso;
    public TextMeshProUGUI textoInstruccion;

    [Header("Texto")]
    public string mensajeInstruccion = "¡Voltea el celular para poner la salsa!";
    public string mensajeListo       = "¡Salsa aplicada! 🌶";

    [Header("Parámetros")]
    [Tooltip("Segundos que tarda la barra en llegar al 100% con inclinación máxima")]
    public float tiempoTotal    = 2.5f;

    [Tooltip("Umbral mínimo de |gravity.x| para considerar que se está volcando de lado (0=nada, 1=completamente de lado)")]
    public float umbralGravedad = 0.3f;

    [Header("Colores barra")]
    public Color colorInicio = new Color(1f, 0.4f, 0f);   // naranja
    public Color colorFinal  = new Color(0.8f, 0f, 0f);   // rojo

    // ── ESTADO INTERNO ────────────────────────────────────────────────────────

    private float  _progreso       = 0f;
    private bool   _activo         = false;
    private bool   _completado     = false;
    private OrdenLista _ordenLista = null;

    // ── CICLO ─────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Asegurarse de que el giroscopio esté habilitado
        if (SystemInfo.supportsGyroscope)
            Input.gyro.enabled = true;

        if (panelSalsa != null)
            panelSalsa.SetActive(false);
    }

    private void Update()
    {
        if (!_activo || _completado) return;

        float inclinacion = ObtenerInclinacion();

        if (inclinacion > umbralGravedad)
        {
            // Avanzar proporcionalmente a la inclinación
            float delta = (inclinacion / 1f) * (1f / tiempoTotal) * Time.deltaTime;
            _progreso = Mathf.Clamp01(_progreso + delta);
            ActualizarBarra();

            if (_progreso >= 1f)
                CompletarSalsa();
        }
    }

    // ── API PÚBLICA ───────────────────────────────────────────────────────────

    /// <summary>
    /// Llamado por Gestos.cs cuando detecta HOLD sobre el GameObject de salsa.
    /// </summary>
    public void IniciarSalsa(OrdenLista ordenLista)
    {
        if (_activo || ordenLista == null) return;

        // Solo aplicar si hay tacos cargados
        if (ordenLista.ObtenerCantidadTacos() <= 0)
        {
            Debug.Log("[SalsaGiroscopio] No hay tacos cargados en OrdenLista.");
            return;
        }

        _ordenLista = ordenLista;
        _progreso   = 0f;
        _completado = false;
        _activo     = true;

        if (panelSalsa != null)
            panelSalsa.SetActive(true);

        if (textoInstruccion != null)
            textoInstruccion.text = mensajeInstruccion;

        if (barraProgreso != null)
        {
            barraProgreso.fillAmount = 0f;
            barraProgreso.color      = colorInicio;
        }
    }

    /// <summary>
    /// Llamado por Gestos.cs cuando el jugador levanta el dedo antes de terminar.
    /// </summary>
    public void CancelarSalsa()
    {
        if (!_activo || _completado) return;
        _activo     = false;
        _progreso   = 0f;
        _ordenLista = null;

        if (panelSalsa != null)
            panelSalsa.SetActive(false);
    }

    // ── LÓGICA INTERNA ────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve la inclinación LATERAL del celular (0 = plano/vertical de frente, 1 = volcado de lado).
    /// Usa gravity.x del giroscopio — este eje es ~0 cuando el celular está:
    ///   - Plano en la mesa (pantalla arriba)
    ///   - Vertical de frente (como cuando usas el celular normalmente)
    /// Y llega a ±1 cuando lo volteas hacia un lado (portrait → landscape boca arriba/abajo).
    /// El valor absoluto hace que ambos lados cuenten igual.
    /// </summary>
    private float ObtenerInclinacion()
    {
        if (SystemInfo.supportsGyroscope && Input.gyro.enabled)
        {
            // gravity.x mide inclinación lateral: 0 = plano o de frente, ±1 = volcado de lado
            float gx = Mathf.Abs(Input.gyro.gravity.x);
            return Mathf.Clamp01(gx);
        }

        // FALLBACK para editor: mantener pulsada la tecla S simula inclinación
        return Input.GetKey(KeyCode.S) ? 1f : 0f;
    }

    private void ActualizarBarra()
    {
        if (barraProgreso == null) return;
        barraProgreso.fillAmount = _progreso;
        barraProgreso.color      = Color.Lerp(colorInicio, colorFinal, _progreso);
    }

    private void CompletarSalsa()
    {
        _completado = true;
        _activo     = false;

        // Marcar que la orden lleva salsa
        if (_ordenLista != null)
            _ordenLista.AplicarSalsa();

        if (textoInstruccion != null)
            textoInstruccion.text = mensajeListo;

        if (barraProgreso != null)
            barraProgreso.fillAmount = 1f;

        // Cerrar panel después de un momento
        StartCoroutine(CerrarPanelConDelay(1.2f));
    }

    private System.Collections.IEnumerator CerrarPanelConDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (panelSalsa != null)
            panelSalsa.SetActive(false);
        _ordenLista = null;
    }
}