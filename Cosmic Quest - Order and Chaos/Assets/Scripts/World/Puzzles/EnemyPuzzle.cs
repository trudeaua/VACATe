using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Puzzle variant that spawns enemies and completes when the enemies are killed
/// </summary>
public class EnemyPuzzle : Puzzle
{
    [Tooltip("Indicates whether the puzzle represents a boss fight or not")]
    public bool isBoss = false;
    [Tooltip("Indicates whether the number of enemies spawned should be auto calculated")]
    public bool autoDetermineNumEnemies = false;
    [Tooltip("Damage modifier value on the spawned enemies")]
    [Range(0, 10)]
    public int damageModifier = 0;
    [Tooltip("Defense modifier value on the spawned enemies")]
    [Range(0, 10)]
    public int defenseModifier = 0;
    [Tooltip("Prefab objects of an enemies to instantiate in the puzzle")]
    public GameObject[] enemyPrefabs;

    protected int numEnemies;
    protected int numEnemiesDead;
    private List<GameObject> loadedEnemies = new List<GameObject>();

    /// <summary>
    /// Set up the puzzle
    /// </summary>
    protected virtual void Setup()
    {
        loadedEnemies.Clear();
        numEnemiesDead = 0;
        if (autoDetermineNumEnemies)
        {
            // Base # enemies off number of players
            numEnemies = playerColours.Length * 2;
        }
        else
        {
            numEnemies = enemyPrefabs.Length;
        }
        // spawn enemies
        for (int i = 0; i < numEnemies; i++)
        {
            int enemyIndex = UnityEngine.Random.Range(0, enemyPrefabs.Length);
            GameObject enemyObj = Instantiate(enemyPrefabs[enemyIndex], transform);
            loadedEnemies.Add(enemyObj);

            // add any modifieres to the enemy
            EnemyStatsController enemyStats = enemyObj.GetComponent<EnemyStatsController>();
            enemyStats.damage.AddModifier(damageModifier);
            enemyStats.damage.AddModifier(defenseModifier);

            enemyStats.characterColour = CharacterColour.None;
            enemyStats.onDeath.AddListener(EnemyDied);
        }
        if (isBoss)
        {
            GameManager.Instance.SetBossState();
            MusicManager.Instance.PlayMusic();
        }
    }

    /// <summary>
    /// Reset the puzzle
    /// </summary>
    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        foreach(GameObject loadedEnemy in loadedEnemies)
        {
            Destroy(loadedEnemy);
        }
        Setup();
    }

    /// <summary>
    /// Callback for when an enemy dies
    /// </summary>
    protected void EnemyDied()
    {
        numEnemiesDead += 1;
        // all dead
        if (numEnemiesDead == numEnemies)
        {
            GameManager.Instance.SetPlayState();
            SetComplete();
        }
    }
}