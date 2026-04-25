using TMPro;
using UnityEngine;

public class StartDayPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txt_inicio_dia;
    [SerializeField] private float duracion = 2.5f;
    private float _timer;

    private void OnEnable()
    {
        _timer = duracion;

        if (txt_inicio_dia != null && GameManager.Instance != null)
            txt_inicio_dia.text = $"DIA {GameManager.Instance.CurrentDay}";
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            GameManager.Instance?.AdvanceToNextState();
    }
}