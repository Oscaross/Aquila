using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Slinger slingerPrefab;
    [Tooltip("The offset vector applied from the center of the enemy spawner to the actual spawn position of new enemies.")]
    [SerializeField] private Vector2 spawnOffset;

    [ContextMenu("Spawn Slinger")]
    private void SpawnSlinger()
    {
        Slinger s = Instantiate(slingerPrefab, new Vector2(transform.position.x, transform.position.y) + spawnOffset, Quaternion.identity, transform.parent);
        s.SetTarget(new Vector2(Random.Range(-30f, 45f), 0f)); // DEBUG
    }
}
