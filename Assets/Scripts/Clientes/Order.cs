using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Orden
{
    // ENUMS

    public enum TipoCarne
    {
        Pastor,
        Picadillo,
        Trompo,
        Desebrada
    }

    public enum TipoTopping
    {
        Cebolla,
        Cilantro,
        Pina,
        Salsa
    }

    // DATOS DEL PEDIDO

    public string IDOrden { get; private set; }
    public TipoCarne Carne { get; private set; }
    public List<TipoTopping> Toppings { get; private set; }
    public bool NecesitaTortilla { get; private set; }
    public float TiempoDePaciencia { get; private set; }
    public int PrecioBase { get; private set; }

    // CONSTRUCTOR

    public Orden(TipoCarne carne, List<TipoTopping> toppings, bool necesitaTortilla,
                 float tiempoDepaciencia, int precioBase)
    {
        IDOrden           = System.Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        Carne             = carne;
        Toppings          = toppings ?? new List<TipoTopping>();
        NecesitaTortilla  = necesitaTortilla;
        TiempoDePaciencia = tiempoDepaciencia;
        PrecioBase        = precioBase;
    }

    // GENERACIÓN ALEATORIA

    // Genera un pedido aleatorio escalado al día actual.
    // Días 1-3:  Solo Pastor. Toppings: solo Salsa (50% chance) o ninguno. Sin Cebolla ni Cilantro. Paciencia larga.
    // Días 4-7:  Pastor o Picadillo. Toppings: Cebolla, Cilantro, Piña, Salsa. 1-2 toppings. Paciencia media.
    //Días 8+:   Todas las carnes. Todos los toppings. 2-3 toppings. Paciencia corta.
    public static Orden GenerarAleatoria(int diaActual)
    {
        TipoCarne         carne    = ObtenerCarneAleatoria(diaActual);
        List<TipoTopping> toppings = ObtenerToppingsAleatorios(diaActual);
        float             paciencia = ObtenerPaciencia(diaActual);
        int               precio   = CalcularPrecioBase(carne, toppings);

        return new Orden(carne, toppings, necesitaTortilla: true, paciencia, precio);
    }

    // HELPERS PRIVADOS DE GENERACIÓN

    private static TipoCarne ObtenerCarneAleatoria(int dia)
    {
        if (dia >= 8)
            return (TipoCarne)Random.Range(0, System.Enum.GetValues(typeof(TipoCarne)).Length);
        if (dia >= 4)
            return (TipoCarne)Random.Range(0, 2); // Pastor o Picadillo
        return TipoCarne.Pastor;                  // Días 1-3: solo Pastor
    }

    private static List<TipoTopping> ObtenerToppingsAleatorios(int dia)
    {
        var resultado = new List<TipoTopping>();

        if (dia <= 3)
        {
            // ── DÍAS 1-3 ───────────────────────────────────────────────────────
            // Sin Cebolla ni Cilantro. Solo puede pedir Salsa, y al 50% de probabilidad.
            if (Random.value < 0.5f)
                resultado.Add(TipoTopping.Salsa);
            // Si no cayó el 50%, devuelve lista vacía (sin toppings)
            return resultado;
        }

        // ── DÍAS 4-7 ──────────────────────────────────────────────────────────
        // Cebolla, Cilantro, Piña, Salsa disponibles. 1-2 toppings.
        // ── DÍAS 8+ ───────────────────────────────────────────────────────────
        // Todos los toppings. 2-3 toppings.

        int maxToppings = dia >= 8 ? 3 : 2;
        int cantidad    = Random.Range(1, maxToppings + 1);

        var disponibles = new List<TipoTopping>
        {
            TipoTopping.Cebolla,
            TipoTopping.Cilantro,
            TipoTopping.Pina,
            TipoTopping.Salsa
        };

        // Fisher-Yates shuffle
        for (int i = disponibles.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (disponibles[i], disponibles[j]) = (disponibles[j], disponibles[i]);
        }

        for (int i = 0; i < Mathf.Min(cantidad, disponibles.Count); i++)
            resultado.Add(disponibles[i]);

        return resultado;
    }

    private static float ObtenerPaciencia(int dia)
    {
        if (dia >= 8) return Random.Range(20f, 30f);  // Corta
        if (dia >= 4) return Random.Range(30f, 45f);  // Media
        return Random.Range(45f, 60f);                // Larga (días 1-3)
    }

    private static int CalcularPrecioBase(TipoCarne carne, List<TipoTopping> toppings)
    {
        int precio = carne switch
        {
            TipoCarne.Pastor    => 20,
            TipoCarne.Picadillo => 25,
            TipoCarne.Trompo    => 35,
            TipoCarne.Desebrada => 30,
            _                   => 20
        };
        precio += toppings.Count * 2;
        return precio;
    }

    // EVALUACIÓN

    public bool Coincide(TipoCarne carneEntregada, List<TipoTopping> toppingsEntregados, bool tieneTortilla)
    {
        if (carneEntregada != Carne)                        return false;
        if (tieneTortilla  != NecesitaTortilla)             return false;
        if (toppingsEntregados.Count != Toppings.Count)     return false;

        foreach (var topping in Toppings)
            if (!toppingsEntregados.Contains(topping))      return false;

        return true;
    }

    // PROPINA

    public int CalcularPropina(float proporcionTiempo)
    {
        if (proporcionTiempo > 1f)     return 0;
        if (proporcionTiempo <= 0.33f) return Mathf.RoundToInt(PrecioBase * 0.20f);
        if (proporcionTiempo <= 0.66f) return Mathf.RoundToInt(PrecioBase * 0.10f);
        return 0;
    }

    // DEBUG

    public override string ToString()
    {
        string toppingsStr = Toppings.Count > 0
            ? string.Join(", ", Toppings)
            : "sin toppings";
        return $"[Orden {IDOrden}] {Carne} + {toppingsStr} | ${PrecioBase} | {TiempoDePaciencia}s";
    }
}