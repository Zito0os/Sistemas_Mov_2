using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class IngredienteSeleccionadoFX : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("A qué ingrediente corresponde este objeto en la cocina.")]
    public IngredienteCocina ingrediente = IngredienteCocina.Ninguno;

    [Header("Parpadeo")]
    [Tooltip("Velocidad del parpadeo (ciclos por segundo). 2 = parpadeo notable, 4 = rápido.")]
    [Range(0.5f, 6f)]
    public float velocidadParpadeo = 2.5f;

    [Tooltip("Color del destello. Blanco puro por defecto.")]
    public Color colorDestello = Color.white;

    [Tooltip("Color emisivo del destello (para URP con emisión activada en el material).")]
    public Color colorEmisivo = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Intensidad HDR del emisivo cuando está en destello.")]
    [Range(0f, 4f)]
    public float intensidadEmisiva = 1.5f;

    // ESTADO INTERNO

    private Renderer _renderer;
    private Material[] _materialesOriginales;
    private Material[] _materialesParpadeo;
    private bool _estaSeleccionado = false;
    private bool _parpadeandoActivo = false;
    private Coroutine _coroutineParpadeo;

    private static readonly int PropColor    = Shader.PropertyToID("_BaseColor");
    private static readonly int PropEmission = Shader.PropertyToID("_EmissionColor");

    // CICLO

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
            _renderer = GetComponentInChildren<Renderer>();

        if (_renderer == null)
        {
            Debug.LogWarning($"[IngredienteSeleccionadoFX] {gameObject.name} no tiene Renderer. El efecto no funcionará.");
            enabled = false;
            return;
        }

        // Guardar los materiales originales
        _materialesOriginales = _renderer.materials;

        // Crear copias para el parpadeo (nunca modificar los materiales del proyecto)
        _materialesParpadeo = new Material[_materialesOriginales.Length];
        for (int i = 0; i < _materialesOriginales.Length; i++)
        {
            _materialesParpadeo[i] = new Material(_materialesOriginales[i]);
            _materialesParpadeo[i].EnableKeyword("_EMISSION");
        }
    }

    private void Update()
    {
        if (CookingStation.Instance == null) return;

        IngredienteCocina seleccionado = CookingStation.Instance.ObtenerIngredienteSeleccionado();
        bool deberiaEstarSeleccionado = (seleccionado == ingrediente && ingrediente != IngredienteCocina.Ninguno);

        if (deberiaEstarSeleccionado && !_estaSeleccionado)
            ActivarParpadeo();
        else if (!deberiaEstarSeleccionado && _estaSeleccionado)
            DesactivarParpadeo();
    }

    private void OnDestroy()
    {
        // Limpiar los materiales de runtime para evitar memory leaks
        if (_materialesParpadeo != null)
        {
            foreach (var mat in _materialesParpadeo)
                if (mat != null) Destroy(mat);
        }
    }

    // PARPADEO

    private void ActivarParpadeo()
    {
        _estaSeleccionado = true;
        _renderer.materials = _materialesParpadeo;

        if (_coroutineParpadeo != null)
            StopCoroutine(_coroutineParpadeo);

        _coroutineParpadeo = StartCoroutine(CoroutineParpadeo());
    }

    private void DesactivarParpadeo()
    {
        _estaSeleccionado = false;
        _parpadeandoActivo = false;

        if (_coroutineParpadeo != null)
        {
            StopCoroutine(_coroutineParpadeo);
            _coroutineParpadeo = null;
        }

        // Restaurar materiales originales
        _renderer.materials = _materialesOriginales;
    }

    private IEnumerator CoroutineParpadeo()
    {
        float intervalo = 1f / (velocidadParpadeo * 2f);
        _parpadeandoActivo = true;
        bool enDestello = false;

        while (_parpadeandoActivo)
        {
            enDestello = !enDestello;

            foreach (var mat in _materialesParpadeo)
            {
                if (mat == null) continue;

                if (enDestello)
                {
                    // Color blanco
                    if (mat.HasProperty(PropColor))
                        mat.SetColor(PropColor, colorDestello);

                    // Emisión blanca
                    if (mat.HasProperty(PropEmission))
                        mat.SetColor(PropEmission, colorEmisivo * intensidadEmisiva);
                }
                else
                {
                    // Restaurar color original de la copia
                    int idx = System.Array.IndexOf(_materialesParpadeo, mat);
                    if (idx >= 0 && idx < _materialesOriginales.Length)
                    {
                        if (mat.HasProperty(PropColor) && _materialesOriginales[idx].HasProperty(PropColor))
                            mat.SetColor(PropColor, _materialesOriginales[idx].GetColor(PropColor));

                        if (mat.HasProperty(PropEmission))
                            mat.SetColor(PropEmission, Color.black);
                    }
                }
            }

            yield return new WaitForSeconds(intervalo);
        }
    }

    // QUERY PÚBLICA 
    public bool EstaSeleccionado => _estaSeleccionado;
}