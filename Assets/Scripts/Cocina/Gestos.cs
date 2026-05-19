using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Gestos : MonoBehaviour
{
    [Header("Raycast")]
    public Camera camaraApuntado;
    public float distanciaMaxima = 8f;
    public LayerMask mascaraRaycast = ~0;
    public string tagTeleport    = "tp";
    public string tagTeleportLibre = "tp_libre";
    public string tagTortilla    = "tortilla";
    public string tagPastor      = "pastor";
    public string tagPicadillo   = "picadillo";
    public string tagDesebrada   = "desebrada";
    public string tagTrompo      = "trompo";
    public string tagSlot        = "slot";
    public string tagCartelTurno = "cartelTurno";
    public string tagSalsa       = "salsa";      // ← NUEVO: tag del GameObject de salsa
    public string tagOrdenLista  = "ordenLista"; // ← ya existía, se usa también para hold

    [Header("Jugador")]
    public PlayerController playerController;

    [Header("Modo Trompo")]
    public float rotacionPlayerTrompoY  = 77f;
    public float rotacionCamaraTrompoX  = 11.4f;
    public float distanciaMinimaSwipe   = 80f;

    [Header("Hold para agarrar carne (pastor/picadillo/desebrada)")]
    [Tooltip("Segundos que hay que mantener el dedo para agarrar carne cruda.")]
    public float tiempoMantenerCarne = 1f;

    [Header("Hold para salsa (giroscopio)")]
    [Tooltip("Segundos de hold sobre el objeto salsa para activar el panel de giroscopio.")]
    public float tiempoMantenerSalsa  = 0.8f;
    [Tooltip("Referencia al script SalsaGiroscopio (asignar en Inspector).")]
    public SalsaGiroscopio salsaGiroscopio;
    [Tooltip("Referencia a la OrdenLista que recibirá la salsa.")]
    public OrdenLista ordenListaParaSalsa;

    [Header("Hold para entregar orden (manual)")]
    [Tooltip("Segundos de hold sobre la OrdenLista para entregar la orden manualmente.")]
    public float tiempoMantenerEntrega = 1.2f;
    [Tooltip("TextMeshProUGUI que muestra '¡Orden Entregada!' tras entregar manualmente.")]
    public TextMeshProUGUI textoOrdenEntregada;
    [Tooltip("Cuántos segundos se muestra el texto de confirmación.")]
    public float duracionTextoEntrega  = 1.5f;

    [Header("UI Trompo")]
    public GameObject trompoPanel;
    public Button botonRegresarTrompo;
    public string nombrePanelTrompo   = "Trompo_panel";
    public string nombreBotonRegresar = "regresar";

    [Header("Drag and Drop")]
    public string tagCarneAcumulada   = "carneAcumulada";
    public string tagTortillaConCarne = "tortillaConCarne";
    public float distanciaDeteccionDrop = 1f;

    // ── ESTADO INTERNO ────────────────────────────────────────────────────────

    private CameraController cameraController;
    private bool modoTrompoActivo         = false;
    private Vector2 inicioSwipeTrompo;
    private int dedoSwipeTrompo           = -1;
    private float velocidadOriginalPlayer = 8f;
    private bool velocidadOriginalGuardada = false;

    // Hold para agarrar carne cruda
    private bool mouseMantenerActivo      = false;
    private float mouseMantenerInicio     = 0f;
    private Vector2 mouseMantenerPosicion;
    private bool mouseMantenerProcesado   = false;
    private int touchMantenerFingerId     = -1;
    private float touchMantenerInicio     = 0f;
    private Vector2 touchMantenerPosicion;
    private bool touchMantenerProcesado   = false;

    // Hold para salsa ── NUEVO
    private bool  _holdSalsaActivo      = false;
    private float _holdSalsaInicio      = 0f;
    private bool  _holdSalsaProcesado   = false;
    private int   _holdSalsaFingerId    = -1;
    private Vector2 _holdSalsaPosicion;
    private int   _fingerIdPendiente    = -1;

    // Hold para entregar orden ── NUEVO
    private bool  _holdEntregaActivo    = false;
    private float _holdEntregaInicio    = 0f;
    private bool  _holdEntregaProcesado = false;
    private int   _holdEntregaFingerId  = -1;
    private Vector2 _holdEntregaPosicion;
    private Coroutine _corrutinaTextoEntrega;

    // Drag and drop
    private SlotAcumulativoCarne carneDragActual     = null;
    private int dedoDragCarne                        = -1;
    private SlotCocina tortillaTargetDrop            = null;
    private OrdenLista ordenListaTargetDrop          = null;
    private Vector3 posicionOriginalCarneDrag;
    private bool estaDraqueandoCarne                 = false;

    // ── INICIO ────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (camaraApuntado == null)
            camaraApuntado = Camera.main;

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        if (camaraApuntado != null)
            cameraController = camaraApuntado.GetComponent<CameraController>();

        if (trompoPanel == null)
            trompoPanel = GameObject.Find(nombrePanelTrompo);

        if (botonRegresarTrompo == null && trompoPanel != null)
        {
            Transform boton = trompoPanel.transform.Find(nombreBotonRegresar);
            if (boton != null)
                botonRegresarTrompo = boton.GetComponent<Button>();
        }

        if (botonRegresarTrompo != null)
            botonRegresarTrompo.onClick.AddListener(SalirModoTrompo);

        if (trompoPanel != null)
            trompoPanel.SetActive(false);

        // Auto-buscar SalsaGiroscopio si no está asignado en el Inspector
        if (salsaGiroscopio == null)
            salsaGiroscopio = FindFirstObjectByType<SalsaGiroscopio>();

        // Auto-buscar OrdenLista si no está asignada en el Inspector
        if (ordenListaParaSalsa == null)
            ordenListaParaSalsa = FindFirstObjectByType<OrdenLista>();

        // Texto entrega oculto al inicio
        if (textoOrdenEntregada != null)
            textoOrdenEntregada.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (botonRegresarTrompo != null)
            botonRegresarTrompo.onClick.RemoveListener(SalirModoTrompo);
    }

    // ── UPDATE ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (modoTrompoActivo)
        {
            ProcesarSwipeModoTrompo();
            return;
        }

        ProcesarDragDesdeCarne();
        DetectarToque();
        ProcesarMantenerParaCarneCreda();
        ProcesarHoldSalsa();       // ← NUEVO
        ProcesarHoldEntrega();     // ← NUEVO
    }

    // ── DETECCIÓN DE TOQUE GENERAL ────────────────────────────────────────────

    private void DetectarToque()
    {
        if (camaraApuntado == null) return;

        // Mouse (editor / Unity Remote)
        if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                mouseMantenerActivo    = true;
                mouseMantenerInicio    = Time.time;
                mouseMantenerPosicion  = Input.mousePosition;
                mouseMantenerProcesado = false;
                _fingerIdPendiente = -1;
                RevisarPuntoDeInteraccion(Input.mousePosition);
            }
        }

        if (Input.touchCount <= 0) return;

        // Touch
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase != TouchPhase.Began) continue;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                continue;

            if (touchMantenerFingerId == -1)
            {
                touchMantenerFingerId  = touch.fingerId;
                touchMantenerInicio    = Time.time;
                touchMantenerPosicion  = touch.position;
                touchMantenerProcesado = false;
            }
            _fingerIdPendiente = touch.fingerId;
            RevisarPuntoDeInteraccion(touch.position);
        }
    }

    // ── REVISAR QUÉ SE TOCÓ ──────────────────────────────────────────────────

    private void RevisarPuntoDeInteraccion(Vector2 pantallaPosicion)
    {
        Ray rayo = camaraApuntado.ScreenPointToRay(pantallaPosicion);
        if (!Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
            return;

        // ── Slot de plancha ───────────────────────────────────────────────────
        if (hit.collider.CompareTag(tagSlot))
        {
            SlotCocina slotCocina = hit.collider.GetComponentInParent<SlotCocina>();
            if (slotCocina != null)
                slotCocina.InteractuarConSlot();
            return;
        }

        // ── Teleport normal (congela movimiento) ──────────────────────────────
        if (hit.collider.CompareTag(tagTeleport))
        {
            Position positionData = hit.collider.GetComponent<Position>();
            if (positionData == null || positionData.POSITION_TO_GO == null) return;

            if (playerController == null)
                playerController = GetComponentInParent<PlayerController>();
            if (playerController == null) return;

            playerController.moveSpeed = 0f;
            playerController.transform.position = positionData.POSITION_TO_GO.position;
            return;
        }

        // ── Teleport libre (restaura velocidad normal) ───────────────────────
        if (hit.collider.CompareTag(tagTeleportLibre))
        {
            Position positionData = hit.collider.GetComponent<Position>();
            if (positionData == null || positionData.POSITION_TO_GO == null) return;

            if (playerController == null)
                playerController = GetComponentInParent<PlayerController>();
            if (playerController == null) return;

            playerController.transform.position = positionData.POSITION_TO_GO.position;
            playerController.moveSpeed = velocidadOriginalGuardada ? velocidadOriginalPlayer : 8f;
            velocidadOriginalGuardada = false;
            return;
        }

        // ── Cartel de inicio de turno ─────────────────────────────────────────
        if (hit.collider.CompareTag(tagCartelTurno))
        {
            CartelInicioTurno cartel = hit.collider.GetComponent<CartelInicioTurno>();
            if (cartel == null)
                cartel = hit.collider.GetComponentInParent<CartelInicioTurno>();
            if (cartel != null)
                cartel.InteractuarConCartel();
            return;
        }

        // ── Salsa → registrar inicio del hold ────────────────────────────────
        // (El procesamiento real ocurre en ProcesarHoldSalsa)
        if (hit.collider.CompareTag(tagSalsa))
        {
            IniciarHoldSalsa(pantallaPosicion);
            return;
        }

        // ── OrdenLista → registrar inicio del hold para entrega manual ────────
        if (hit.collider.CompareTag(tagOrdenLista))
        {
            OrdenLista ol = hit.collider.GetComponentInParent<OrdenLista>();
            if (ol != null && !ol.entregarAutomaticamente)
            {
                IniciarHoldEntrega(pantallaPosicion, ol);
                return;
            }
        }

        if (CookingStation.Instance == null) return;

        // ── Tortilla → agarrar con tap (requiere stock) ───────────────────────
        if (hit.collider.CompareTag(tagTortilla))
        {
            if (CookingStation.Instance.stock_tortillas <= 0)
            {
                StockMensajeUI.Instancia?.Mostrar("Sin stock de Tortilla");
                return;
            }
            if (CookingStation.Instance.AgarrarIngrediente(IngredienteCocina.Tortilla))
                CookingStation.Instance.ConsumirStockCrudo(IngredienteCocina.Tortilla);
            return;
        }

        // ── Carnes crudas → solo registra el inicio del hold ─────────────────
        if (hit.collider.CompareTag(tagPastor))    return;
        if (hit.collider.CompareTag(tagPicadillo)) return;
        if (hit.collider.CompareTag(tagDesebrada)) return;

        // ── Trompo → modo trompo (swipe) ─────────────────────────────────────
        if (hit.collider.CompareTag(tagTrompo))
        {
            if (playerController == null)
                playerController = GetComponentInParent<PlayerController>();

            if (cameraController == null && camaraApuntado != null)
                cameraController = camaraApuntado.GetComponent<CameraController>();

            Position positionData = hit.collider.GetComponent<Position>();
            if (positionData != null && positionData.POSITION_TO_GO != null && playerController != null)
                playerController.transform.position = positionData.POSITION_TO_GO.position;

            if (playerController != null)
            {
                if (!velocidadOriginalGuardada)
                {
                    velocidadOriginalPlayer   = playerController.moveSpeed;
                    velocidadOriginalGuardada = true;
                }
                playerController.AplicarRotacionForzada(rotacionPlayerTrompoY, rotacionCamaraTrompoX);
                playerController.moveSpeed = 0f;
                playerController.Bloquear(true);
            }

            if (cameraController != null)
                cameraController.enabled = false;

            modoTrompoActivo = true;
            dedoSwipeTrompo  = -1;

            if (trompoPanel != null)
                trompoPanel.SetActive(true);

            return;
        }
    }

    // ── HOLD PARA AGARRAR CARNE CRUDA ────────────────────────────────────────

    private void ProcesarMantenerParaCarneCreda()
    {
        // Mouse
        if (mouseMantenerActivo)
        {
            if (!Input.GetMouseButton(0))
            {
                mouseMantenerActivo    = false;
                mouseMantenerProcesado = false;
            }
            else if (!mouseMantenerProcesado && Time.time - mouseMantenerInicio >= tiempoMantenerCarne)
            {
                IntentarAgarrarCarnePorHold(mouseMantenerPosicion);
                mouseMantenerProcesado = true;
            }
        }

        // Touch
        if (touchMantenerFingerId == -1) return;

        bool touchActivo = false;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId != touchMantenerFingerId) continue;

            touchActivo = true;

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                touchMantenerFingerId  = -1;
                touchMantenerProcesado = false;
                break;
            }

            if (!touchMantenerProcesado && Time.time - touchMantenerInicio >= tiempoMantenerCarne)
            {
                IntentarAgarrarCarnePorHold(touchMantenerPosicion);
                touchMantenerProcesado = true;
            }
            break;
        }

        if (!touchActivo)
        {
            touchMantenerFingerId  = -1;
            touchMantenerProcesado = false;
        }
    }

    private void IntentarAgarrarCarnePorHold(Vector2 pantallaPosicion)
    {
        if (CookingStation.Instance == null) return;

        Ray rayo = camaraApuntado.ScreenPointToRay(pantallaPosicion);
        if (!Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
            return;

        if (hit.collider.CompareTag(tagPastor))
        {
            if (CookingStation.Instance.stock_pastor <= 0)
            {
                StockMensajeUI.Instancia?.Mostrar("Sin stock de Pastor");
                return;
            }
            if (CookingStation.Instance.AgarrarIngrediente(IngredienteCocina.Pastor))
                CookingStation.Instance.ConsumirStockCrudo(IngredienteCocina.Pastor);
            return;
        }

        if (hit.collider.CompareTag(tagPicadillo))
        {
            if (CookingStation.Instance.stock_picadillo <= 0)
            {
                StockMensajeUI.Instancia?.Mostrar("Sin stock de Picadillo");
                return;
            }
            if (CookingStation.Instance.AgarrarIngrediente(IngredienteCocina.Picadillo))
                CookingStation.Instance.ConsumirStockCrudo(IngredienteCocina.Picadillo);
            return;
        }

        if (hit.collider.CompareTag(tagDesebrada))
        {
            if (CookingStation.Instance.stock_desebrada <= 0)
            {
                StockMensajeUI.Instancia?.Mostrar("Sin stock de Desebrada");
                return;
            }
            if (CookingStation.Instance.AgarrarIngrediente(IngredienteCocina.Desebrada))
                CookingStation.Instance.ConsumirStockCrudo(IngredienteCocina.Desebrada);
            return;
        }
    }

    // ── HOLD PARA SALSA (NUEVO) ───────────────────────────────────────────────

    private void IniciarHoldSalsa(Vector2 posicion)
    {
        _holdSalsaActivo    = true;
        _holdSalsaInicio    = Time.time;
        _holdSalsaPosicion  = posicion;
        _holdSalsaProcesado = false;
        _holdSalsaFingerId  = _fingerIdPendiente; // -1 = mouse, ≥0 = touch real

        Debug.Log($"[Gestos] IniciarHoldSalsa — fingerId={_holdSalsaFingerId}, pos={posicion}");

    }

    private void ProcesarHoldSalsa()
    {
        if (!_holdSalsaActivo || _holdSalsaProcesado) return;

        bool dedoPresionado = false;

        if (_holdSalsaFingerId == -1)
        {
            // Mouse
            dedoPresionado = Input.GetMouseButton(0);
            Debug.Log($"[Salsa] dedo={dedoPresionado}  t={Time.time - _holdSalsaInicio:F2}s");
        }
        else
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.fingerId != _holdSalsaFingerId) continue;
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    break;
                dedoPresionado = true;
                break;
            }
        }

        if (!dedoPresionado)
        {
            // Cancelar salsa si el jugador levanta el dedo antes
            if (salsaGiroscopio != null)
                salsaGiroscopio.CancelarSalsa();
            _holdSalsaActivo    = false;
            _holdSalsaFingerId  = -1;
            return;
        }

        // Tiempo de hold alcanzado → activar salsa
        if (Time.time - _holdSalsaInicio >= tiempoMantenerSalsa)
        {
            _holdSalsaProcesado = true;
            _holdSalsaActivo    = false;

            // Reintentar find por si Start() corrió antes de que existieran
            if (salsaGiroscopio == null)
                salsaGiroscopio = FindFirstObjectByType<SalsaGiroscopio>();

            if (ordenListaParaSalsa == null)
                ordenListaParaSalsa = FindFirstObjectByType<OrdenLista>();

            if (salsaGiroscopio != null && ordenListaParaSalsa != null)
                salsaGiroscopio.IniciarSalsa(ordenListaParaSalsa);
            else
                Debug.LogWarning("[Gestos] SalsaGiroscopio o OrdenLista no encontrados en la escena.");

            _holdSalsaFingerId = -1;
        }
    }

    // ── HOLD PARA ENTREGAR ORDEN MANUALMENTE (NUEVO) ─────────────────────────

    private void IniciarHoldEntrega(Vector2 posicion, OrdenLista ordenLista)
    {
        // Guardamos la referencia a la lista en ordenListaTargetDrop temporalmente
        // para no agregar otro campo; sin embargo es más claro tener uno dedicado:
        _holdEntregaActivo    = true;
        _holdEntregaInicio    = Time.time;
        _holdEntregaPosicion  = posicion;
        _holdEntregaProcesado = false;
        _pendingOrdenLista    = ordenLista;

        foreach (Touch touch in Input.touches)
            if (touch.phase == TouchPhase.Began && Vector2.Distance(touch.position, posicion) < 5f)
            { _holdEntregaFingerId = touch.fingerId; break; }
    }
    private OrdenLista _pendingOrdenLista = null;

    private void ProcesarHoldEntrega()
    {
        if (!_holdEntregaActivo || _holdEntregaProcesado) return;

        bool dedoPresionado = false;

        if (_holdEntregaFingerId == -1)
        {
            dedoPresionado = Input.GetMouseButton(0);
        }
        else
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.fingerId != _holdEntregaFingerId) continue;
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    break;
                dedoPresionado = true;
                break;
            }
        }

        if (!dedoPresionado)
        {
            _holdEntregaActivo    = false;
            _holdEntregaFingerId  = -1;
            _pendingOrdenLista    = null;
            return;
        }

        if (Time.time - _holdEntregaInicio >= tiempoMantenerEntrega)
        {
            _holdEntregaProcesado = true;
            _holdEntregaActivo    = false;
            _holdEntregaFingerId  = -1;

            if (_pendingOrdenLista != null)
            {
                _pendingOrdenLista.EntregarOrdenManual();
                MostrarTextoOrdenEntregada();
            }
            _pendingOrdenLista = null;
        }
    }

    private void MostrarTextoOrdenEntregada()
    {
        if (textoOrdenEntregada == null) return;

        if (_corrutinaTextoEntrega != null)
            StopCoroutine(_corrutinaTextoEntrega);

        _corrutinaTextoEntrega = StartCoroutine(MostrarTextoTemporal());
    }

    private System.Collections.IEnumerator MostrarTextoTemporal()
    {
        textoOrdenEntregada.gameObject.SetActive(true);
        textoOrdenEntregada.text = "¡Orden Entregada!";
        yield return new WaitForSeconds(duracionTextoEntrega);
        textoOrdenEntregada.gameObject.SetActive(false);
        _corrutinaTextoEntrega = null;
    }

    // ── MODO TROMPO (swipe) ───────────────────────────────────────────────────

    private void ProcesarSwipeModoTrompo()
    {
        if (Input.touchCount <= 0) return;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;

                if (dedoSwipeTrompo == -1)
                {
                    dedoSwipeTrompo   = touch.fingerId;
                    inicioSwipeTrompo = touch.position;
                }
                continue;
            }

            if (touch.fingerId != dedoSwipeTrompo) continue;

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                Vector2 delta = touch.position - inicioSwipeTrompo;
                bool esSwipeHaciaAbajo = delta.y <= -distanciaMinimaSwipe && Mathf.Abs(delta.y) > Mathf.Abs(delta.x);

                if (esSwipeHaciaAbajo && CookingStation.Instance != null)
                    CookingStation.Instance.AgregarIngredienteCocido(IngredienteCocina.Trompo, 1);

                dedoSwipeTrompo = -1;
            }
        }
    }

    public void SalirModoTrompo()
    {
        modoTrompoActivo = false;
        dedoSwipeTrompo  = -1;

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        if (playerController != null)
        {
            playerController.Bloquear(false);

            if (velocidadOriginalGuardada)
            {
                playerController.moveSpeed    = velocidadOriginalPlayer;
                velocidadOriginalGuardada     = false;
            }
        }

        if (cameraController == null && camaraApuntado != null)
            cameraController = camaraApuntado.GetComponent<CameraController>();

        if (cameraController != null)
            cameraController.enabled = true;

        if (trompoPanel != null)
            trompoPanel.SetActive(false);
    }

    // ── DRAG AND DROP: CARNE COCINADA → TORTILLA → ORDEN ─────────────────────

    private void ProcesarDragDesdeCarne()
    {
        // ── Mouse ─────────────────────────────────────────────────────────────
        if (Input.GetMouseButtonDown(0))
        {
            Ray rayo = camaraApuntado.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
            {
                SlotAcumulativoCarne slotCarne = hit.collider.GetComponentInParent<SlotAcumulativoCarne>();
                if (slotCarne != null && slotCarne.PuedeSertomada())
                {
                    carneDragActual           = slotCarne;
                    posicionOriginalCarneDrag = slotCarne.transform.position;
                    slotCarne.IniciarDrag();
                    estaDraqueandoCarne = true;
                    dedoDragCarne       = -1;
                    return;
                }

                SlotCocina slotTortilla = hit.collider.GetComponentInParent<SlotCocina>();
                if (slotTortilla != null && slotTortilla.TieneTaco())
                {
                    tortillaTargetDrop  = slotTortilla;
                    estaDraqueandoCarne = true;
                    dedoDragCarne       = -1;
                    return;
                }
            }
        }

        if (Input.GetMouseButton(0) && estaDraqueandoCarne)
        {
            Ray rayo = camaraApuntado.ScreenPointToRay(Input.mousePosition);

            if (carneDragActual != null)
            {
                carneDragActual.transform.position = rayo.origin + rayo.direction * 3f;

                if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
                {
                    SlotCocina slotTortilla = hit.collider.GetComponentInParent<SlotCocina>();
                    tortillaTargetDrop = (slotTortilla != null && slotTortilla.PuedeLlevarCarne()) ? slotTortilla : null;
                }
            }
            else if (tortillaTargetDrop != null && tortillaTargetDrop.TieneTaco())
            {
                tortillaTargetDrop.transform.position = rayo.origin + rayo.direction * 3f;

                if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
                {
                    OrdenLista ordenLista = hit.collider.GetComponentInParent<OrdenLista>();
                    ordenListaTargetDrop = (ordenLista != null && ordenLista.PuedeLlevarTaco()) ? ordenLista : null;
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && estaDraqueandoCarne)
            SoltarDragMouse();

        // ── Touch ─────────────────────────────────────────────────────────────
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;

                Ray rayo = camaraApuntado.ScreenPointToRay(touch.position);
                if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
                {
                    SlotAcumulativoCarne slotCarne = hit.collider.GetComponentInParent<SlotAcumulativoCarne>();
                    if (slotCarne != null && slotCarne.PuedeSertomada())
                    {
                        carneDragActual           = slotCarne;
                        posicionOriginalCarneDrag  = slotCarne.transform.position;
                        slotCarne.IniciarDrag();
                        estaDraqueandoCarne = true;
                        dedoDragCarne       = touch.fingerId;
                        break;
                    }

                    SlotCocina slotTortilla = hit.collider.GetComponentInParent<SlotCocina>();
                    if (slotTortilla != null && slotTortilla.TieneTaco())
                    {
                        tortillaTargetDrop  = slotTortilla;
                        estaDraqueandoCarne = true;
                        dedoDragCarne       = touch.fingerId;
                        break;
                    }
                }
            }

            if (touch.fingerId != dedoDragCarne || dedoDragCarne == -1) continue;

            if (touch.phase == TouchPhase.Moved && estaDraqueandoCarne)
            {
                Ray rayo = camaraApuntado.ScreenPointToRay(touch.position);

                if (carneDragActual != null)
                {
                    carneDragActual.transform.position = rayo.origin + rayo.direction * 3f;

                    if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
                    {
                        SlotCocina slotTortilla = hit.collider.GetComponentInParent<SlotCocina>();
                        tortillaTargetDrop = (slotTortilla != null && slotTortilla.PuedeLlevarCarne()) ? slotTortilla : null;
                    }
                }
                else if (tortillaTargetDrop != null && tortillaTargetDrop.TieneTaco())
                {
                    tortillaTargetDrop.transform.position = rayo.origin + rayo.direction * 3f;

                    if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
                    {
                        OrdenLista ordenLista = hit.collider.GetComponentInParent<OrdenLista>();
                        ordenListaTargetDrop = (ordenLista != null && ordenLista.PuedeLlevarTaco()) ? ordenLista : null;
                    }
                }
            }

            if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && estaDraqueandoCarne)
                SoltarDragTouch();
        }
    }

    private void SoltarDragMouse()
    {
        if (carneDragActual != null)
        {
            if (tortillaTargetDrop != null && tortillaTargetDrop.PuedeLlevarCarne())
            {
                tortillaTargetDrop.RecibirCarne(carneDragActual.ObtenerIngrediente());
                carneDragActual.DevolverCarne();
            }
            else
            {
                carneDragActual.FinalizarDrag();
            }
            carneDragActual = null;
        }
        else if (tortillaTargetDrop != null && tortillaTargetDrop.TieneTaco())
        {
            if (ordenListaTargetDrop != null && ordenListaTargetDrop.PuedeLlevarTaco())
                ordenListaTargetDrop.RecibirTaco(tortillaTargetDrop);
            else
                tortillaTargetDrop.ReestablecerPosicion();
        }

        LimpiarEstadoDrag();
    }

    private void SoltarDragTouch()
    {
        if (carneDragActual != null)
        {
            if (tortillaTargetDrop != null && tortillaTargetDrop.PuedeLlevarCarne())
            {
                tortillaTargetDrop.RecibirCarne(carneDragActual.ObtenerIngrediente());
                carneDragActual.DevolverCarne();
            }
            else
            {
                carneDragActual.FinalizarDrag();
            }
            carneDragActual = null;
        }
        else if (tortillaTargetDrop != null && tortillaTargetDrop.TieneTaco())
        {
            if (ordenListaTargetDrop != null && ordenListaTargetDrop.PuedeLlevarTaco())
                ordenListaTargetDrop.RecibirTaco(tortillaTargetDrop);
            else
                tortillaTargetDrop.ReestablecerPosicion();
        }

        LimpiarEstadoDrag();
    }

    private void LimpiarEstadoDrag()
    {
        tortillaTargetDrop   = null;
        ordenListaTargetDrop = null;
        estaDraqueandoCarne  = false;
        dedoDragCarne        = -1;
    }
}