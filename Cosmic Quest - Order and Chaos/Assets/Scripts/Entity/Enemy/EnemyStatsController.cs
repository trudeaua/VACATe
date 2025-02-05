﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class EnemyColouring {
    public Material Green;
    public Material Blue;
    public Material Red;
    public Material Yellow;
    public Material Default;
}

[RequireComponent(typeof(EnemyBrainController))]
public class EnemyStatsController : EntityStatsController
{
    protected EnemyBrainController Brain;
    protected NavMeshAgent Agent;

    private float _minTimeBetweenDamageText = 0.3f;
    private float _damageTextValue = 0f;
    private float _damageTextCounter = 0f;
    public GameObject FloatingText;

    private Collider _collider;
    
    [Header("Colour Config")]
    [Tooltip("Coloured materials that will be assigned to an enemy")]
    [SerializeField] protected EnemyColouring EnemyColouring;
    [Tooltip("Used for bosses; Indicates whether the enemy should rotate through player colours or not")]
    [SerializeField] protected bool rotateColouring = false;
    [SerializeField] protected float minTimeBetweenColourChanges = 7.0f;
    protected float colourChangeTimeCounter = 0;
    public float colourResistanceModifier = 0.35f;
    public bool isBoss = false;

    protected override void Awake()
    {
        base.Awake();

        Brain = GetComponent<EnemyBrainController>();
        Agent = GetComponent<NavMeshAgent>();
        _collider = gameObject.GetComponent<Collider>();
    }

    private void Start()
    {
        // Assign enemy a colour
        if (characterColour == CharacterColour.None)
            AssignRandomColour();
        else
            AssignEnemyColour(characterColour);

        ScaleEnemies(PlayerManager.Instance.NumPlayers);
        
        if (shouldSpawn)
        {
            // Create a VFX where the enemy will spawn - just slightly above the stage (0.1f) - and change the VFX colour to match the enemy colour
            StartCoroutine(VfxHelper.CreateVFX(spawnVFX, transform.position + new Vector3(0, 0.01f, 0),
                Quaternion.identity, PlayerManager.colours.GetColour(characterColour), 0.5f));
            // "Spawn" the enemy (they float up through the stage)
            //StartCoroutine(Spawn(gameObject, spawnSpeed, spawnDelay, spawnCooldown));
        }
        Anim.SetTrigger("Spawn");
    }

    protected override void Update()
    {
        base.Update();

        if (_damageTextCounter > 0f)
            _damageTextCounter -= Time.deltaTime;

        if (rotateColouring) {
            colourChangeTimeCounter += Time.deltaTime;
            if (colourChangeTimeCounter > minTimeBetweenColourChanges) {
                colourChangeTimeCounter = 0;
                AssignRandomColour();
            }
        }
    }

    /// <summary>
    /// Take damage from an attacker
    /// </summary>
    /// <param name="attacker">Stats controller of the attacking entity</param>
    /// <param name="damageValue">Approximate damage value to apply to enemy health</param>
    /// <param name="timeDelta">Time since last damage calculation</param>
    public override void TakeDamage(EntityStatsController attacker, float damageValue, float timeDelta = 1f)
    {
        // Ignore attacks if already dead or invincible
        if (isDead || invincible)
            return;

        float colourDamagePercentage  = characterColour == CharacterColour.All || attacker.characterColour == characterColour ? 1 : colourResistanceModifier;

        // Calculate any changes based on stats and modifiers here first
        float hitValue = Mathf.Max(colourDamagePercentage * (damageValue - ComputeDefenseModifier()), 0) * timeDelta;
        health.Subtract(hitValue);
        ShowDamage(hitValue);
        Anim.SetTrigger("TakeDamage");

        // Pass damage information to brain
        Brain.OnDamageTaken(attacker.gameObject, hitValue);

        if (Mathf.Approximately(health.CurrentValue, 0f))
        {
            Die();
        }
    }

    /// <summary>
    /// Display the amount of damage taken
    /// </summary>
    /// <param name="value">Value to display</param>
    /// <param name="duration">How long to show the damage value for</param>
    protected void ShowDamage(float value, float duration = 0.5f)
    {
        _damageTextValue += value;
        if (_damageTextCounter > 0f || _damageTextValue < 0.5f)
            return;

        Vector3 offset = new Vector3(0, _collider.bounds.size.y + 4f, 0);
        float x = 1f, y = 0.5f;
        Vector3 random = new Vector3(Random.Range(-x, x), Random.Range(-y, y));

        GameObject text = Instantiate(FloatingText, transform.position + offset + random, Quaternion.identity);
        text.GetComponent<TMP_Text>().text = _damageTextValue.ToString("F2");

        Destroy(text, duration);

        // Reset the damage text timer between text instances
        _damageTextCounter = _minTimeBetweenDamageText;
        _damageTextValue = 0f;
    }

