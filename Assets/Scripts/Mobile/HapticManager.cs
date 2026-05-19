using UnityEngine;

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

    private void AlGenerarOrden(ClienteIA cliente, Orden orden)
    {
        if (_logsActivos)
            Debug.Log($"[HapticsManager] Vibrar (corto) — orden recibida de {cliente.name}");
        VibrarCorto();
    }

    private void AlCompletarOrden(Orden orden, int pagoTotal, bool correcto, int cantidadRequerida)
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

    private void AlCancelarOrden(Orden orden)
    {
        if (_logsActivos)
            Debug.Log("[HapticsManager] Vibrar (medio) — orden cancelada por timeout");
        VibrarMedio();
    }

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

    private void AlGameOver()
    {
        if (_logsActivos)
            Debug.Log("[HapticsManager] Vibrar (game over) — fin de partida");
        VibrarPatron(new long[] { 0, 600, 200, 600 });
    }

    // API PÚBLICA DE VIBRACIÓN

    public void VibrarCorto()
    {
        EjecutarVibracion(DURACION_CORTA);
    }

    public void VibrarMedio()
    {
        EjecutarVibracion(DURACION_MEDIA);
    }

    public void VibrarLargo()
    {
        EjecutarVibracion(DURACION_LARGA);
    }

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

            Handheld.Vibrate();
        }
    }
}