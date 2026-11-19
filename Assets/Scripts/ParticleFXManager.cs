using UnityEngine;

/// <summary>
/// Full-featured Unity Particle System manager showcasing ColorOverLifetime, SizeOverLifetime,
/// VelocityOverLifetime (orbital & directional), Noise turbulence, Trails, RotationOverLifetime,
/// and multiple emitter shapes (Cone, Edge, Circle, Donut).
/// </summary>
public class ParticleFXManager : MonoBehaviour
{
    public static ParticleFXManager Instance { get; private set; }

    [Header("Particle Textures")]
    [SerializeField] private Texture2D dustTexture;
    [SerializeField] private Texture2D sparkleTexture;

    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem landingDustPS;
    [SerializeField] private ParticleSystem jumpThrusterPS;
    [SerializeField] private ParticleSystem footstepDustPS;
    [SerializeField] private ParticleSystem steamPuffPS;
    [SerializeField] private ParticleSystem dashPS;
    [SerializeField] private ParticleSystem sparklePickupPS;
    [SerializeField] private ParticleSystem enemyDefeatedPS;
    [SerializeField] private ParticleSystem deathBurstPS;
    [SerializeField] private ParticleSystem victoryBurstPS;
    [SerializeField] private ParticleSystem spikeTelegraphPS;
    [SerializeField] private ParticleSystem spikeEruptPS;
    [SerializeField] private ParticleSystem rubbleBurstPS;
    [SerializeField] private ParticleSystem acidSplashPS;

    private Material dustMat;
    private Material sparkleMat;
    private Material trailMat;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateMaterials();
        CreateAllParticleSystems();
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