    /// <summary>
    /// Apply an explosive force to the rigidbody
    /// </summary>
    /// <param name="explosionForce">Value to display</param>
    /// <param name="explosionPoint">Where the explosion originates from</param>
    /// <param name="explosionRadius">Explosion effect radius</param>
    /// <param name="stunTime">Amount of time to stun the enemy</param>
    /// <returns>An IEnumerator</returns>
    protected override IEnumerator ApplyExplosiveForce(float explosionForce, Vector3 explosionPoint, float explosionRadius, float stunTime)
    {
        // Set to stunned before applying explosive force
        SetStunned(true);
        rb.isKinematic = false;

        rb.AddExplosionForce(explosionForce, explosionPoint, explosionRadius);

        // Wait for a moment before re-enabling the navMeshAgent
        yield return new WaitForSeconds(stunTime);
        rb.isKinematic = true;
        SetStunned(false);
    }

    /// <summary>
    /// Instantly kills the enemy
    /// </summary>
    public void Kill()
    {
        // Handle any death activities
        Die();
    }

    /// <summary>
    /// Kill the enemy
    /// </summary>
    protected override void Die()
    {
        isDead = true;
        onDeath.Invoke();
        if (Agent) Agent.enabled = false;
        StartCoroutine(AudioHelper.PlayAudioOverlap(VocalAudio, entityDeathVocalSFX));
        Anim.SetBool("Dead", true);
    }

    /// <summary>
    /// Set the stun value of the enemy and toggle the movement navigation
    /// </summary>
    private void SetStunned(bool isStunned)
    {
        // Disable the nav and stun the brain
        if (Agent) Agent.enabled = !isStunned;
        Brain.SetStunned(isStunned);
    }

    /// <summary>
    /// "Spawn" the enemy by causing them to float up through the stage
    /// </summary>
    /// <param name="obj">Object to spawn</param>
    /// <param name="speed">How fast the spawn should be</param>
    /// <param name="delay">How many seconds to wait before spawning</param>
    /// <param name="cooldown">How many seconds to wait before enabling the enemy's movement</param>
    protected override IEnumerator Spawn(GameObject obj, float speed = 0.05F, float delay = 0, float cooldown = 0)
    {
        // weird stuff happens when the nav mesh is enabled during the spawn
        if (Agent) Agent.enabled = false;
        yield return base.Spawn(obj, speed, delay, cooldown);
        if (Agent) Agent.enabled = true;
    }

    /// <summary>
    /// Set the texture and highlight materials for the enemy based on a character colour
    /// </summary>
    /// <param name="colour">Character colour to base colour assignment decision on</param>
    public void AssignEnemyColour(CharacterColour colour)
    {
        characterColour = colour;
        SkinnedMeshRenderer skin = GetComponentInChildren<SkinnedMeshRenderer>();
        Material skinMaterial;
        switch (colour)
        {
            case CharacterColour.Red:
                skinMaterial = EnemyColouring.Red;
                break;
            case CharacterColour.Yellow:
                skinMaterial = EnemyColouring.Yellow;
                break;
            case CharacterColour.Green:
                skinMaterial = EnemyColouring.Green;
                break;
            case CharacterColour.Blue:
                skinMaterial = EnemyColouring.Blue;
                break;
            default:
                skinMaterial = EnemyColouring.Default;
                break;
        }

        if (skinMaterial)
            skin.material = skinMaterial;
    }
    
    /// <summary>
    /// Assign a random colour to the enemy
    /// </summary>
    protected void AssignRandomColour()
    {
        CharacterColour randomColour;
        // Get a colour that is used by a registered player
        // Keep choosing a random colour until a different one is chosen
        do {
            randomColour = PlayerManager.Instance.PlayerColours[Random.Range(0, PlayerManager.Instance.NumPlayers)];
        } while (randomColour == characterColour);
        
        // Assign the enemy colour
        AssignEnemyColour(randomColour);
    }

    private void ScaleEnemies(int numPlayers)
    {
        // Scale enemy HP to number of players
        float healthMultiplier = 1 + numPlayers * (isBoss ? 0.5f : 0.1f);
        health.maxValue = health.maxValue * healthMultiplier;
        health.Init();

        // Scale player damage on colour resistant enemies(to different colours)
        colourResistanceModifier =  0.35f + (4 - numPlayers) * 0.15f;
    }
}