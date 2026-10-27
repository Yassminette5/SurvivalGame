using UnityEngine;
using UnityEngine.AI;

// P4 - Fait apparaitre des ennemis autour du joueur, beaucoup plus la nuit.
public class EnemySpawner : MonoBehaviour
{
    public EnemyAI enemyPrefab;
    public Transform player;
    public int maxEnemiesDay = 3;
    public int maxEnemiesNight = 8;
    public float spawnInterval = 10f;
    public float minDistance = 22f;
    public float maxDistance = 40f;
    public float worldHalfSize = 95f;

    float nextSpawn;

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (Time.time < nextSpawn) return;
        nextSpawn = Time.time + spawnInterval;

        bool night = DayNightCycle.Instance && DayNightCycle.Instance.IsNight;
        if (EnemyAI.All.Count >= (night ? maxEnemiesNight : maxEnemiesDay)) return;
        TrySpawnNearPlayer();
    }

    void TrySpawnNearPlayer()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(minDistance, maxDistance);
            Vector3 p = player.position + new Vector3(dir.x, 0f, dir.y);
            if (Mathf.Abs(p.x) > worldHalfSize || Mathf.Abs(p.z) > worldHalfSize) continue;
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                Spawn(hit.position, enemyPrefab.maxHealth);
                return;
            }
        }
    }

    public EnemyAI Spawn(Vector3 position, float health)
    {
        var e = Instantiate(enemyPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        e.SetHealth(health);
        return e;
    }

    public void ClearAll()
    {
        for (int i = EnemyAI.All.Count - 1; i >= 0; i--)
            Destroy(EnemyAI.All[i].gameObject);
        EnemyAI.All.Clear();
        nextSpawn = Time.time + spawnInterval;
    }
}
