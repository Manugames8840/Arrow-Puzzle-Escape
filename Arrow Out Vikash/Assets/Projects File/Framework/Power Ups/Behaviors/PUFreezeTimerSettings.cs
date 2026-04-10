using UnityEngine;
using Framework;
using Framework.Core;



namespace Watermelon
{
    [CreateAssetMenu(fileName = "PU Freeze Timer Settings", menuName = "Data/Power Ups/PU Freeze Timer Settings")]
    public class PUFreezeTimerSettings : PUSettings
    {
        [LineSpacer("Timer")]
        [SerializeField] float timeFreezeDuration = 10.0f;
        public float TimeFreezeDuration => timeFreezeDuration;

        public override void Init()
        {

        }
    }
}
