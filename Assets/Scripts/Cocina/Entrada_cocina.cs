using UnityEngine;

public class Entrada_cocina : MonoBehaviour
{
    public Transform punto_cocina;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null || punto_cocina == null)
            return;

        player.moveSpeed = 0f;
        player.transform.position = punto_cocina.position;
    }
}
