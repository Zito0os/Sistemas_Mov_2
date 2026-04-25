using UnityEngine;

/// <summary>
/// HapticsManager — Maneja toda la vibración del dispositivo y los patrones predefinidos.
///
/// NO es un MonoBehaviour. Es una clase plana gestionada por MobileServices.
/// Eso permite que su ciclo de vida (suscripciones / desuscripciones) lo controle el singleton.
///
/// Patrones de vibración:
///   VibrarCorto()   ~50ms   → feedback ligero (cliente llega, click confirmado)
///   VibrarMedio()   ~150ms  → confirmación (orden recibida, pago)
///   VibrarLargo()   ~400ms  → alerta importante (orden cancelada, error)
///   VibrarPatron()         → patrones custom Android (vibrar-pausar-vibrar)
///
/// Eventos del juego a los que se suscribe:
///   - ClienteIA.alGenerarOrden         → corto (cliente llega y pide)
///   - SistemaOrdenes.alCompletarOrden  → medio si correcto, largo si incorrecto
///   - SistemaOrdenes.alCancelarOrden   → medio (timeout)
///   - CuotaDePiso.OnResultadoCuota     → patrón largo si NO pagó (tensión)
///   - GameManager.OnGameOver           → patrón final
/// </summary>
public class HapticsManager
{
    // DURACIONES (en milisegundos, para uso con AndroidJavaObject)

    private const long DURACION_CORTA = 50;
    private const long DURACION_MEDIA = 150;
    private const long DURACION_LARGA = 400;

    // REFERENCIAS

    private readonly MobileServices _services;
    private readonly bool _logsActivos;

    // ANDROID — referencia a la API de Vibrator (cacheada para no buscarla cada vez)

    private AndroidJavaObject _vibrator;
    private bool _androidVibratorListo;

    // CONSTRUCTOR

    public HapticsManager(MobileServices services, bool logsActivos)
    {
        _services = services;
        _logsActivos = logsActivos;
        InicializarAndroidVibrator();
    }

    // INICIALIZACIÓN ANDROID

    private void InicializarAndroidVibrator()
    {
        // Solo en Android (en editor / iOS / standalone usaremos Handheld.Vibrate como fallback)
        if (Application.platform != RuntimePlatform.Android) return;

        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                _vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                _androidVibratorListo = _vibrator != null;
            }

