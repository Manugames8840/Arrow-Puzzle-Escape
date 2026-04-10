using UnityEngine;
using Framework;
using Framework.Core;



namespace Watermelon
{
    public class ConstantRotation : MonoBehaviour, IUIPageElement
    {
        [SerializeField] Vector3 rotationSpeed = new Vector3(0, 0, 100);

        public void Init(UIPage page)
        {

        }

        public void OnPageStateChanged(bool state)
        {
            enabled = state;
        }

        private void Update()
        {
            transform.Rotate(rotationSpeed * Time.unscaledDeltaTime);
        }
    }
}
