using UnityEngine;
using System.Collections;
public class Brick : MonoBehaviour
{
    private Animator animator;
    public AudioSource audioSource;
    public AudioClip breakSFX;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Ball>() != null)
        {
            audioSource.PlayOneShot(breakSFX);
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
