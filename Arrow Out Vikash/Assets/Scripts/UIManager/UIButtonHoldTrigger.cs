using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ArrowOut
{
    /// <summary>
    /// Attach to a UI Button to detect pointer hold and release events.
    /// Used by CameraController to zoom while the button is held down.
    /// </summary>
    public class UIButtonHoldTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action OnHeld;
        public Action OnReleased;

        bool isHeld;

        public void OnPointerDown(PointerEventData eventData)
        {
            isHeld = true;
            OnHeld?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        void Release()
        {
            if (!isHeld) return;
            isHeld = false;
            OnReleased?.Invoke();
        }
    }
}
