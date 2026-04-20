using UnityEngine;
using UnityEngine.UI;

public class TP : MonoBehaviour
{
    [Header("Raycast")]
    public Camera camaraApuntado;
    public float distanciaMaxima = 8f;
    public LayerMask mascaraRaycast = ~0;
    public string tagTeleport = "tp";

    [Header("UI")]
    public Button trompoButton;

    [Header("Jugador")]
    public PlayerController playerController;

    private Transform destinoActual;

    private void Start()
    {
        if (camaraApuntado == null)
            camaraApuntado = Camera.main;

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        if (trompoButton == null)
        {
            GameObject panel = GameObject.Find("Buttons_panel");
            if (panel != null)
            {
                Transform trompo = panel.transform.Find("trompo");
                if (trompo != null)
                    trompoButton = trompo.GetComponent<Button>();
            }
        }

        if (trompoButton != null)
        {
            trompoButton.gameObject.SetActive(false);
            trompoButton.onClick.AddListener(TeletransportarAJugador);
        }
    }

    private void Update()
    {
        ActualizarObjetivoTeleport();
    }

    private void OnDestroy()
    {
        if (trompoButton != null)
            trompoButton.onClick.RemoveListener(TeletransportarAJugador);
    }

    private void ActualizarObjetivoTeleport()
    {
        destinoActual = null;

        if (camaraApuntado == null)
        {
            MostrarBoton(false);
            return;
        }

        Ray rayo = camaraApuntado.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, mascaraRaycast, QueryTriggerInteraction.Collide))
        {
            MostrarBoton(false);
            return;
        }

        if (!hit.collider.CompareTag(tagTeleport))
        {
            MostrarBoton(false);
            return;
        }

        Position positionData = hit.collider.GetComponent<Position>();
        if (positionData == null || positionData.POSITION_TO_GO == null)
        {
            MostrarBoton(false);
            return;
        }

        destinoActual = positionData.POSITION_TO_GO;
        MostrarBoton(true);
    }

    private void TeletransportarAJugador()
    {
        if (destinoActual == null)
            return;

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        if (playerController == null)
            return;

        playerController.moveSpeed = 0f;
        playerController.transform.position = destinoActual.position;
        MostrarBoton(false);
    }

    private void MostrarBoton(bool mostrar)
    {
        if (trompoButton == null)
            return;

        trompoButton.gameObject.SetActive(mostrar);
    }
}
