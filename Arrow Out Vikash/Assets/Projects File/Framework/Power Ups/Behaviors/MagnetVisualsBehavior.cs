using UnityEngine;
using Framework;
using Framework.Core;



namespace Watermelon
{
    [RequireComponent(typeof(Animator))]
    public class MagnetVisualsBehavior : MonoBehaviour
    {
        [SerializeField] ParticleSystem magnetParticleSystem;

        private Animator animator;
        private PUMagnetBehavior magnetBehavior;

        public void Init(PUMagnetBehavior magnetBehavior)
        {
            this.magnetBehavior = magnetBehavior;

            animator = GetComponent<Animator>();

            gameObject.SetActive(false);
        }

        public void PlayAnimation(Vector3 magnetPosition)
        {
            gameObject.SetActive(true);

            transform.position = magnetPosition;

            animator.Play("Appear", -1, 0);
        }

        public void StopAnimation()
        {
            magnetParticleSystem.Stop();

            animator.SetTrigger("Disappear");
        }

        public void ActivateParticle()
        {
            magnetParticleSystem.Play();
        }

        public void OnDisappeared()
        {
            animator.Play("Default", -1, 0);

            gameObject.SetActive(false);
        }
    }
}
