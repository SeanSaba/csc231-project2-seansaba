using UnityEngine;

public class ParticleFXManager : MonoBehaviour
{
    public static ParticleFXManager Instance { get; private set; }

    [SerializeField] private ParticleSystem landingDustPS;
    [SerializeField] private ParticleSystem footstepDustPS;
    [SerializeField] private ParticleSystem dashPS;
    [SerializeField] private ParticleSystem sparklePickupPS;
    [SerializeField] private ParticleSystem enemyDefeatedPS;
    [SerializeField] private ParticleSystem deathBurstPS;
    [SerializeField] private ParticleSystem victoryBurstPS;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateFallbackSystemsIfNeeded();
    }

    private void CreateFallbackSystemsIfNeeded()
    {
        if (landingDustPS == null) landingDustPS = CreateParticleSystem("LandingDust", Color.white, 12, 0.4f, 0.25f, 2.5f);
        if (footstepDustPS == null) footstepDustPS = CreateParticleSystem("FootstepDust", new Color(0.85f, 0.85f, 0.85f, 0.6f), 3, 0.25f, 0.15f, 1.2f);
        if (dashPS == null) dashPS = CreateParticleSystem("DashTrail", new Color(0f, 0.9f, 1f, 0.8f), 20, 0.35f, 0.3f, 4f);
        if (sparklePickupPS == null) sparklePickupPS = CreateParticleSystem("SparklePickup", new Color(1f, 0.9f, 0.2f, 1f), 25, 0.6f, 0.25f, 3.5f);
        if (enemyDefeatedPS == null) enemyDefeatedPS = CreateParticleSystem("EnemyDefeat", new Color(1f, 0.5f, 0.1f, 1f), 30, 0.5f, 0.35f, 4.5f);
        if (deathBurstPS == null) deathBurstPS = CreateParticleSystem("DeathBurst", new Color(1f, 0.2f, 0.3f, 1f), 35, 0.7f, 0.4f, 5f);
        if (victoryBurstPS == null) victoryBurstPS = CreateParticleSystem("VictoryBurst", new Color(0.2f, 1f, 0.6f, 1f), 50, 1.2f, 0.5f, 6f);
    }

    private ParticleSystem CreateParticleSystem(string name, Color color, int burstCount, float lifetime, float size, float speed)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSize = size;
        main.startSpeed = speed;
        main.startColor = color;
        main.gravityModifier = 0.5f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, burstCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 10;
        // Uses default particle material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit") ?? Shader.Find("Sprites/Default"));
        renderer.material = mat;

        return ps;
    }

    public void PlayLandingDust(Vector3 position)
    {
        if (landingDustPS != null)
        {
            landingDustPS.transform.position = position;
            landingDustPS.Play();
        }
    }

    public void PlayJumpDust(Vector3 position)
    {
        if (landingDustPS != null)
        {
            landingDustPS.transform.position = position;
            landingDustPS.Play();
        }
    }

    public void EmitFootstepDust(Vector3 position)
    {
        if (footstepDustPS != null)
        {
            footstepDustPS.transform.position = position;
            footstepDustPS.Emit(1);
        }
    }

    public void PlayDashEffect(Vector3 position, int direction)
    {
        if (dashPS != null)
        {
            dashPS.transform.position = position;
            dashPS.Play();
        }
    }

    public void PlaySparklePickup(Vector3 position)
    {
        if (sparklePickupPS != null)
        {
            sparklePickupPS.transform.position = position;
            sparklePickupPS.Play();
        }
    }

    public void PlayEnemyDefeated(Vector3 position)
    {
        if (enemyDefeatedPS != null)
        {
            enemyDefeatedPS.transform.position = position;
            enemyDefeatedPS.Play();
        }
    }

    public void PlayDeathBurst(Vector3 position)
    {
        if (deathBurstPS != null)
        {
            deathBurstPS.transform.position = position;
            deathBurstPS.Play();
        }
    }

    public void PlayVictoryBurst(Vector3 position)
    {
        if (victoryBurstPS != null)
        {
            victoryBurstPS.transform.position = position;
            victoryBurstPS.Play();
        }
    }
}