        trailMat = new Material(particleShader);
    }

    private void CreateAllParticleSystems()
    {
        // 1. Wide Horizontal Edge Landing Dust Plume
        if (landingDustPS == null)
        {
            landingDustPS = CreateBasePS("LandingDust", new Color(0.84f, 0.94f, 0.96f, 0.90f),
                new Color(0.55f, 0.72f, 0.78f, 0f), 18, 0.45f, 0.34f, 3.2f, 0.35f, dustMat, 15);
            var shape = landingDustPS.shape;
            shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
            shape.radius = 0.55f;
            var vel = landingDustPS.velocityOverLifetime;
            vel.enabled = true;
            vel.radial = new ParticleSystem.MinMaxCurve(1.6f);
            vel.y = new ParticleSystem.MinMaxCurve(0.2f, 0.9f);
        }

        // 2. Downward Steam-Jet Jump Launch Burst (Cone Shape + Warm/Cyan Core)
        if (jumpThrusterPS == null)
        {
            jumpThrusterPS = CreateBasePS("JumpThruster", new Color(0.35f, 0.98f, 1f, 0.95f),
                new Color(0.95f, 0.95f, 1f, 0f), 16, 0.36f, 0.30f, 3.8f, 0.1f, sparkleMat, 14);
            var shape = jumpThrusterPS.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 32f;
            shape.radius = 0.20f;
            shape.rotation = new Vector3(90f, 0f, 0f); // spray downward
            AddNoise(jumpThrusterPS, 0.6f, 1.8f);
        }

        // 3. Running Footstep Dust
        if (footstepDustPS == null)
        {
            footstepDustPS = CreateBasePS("FootstepDust", new Color(0.78f, 0.90f, 0.90f, 0.68f),
                new Color(0.55f, 0.68f, 0.70f, 0f), 3, 0.32f, 0.22f, 1.4f, 0.08f, dustMat, 12);
            AddNoise(footstepDustPS, 0.4f, 1.5f);
        }

        // 4. Kip Brass Steam-Backpack Idle/Run Puffs
        if (steamPuffPS == null)
        {
            steamPuffPS = CreateBasePS("SteamPackPuff", new Color(0.95f, 0.98f, 1f, 0.55f),
                new Color(0.65f, 0.85f, 0.92f, 0f), 2, 0.55f, 0.24f, 0.9f, -0.35f, dustMat, 11);
            AddNoise(steamPuffPS, 0.7f, 1.4f);
        }

        // 5. Air Dash Energy Ribbon & Spark Trail (with Unity Trails Module!)
        if (dashPS == null)
        {
            dashPS = CreateBasePS("DashTrail", new Color(0.15f, 0.98f, 1f, 1f),
                new Color(0.05f, 0.45f, 1f, 0f), 22, 0.38f, 0.38f, 4.5f, 0f, sparkleMat, 16);
            AddTrails(dashPS, 0.55f, 0.22f, new Color(0.2f, 0.98f, 1f, 0.85f), new Color(0.1f, 0.55f, 1f, 0f));
            AddNoise(dashPS, 0.9f, 2.5f);
        }

        // 6. Aether Crystal Pickup Spiral (Orbital Velocity!)
        if (sparklePickupPS == null)
        {
            sparklePickupPS = CreateBasePS("SparklePickup", new Color(0.45f, 1f, 1f, 1f),
                new Color(1f, 0.92f, 0.45f, 0f), 30, 0.75f, 0.38f, 3.6f, 0.05f, sparkleMat, 17);
            var vel = sparklePickupPS.velocityOverLifetime;
            vel.enabled = true;
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(4.5f);
            AddTrails(sparklePickupPS, 0.4f, 0.16f, new Color(0.4f, 1f, 1f, 0.8f), new Color(1f, 0.9f, 0.4f, 0f));
        }

        // 7. Enemy Defeated Clockwork Explosion (Trails + Gravity + Rotation!)
        if (enemyDefeatedPS == null)
        {
            enemyDefeatedPS = CreateBasePS("EnemyDefeat", new Color(1f, 0.72f, 0.18f, 1f),
                new Color(1f, 0.22f, 0.08f, 0f), 36, 0.65f, 0.44f, 5.8f, 0.95f, sparkleMat, 17);
            AddTrails(enemyDefeatedPS, 0.55f, 0.20f, new Color(1f, 0.7f, 0.2f, 0.9f), new Color(0.9f, 0.15f, 0.05f, 0f));
            AddRotation(enemyDefeatedPS, 280f);
            AddNoise(enemyDefeatedPS, 0.8f, 2.0f);
        }

        // 8. Player Death Shatter Burst
        if (deathBurstPS == null)
        {
            deathBurstPS = CreateBasePS("DeathBurst", new Color(1f, 0.25f, 0.38f, 1f),
                new Color(0.25f, 0.95f, 1f, 0f), 42, 0.80f, 0.48f, 5.6f, 0.55f, sparkleMat, 18);
            AddTrails(deathBurstPS, 0.5f, 0.22f, new Color(1f, 0.3f, 0.4f, 0.85f), new Color(0.2f, 0.9f, 1f, 0f));
            AddNoise(deathBurstPS, 1.1f, 2.2f);
        }

        // 9. Victory Grand Fireworks (Trails + Orbital + Golden-Cyan-Magenta Burst)
        if (victoryBurstPS == null)
        {
            victoryBurstPS = CreateBasePS("VictoryBurst", new Color(1f, 0.90f, 0.25f, 1f),
                new Color(0.2f, 1f, 0.95f, 0f), 75, 1.55f, 0.54f, 7.2f, 0.45f, sparkleMat, 19);
            AddTrails(victoryBurstPS, 0.75f, 0.28f, new Color(1f, 0.92f, 0.3f, 0.95f), new Color(0.2f, 1f, 0.9f, 0f));
            AddNoise(victoryBurstPS, 0.9f, 1.8f);
        }

        // 10. Pop-Up Spike Electric Telegraph Sparks
        if (spikeTelegraphPS == null)
        {
            spikeTelegraphPS = CreateBasePS("SpikeTelegraph", new Color(0.55f, 0.95f, 1f, 1f),
                new Color(0.2f, 0.55f, 1f, 0f), 14, 0.26f, 0.26f, 2.6f, 0.3f, sparkleMat, 16);
            var shape = spikeTelegraphPS.shape;
            shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
            shape.radius = 0.75f;
            AddNoise(spikeTelegraphPS, 1.6f, 5.0f);
        }

        // 11. Pop-Up Spike Eruption Vertical Cone Burst
        if (spikeEruptPS == null)
        {
            spikeEruptPS = CreateBasePS("SpikeErupt", new Color(0.85f, 0.98f, 1f, 1f),
                new Color(0.15f, 0.78f, 1f, 0f), 26, 0.42f, 0.34f, 5.4f, 1.1f, sparkleMat, 16);
            var shape = spikeEruptPS.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 26f;
            shape.radius = 0.65f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // erupt straight upward
            AddTrails(spikeEruptPS, 0.4f, 0.14f, new Color(0.75f, 0.98f, 1f, 0.9f), new Color(0.15f, 0.65f, 1f, 0f));
        }

        // 12. Crumbling Platform & Stalactite Rubble Dust
        if (rubbleBurstPS == null)
        {
            rubbleBurstPS = CreateBasePS("RubbleBurst", new Color(0.68f, 0.66f, 0.74f, 0.95f),
                new Color(0.32f, 0.36f, 0.42f, 0f), 24, 0.68f, 0.36f, 4.2f, 1.45f, dustMat, 15);
            var shape = rubbleBurstPS.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.8f, 0.4f, 0.1f);
            AddRotation(rubbleBurstPS, 220f);
        }

        // 13. Acid Water Splash Droplets
        if (acidSplashPS == null)
        {
            acidSplashPS = CreateBasePS("AcidSplash", new Color(0.25f, 1f, 0.88f, 1f),
                new Color(0.05f, 0.75f, 0.65f, 0f), 24, 0.58f, 0.32f, 4.8f, 1.35f, sparkleMat, 16);
            var shape = acidSplashPS.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 38f;
            shape.radius = 0.45f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            AddNoise(acidSplashPS, 0.6f, 2.2f);
        }
    }

    private ParticleSystem CreateBasePS(string psName, Color startCol, Color endCol, int burstCount,
        float lifetime, float size, float speed, float gravity, Material mat, int sortingOrder)
    {
        GameObject go = new GameObject(psName);
        go.transform.SetParent(transform);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.65f, lifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
        main.startColor = startCol;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 250;

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
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(startCol, 0.25f),
                new GradientColorKey(endCol, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0.7f),
            new Keyframe(0.2f, 1.08f),
            new Keyframe(1f, 0f)
        );
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.material = mat;
        renderer.trailMaterial = trailMat;

        return ps;
    }

    private void AddTrails(ParticleSystem ps, float ratio, float lifetime, Color startCol, Color endCol)
    {
        var trails = ps.trails;
        trails.enabled = true;
        trails.ratio = ratio;
        trails.lifetime = new ParticleSystem.MinMaxCurve(lifetime);
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.45f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        trails.inheritParticleColor = false;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(startCol, 0f), new GradientColorKey(endCol, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(startCol.a, 0f), new GradientAlphaKey(0f, 1f) }
        );
        trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(g);
    }

    private void AddNoise(ParticleSystem ps, float strength, float frequency)
    {
        var noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = false;
        noise.strength = strength;
        noise.frequency = frequency;
        noise.scrollSpeed = 0.8f;
        noise.quality = ParticleSystemNoiseQuality.Medium;
    }

    private void AddRotation(ParticleSystem ps, float angularSpeed)
    {
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-angularSpeed * Mathf.Deg2Rad, angularSpeed * Mathf.Deg2Rad);
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
        if (jumpThrusterPS != null)
        {
            jumpThrusterPS.transform.position = position + Vector3.up * 0.25f;
            jumpThrusterPS.Play();
        }
    }

    public void EmitFootstepDust(Vector3 position)
    {
        if (footstepDustPS != null)
        {
            footstepDustPS.transform.position = position;
            footstepDustPS.Emit(2);
        }
    }

    public void EmitSteamPackPuff(Vector3 position)
    {
        if (steamPuffPS != null)
        {
            steamPuffPS.transform.position = position;
            steamPuffPS.Emit(1);
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
            dashPS.Emit(4);
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
        if (rubbleBurstPS != null)
        {
            rubbleBurstPS.transform.position = position;
            rubbleBurstPS.Emit(10);
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

    public void PlaySpikeTelegraph(Vector3 position)
    {
        if (spikeTelegraphPS != null)
        {
            spikeTelegraphPS.transform.position = position;
            spikeTelegraphPS.Play();
        }
    }

    public void PlaySpikeErupt(Vector3 position)
    {
        if (spikeEruptPS != null)
        {
            spikeEruptPS.transform.position = position;
            spikeEruptPS.Play();
        }
    }

    public void EmitRubbleDust(Vector3 position)
    {
        if (rubbleBurstPS != null)
        {
            rubbleBurstPS.transform.position = position;
            rubbleBurstPS.Emit(4);
        }
    }

    public void PlayPlatformCollapse(Vector3 position)
    {
        if (rubbleBurstPS != null)
        {
            rubbleBurstPS.transform.position = position;
            rubbleBurstPS.Play();
        }
    }

    public void PlayAcidSplash(Vector3 position)
    {
        if (acidSplashPS != null)
        {
            acidSplashPS.transform.position = position;
            acidSplashPS.Play();
        }
    }

    public void PlayStalactiteShatter(Vector3 position)
    {
        if (rubbleBurstPS != null)
        {
            rubbleBurstPS.transform.position = position;
            rubbleBurstPS.Play();
        }
        if (acidSplashPS != null)
        {
            acidSplashPS.transform.position = position;
            acidSplashPS.Play();
        }
    }
}
