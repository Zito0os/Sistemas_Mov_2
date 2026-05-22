using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonBubblePop : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(PlayBubblePop);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayBubblePop);
    }

    private static void PlayBubblePop()
    {
        SoundManager.PlaySound(SoundType.BubblePop);
    }
}
