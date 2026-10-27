using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Partage : bouton qu'on garde appuye (ex : Sprint).
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public event Action<bool> Held;

    public void OnPointerDown(PointerEventData e) => Held?.Invoke(true);
    public void OnPointerUp(PointerEventData e) => Held?.Invoke(false);
    void OnDisable() => Held?.Invoke(false);
}
