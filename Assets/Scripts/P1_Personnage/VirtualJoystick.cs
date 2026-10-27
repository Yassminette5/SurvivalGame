using UnityEngine;
using UnityEngine.EventSystems;

// P1 - Joystick virtuel : apparait la ou le pouce touche (moitie gauche de l'ecran).
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    RectTransform background;
    RectTransform handle;
    float radius;
    RectTransform area;
    Vector2 restPosition;

    public void Init(RectTransform background, RectTransform handle, float radius)
    {
        this.background = background;
        this.handle = handle;
        this.radius = radius;
        area = (RectTransform)transform;
        restPosition = background.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!area) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out Vector2 p))
            background.anchoredPosition = p - area.rect.min;
        OnDrag(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!area) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out Vector2 p)) return;
        Vector2 center = background.anchoredPosition + area.rect.min;
        Vector2 d = Vector2.ClampMagnitude(p - center, radius);
        handle.anchoredPosition = d;
        if (InputHub.Instance) InputHub.Instance.joystick = d / radius;
    }

    public void OnPointerUp(PointerEventData e) => Release();

    void OnDisable() => Release();

    void Release()
    {
        if (background) background.anchoredPosition = restPosition;
        if (handle) handle.anchoredPosition = Vector2.zero;
        if (InputHub.Instance) InputHub.Instance.joystick = Vector2.zero;
    }
}
