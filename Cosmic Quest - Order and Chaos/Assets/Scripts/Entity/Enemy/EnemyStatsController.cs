﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class EnemyColouring {
    [System.Serializable]
    public class ColourVariant {
        [Tooltip("Material containing the enemy \"picture\"")]
        public Material textureMaterial;
        [Tooltip("Emissive material that accents the enemy colour")]
        public Material highlightMaterial;
    }

    public ColourVariant Green;
    public ColourVariant Purple;
    public ColourVariant Red;
    public ColourVariant Yellow;
    public ColourVariant Default;
}

[RequireComponent(typeof(EnemyBrainController))]
public class EnemyStatsController : EntityStatsController
{
    private EnemyBrainController _brain;
    private NavMeshAgent _agent;

    private float _minTimeBetweenDamageText = 0.5f;
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


    protected override void Awake()
    {
        base.Awake();

        _brain = GetComponent<EnemyBrainController>();
        _agent = GetComponent<NavMeshAgent>();
        _collider = gameObject.GetComponent<Collider>();
    }

    private void Start()
    {
<<<<<<< HEAD
=======
        // Assign enemy a colour
        if (characterColour == CharacterColour.None) AssignEnemyColour();
>>>>>>> 0c286da32416a0161e984e5240a9090002e14e1e

        // Create a VFX where the enemy will spawn - just slightly above the stage (0.1f) - and change the VFX colour to match the enemy colour
        StartCoroutine(VfxHelper.CreateVFX(spawnVFX, transform.position + new Vector3(0, 0.01f, 0),
            Quaternion.identity, PlayerManager.colours.GetColour(characterColour), 0.5f));
        // "Spawn" the enemy (they float up through the stage)
        StartCoroutine(Spawn(gameObject, spawnSpeed, spawnDelay, spawnCooldown));
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
                StartCoroutine(AssignRandomColour());
            }
        }
    }

    public override void TakeDamage(EntityStatsController attacker, float damageValue, float timeDelta = 1f)
    {
        // Ignore attacks if already dead
        if (isDead)
            return;

        if (characterColour != CharacterColour.All && attacker.characterColour != characterColour)
        {
            return;
        }

        // Calculate any changes based on stats and modifiers here first
        float hitValue = Mathf.Max(damageValue - ComputeDefenseModifier(), 0) * timeDelta;
        health.Subtract(hitValue);
        ShowDamage(hitValue);
        Anim.SetTrigger("TakeDamage");

        // Pass damage information to brain
        _brain.OnDamageTaken(attacker.gameObject, hitValue);

        if (Mathf.Approximately(health.CurrentValue, 0f))
        {
            Die();
        }
    }

    private void ShowDamage(float value, float duration = 0.5f)
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

    protected override IEnumerator ApplyExplosiveForce(float explosionForce, Vector3 explosionPoint, float explosionRadius, float stunTime)
    {
        // Set to stunned before applying explosive force
        SetStunned(true);
        rb.isKinematic = false;

        // TODO change this to AddForce(<force vector>, ForceMode.Impulse);
        rb.AddExplosionForce(explosionForce, explosionPoint, explosionRadius);

        // Wait for a moment before re-enabling the navMeshAgent
        yield return new WaitForSeconds(stunTime);
        rb.isKinematic = true;
        SetStunned(false);
    }

    protected override void Die()
    {
        Debug.Log(transform.name + " died.");
        isDead = true;
        _agent.enabled = false;
        StartCoroutine(AudioHelper.PlayAudioOverlap(VocalAudio, entityDeathVocalSFX));
        StartCoroutine(EnemyDeath());
    }


    private IEnumerator EnemyDeath()
    {
        Anim.SetTrigger("Die");
        yield return new WaitForSeconds(5.0f);
        transform.gameObject.SetActive(false);
    }

    private void SetStunned(bool isStunned)
    {
        // Disable the nav and stun the brain
        _agent.enabled = !isStunned;
        _brain.SetStunned(isStunned);
    }

    protected override IEnumerator Spawn(GameObject obj, float speed = 0.05F, float delay = 0, float cooldown = 0)
    {
        // weird stuff happens when the nav mesh is enabled during the spawn
        NavMeshAgent navMesh = obj.GetComponent<NavMeshAgent>();
        navMesh.enabled = false;
        yield return base.Spawn(obj, speed, delay, cooldown);
        navMesh.enabled = true;
    }

    private void AssignEnemyColour(CharacterColour colour)
    {
        characterColour = colour;
        SkinnedMeshRenderer skin = GetComponentInChildren<SkinnedMeshRenderer>();
        EnemyColouring.ColourVariant enemyColouring;
        switch (colour) {
            case CharacterColour.Red:
                enemyColouring = EnemyColouring.Red;
                break;
            case CharacterColour.Yellow:
                enemyColouring = EnemyColouring.Yellow;
                break;
            case CharacterColour.Green:
                enemyColouring = EnemyColouring.Green;
                break;
            case CharacterColour.Purple:
                enemyColouring = EnemyColouring.Purple;
                break;
            default:
                enemyColouring = EnemyColouring.Default;
                break;
        }
        if (!enemyColouring.textureMaterial)
        {
            skin.materials = new Material[] { skin.materials[0], enemyColouring.highlightMaterial };
        }
        else
        {
            skin.materials = new Material[] { enemyColouring.textureMaterial, enemyColouring.highlightMaterial };
        }
    }

    protected IEnumerator AssignRandomColour() {
        // Get a colour that is used by a registered player
        CharacterColour randomColour = PlayerManager.playerColours[Random.Range(0, PlayerManager.playerColours.Count)];
        // keep choosing a random colour until a different one is chosen
        while (randomColour == characterColour) {
            randomColour = PlayerManager.playerColours[Random.Range(0, PlayerManager.playerColours.Count)];
        }
        // Assign the enemy colour
        AssignEnemyColour(randomColour);
        yield return null;
    }
}