using UnityEngine;
using Framework;
using Framework.Core;



[RequireComponent(typeof(Animator))]
public class PUFreezeAnimationBehavior : MonoBehaviour
{
    private Animator animator;

    private SimpleCallback onCompletedCallback;

    private bool isPlaying;

    public void Init()
    {
        animator = GetComponent<Animator>();

        gameObject.SetActive(false);

        isPlaying = false;
    }

    public void PlayAnimation(Vector3 position, SimpleCallback onCompletedCallback)
    {
        if (isPlaying)
            return;

        this.onCompletedCallback = onCompletedCallback;

        isPlaying = true;

        gameObject.SetActive(true);

        transform.position = position;
        animator.Play("Rotate", -1, 0);
    }

    public void OnRotateAnimationEnded()
    {
        gameObject.SetActive(false);

        onCompletedCallback?.Invoke();
        onCompletedCallback = null;

        isPlaying = false;
    }
}
