using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    Light luz;

    [Header("Secuencia de intensidades")]
    public float[] secuencia = { 800f, 100f, 750f, 50f, 820f, 200f, 900f, 80f };

    [Header("Tiempo entre cambios")]
    public float frecuenciaMin = 0.3f;
    public float frecuenciaMax = 1.2f;

    [Header("Transición")]
    public float velocidadLerp = 4f;

    float intensidadObjetivo;
    float timer;
    int indiceActual = 0;

    void Start()
    {
        luz = GetComponent<Light>();
        intensidadObjetivo = secuencia[0];
        luz.intensity = intensidadObjetivo;
        timer = Random.Range(frecuenciaMin, frecuenciaMax);
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            indiceActual = (indiceActual + 1) % secuencia.Length;
            intensidadObjetivo = secuencia[indiceActual];
            timer = Random.Range(frecuenciaMin, frecuenciaMax);
        }

        luz.intensity = Mathf.Lerp(luz.intensity, intensidadObjetivo,
                                   Time.deltaTime * velocidadLerp);

    }
}