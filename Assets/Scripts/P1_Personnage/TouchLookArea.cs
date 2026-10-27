using UnityEngine;
using UnityEngine.EventSystems;

// P1 - Zone a droite de l'ecran : glisser le doigt tourne la camera (secours si pas de gyroscope).
public class TouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public void OnPointerDown(PointerEventData e) { }

    public void OnDrag(PointerEventData e)
    {
        if (InputHub.Instance) InputHub.Instance.AddLook(e.delta);
    }
}
