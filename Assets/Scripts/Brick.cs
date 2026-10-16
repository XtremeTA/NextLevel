using UnityEngine;
using System.Collections;
public class Brick : MonoBehaviour
{
    private Animator animator;
    private bool isBreaking;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isBreaking && collision.gameObject.GetComponent<Ball>() != null)
        {
            isBreaking = true;
            GameAudio.Instance?.Play(SoundCue.BrickBreak);
            animator.SetTrigger("Break");
            StartCoroutine(DestroyAfterAnimation());
        }
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(0.3f); // match your animation length
        Destroy(gameObject);
    }
}
