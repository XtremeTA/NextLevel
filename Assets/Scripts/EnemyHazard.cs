using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class EnemyHazard : MonoBehaviour
{
    private bool despawning;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Hit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Hit(other);
    }

    private void Hit(Collider2D other)
    {
        if (despawning || !isActiveAndEnabled || other.isTrigger)
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player != null && player.TakeEnemyHit(transform.position))
        {
            despawning = true;
            // Hide and stop the enemy now, including any pending split coroutine.
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