            if (_logsActivos)
                Debug.Log($"[HapticsManager] Android Vibrator listo: {_androidVibratorListo}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HapticsManager] No se pudo inicializar Vibrator de Android: {e.Message}");
            _androidVibratorListo = false;
        }
    }

    // SUSCRIPCIÓN A EVENTOS DEL JUEGO

    public void RegistrarSuscripciones()
    {
        ClienteIA.alGenerarOrden += AlGenerarOrden;
        SistemaOrdenes.alCompletarOrden += AlCompletarOrden;
        SistemaOrdenes.alCancelarOrden += AlCancelarOrden;
        CuotaDePiso.OnResultadoCuota += AlResultadoCuota;
        GameManager.OnGameOver += AlGameOver;

        if (_logsActivos)
            Debug.Log("[HapticsManager] Suscripciones registradas.");
    }

    public void LiberarSuscripciones()
    {
        ClienteIA.alGenerarOrden -= AlGenerarOrden;
        SistemaOrdenes.alCompletarOrden -= AlCompletarOrden;
        SistemaOrdenes.alCancelarOrden -= AlCancelarOrden;
        CuotaDePiso.OnResultadoCuota -= AlResultadoCuota;
        GameManager.OnGameOver -= AlGameOver;
    }

    // HANDLERS DE EVENTOS

    /// <summary>Cliente llegó al mostrador y generó su pedido → vibración corta.</summary>
    private void AlGenerarOrden(ClienteIA cliente, Orden orden)
    {
        if (_logsActivos)
            Debug.Log($"[HapticsManager] Vibrar (corto) — orden recibida de {cliente.name}");
        VibrarCorto();
    }

    /// <summary>Orden completada — corto si correcto, largo si incorrecto.</summary>
    private void AlCompletarOrden(Orden orden, int pagoTotal, bool correcto)
    {
        if (correcto)
        {
            if (_logsActivos)
                Debug.Log($"[HapticsManager] Vibrar (medio) — pedido correcto, $${pagoTotal}");
            VibrarMedio();
        }
        else
        {
            if (_logsActivos)
                Debug.Log("[HapticsManager] Vibrar (largo) — pedido incorrecto");
            VibrarLargo();
        }
    }

    /// <summary>Orden cancelada por timeout (cliente se fue) → vibración media.</summary>
    private void AlCancelarOrden(Orden orden)
    {
        if (_logsActivos)
            Debug.Log("[HapticsManager] Vibrar (medio) — orden cancelada por timeout");
        VibrarMedio();
    }

    /// <summary>Resultado de cuota — patrón tenso si NO se pagó.</summary>
    private void AlResultadoCuota(bool pagada, int cuota, int balanceAntes, int balanceDespues, int semana)
    {
        if (pagada)
        {
            if (_logsActivos)
                Debug.Log("[HapticsManager] Vibrar (medio) — cuota pagada");
            VibrarMedio();
        }
        else
        {
            if (_logsActivos)
                Debug.Log("[HapticsManager] Vibrar (patrón tenso) — cuota NO pagada");
            // Patrón: vibrar 200ms, pausa 100ms, vibrar 200ms, pausa 100ms, vibrar 400ms
            VibrarPatron(new long[] { 0, 200, 100, 200, 100, 400 });
        }
    }

    /// <summary>Game over → patrón final largo.</summary>
    private void AlGameOver()
    {
        if (_logsActivos)
            Debug.Log("[HapticsManager] Vibrar (game over) — fin de partida");
        VibrarPatron(new long[] { 0, 600, 200, 600 });
    }

    // API PÚBLICA DE VIBRACIÓN

    /// <summary>Vibración corta (~50ms). Para feedback ligero.</summary>
    public void VibrarCorto()
    {
        EjecutarVibracion(DURACION_CORTA);
    }

    /// <summary>Vibración media (~150ms). Para confirmaciones.</summary>
    public void VibrarMedio()
    {
        EjecutarVibracion(DURACION_MEDIA);
    }

    /// <summary>Vibración larga (~400ms). Para alertas importantes.</summary>
    public void VibrarLargo()
    {
        EjecutarVibracion(DURACION_LARGA);
    }

    /// <summary>
    /// Vibración con patrón custom (solo Android).
    /// El array sigue el formato Android: [pausa, vibrar, pausa, vibrar, ...]
    /// Ejemplo: new long[] { 0, 200, 100, 200 } = vibrar 200ms, pausa 100ms, vibrar 200ms.
    /// En plataformas no Android, se reemplaza por una vibración corta como fallback.
    /// </summary>
    public void VibrarPatron(long[] patron)
    {
        if (!_services.VibracionActivada)
        {
            if (_logsActivos)
                Debug.Log("[HapticsManager] Vibración solicitada pero está DESACTIVADA en opciones.");
            return;
        }

        if (Application.platform == RuntimePlatform.Android && _androidVibratorListo)
        {
            try
            {
                _vibrator.Call("vibrate", patron, -1); // -1 = no repetir
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticsManager] Error en vibración con patrón: {e.Message}. Fallback a corta.");
                Handheld.Vibrate();
            }
        }
        else
        {
            // Fallback: en editor o iOS, una vibración simple
            Handheld.Vibrate();
        }
    }

    // EJECUCIÓN INTERNA

    private void EjecutarVibracion(long milisegundos)
    {
        if (!_services.VibracionActivada)
        {
            if (_logsActivos)
                Debug.Log("[HapticsManager] Vibración solicitada pero está DESACTIVADA en opciones.");
            return;
        }

        if (Application.platform == RuntimePlatform.Android && _androidVibratorListo)
        {
            try
            {
                _vibrator.Call("vibrate", milisegundos);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticsManager] Error vibrando {milisegundos}ms: {e.Message}. Fallback.");
                Handheld.Vibrate();
            }
        }
        else
        {
            // Editor / iOS / standalone → vibración simple del sistema
            // En el editor de Unity esto NO vibra nada (no hay hardware), pero no rompe.
            Handheld.Vibrate();
        }
    }
}