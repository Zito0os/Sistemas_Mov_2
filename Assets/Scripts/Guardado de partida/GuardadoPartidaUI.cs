using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GuardadoPartidaUI : MonoBehaviour
{
    private enum ModoInterfaz
    {
        Ninguno,
        CrearNueva,
        ConfirmarAccion,
        MensajeSimple,
        PartidaGuardada
    }

    private enum TipoAccionPendiente
    {
        Ninguna,
        CrearNueva,
        CargarExistente,
        SobrescribirExistente
    }

    [Header("Mensajes hardcodeados")]
    [SerializeField] private string mensajeSinInformacion = "Sin informacion de partida";
    [SerializeField] private string mensajeSeleccionarSlot = "Seleccionar Slot";
    [SerializeField] private string mensajeCrearPartida = "Quieres crear una nueva partida en el slot {0}?";
    [SerializeField] private string mensajeCargarPartida = "Quieres cargar la partida {0}?";
    [SerializeField] private string mensajeSobrescribir = "Quieres sobreescribir la partida del slot {0}?";
    [SerializeField] private string mensajePartidaGuardada = "Partida Guardada";

    [Header("Ventana")]
    [SerializeField] private Vector2 tamanoVentana = new Vector2(420f, 180f);

    private readonly Dictionary<int, Button> _botonesSlot = new Dictionary<int, Button>();
    private Button _botonCrear;
    private Button _botonGuardar;
    private CanvasGroup _canvasGroup;
    private GameObject _modalRoot;
    private Text _modalTexto;
    private Button _modalSi;
    private Button _modalNo;
    private Button _modalOk;

    private ModoInterfaz _modo = ModoInterfaz.Ninguno;
    private string _mensajeActual = string.Empty;
    private int _slotPendiente = -1;
    private TipoAccionPendiente _accionPendiente = TipoAccionPendiente.Ninguna;
    private bool _esperandoSeleccionCreacion;
    private bool _configurado;
    private float _tiempoMensajeGuardado = 0f;

    private void Awake()
    {
        AsegurarEventSystemCompat();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        ConfigurarAuto();
        CrearModalUI();
        SetModalVisible(false);
    }

    private void Start()
    {
        ConfigurarAuto();
        AsegurarEventSystemCompat();
        ActualizarTextoSlots();
        LogRuntimeUIState();
        SincronizarModal();
    }

    private void Update()
    {
        if (_modo == ModoInterfaz.PartidaGuardada)
        {
            _tiempoMensajeGuardado -= Time.deltaTime;
            if (_tiempoMensajeGuardado <= 0f)
                CerrarDialogo(true);
        }
    }

    private void OnDestroy()
    {
        DesvincularBotones();
    }

    private void ConfigurarAuto()
    {
        if (_configurado)
            return;

        Button[] botones = GetComponentsInChildren<Button>(true);
        foreach (Button boton in botones)
        {
            if (boton == null)
                continue;

            if (boton.name == "Crear")
            {
                _botonCrear = boton;
                _botonCrear.onClick.AddListener(AlPresionarCrear);
                continue;
            }

            if (boton.name == "btn_guardar")
            {
                _botonGuardar = boton;
                _botonGuardar.onClick.AddListener(AlPresionarGuardar);
                continue;
            }

            if (!boton.name.StartsWith("Partida_"))
                continue;

            if (!int.TryParse(boton.name.Substring("Partida_".Length), out int slot))
                continue;

            if (slot < 1 || slot > GuardadoPartidaManager.MaxSlots)
                continue;

            _botonesSlot[slot] = boton;
            int slotLocal = slot;
            boton.onClick.AddListener(() => AlPresionarSlot(slotLocal));
        }

        _configurado = true;
    }

    private void DesvincularBotones()
    {
        if (_botonCrear != null)
            _botonCrear.onClick.RemoveListener(AlPresionarCrear);

        if (_botonGuardar != null)
            _botonGuardar.onClick.RemoveListener(AlPresionarGuardar);
    }

    private void ActualizarTextoSlots()
    {
        foreach (KeyValuePair<int, Button> par in _botonesSlot)
        {
            if (par.Value == null)
                continue;

            ActualizarTextoSlot(par.Key, par.Value);
        }
    }

    private void ActualizarTextoSlot(int slot, Button boton)
    {
        if (boton == null)
            return;

        TextMeshProUGUI textoTmp = boton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (textoTmp == null)
            return;

        textoTmp.text = GuardadoPartidaManager.Instance != null
            ? GuardadoPartidaManager.Instance.ObtenerResumenSlot(slot)
            : "Sin informacion de partida";
    }

    private void AlPresionarCrear()
    {
        if (!PuedoProcesarEntrada())
            return;

        _esperandoSeleccionCreacion = true;
        _slotPendiente = -1;
        _accionPendiente = TipoAccionPendiente.Ninguna;
        MostrarMensajeSimple(mensajeSeleccionarSlot);
    }

    private void AlPresionarSlot(int slot)
    {
        if (!PuedoProcesarEntrada())
            return;

        GuardadoPartidaManager gestor = GuardadoPartidaManager.Instance;
        if (gestor == null)
        {
            Debug.LogError("[GuardadoPartidaUI] No existe GuardadoPartidaManager.");
            return;
        }

        if (_esperandoSeleccionCreacion)
        {
            _slotPendiente = slot;

            if (gestor.SlotTieneDatos(slot))
            {
                _accionPendiente = TipoAccionPendiente.SobrescribirExistente;
                MostrarConfirmacion(string.Format(mensajeSobrescribir, slot));
                return;
            }

            _accionPendiente = TipoAccionPendiente.CrearNueva;
            MostrarConfirmacion(string.Format(mensajeCrearPartida, slot));
            return;
        }

        if (!gestor.SlotTieneDatos(slot))
        {
            MostrarMensajeSimple(mensajeSinInformacion);
            return;
        }

        _slotPendiente = slot;
        _accionPendiente = TipoAccionPendiente.CargarExistente;
        MostrarConfirmacion(string.Format(mensajeCargarPartida, slot));
    }

    private void AlPresionarGuardar()
    {
        if (!PuedoProcesarEntrada())
            return;

        GuardadoPartidaManager gestor = GuardadoPartidaManager.Instance;
        if (gestor == null)
        {
            Debug.LogError("[GuardadoPartidaUI] No existe GuardadoPartidaManager.");
            return;
        }

        if (!gestor.HaySlotValidoSeleccionado)
        {
            MostrarMensajeSimple("Selecciona un slot primero");
            return;
        }

        if (gestor.GuardarPartidaActual())
        {
            ActualizarTextoSlots();
            MostrarMensajeGuardado();
            return;
        }

        MostrarMensajeSimple("No se pudo guardar la partida");
    }

    private void CargarEscenaJuego()
    {
        _modo = ModoInterfaz.Ninguno;
        _mensajeActual = string.Empty;
        SceneManager.LoadScene(GuardadoPartidaManager.EscenaJuego);
    }

    private void ConfirmarSobrescritura(bool confirmar)
    {
        ProcesarConfirmacion(confirmar);
    }

    private void ProcesarConfirmacion(bool confirmar)
    {
        GuardadoPartidaManager gestor = GuardadoPartidaManager.Instance;
        if (gestor == null)
        {
            CerrarDialogo(true);
            return;
        }

        if (!confirmar)
        {
            _modo = ModoInterfaz.Ninguno;
            _slotPendiente = -1;
            _accionPendiente = TipoAccionPendiente.Ninguna;
            _esperandoSeleccionCreacion = false;
            CerrarDialogo(true);
            return;
        }

        if (_slotPendiente < 1)
        {
            CerrarDialogo(true);
            _modo = ModoInterfaz.Ninguno;
            _accionPendiente = TipoAccionPendiente.Ninguna;
            _esperandoSeleccionCreacion = false;
            return;
        }

        switch (_accionPendiente)
        {
            case TipoAccionPendiente.CrearNueva:
                gestor.PrepararNuevaPartida(_slotPendiente);
                gestor.GuardarDatosPendientes();
                _esperandoSeleccionCreacion = false;
                ActualizarTextoSlots();
                MostrarMensajeGuardado();
                _accionPendiente = TipoAccionPendiente.Ninguna;
                _slotPendiente = -1;
                return;

            case TipoAccionPendiente.SobrescribirExistente:
                gestor.EliminarSlot(_slotPendiente);
                gestor.PrepararNuevaPartida(_slotPendiente);
                gestor.GuardarDatosPendientes();
                _esperandoSeleccionCreacion = false;
                ActualizarTextoSlots();
                MostrarMensajeGuardado();
                _accionPendiente = TipoAccionPendiente.Ninguna;
                _slotPendiente = -1;
                return;

            case TipoAccionPendiente.CargarExistente:
                if (!gestor.CargarPartidaEnSlot(_slotPendiente))
                {
                    MostrarMensajeSimple(mensajeSinInformacion);
                    return;
                }

                _esperandoSeleccionCreacion = false;
                CargarEscenaJuego();
                return;
        }

        _accionPendiente = TipoAccionPendiente.Ninguna;
        _slotPendiente = -1;
    }

    private void MostrarMensajeSimple(string texto)
    {
        _modo = ModoInterfaz.MensajeSimple;
        _mensajeActual = texto;
        SincronizarModal();
    }

    private void MostrarConfirmacion(string texto)
    {
        _modo = ModoInterfaz.ConfirmarAccion;
        _mensajeActual = texto;
        SincronizarModal();
    }

    private void MostrarMensajeGuardado()
    {
        _modo = ModoInterfaz.PartidaGuardada;
        _mensajeActual = mensajePartidaGuardada;
        _tiempoMensajeGuardado = 1.2f;
        SincronizarModal();
    }

    private void CerrarDialogo()
    {
        CerrarDialogo(false);
    }

    private void CerrarDialogo(bool forzar)
    {
        if (_modo == ModoInterfaz.PartidaGuardada && !forzar)
            return;

        _modo = ModoInterfaz.Ninguno;
        _mensajeActual = string.Empty;
        SincronizarModal();
    }

    private bool PuedoProcesarEntrada()
    {
        return _modo == ModoInterfaz.Ninguno;
    }

    private void SetInteraccionBloqueada(bool bloqueada)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.interactable = !bloqueada;
        _canvasGroup.blocksRaycasts = !bloqueada;
    }

    private void SincronizarModal()
    {
        if (_modalRoot == null)
            return;

        bool visible = _modo != ModoInterfaz.Ninguno;
        SetModalVisible(visible);

        if (_modalTexto != null)
            _modalTexto.text = _mensajeActual;

        if (_modalSi != null)
            _modalSi.gameObject.SetActive(_modo == ModoInterfaz.ConfirmarAccion);

        if (_modalNo != null)
            _modalNo.gameObject.SetActive(_modo == ModoInterfaz.ConfirmarAccion);

        if (_modalOk != null)
            _modalOk.gameObject.SetActive(_modo == ModoInterfaz.MensajeSimple || _modo == ModoInterfaz.PartidaGuardada);
    }

    private void SetModalVisible(bool visible)
    {
        if (_modalRoot != null)
            _modalRoot.SetActive(visible);
    }

    private void CrearModalUI()
    {
        if (_modalRoot != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[GuardadoPartidaUI] No se encontró Canvas para crear el modal.");
            return;
        }

        _modalRoot = new GameObject("GuardadoPartidaModal");
        _modalRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRt = _modalRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image fondo = _modalRoot.AddComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.55f);
        fondo.raycastTarget = true;

        Button bloquear = _modalRoot.AddComponent<Button>();
        bloquear.transition = Selectable.Transition.None;
        bloquear.targetGraphic = fondo;

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(_modalRoot.transform, false);

        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = tamanoVentana;
        panelRt.anchoredPosition = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.12f, 0.12f, 0.98f);

        GameObject texto = new GameObject("Texto");
        texto.transform.SetParent(panel.transform, false);
        _modalTexto = texto.AddComponent<Text>();
        _modalTexto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _modalTexto.fontSize = 28;
        _modalTexto.alignment = TextAnchor.MiddleCenter;
        _modalTexto.color = Color.white;
        _modalTexto.resizeTextForBestFit = true;

        RectTransform textoRt = _modalTexto.rectTransform;
        textoRt.anchorMin = new Vector2(0.08f, 0.38f);
        textoRt.anchorMax = new Vector2(0.92f, 0.88f);
        textoRt.offsetMin = Vector2.zero;
        textoRt.offsetMax = Vector2.zero;

        _modalSi = CrearBoton(panel.transform, "Si", new Vector2(-90f, -58f));
        _modalNo = CrearBoton(panel.transform, "No", new Vector2(90f, -58f));
        _modalOk = CrearBoton(panel.transform, "OK", new Vector2(0f, -58f));

        _modalSi.onClick.AddListener(() => ProcesarConfirmacion(true));
        _modalNo.onClick.AddListener(() => ProcesarConfirmacion(false));
        _modalOk.onClick.AddListener(() => CerrarDialogo(true));

        bloqueator(bloquear);
    }

    private static void bloqueator(Button bloquear)
    {
        if (bloquear != null)
            bloquear.onClick.AddListener(() => { });
    }

    private Button CrearBoton(Transform padre, string texto, Vector2 posicion)
    {
        GameObject go = new GameObject(texto);
        go.transform.SetParent(padre, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(110f, 42f);
        rt.anchoredPosition = posicion;

        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.18f);

        Button boton = go.AddComponent<Button>();

        GameObject label = new GameObject("Label");
        label.transform.SetParent(go.transform, false);

        Text txt = label.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = texto;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        RectTransform labelRt = txt.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        boton.targetGraphic = img;
        return boton;
    }

    // Diagnostics helper: logs UI runtime state to help debug input issues on device builds
    private void LogRuntimeUIState()
    {
        try
        {
            Debug.Log("[GuardadoPartidaUI] Runtime UI diagnostics:");

            // EventSystem
            Debug.Log($"EventSystem.current != null: {EventSystem.current != null}");
            if (EventSystem.current != null)
                Debug.Log($"InputModule: {EventSystem.current.currentInputModule?.GetType().Name}");

            // Canvases
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            Debug.Log($"Found {canvases.Length} Canvas(es)");
            foreach (Canvas c in canvases)
            {
                var gr = c.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                var groups = c.GetComponentsInChildren<CanvasGroup>(true);
                Debug.Log($"Canvas '{c.gameObject.name}' active:{c.gameObject.activeInHierarchy} renderMode:{c.renderMode} hasGraphicRaycaster:{(gr!=null)} canvasGroups:{groups.Length} sortingOrder:{c.sortingOrder}");

                // list any CanvasGroup on this canvas that blocks raycasts
                foreach (var g in groups)
                {
                    Debug.Log($"  CanvasGroup on '{g.gameObject.name}' blocksRaycasts:{g.blocksRaycasts} interactable:{g.interactable} alpha:{g.alpha}");
                }
            }

            // Check for full-screen UI elements with RaycastTarget enabled
            UnityEngine.UI.Image[] images = FindObjectsOfType<UnityEngine.UI.Image>(true);
            foreach (var img in images)
            {
                if (!img.raycastTarget)
                    continue;

                RectTransform rt = img.rectTransform;
                Vector2 size = rt.rect.size;
                // if width/height close to screen size, warn
                if (size.x >= Screen.width * 0.9f && size.y >= Screen.height * 0.9f)
                {
                    Debug.LogWarning($"Full-screen Image with RaycastTarget found: '{img.gameObject.name}' (size {size.x}x{size.y})");
                }
            }

            // Buttons count and their interactable state
            UnityEngine.UI.Button[] buttons = FindObjectsOfType<UnityEngine.UI.Button>(true);
            Debug.Log($"Found {buttons.Length} Button(s)");
            foreach (var b in buttons)
            {
                Debug.Log($" Button '{b.gameObject.name}' active:{b.gameObject.activeInHierarchy} interactable:{b.interactable}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GuardadoPartidaUI] Diagnostics failed: {ex}");
        }
    }

    private static void AsegurarEventSystemCompat()
    {
        EventSystem[] systems = FindObjectsOfType<EventSystem>(true);

        EventSystem principal = null;
        foreach (EventSystem es in systems)
        {
            if (es != null && es.gameObject.activeInHierarchy)
            {
                principal = es;
                break;
            }
        }

        if (principal == null && systems.Length > 0)
            principal = systems[0];

        if (principal == null)
        {
            GameObject root = new GameObject("EventSystem");
            principal = root.AddComponent<EventSystem>();
            Debug.Log("[GuardadoPartidaUI] EventSystem creado en runtime.");
        }

        BaseInputModule[] modules = principal.GetComponents<BaseInputModule>();
        foreach (BaseInputModule module in modules)
        {
            if (module == null)
                continue;

            string moduleType = module.GetType().Name;
            if (moduleType.Contains("InputSystemUIInputModule"))
            {
                Destroy(module);
                Debug.Log("[GuardadoPartidaUI] InputSystemUIInputModule removido para compatibilidad.");
            }
        }

        if (principal.GetComponent<StandaloneInputModule>() == null)
        {
            principal.gameObject.AddComponent<StandaloneInputModule>();
            Debug.Log("[GuardadoPartidaUI] StandaloneInputModule agregado para compatibilidad.");
        }

        foreach (EventSystem es in systems)
        {
            if (es == null || es == principal)
                continue;

            es.gameObject.SetActive(false);
            Debug.Log($"[GuardadoPartidaUI] EventSystem duplicado desactivado: {es.gameObject.name}");
        }
    }

}
