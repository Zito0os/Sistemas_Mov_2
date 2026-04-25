using UnityEngine;

public class TrompoRotacion : MonoBehaviour
{
    [Header("Rotación")]
    [SerializeField] private float velocidadRotacion = 120f;
    [SerializeField] private Vector3 ejeRotacion = Vector3.up;

    private void Update()
    {
        transform.Rotate(ejeRotacion * velocidadRotacion * Time.deltaTime);
    }
}