using UnityEngine;
using Framework;
using Framework.Core;
using ArrowOut;

namespace Watermelon
{
    [RequireComponent(typeof(Animator))]
    public class HammerAnimationBehavior : MonoBehaviour
    {
        private Animator animator;
        private PUHammerBehavior hammerBehavior;

        private SimpleCallback onCompletedCallback;
        private Arrow levelBlockBehavior;

        private bool isPlaying;

        public void Init(PUHammerBehavior hammerBehavior)
        {
            this.hammerBehavior = hammerBehavior;

            animator = GetComponent<Animator>();

            gameObject.SetActive(false);

            isPlaying = false;
            levelBlockBehavior = null;
        }

        public void PlayHitAnimation(Vector3 position, Arrow arrow, SimpleCallback onCompletedCallback)
        {
            if (isPlaying) return;

            this.onCompletedCallback = onCompletedCallback;

            isPlaying = true;

            gameObject.SetActive(true);

            transform.position = position;
            animator.Play("Hit", -1, 0);
        }

        public void OnHitAnimationEnded()
        {
            gameObject.SetActive(false);

            onCompletedCallback?.Invoke();
            onCompletedCallback = null;

            isPlaying = false;

            levelBlockBehavior = null;
        }
    }
}
