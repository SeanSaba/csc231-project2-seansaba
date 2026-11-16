using UnityEngine;

public class ParticleFXManager : MonoBehaviour
{
    public static ParticleFXManager Instance { get; private set; }

    [Header("Particle Textures")]
    [SerializeField] private Texture2D dustTexture;
    [SerializeField] private Texture2D sparkleTexture;

    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem landingDustPS;
    [SerializeField] private ParticleSystem footstepDustPS;
    [SerializeField] private ParticleSystem dashPS;
    [SerializeField] private ParticleSystem sparklePickupPS;
    [SerializeField] private ParticleSystem enemyDefeatedPS;
    [SerializeField] private ParticleSystem deathBurstPS;
    [SerializeField] private ParticleSystem victoryBurstPS;

    private Material dustMat;
    private Material sparkleMat;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateMaterials();
        CreateFallbackSystemsIfNeeded();
    }

    private void CreateMaterials()
    {
        Shader particleShader = Shader.Find("Sprites/Default");
        if (particleShader == null) particleShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");

        dustMat = new Material(particleShader);
        if (dustTexture != null) dustMat.mainTexture = dustTexture;

        sparkleMat = new Material(particleShader);
        if (sparkleTexture != null) sparkleMat.mainTexture = sparkleTexture;
    }

    private void CreateFallbackSystemsIfNeeded()
    {
        if (landingDustPS == null)
        {
            landingDustPS = CreateParticleSystem("LandingDust", new Color(0.85f, 0.92f, 0.95f, 0.85f), 14, 0.42f, 0.32f, 2.8f, 0.25f, dustMat);
        }
        if (footstepDustPS == null)
        {
            footstepDustPS = CreateParticleSystem("FootstepDust", new Color(0.80f, 0.88f, 0.90f, 0.60f), 2, 0.28f, 0.20f, 1.3f, 0.1f, dustMat);
        }
        if (dashPS == null)
        {
            dashPS = CreateParticleSystem("DashTrail", new Color(0.1f, 0.95f, 1f, 0.95f), 18, 0.36f, 0.42f, 3.8f, 0f, sparkleMat);
        }
        if (sparklePickupPS == null)
        {
            sparklePickupPS = CreateParticleSystem("SparklePickup", new Color(0.3f, 1f, 1f, 1f), 26, 0.65f, 0.36f, 3.8f, 0.15f, sparkleMat);
        }
        if (enemyDefeatedPS == null)
        {
            enemyDefeatedPS = CreateParticleSystem("EnemyDefeat", new Color(1f, 0.55f, 0.15f, 1f), 30, 0.55f, 0.45f, 4.8f, 0.6f, sparkleMat);
        }
        if (deathBurstPS == null)
        {
            deathBurstPS = CreateParticleSystem("DeathBurst", new Color(1f, 0.22f, 0.32f, 1f), 36, 0.75f, 0.48f, 5.2f, 0.4f, sparkleMat);
        }
        if (victoryBurstPS == null)
        {
            victoryBurstPS = CreateParticleSystem("VictoryBurst", new Color(1f, 0.88f, 0.25f, 1f), 55, 1.35f, 0.58f, 6.2f, 0.35f, sparkleMat);
        }
    }

    private ParticleSystem CreateParticleSystem(string psName, Color color, int burstCount, float lifetime, float size, float speed, float gravity, Material mat)
    {
        GameObject go = new GameObject(psName);
        go.transform.SetParent(transform);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.65f, size);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.22f;

        var colOverLife = ps.colorOverLifetime;
        colOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0.8f),
            new Keyframe(0.25f, 1.0f),
            new Keyframe(1f, 0f)
        );
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 15;
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

    public void EmitDashTrail(Vector3 position)
    {
        if (dashPS != null)
        {
            dashPS.transform.position = position;
            dashPS.Emit(3);
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
