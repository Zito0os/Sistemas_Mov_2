using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orden — Data class que representa el pedido de un cliente.
/// No tiene lógica propia: solo almacena qué quiere el cliente y en cuánto tiempo.
/// Se genera desde ClienteIA y se evalúa desde SistemaOrdenes.
/// </summary>
[System.Serializable]
public class Orden
{
    // ENUMS

    public enum TipoCarne
    {
        Pastor,
        Bisteck,
        Arrachera,
        Longaniza
    }

    public enum TipoTopping
    {
        Cebolla,
        Cilantro,
        Pina,
        Salsa
    }

    // DATOS DEL PEDIDO

    //ID único de esta orden (para debug y UI)
    public string IDOrden { get; private set; }

    //Tipo de carne que pide el cliente
    public TipoCarne Carne { get; private set; }

    //Toppings que lleva el taco (cebolla, cilantro, piña, salsa)
    public List<TipoTopping> Toppings { get; private set; }

    //¿Incluye tortilla calentada?
    public bool NecesitaTortilla { get; private set; }

    //Segundos que el cliente esperará antes de irse. Varía por día.
    public float TiempoDePaciencia { get; private set; }

    //Precio base del taco antes de propina
    public int PrecioBase { get; private set; }

    // CONSTRUCTOR
    /// <summary>
    /// Crea una orden con todos sus parámetros definidos.
    /// Normalmente se llama desde Orden.GenerarAleatoria().
    /// </summary>
    public Orden(TipoCarne carne, List<TipoTopping> toppings, bool necesitaTortilla,
                 float tiempoDepaciencia, int precioBase)
    {
        IDOrden            = System.Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        Carne              = carne;
        Toppings           = toppings ?? new List<TipoTopping>();
        NecesitaTortilla   = necesitaTortilla;
        TiempoDePaciencia  = tiempoDepaciencia;
        PrecioBase         = precioBase;
    }

    // GENERACIÓN ALEATORIA

    /// <summary>
    /// Genera un pedido aleatorio escalado al día actual.
    ///
    /// Días 1-3:  solo Pastor, 1 topping, paciencia larga, precio bajo
    /// Días 4-7:  Pastor o Bisteck, 1-2 toppings, paciencia media
    /// Días 8+:   cualquier carne desbloqueada, 2-3 toppings, paciencia corta
    /// </summary>
    public static Orden GenerarAleatoria(int diaActual)
    {
        TipoCarne           carne    = ObtenerCarneAleatoria(diaActual);
        List<TipoTopping>   toppings = ObtenerToppingsAleatorios(diaActual);
        float               paciencia = ObtenerPaciencia(diaActual);
        int                 precio   = CalcularPrecioBase(carne, toppings);

        return new Orden(carne, toppings, necesitaTortilla: true, paciencia, precio);
    }

    // HELPERS PRIVADOS DE GENERACIÓN
    private static TipoCarne ObtenerCarneAleatoria(int dia)
    {
        if (dia >= 8)
        {
            // Todas las carnes disponibles
            return (TipoCarne)Random.Range(0, System.Enum.GetValues(typeof(TipoCarne)).Length);
        }
        else if (dia >= 4)
        {
            // Pastor y Bisteck
            return (TipoCarne)Random.Range(0, 2);
        }
        else
        {
            // Solo Pastor los primeros días
            return TipoCarne.Pastor;
        }
    }

    private static List<TipoTopping> ObtenerToppingsAleatorios(int dia)
    {
        var resultado     = new List<TipoTopping>();
        int maxToppings   = dia >= 8 ? 3 : dia >= 4 ? 2 : 1;
        int cantidad      = Random.Range(1, maxToppings + 1);

        // Pool de toppings disponibles según el día
        var disponibles = new List<TipoTopping> { TipoTopping.Cebolla, TipoTopping.Cilantro };
        if (dia >= 4) disponibles.Add(TipoTopping.Pina);
        if (dia >= 4) disponibles.Add(TipoTopping.Salsa);

        // Mezcla el pool y toma los primeros 'cantidad'
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
        if (dia >= 8) return Random.Range(20f, 30f);   // Corta
        if (dia >= 4) return Random.Range(30f, 45f);   // Media
        return Random.Range(45f, 60f);                 // Larga
    }

    private static int CalcularPrecioBase(TipoCarne carne, List<TipoTopping> toppings)
    {
        int precio = carne switch
        {
            TipoCarne.Pastor    => 20,
            TipoCarne.Bisteck   => 25,
            TipoCarne.Arrachera => 35,
            TipoCarne.Longaniza => 30,
            _                   => 20
        };

        // Cada topping suma un poco al precio
        precio += toppings.Count * 2;

        return precio;
    }

    // EVALUACIÓN

    /// <summary>
    /// Compara esta orden contra un taco entregado.
    /// Devuelve true si el taco cumple exactamente con lo pedido.
    /// Llamado desde SistemaOrdenes al recibir un taco del Dev A.
    /// </summary>
    public bool Coincide(TipoCarne carneEntregada, List<TipoTopping> toppingsEntregados, bool tieneTortilla)
    {
        if (carneEntregada != Carne)              return false;
        if (tieneTortilla  != NecesitaTortilla)   return false;
        if (toppingsEntregados.Count != Toppings.Count) return false;

        foreach (var topping in Toppings)
            if (!toppingsEntregados.Contains(topping)) return false;

        return true;
    }

    // PROPINA

    /// <summary>
    /// Calcula la propina según qué tan rápido se entregó el taco.
    ///
    /// proporcionTiempo = tiempoUsado / TiempoDepaciencia  (0 = instantáneo, 1 = justo a tiempo)
    ///
    ///   0.00 – 0.33 → propina alta  (20% del precio base)
    ///   0.33 – 0.66 → propina media (10% del precio base)
    ///   0.66 – 1.00 → sin propina
    ///   > 1.00      → timeout, sin pago
    /// </summary>
    public int CalcularPropina(float proporcionTiempo)
    {
        if (proporcionTiempo > 1f)     return 0;
        if (proporcionTiempo <= 0.33f) return Mathf.RoundToInt(PrecioBase * 0.20f); // Propina alta
        if (proporcionTiempo <= 0.66f) return Mathf.RoundToInt(PrecioBase * 0.10f); // Propina media
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
