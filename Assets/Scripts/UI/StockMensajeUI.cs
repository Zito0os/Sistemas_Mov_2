using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class StockMensajeUI : MonoBehaviour
{
    public static StockMensajeUI Instancia { get; private set; }

    [Header("Configuración")]
    [Tooltip("Segundos que el mensaje permanece visible antes de desvanecerse.")]
    [SerializeField] private float duracionVisible = 1.2f;

    [Tooltip("Segundos que tarda el fade out.")]
    [SerializeField] private float duracionFade = 0.5f;

    private TextMeshProUGUI _tmp;
    private Coroutine _coroutineActual;

    // ── Ciclo de vida ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;

        _tmp = GetComponent<TextMeshProUGUI>();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    // ── API pública ────────────────────────────────────────────────────────────

    public void Mostrar(string mensaje)
    {
        if (_coroutineActual != null)
            StopCoroutine(_coroutineActual);

        gameObject.SetActive(true);
        _tmp.text = mensaje;

        Color c = _tmp.color;
        c.a = 1f;
        _tmp.color = c;

        _coroutineActual = StartCoroutine(DesvanecerMensaje());
    }

    // ── Coroutine ──────────────────────────────────────────────────────────────

    private IEnumerator DesvanecerMensaje()
    {
        yield return new WaitForSeconds(duracionVisible);

        float tiempo = 0f;
        Color c = _tmp.color;

        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, tiempo / duracionFade);
            _tmp.color = c;
            yield return null;
        }

        gameObject.SetActive(false);
        _coroutineActual = null;
    }
}