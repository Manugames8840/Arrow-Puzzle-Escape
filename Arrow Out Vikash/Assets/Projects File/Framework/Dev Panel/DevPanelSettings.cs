using UnityEngine;
using Framework;
using Framework.Core;



namespace Watermelon
{
    [CreateAssetMenu(fileName = "Dev Panel Settings", menuName = "Data/Core/Dev Panel Settings")]
    public class DevPanelSettings : ScriptableObject
    {
        [SerializeField] bool isEnabled = true;
        public bool IsEnabled => isEnabled;
    }
}
