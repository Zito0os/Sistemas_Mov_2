using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public RectTransform joystickBackground;  // Base del joystick
    public RectTransform joystickHandle;      // Palanca del joystick
    private Vector2 inputVector = Vector2.zero;

    public Vector2 GetInput() => inputVector;

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos = eventData.position - (Vector2)joystickBackground.position;
        float radius = joystickBackground.sizeDelta.x / 2;
        inputVector = Vector2.ClampMagnitude(pos / radius, 1.0f);
        joystickHandle.anchoredPosition = inputVector * radius;
    }

    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
    }
}
