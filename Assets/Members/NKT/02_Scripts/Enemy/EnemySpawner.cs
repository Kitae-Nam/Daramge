using System;
using System.Collections;
using System.Collections.Generic;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.Player;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;
using UnityEngine.Events;

namespace NKT.Enemy
{
    //방(던전) 하나의 적 생성과 전멸 판정을 담당한다.
    //DungeonRoomController.onSpawnEnemies -> SpawnEnemies
    //onAllEnemiesDefeated -> DungeonRoomController.NotifyAllEnemiesDefeated
    //스폰할 때 플레이어 전투 상태를 켜고, 전멸하거나 정리될 때 끈다.
    public class EnemySpawner : MonoBehaviour
    {
        [Serializable]
        private class SpawnEntry
        {
            public PoolItemSO enemyItem;
            public Transform spawnPoint;
        }

        [SerializeField] private PoolManagerSO poolManager;
        [SerializeField] private List<SpawnEntry> spawnEntries = new List<SpawnEntry>();

        [Tooltip("사망 후 풀로 돌려보내기까지 기다리는 시간 (사망 연출 시간)")]
        [SerializeField] private float returnDelay = 3f;
        [Tooltip("스폰 지점에서 NavMesh를 찾는 거리")]
        [SerializeField] private float spawnSampleDistance = 2f;

        [Tooltip("비워두면 씬에서 PlayerController를 찾는다")]
        [SerializeField] private PlayerController player;

        [SerializeField] private UnityEvent onAllEnemiesDefeated;

        public int AliveCount => _aliveEnemies.Count;

        private ICombatState _combatState;
        private bool _isCombatActive;

        private readonly List<AbstractEnemy> _aliveEnemies = new List<AbstractEnemy>();
        private readonly List<AbstractEnemy> _dyingEnemies = new List<AbstractEnemy>();
        private readonly Dictionary<AbstractEnemy, UnityAction> _deathHandlers = new Dictionary<AbstractEnemy, UnityAction>();

        public void SpawnEnemies()
        {
            SetPlayerCombat(true);

            foreach (SpawnEntry entry in spawnEntries)
                Spawn(entry);

            if (_aliveEnemies.Count == 0)
            {
                Debug.LogWarning($"{name}: 생성된 적이 없습니다. 바로 클리어 처리합니다.", this);
                CompleteCombat();
            }
        }

        //방을 나가거나 리셋할 때 남은 적을 전부 풀로 돌려보낸다.
        public void DespawnAll()
        {
            SetPlayerCombat(false);
            StopAllCoroutines();

            foreach (AbstractEnemy enemy in _aliveEnemies)
            {
                UnsubscribeDeath(enemy);
                ReturnToPool(enemy);
            }
            foreach (AbstractEnemy enemy in _dyingEnemies)
                ReturnToPool(enemy);

            _aliveEnemies.Clear();
            _dyingEnemies.Clear();
            _deathHandlers.Clear();
        }

        private void Spawn(SpawnEntry entry)
        {
            if (entry.enemyItem == null || entry.spawnPoint == null)
            {
                Debug.LogWarning($"{name}: 스폰 항목에 적 PoolItem이나 스폰 지점이 비어 있습니다.", this);
                return;
            }

            AbstractEnemy enemy = poolManager.Pop<AbstractEnemy>(entry.enemyItem);
            if (enemy == null)
            {
                Debug.LogError($"{name}: {entry.enemyItem.name}을 꺼내지 못했습니다. PoolManager의 itemList에 등록되어 있는지 확인하세요.", this);
                return;
            }

            enemy.PlaceAt(entry.spawnPoint.position, entry.spawnPoint.rotation, spawnSampleDistance);

            UnityAction handler = () => HandleEnemyDeath(enemy);
            _deathHandlers[enemy] = handler;
            enemy.OnDeath.AddListener(handler);
            _aliveEnemies.Add(enemy);
        }

        private void HandleEnemyDeath(AbstractEnemy enemy)
        {
            UnsubscribeDeath(enemy);
            if (!_aliveEnemies.Remove(enemy)) return;

            _dyingEnemies.Add(enemy);
            StartCoroutine(ReturnAfterDelay(enemy));

            if (_aliveEnemies.Count == 0)
                CompleteCombat();
        }

        private void CompleteCombat()
        {
            SetPlayerCombat(false);
            onAllEnemiesDefeated?.Invoke();
        }

        private void SetPlayerCombat(bool isCombat)
        {
            //전투를 시작하지 않은 스포너가 정리될 때(씬 전환 등) 다른 방의 전투 상태를 끄지 않게 한다.
            if (_isCombatActive == isCombat) return;
            _isCombatActive = isCombat;

            ICombatState combatState = GetCombatState();
            if (combatState != null)
                combatState.SetCombat(isCombat);
        }

        private ICombatState GetCombatState()
        {
            if (player == null)
            {
                _combatState = null;
                player = FindFirstObjectByType<PlayerController>();
                if (player == null)
                {
                    Debug.LogWarning($"{name}: 씬에서 PlayerController를 찾지 못해 전투 상태를 바꾸지 못했습니다.", this);
                    return null;
                }
            }

            if (_combatState == null)
            {
                _combatState = player.GetModule<ICombatState>();
                if (_combatState == null)
                    Debug.LogWarning($"{name}: 플레이어에 ICombatState 모듈(PlayerCombatStateModule)이 없습니다.", player);
            }
            return _combatState;
        }

        private IEnumerator ReturnAfterDelay(AbstractEnemy enemy)
        {
            yield return new WaitForSeconds(returnDelay);
            if (_dyingEnemies.Remove(enemy))
                ReturnToPool(enemy);
        }

        private void UnsubscribeDeath(AbstractEnemy enemy)
        {
            if (enemy != null && _deathHandlers.Remove(enemy, out UnityAction handler))
                enemy.OnDeath.RemoveListener(handler);
        }

        private void ReturnToPool(AbstractEnemy enemy)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
                poolManager.Push(enemy);
        }

        //풀은 씬이 바뀌어도 살아있으므로, 스포너가 사라질 때 꺼내둔 적을 돌려놓는다.
        private void OnDestroy() => DespawnAll();

        [ContextMenu("Test Spawn")]
        private void TestSpawn()
        {
            if (Application.isPlaying) SpawnEnemies();
        }

        [ContextMenu("Test Despawn All")]
        private void TestDespawnAll()
        {
            if (Application.isPlaying) DespawnAll();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
            foreach (SpawnEntry entry in spawnEntries)
            {
                if (entry?.spawnPoint == null) continue;
                Vector3 pos = entry.spawnPoint.position;
                Gizmos.DrawWireSphere(pos, 0.5f);
                Gizmos.DrawLine(pos, pos + entry.spawnPoint.forward);
            }
        }
    }
}
