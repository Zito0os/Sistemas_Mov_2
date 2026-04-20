using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Gestos : MonoBehaviour
{
    [Header("Raycast")]
    public Camera camaraApuntado;
    public float distanciaMaxima = 8f;
    public LayerMask mascaraRaycast = ~0;
    public string tagTeleport = "tp";
    public string tagTortilla = "tortilla";
    public string tagPastor = "pastor";
    public string tagPicadillo = "picadillo";
    public string tagDesebrada = "desebrada";
    public string tagTrompo = "trompo";
    public string tagSlot = "slot";

    [Header("Jugador")]
    public PlayerController playerController;

    [Header("Modo Trompo")]
    public float rotacionPlayerTrompoY = 77f;
    public float rotacionCamaraTrompoX = 11.4f;
    public float distanciaMinimaSwipe = 80f;

    [Header("Mantener Para Tomar Carne")]
    public float tiempoMantenerCarne = 1f;

    [Header("UI Trompo")]
    public GameObject trompoPanel;
    public Button botonRegresarTrompo;
    public GameObject buttonsPanel;
    public string nombrePanelTrompo = "Trompo_panel";
    public string nombreBotonRegresar = "regresar";
    public string nombreButtonsPanel = "Buttons_panel";

    [Header("Drag and Drop")]
    public string tagCarneAcumulada = "carneAcumulada";
    public string tagTortillaConCarne = "tortillaConCarne";
    public string tagOrdenLista = "ordenLista";
    public float distanciaDeteccionDrop = 1f;

    private CameraController cameraController;
    private bool modoTrompoActivo = false;
    private Vector2 inicioSwipeTrompo;
    private int dedoSwipeTrompo = -1;
    private float velocidadOriginalPlayer = 8f;
    private bool velocidadOriginalGuardada = false;
    private bool mouseMantenerActivo = false;
    private float mouseMantenerInicio = 0f;
    private Vector2 mouseMantenerPosicion;
    private bool mouseMantenerProcesado = false;
    private int touchMantenerFingerId = -1;
    private float touchMantenerInicio = 0f;
    private Vector2 touchMantenerPosicion;
    private bool touchMantenerProcesado = false;
    private SlotAcumulativoCarne carneDragActual = null;
    private int dedoDragCarne = -1;
    private SlotCocina tortillaTargetDrop = null;
    private OrdenLista ordenListaTargetDrop = null;
    private Vector3 posicionOriginalCarneDrag;
    private bool estaDraqueandoCarne = false;
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

        if (buttonsPanel == null)
            buttonsPanel = GameObject.Find(nombreButtonsPanel);

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
    }

    private void OnDestroy()
    {
        if (botonRegresarTrompo != null)
            botonRegresarTrompo.onClick.RemoveListener(SalirModoTrompo);
    }

    private void Update()
    {
        if (modoTrompoActivo)
        {
            ProcesarSwipeModoTrompo();
            return;
        }

        ProcesarDragDesdeCarne();
        DetectarToqueTortilla();
        ProcesarMantenerParaCarne();
    }

    private void ProcesarMantenerParaCarne()
    {
        if (mouseMantenerActivo)
        {
            if (!Input.GetMouseButton(0))
            {
                mouseMantenerActivo = false;
                mouseMantenerProcesado = false;
            }
            else if (!mouseMantenerProcesado && Time.time - mouseMantenerInicio >= tiempoMantenerCarne)
            {
                IntentarAgregarCarnePorMantener(mouseMantenerPosicion);
                mouseMantenerProcesado = true;
            }
        }

        if (touchMantenerFingerId == -1)
            return;

        bool touchActivo = false;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId != touchMantenerFingerId)
                continue;

            touchActivo = true;

            if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                touchMantenerFingerId = -1;
                touchMantenerProcesado = false;
                break;
            }

            if (!touchMantenerProcesado && Time.time - touchMantenerInicio >= tiempoMantenerCarne)
            {
                IntentarAgregarCarnePorMantener(touchMantenerPosicion);
                touchMantenerProcesado = true;
            }

            break;
        }

        if (!touchActivo)
        {
            touchMantenerFingerId = -1;
            touchMantenerProcesado = false;
        }
    }

    private void ProcesarSwipeModoTrompo()
    {
        if (Input.touchCount <= 0)
            return;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;

                if (dedoSwipeTrompo == -1)
                {
                    dedoSwipeTrompo = touch.fingerId;
                    inicioSwipeTrompo = touch.position;
                }

                continue;
            }

            if (touch.fingerId != dedoSwipeTrompo)
                continue;

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

    private void DetectarToqueTortilla()
    {
        if (camaraApuntado == null)
            return;

        if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                mouseMantenerActivo = true;
                mouseMantenerInicio = Time.time;
                mouseMantenerPosicion = Input.mousePosition;
                mouseMantenerProcesado = false;
                RevisarPuntoDeInteraccion(Input.mousePosition);
            }
        }

        if (Input.touchCount <= 0)
            return;

        foreach (Touch touch in Input.touches)
        {
            if (touch.phase != TouchPhase.Began)
                continue;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                continue;

            if (touchMantenerFingerId == -1)
            {
                touchMantenerFingerId = touch.fingerId;
                touchMantenerInicio = Time.time;
                touchMantenerPosicion = touch.position;
                touchMantenerProcesado = false;
            }

            RevisarPuntoDeInteraccion(touch.position);
        }
    }

    private void IntentarAgregarCarnePorMantener(Vector2 pantallaPosicion)
    {
        if (CookingStation.Instance == null)
            return;

        Ray rayo = camaraApuntado.ScreenPointToRay(pantallaPosicion);
        if (!Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
            return;

        if (hit.collider.CompareTag(tagPastor))
        {
            CookingStation.Instance.AgregarCarnePastor(1);
            return;
        }

        if (hit.collider.CompareTag(tagPicadillo))
        {
            CookingStation.Instance.AgregarCarnePicadillo(1);
            return;
        }

        if (hit.collider.CompareTag(tagDesebrada))
        {
            CookingStation.Instance.AgregarCarneDesebrada(1);
            return;
        }

        if (hit.collider.CompareTag(tagTortilla))
        {
            CookingStation.Instance.AgregarTortilla(1);
            return;
        }
    }

    private void RevisarPuntoDeInteraccion(Vector2 pantallaPosicion)
    {
        Ray rayo = camaraApuntado.ScreenPointToRay(pantallaPosicion);
        if (!Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
            return;

        if (hit.collider.CompareTag(tagSlot))
        {
            SlotCocina slotCocina = hit.collider.GetComponentInParent<SlotCocina>();
            if (slotCocina != null)
                slotCocina.InteractuarConSlot();

            return;
        }

        if (hit.collider.CompareTag(tagTeleport))
        {
            Position positionData = hit.collider.GetComponent<Position>();
            if (positionData == null || positionData.POSITION_TO_GO == null)
                return;

            if (playerController == null)
                playerController = GetComponentInParent<PlayerController>();

            if (playerController == null)
                return;

            playerController.moveSpeed = 0f;
            playerController.transform.position = positionData.POSITION_TO_GO.position;
            return;
        }

        if (CookingStation.Instance == null)
            return;

        if (hit.collider.CompareTag(tagTortilla))
            return;

        if (hit.collider.CompareTag(tagPastor))
        {
            return;
        }

        if (hit.collider.CompareTag(tagPicadillo))
        {
            return;
        }

        if (hit.collider.CompareTag(tagDesebrada))
        {
            return;
        }

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
                    velocidadOriginalPlayer = playerController.moveSpeed;
                    velocidadOriginalGuardada = true;
                }

                playerController.AplicarRotacionForzada(rotacionPlayerTrompoY, rotacionCamaraTrompoX);
                playerController.moveSpeed = 0f;
                playerController.enabled = false;
            }

            if (cameraController != null)
                cameraController.enabled = false;

            modoTrompoActivo = true;
            dedoSwipeTrompo = -1;

            if (trompoPanel != null)
                trompoPanel.SetActive(true);

            if (buttonsPanel != null)
                buttonsPanel.SetActive(false);

            return;
        }
    }

    public void SalirModoTrompo()
    {
        modoTrompoActivo = false;
        dedoSwipeTrompo = -1;

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        if (playerController != null)
        {
            playerController.enabled = true;

            if (velocidadOriginalGuardada)
            {
                playerController.moveSpeed = velocidadOriginalPlayer;
                velocidadOriginalGuardada = false;
            }
        }

        if (cameraController == null && camaraApuntado != null)
            cameraController = camaraApuntado.GetComponent<CameraController>();

        if (cameraController != null)
            cameraController.enabled = false;

        if (trompoPanel != null)
            trompoPanel.SetActive(false);

        if (buttonsPanel != null)
            buttonsPanel.SetActive(true);
    }

    private void ProcesarDragDesdeCarne()
    {
        // Manejo de mouse
        if (Input.GetMouseButtonDown(0))
        {
            Ray rayo = camaraApuntado.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
            {
                // Intentar arrastrar carne acumulada
                SlotAcumulativoCarne slotCarne = hit.collider.GetComponentInParent<SlotAcumulativoCarne>();
                if (slotCarne != null && slotCarne.PuedeSertomada())
                {
                    carneDragActual = slotCarne;
                    posicionOriginalCarneDrag = slotCarne.transform.position;
                    slotCarne.IniciarDrag();
                    estaDraqueandoCarne = true;
                    dedoDragCarne = -1; // Mouse
                    return;
                }

                // Intentar arrastrar taco desde tortilla
                SlotCocina slotTortilla = hit.collider.GetComponentInParent<SlotCocina>();
                if (slotTortilla != null && slotTortilla.TieneTaco())
                {
                    // Iniciar drag de taco
                    tortillaTargetDrop = slotTortilla;
                    estaDraqueandoCarne = true;
                    dedoDragCarne = -1; // Mouse
                    return;
                }
            }
        }

        if (Input.GetMouseButton(0) && estaDraqueandoCarne)
        {
            Ray rayo = camaraApuntado.ScreenPointToRay(Input.mousePosition);

            // Si estamos arrastrando carne acumulada
            if (carneDragActual != null)
            {
                carneDragActual.transform.position = rayo.origin + rayo.direction * 3f;

                if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
                {
                    SlotCocina slotTortilla = hit.collider.GetComponentInParent<SlotCocina>();
                    if (slotTortilla != null && slotTortilla.PuedeLlevarCarne())
                    {
                        tortillaTargetDrop = slotTortilla;
                    }
                    else
                    {
                        tortillaTargetDrop = null;
                    }
                }
            }
            // Si estamos arrastrando un taco
            else if (tortillaTargetDrop != null && tortillaTargetDrop.TieneTaco())
            {
                tortillaTargetDrop.transform.position = rayo.origin + rayo.direction * 3f;

                if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
                {
                    OrdenLista ordenLista = hit.collider.GetComponentInParent<OrdenLista>();
                    if (ordenLista != null && ordenLista.PuedeLlevarTaco())
                    {
                        ordenListaTargetDrop = ordenLista;
                    }
                    else
                    {
                        ordenListaTargetDrop = null;
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && estaDraqueandoCarne)
        {
            // Si estamos soltando carne en tortilla
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
            // Si estamos soltando taco en orden lista
            else if (tortillaTargetDrop != null && tortillaTargetDrop.TieneTaco())
            {
                if (ordenListaTargetDrop != null && ordenListaTargetDrop.PuedeLlevarTaco())
                {
                    ordenListaTargetDrop.RecibirTaco(tortillaTargetDrop);
                }
                else
                {
                    tortillaTargetDrop.ReestablecerPosicion();
                }
            }

            tortillaTargetDrop = null;
            ordenListaTargetDrop = null;
            estaDraqueandoCarne = false;
            dedoDragCarne = -1;
        }

        // Manejo de touch
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
                    // Intentar arrastrar carne acumulada
                    SlotAcumulativoCarne slotCarne = hit.collider.GetComponentInParent<SlotAcumulativoCarne>();
                    if (slotCarne != null && slotCarne.PuedeSertomada())
                    {
                        carneDragActual = slotCarne;
                        posicionOriginalCarneDrag = slotCarne.transform.position;
                        slotCarne.IniciarDrag();
                        estaDraqueandoCarne = true;
                        dedoDragCarne = touch.fingerId;
                        break;
                    }

                    // Intentar arrastrar taco desde tortilla
                    SlotCocina slotTortilla = hit.collider.GetComponentInParent<SlotCocina>();
                    if (slotTortilla != null && slotTortilla.TieneTaco())
                    {
                        tortillaTargetDrop = slotTortilla;
                        estaDraqueandoCarne = true;
                        dedoDragCarne = touch.fingerId;
                        break;
                    }
                }
            }

            if (touch.fingerId != dedoDragCarne || dedoDragCarne == -1)
                continue;

            if (touch.phase == TouchPhase.Moved && estaDraqueandoCarne)
            {
                Ray rayo = camaraApuntado.ScreenPointToRay(touch.position);

                // Si estamos arrastrando carne acumulada
                if (carneDragActual != null)
                {
                    carneDragActual.transform.position = rayo.origin + rayo.direction * 3f;

                    if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
                    {
                        SlotCocina slotTortilla = hit.collider.GetComponentInParent<SlotCocina>();
                        if (slotTortilla != null && slotTortilla.PuedeLlevarCarne())
                        {
                            tortillaTargetDrop = slotTortilla;
                        }
                        else
                        {
                            tortillaTargetDrop = null;
                        }
                    }
                }
                // Si estamos arrastrando un taco
                else if (tortillaTargetDrop != null && tortillaTargetDrop.TieneTaco())
                {
                    tortillaTargetDrop.transform.position = rayo.origin + rayo.direction * 3f;

                    if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
                    {
                        OrdenLista ordenLista = hit.collider.GetComponentInParent<OrdenLista>();
                        if (ordenLista != null && ordenLista.PuedeLlevarTaco())
                        {
                            ordenListaTargetDrop = ordenLista;
                        }
                        else
                        {
                            ordenListaTargetDrop = null;
                        }
                    }
                }
            }

            if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && estaDraqueandoCarne)
            {
                // Si estamos soltando carne en tortilla
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
                // Si estamos soltando taco en orden lista
                else if (tortillaTargetDrop != null && tortillaTargetDrop.TieneTaco())
                {
                    if (ordenListaTargetDrop != null && ordenListaTargetDrop.PuedeLlevarTaco())
                    {
                        ordenListaTargetDrop.RecibirTaco(tortillaTargetDrop);
                    }
                    else
                    {
                        tortillaTargetDrop.ReestablecerPosicion();
                    }
                }

                tortillaTargetDrop = null;
                ordenListaTargetDrop = null;
                estaDraqueandoCarne = false;
                dedoDragCarne = -1;
            }
        }
    }
}

