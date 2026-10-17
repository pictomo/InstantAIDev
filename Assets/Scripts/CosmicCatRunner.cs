using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the complete, asset-free Cosmic Cat game when a scene is played.
/// </summary>
public sealed class CosmicCatRunner : MonoBehaviour
{
    private const string BestScoreKey = "CosmicCatRunner.BestScore";
    private const float PlatformWidth = 4.2f;
    private const float PlatformLength = 4.8f;

    private Transform worldRoot;
    private Transform environmentRoot;
    private Camera gameCamera;
    private CosmicCatPlayer player;
    private readonly List<CosmicPlatform> platforms = new List<CosmicPlatform>();
    private readonly List<CosmicStar> starPickups = new List<CosmicStar>();
    private Material platformMaterial;
    private Material edgeCyanMaterial;
    private Material edgePinkMaterial;
    private Material catFurMaterial;
    private Material catLightMaterial;
    private Material catAccentMaterial;
    private Material catDarkMaterial;
    private Material starMaterial;
    private Material starHaloMaterial;
    private Material planetMaterial;
    private Material planetRingMaterial;
    private Material backgroundStarMaterial;
    private Material rainbowTrailMaterial;
    private Mesh catEarMesh;
    private Mesh starMesh;
    private Mesh orbitRingMesh;
    private bool gameOver;
    private float bestReachZ;
    private float generatedThroughZ;
    private int stars;
    private int bestScore;
    private float scoreScale;
    private GUIStyle cardStyle;
    private GUIStyle buttonStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle smallStyle;
    private GUIStyle overlayStyle;
    private GUIStyle gameOverHeadingStyle;
    private Texture2D cardTexture;
    private Texture2D buttonTexture;
    private Texture2D overlayTexture;
    private Vector3 previousGravity;
    private System.Random runRandom = new System.Random(4162);

    public bool IsGameOver { get { return gameOver; } }
    public int StarCount { get { return stars; } }
    public Transform WorldRoot { get { return worldRoot; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartGameInAnyScene()
    {
        if (!Application.isPlaying || FindObjectOfType<CosmicCatRunner>() != null)
            return;

        new GameObject("COSMIC CAT | STAR RUN").AddComponent<CosmicCatRunner>();
    }

    private void Awake()
    {
        previousGravity = Physics.gravity;
        Physics.gravity = new Vector3(0f, -22f, 0f);
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        CreateMaterials();
        PrepareCameraAndLighting();
        BuildEnvironment();
        BeginRun();
    }

    private void OnDestroy()
    {
        Physics.gravity = previousGravity;
        if (worldRoot != null)
            Destroy(worldRoot.gameObject);
        if (environmentRoot != null)
            Destroy(environmentRoot.gameObject);
        DestroyRuntimeObject(platformMaterial);
        DestroyRuntimeObject(edgeCyanMaterial);
        DestroyRuntimeObject(edgePinkMaterial);
        DestroyRuntimeObject(catFurMaterial);
        DestroyRuntimeObject(catLightMaterial);
        DestroyRuntimeObject(catAccentMaterial);
        DestroyRuntimeObject(catDarkMaterial);
        DestroyRuntimeObject(starMaterial);
        DestroyRuntimeObject(starHaloMaterial);
        DestroyRuntimeObject(planetMaterial);
        DestroyRuntimeObject(planetRingMaterial);
        DestroyRuntimeObject(backgroundStarMaterial);
        DestroyRuntimeObject(rainbowTrailMaterial);
        DestroyRuntimeObject(catEarMesh);
        DestroyRuntimeObject(starMesh);
        DestroyRuntimeObject(orbitRingMesh);
        DestroyRuntimeObject(cardTexture);
        DestroyRuntimeObject(buttonTexture);
        DestroyRuntimeObject(overlayTexture);
    }

    private static void DestroyRuntimeObject(Object runtimeObject)
    {
        if (runtimeObject != null)
            Destroy(runtimeObject);
    }

    private void Update()
    {
        if (player == null)
            return;

        if (gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space))
                BeginRun();
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            BeginRun();
            return;
        }

        bestReachZ = Mathf.Max(bestReachZ, player.transform.position.z);
        if (player.transform.position.y < -8f)
            EndRun();

        GenerateAhead();
        CleanupBehind();
    }

    private void LateUpdate()
    {
        if (gameCamera == null || player == null)
            return;

        Vector3 target = player.transform.position;
        Vector3 desiredPosition = target + new Vector3(target.x * 0.22f, 5.6f, -9.7f);
        gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, desiredPosition, Time.deltaTime * 5.2f);
        Vector3 lookTarget = target + new Vector3(0f, 0.8f, 5.6f);
        Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - gameCamera.transform.position, Vector3.up);
        gameCamera.transform.rotation = Quaternion.Slerp(gameCamera.transform.rotation, desiredRotation, Time.deltaTime * 5.2f);
    }

    private void CreateMaterials()
    {
        platformMaterial = MakeMaterial(new Color(0.035f, 0.075f, 0.19f), new Color(0.06f, 0.28f, 0.78f), 0.35f, 0.7f);
        edgeCyanMaterial = MakeMaterial(new Color(0.16f, 0.95f, 1f), new Color(0.08f, 0.95f, 1f), 1f, 0.2f);
        edgePinkMaterial = MakeMaterial(new Color(1f, 0.23f, 0.76f), new Color(1f, 0.08f, 0.57f), 0.9f, 0.15f);
        catFurMaterial = MakeMaterial(new Color(0.72f, 0.62f, 1f), new Color(0.24f, 0.09f, 0.52f), 0.1f, 0.55f);
        catLightMaterial = MakeMaterial(new Color(0.94f, 0.9f, 1f), new Color(0.4f, 0.24f, 0.75f), 0.08f, 0.45f);
        catAccentMaterial = MakeMaterial(new Color(1f, 0.32f, 0.72f), new Color(1f, 0.07f, 0.36f), 0.1f, 0.55f);
        catDarkMaterial = MakeMaterial(new Color(0.035f, 0.045f, 0.11f), new Color(0.015f, 0.03f, 0.1f), 0f, 0.2f);
        starMaterial = MakeMaterial(new Color(1f, 0.82f, 0.2f), new Color(1f, 0.48f, 0.035f), 0.25f, 0.75f);
        starHaloMaterial = MakeMaterial(new Color(1f, 0.75f, 0.18f), new Color(1f, 0.52f, 0.04f), 0f, 1f);
        planetMaterial = MakeMaterial(new Color(0.13f, 0.08f, 0.38f), new Color(0.25f, 0.07f, 0.8f), 0.35f, 0.8f);
        planetRingMaterial = MakeMaterial(new Color(0.12f, 0.82f, 1f), new Color(0.08f, 0.58f, 1f), 0.55f, 0.8f);
        backgroundStarMaterial = MakeMaterial(Color.white, new Color(0.35f, 0.65f, 1f), 0f, 0.65f);
        rainbowTrailMaterial = new Material(Shader.Find("Sprites/Default"));
        catEarMesh = CreateEarMesh();
        starMesh = CreateStarMesh();
        orbitRingMesh = CreateTorusMesh(1.48f, 1.61f, 72, 8);
    }

    private static Material MakeMaterial(Color color, Color emission, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emission);
        return material;
    }

    private void PrepareCameraAndLighting()
    {
        gameCamera = Camera.main;
        if (gameCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            gameCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(0.012f, 0.018f, 0.065f);
        gameCamera.fieldOfView = 57f;
        gameCamera.nearClipPlane = 0.08f;
        gameCamera.farClipPlane = 500f;
        gameCamera.allowHDR = true;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.23f, 0.42f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.012f, 0.018f, 0.065f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.002f;

        Light[] sceneLights = FindObjectsOfType<Light>();
        if (sceneLights.Length == 0)
        {
            GameObject lightObject = new GameObject("Soft Space Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.64f, 0.72f, 1f);
            light.intensity = 0.8f;
            light.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
        }
        else
        {
            sceneLights[0].color = new Color(0.72f, 0.76f, 1f);
            sceneLights[0].intensity = 0.9f;
        }
    }

    private void BuildEnvironment()
    {
        environmentRoot = new GameObject("Faraway Stars and Planets").transform;
        environmentRoot.SetParent(gameCamera.transform, false);

        GameObject starsObject = new GameObject("Procedural Starfield");
        starsObject.transform.SetParent(environmentRoot, false);
        ParticleSystem particles = starsObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = 10000f;
        main.startSpeed = 0f;
        main.startSize = 0.06f;
        main.maxParticles = 220;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;
        ParticleSystemRenderer renderer = starsObject.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = backgroundStarMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Random.State oldRandom = Random.state;
        Random.InitState(4162);
        for (int i = 0; i < 190; i++)
        {
            ParticleSystem.EmitParams particle = new ParticleSystem.EmitParams();
            particle.position = new Vector3(Random.Range(-48f, 48f), Random.Range(-28f, 28f), Random.Range(25f, 150f));
            particle.startSize = Random.Range(0.025f, i % 13 == 0 ? 0.12f : 0.07f);
            particle.startColor = i % 9 == 0 ? new Color(0.66f, 0.82f, 1f) : Color.white;
            particles.Emit(particle, 1);
        }
        Random.state = oldRandom;
        particles.Play();

        CreatePlanet(new Vector3(-25f, 12f, 82f), 8.2f, 0.86f);
        CreatePlanet(new Vector3(33f, -11f, 128f), 11.5f, 1.35f);
    }

    private void CreatePlanet(Vector3 localPosition, float size, float ringScale)
    {
        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = "Faraway Neon Planet";
        planet.transform.SetParent(environmentRoot, false);
        planet.transform.localPosition = localPosition;
        planet.transform.localScale = Vector3.one * size;
        planet.GetComponent<Renderer>().sharedMaterial = planetMaterial;
        Destroy(planet.GetComponent<Collider>());

        GameObject ring = new GameObject("Planet Orbit");
        ring.transform.SetParent(planet.transform, false);
        ring.transform.localRotation = Quaternion.Euler(68f, 8f, 18f);
        ring.transform.localScale = new Vector3(1f, 0.8f, 1f) * ringScale;
        MeshFilter filter = ring.AddComponent<MeshFilter>();
        filter.sharedMesh = orbitRingMesh;
        ring.AddComponent<MeshRenderer>().sharedMaterial = planetRingMaterial;
    }

    private static Mesh CreateTorusMesh(float innerRadius, float outerRadius, int segments, int sides)
    {
        Mesh mesh = new Mesh { name = "Procedural Orbit Ring" };
        Vector3[] vertices = new Vector3[segments * sides];
        int[] triangles = new int[segments * sides * 6];
        float centerRadius = (innerRadius + outerRadius) * 0.5f;
        float tubeRadius = (outerRadius - innerRadius) * 0.5f;
        int t = 0;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            for (int j = 0; j < sides; j++)
            {
                float b = j * Mathf.PI * 2f / sides;
                float r = centerRadius + Mathf.Cos(b) * tubeRadius;
                vertices[i * sides + j] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(b) * tubeRadius, Mathf.Sin(a) * r);
                int nextI = (i + 1) % segments;
                int nextJ = (j + 1) % sides;
                int a0 = i * sides + j;
                int a1 = nextI * sides + j;
                int a2 = nextI * sides + nextJ;
                int a3 = i * sides + nextJ;
                triangles[t++] = a0; triangles[t++] = a1; triangles[t++] = a2;
                triangles[t++] = a0; triangles[t++] = a2; triangles[t++] = a3;
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private void BeginRun()
    {
        if (worldRoot != null)
            Destroy(worldRoot.gameObject);

        runRandom = new System.Random(4162);
        worldRoot = new GameObject("Generated Runner Course").transform;
        platforms.Clear();
        starPickups.Clear();
        stars = 0;
        bestReachZ = 0f;
        generatedThroughZ = 0f;
        gameOver = false;

        CreatePlatform(Vector3.zero, 5.2f, 6.4f, true, false);
        player = CreatePlayer(new Vector3(0f, 0f, -1.45f));
        bestReachZ = player.transform.position.z;
        generatedThroughZ = 0f;
        GenerateAhead();

        if (gameCamera != null)
        {
            gameCamera.transform.position = player.transform.position + new Vector3(0f, 5.6f, -9.7f);
            gameCamera.transform.LookAt(player.transform.position + new Vector3(0f, 0.8f, 5.6f));
        }
    }

    private CosmicCatPlayer CreatePlayer(Vector3 position)
    {
        GameObject cat = new GameObject("Mochi the Space Cat");
        cat.tag = "Player";
        cat.transform.SetParent(worldRoot, false);
        cat.transform.position = position;
        CapsuleCollider capsule = cat.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.51f, 0f);
        capsule.radius = 0.36f;
        capsule.height = 1.06f;
        Rigidbody body = cat.AddComponent<Rigidbody>();
        body.mass = 1.25f;
        body.drag = 0f;
        body.angularDrag = 0.05f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        CosmicCatPlayer controller = cat.AddComponent<CosmicCatPlayer>();
        controller.Initialize(this, body);
        BuildCatVisual(cat.transform, controller);
        return controller;
    }

    private void BuildCatVisual(Transform cat, CosmicCatPlayer controller)
    {
        Transform visual = new GameObject("Cat Shape").transform;
        visual.SetParent(cat, false);
        controller.VisualRoot = visual;

        MakePrimitive(PrimitiveType.Sphere, "Plush Body", visual, new Vector3(0f, 0.51f, -0.08f), new Vector3(0.7f, 0.53f, 0.84f), catFurMaterial);
        MakePrimitive(PrimitiveType.Sphere, "Cream Chest", visual, new Vector3(0f, 0.38f, 0.31f), new Vector3(0.54f, 0.37f, 0.4f), catLightMaterial);
        MakePrimitive(PrimitiveType.Sphere, "Round Head", visual, new Vector3(0f, 1.12f, 0.37f), new Vector3(0.91f, 0.77f, 0.82f), catLightMaterial);

        CreateMeshPart("Left Ear", visual, catEarMesh, catFurMaterial, new Vector3(-0.31f, 1.57f, 0.32f), Quaternion.Euler(0f, 0f, 10f), Vector3.one);
        CreateMeshPart("Right Ear", visual, catEarMesh, catFurMaterial, new Vector3(0.31f, 1.57f, 0.32f), Quaternion.Euler(0f, 0f, -10f), new Vector3(-1f, 1f, 1f));
        CreateMeshPart("Left Inner Ear", visual, catEarMesh, catAccentMaterial, new Vector3(-0.31f, 1.62f, 0.45f), Quaternion.identity, new Vector3(0.53f, 0.58f, 0.45f));
        CreateMeshPart("Right Inner Ear", visual, catEarMesh, catAccentMaterial, new Vector3(0.31f, 1.62f, 0.45f), Quaternion.identity, new Vector3(-0.53f, 0.58f, 0.45f));

        for (int side = -1; side <= 1; side += 2)
        {
            MakePrimitive(PrimitiveType.Sphere, "Eye Shine", visual, new Vector3(side * 0.24f, 1.19f, 0.76f), new Vector3(0.19f, 0.24f, 0.12f), catDarkMaterial);
            MakePrimitive(PrimitiveType.Sphere, "Starry Pupil", visual, new Vector3(side * 0.24f, 1.2f, 0.865f), new Vector3(0.105f, 0.15f, 0.055f), edgeCyanMaterial);
            MakePrimitive(PrimitiveType.Sphere, "Eye Glint", visual, new Vector3(side * 0.24f - 0.03f, 1.26f, 0.91f), new Vector3(0.045f, 0.055f, 0.025f), catLightMaterial);
            MakePrimitive(PrimitiveType.Sphere, "Soft Paw", visual, new Vector3(side * 0.28f, 0.15f, 0.34f), new Vector3(0.24f, 0.18f, 0.28f), catFurMaterial);
            MakePrimitive(PrimitiveType.Sphere, "Back Paw", visual, new Vector3(side * 0.28f, 0.15f, -0.42f), new Vector3(0.24f, 0.18f, 0.26f), catFurMaterial);
            MakePrimitive(PrimitiveType.Sphere, "Whisker Dot", visual, new Vector3(side * 0.17f, 0.93f, 0.78f), new Vector3(0.07f, 0.055f, 0.04f), catAccentMaterial);
            MakePrimitive(PrimitiveType.Cube, "Whisker", visual, new Vector3(side * 0.51f, 0.94f, 0.73f), new Vector3(0.42f, 0.018f, 0.018f), catLightMaterial).transform.localRotation = Quaternion.Euler(0f, side * 6f, side * -4f);
        }

        MakePrimitive(PrimitiveType.Sphere, "Little Nose", visual, new Vector3(0f, 1.01f, 0.89f), new Vector3(0.12f, 0.085f, 0.07f), catAccentMaterial);
        MakePrimitive(PrimitiveType.Cube, "Nose Smile", visual, new Vector3(0f, 0.91f, 0.875f), new Vector3(0.025f, 0.11f, 0.025f), catFurMaterial);

        Transform tail = new GameObject("Fluffy Tail").transform;
        tail.SetParent(visual, false);
        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            MakePrimitive(PrimitiveType.Sphere, "Tail Puff", tail, Vector3.zero, Vector3.one * Mathf.Lerp(0.23f, 0.15f, t), i > 3 ? catAccentMaterial : catFurMaterial);
        }
        controller.Tail = tail;

        GameObject trailObject = new GameObject("Rainbow Star Trail");
        trailObject.transform.SetParent(cat, false);
        trailObject.transform.localPosition = new Vector3(0f, 0.66f, -0.92f);
        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = 1.5f;
        trail.minVertexDistance = 0.08f;
        trail.startWidth = 0.42f;
        trail.endWidth = 0.015f;
        trail.numCapVertices = 5;
        trail.numCornerVertices = 4;
        trail.sharedMaterial = rainbowTrailMaterial;
        trail.colorGradient = CreateRainbowGradient();
        trail.emitting = false;
        controller.RainbowTrail = trail;
    }

    private static Mesh CreateEarMesh()
    {
        Mesh mesh = new Mesh { name = "Soft Triangle Cat Ear" };
        mesh.vertices = new[]
        {
            new Vector3(-0.26f, 0f, 0f), new Vector3(0.26f, 0f, 0f), new Vector3(0f, 0.55f, 0f),
            new Vector3(0f, 0.15f, 0.16f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 3, 1, 1, 3, 2, 2, 3, 0 };
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Gradient CreateRainbowGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.08f, 0.28f), 0f),
                new GradientColorKey(new Color(1f, 0.48f, 0.08f), 0.18f),
                new GradientColorKey(new Color(1f, 0.95f, 0.18f), 0.36f),
                new GradientColorKey(new Color(0.15f, 1f, 0.52f), 0.54f),
                new GradientColorKey(new Color(0.12f, 0.72f, 1f), 0.72f),
                new GradientColorKey(new Color(0.66f, 0.25f, 1f), 0.9f),
                new GradientColorKey(new Color(1f, 0.25f, 0.74f), 1f)
            },
            new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.85f, 0.65f), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }

    private static GameObject MakePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        return part;
    }

    private static GameObject CreateMeshPart(string name, Transform parent, Mesh mesh, Material material, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
        return part;
    }

    private void GenerateAhead()
    {
        if (player == null)
            return;

        while (generatedThroughZ < player.transform.position.z + 58f)
        {
            float nextZ = generatedThroughZ == 0f
                ? 7.25f
                : generatedThroughZ + NextRange(7.0f, 7.85f);
            float previousX = platforms.Count == 0 ? 0f : platforms[platforms.Count - 1].transform.position.x;
            float nextX = Mathf.Clamp(previousX + NextRange(-1.85f, 1.85f), -3.2f, 3.2f);
            CreatePlatform(new Vector3(nextX, 0f, nextZ), PlatformWidth, PlatformLength, false, true);
            generatedThroughZ = nextZ;
        }
    }

    private float NextRange(float min, float max)
    {
        return min + (float)runRandom.NextDouble() * (max - min);
    }

    private CosmicPlatform CreatePlatform(Vector3 position, float width, float length, bool starting, bool placeStar)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = starting ? "Starting Starlight Platform" : "Collapsing Neon Platform";
        platform.transform.SetParent(worldRoot, false);
        platform.transform.position = position + Vector3.down * 0.18f;
        platform.transform.localScale = new Vector3(width, 0.36f, length);
        platform.GetComponent<Renderer>().sharedMaterial = platformMaterial;

        float bar = 0.1f;
        MakePrimitive(PrimitiveType.Cube, "Cyan Edge", platform.transform, new Vector3(-0.5f + bar / (2f * width), 0.56f, 0f), new Vector3(bar / width, 0.22f, 1f), edgeCyanMaterial);
        MakePrimitive(PrimitiveType.Cube, "Pink Edge", platform.transform, new Vector3(0.5f - bar / (2f * width), 0.56f, 0f), new Vector3(bar / width, 0.22f, 1f), edgePinkMaterial);
        MakePrimitive(PrimitiveType.Cube, "Far Edge", platform.transform, new Vector3(0f, 0.56f, 0.5f - bar / (2f * length)), new Vector3(1f, 0.22f, bar / length), edgeCyanMaterial);
        MakePrimitive(PrimitiveType.Cube, "Near Edge", platform.transform, new Vector3(0f, 0.56f, -0.5f + bar / (2f * length)), new Vector3(1f, 0.22f, bar / length), edgePinkMaterial);
        GameObject glowObject = new GameObject("Landing Glow");
        glowObject.transform.SetParent(platform.transform, false);
        glowObject.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        Light glow = glowObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = starting ? new Color(0.14f, 0.82f, 1f) : new Color(0.24f, 0.78f, 1f);
        glow.range = 5.5f;
        glow.intensity = 0f;
        glow.enabled = false;

        CosmicPlatform tile = platform.AddComponent<CosmicPlatform>();
        tile.Initialize(glow, platformMaterial);
        platforms.Add(tile);
        if (placeStar)
            CreateStar(position + new Vector3(0f, 1.28f, 0.15f));
        return tile;
    }

    private void CreateStar(Vector3 position)
    {
        GameObject star = new GameObject("Twinkling Star Pickup");
        star.transform.SetParent(worldRoot, false);
        star.transform.position = position;
        SphereCollider trigger = star.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.62f;

        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        halo.name = "Golden Glow";
        halo.transform.SetParent(star.transform, false);
        halo.transform.localScale = new Vector3(0.68f, 0.68f, 0.34f);
        halo.GetComponent<Renderer>().sharedMaterial = starHaloMaterial;
        Destroy(halo.GetComponent<Collider>());

        GameObject starShape = new GameObject("Five Point Star");
        starShape.transform.SetParent(star.transform, false);
        starShape.transform.localPosition = new Vector3(0f, 0f, -0.23f);
        starShape.transform.localScale = Vector3.one * 0.48f;
        starShape.AddComponent<MeshFilter>().sharedMesh = starMesh;
        starShape.AddComponent<MeshRenderer>().sharedMaterial = starMaterial;

        Light light = star.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.56f, 0.1f);
        light.range = 4.3f;
        light.intensity = 1.4f;

        CosmicStar pickup = star.AddComponent<CosmicStar>();
        pickup.Initialize(this);
        starPickups.Add(pickup);
    }

    private static Mesh CreateStarMesh()
    {
        Mesh mesh = new Mesh { name = "Procedural Five Point Star" };
        const int points = 10;
        Vector3[] vertices = new Vector3[points + 2];
        float depth = 0.12f;
        vertices[0] = new Vector3(0f, 0f, depth);
        vertices[1] = new Vector3(0f, 0f, -depth);
        for (int i = 0; i < points; i++)
        {
            float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
            float radius = i % 2 == 0 ? 1f : 0.48f;
            vertices[i + 2] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, depth * 0.6f);
        }
        List<int> triangles = new List<int>();
        for (int i = 0; i < points; i++)
        {
            int next = (i + 1) % points;
            triangles.Add(0); triangles.Add(i + 2); triangles.Add(next + 2);
            triangles.Add(1); triangles.Add(next + 2); triangles.Add(i + 2);
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CleanupBehind()
    {
        float cutoff = player.transform.position.z - 28f;
        for (int i = platforms.Count - 1; i >= 0; i--)
        {
            CosmicPlatform platform = platforms[i];
            if (platform == null || platform.transform.position.z < cutoff)
            {
                if (platform != null)
                    Destroy(platform.gameObject);
                platforms.RemoveAt(i);
            }
        }

        for (int i = starPickups.Count - 1; i >= 0; i--)
        {
            CosmicStar pickup = starPickups[i];
            if (pickup == null || pickup.transform.position.z < cutoff)
            {
                if (pickup != null)
                    Destroy(pickup.gameObject);
                starPickups.RemoveAt(i);
            }
        }
    }

    public void OnStarCollected()
    {
        if (gameOver)
            return;
        stars++;
        if (player != null)
            player.EnableRainbowTrail();
    }

    public void EndRun()
    {
        if (gameOver)
            return;

        gameOver = true;
        int score = CurrentScore;
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }
        if (player != null)
            player.Freeze();
    }

    private int CurrentScore
    {
        get { return Mathf.Max(0, Mathf.FloorToInt((bestReachZ + 1.45f) * 10f)) + stars * 100; }
    }

    private void OnGUI()
    {
        if (cardStyle == null)
            CreateGuiStyles();

        scoreScale = Mathf.Clamp(Screen.height / 1080f, 0.55f, 1.45f);
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scoreScale);
        float logicalWidth = Screen.width / scoreScale;

        GUI.Box(new Rect(38f, 34f, 318f, 124f), GUIContent.none, cardStyle);
        GUI.Label(new Rect(60f, 51f, 278f, 28f), "COSMIC CAT  /  STAR RUN", smallStyle);
        GUI.Label(new Rect(60f, 81f, 278f, 50f), "SCORE   " + CurrentScore.ToString("D5"), titleStyle);

        GUI.Box(new Rect(logicalWidth - 272f, 34f, 234f, 124f), GUIContent.none, cardStyle);
        GUI.Label(new Rect(logicalWidth - 249f, 52f, 190f, 28f), "STARS COLLECTED", smallStyle);
        GUI.Label(new Rect(logicalWidth - 249f, 78f, 190f, 54f), "STARS   " + stars.ToString("D2"), titleStyle);

        GUI.Box(new Rect((logicalWidth - 665f) * 0.5f, 1018f, 665f, 42f), GUIContent.none, cardStyle);
        GUI.Label(new Rect((logicalWidth - 635f) * 0.5f, 1024f, 635f, 30f), "LEFT / RIGHT   MOVE   •   SPACE   JUMP   •   R   RESTART", smallStyle);

        if (gameOver)
            DrawGameOver(logicalWidth);
        GUI.matrix = Matrix4x4.identity;
    }

    private void CreateGuiStyles()
    {
        cardTexture = SolidTexture(new Color(0.025f, 0.045f, 0.13f, 0.91f));
        buttonTexture = SolidTexture(new Color(0.15f, 0.68f, 0.96f, 1f));
        overlayTexture = SolidTexture(new Color(0.008f, 0.012f, 0.045f, 0.78f));
        cardStyle = new GUIStyle(GUI.skin.box);
        cardStyle.normal.background = cardTexture;
        cardStyle.border = new RectOffset(10, 10, 10, 10);
        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 32;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(0.88f, 0.95f, 1f);
        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.fontSize = 18;
        smallStyle.fontStyle = FontStyle.Bold;
        smallStyle.normal.textColor = new Color(0.38f, 0.86f, 1f);
        bodyStyle = new GUIStyle(GUI.skin.label);
        bodyStyle.fontSize = 22;
        bodyStyle.alignment = TextAnchor.MiddleCenter;
        bodyStyle.normal.textColor = new Color(0.74f, 0.82f, 1f);
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.normal.background = buttonTexture;
        buttonStyle.hover.background = buttonTexture;
        buttonStyle.active.background = buttonTexture;
        buttonStyle.fontSize = 24;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = Color.white;
        overlayStyle = new GUIStyle(GUI.skin.box);
        overlayStyle.normal.background = overlayTexture;
        gameOverHeadingStyle = new GUIStyle(titleStyle);
        gameOverHeadingStyle.fontSize = 50;
        gameOverHeadingStyle.alignment = TextAnchor.MiddleCenter;
        gameOverHeadingStyle.normal.textColor = new Color(1f, 0.47f, 0.83f);
    }

    private static Texture2D SolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void DrawGameOver(float logicalWidth)
    {
        GUI.Box(new Rect(0f, 0f, logicalWidth, Screen.height / scoreScale), GUIContent.none, overlayStyle);
        float x = (logicalWidth - 620f) * 0.5f;
        float y = (Screen.height / scoreScale - 430f) * 0.5f;
        GUI.Box(new Rect(x, y, 620f, 430f), GUIContent.none, cardStyle);

        GUI.Label(new Rect(x + 35f, y + 43f, 550f, 66f), "SPACE CAT DOWN!", gameOverHeadingStyle);
        GUI.Label(new Rect(x + 50f, y + 130f, 520f, 44f), "SCORE   " + CurrentScore.ToString("D5"), titleStyle);
        GUI.Label(new Rect(x + 50f, y + 178f, 520f, 36f), "BEST     " + bestScore.ToString("D5") + "       STARS   " + stars, bodyStyle);
        GUI.Label(new Rect(x + 50f, y + 227f, 520f, 36f), "Your starlight trail is waiting, Mochi!", bodyStyle);
        if (GUI.Button(new Rect(x + 95f, y + 297f, 430f, 66f), "PLAY AGAIN", buttonStyle))
            BeginRun();
        GUI.Label(new Rect(x + 50f, y + 370f, 520f, 30f), "PRESS  R  OR  SPACE  TO GO AGAIN", smallStyle);
    }
}

public sealed class CosmicCatPlayer : MonoBehaviour
{
    private const float ForwardSpeed = 7.6f;
    private CosmicCatRunner game;
    private Rigidbody body;
    private float horizontal;
    private float jumpBufferedUntil = -1f;
    private float lastGroundedTime = -10f;

    public Transform VisualRoot { get; set; }
    public Transform Tail { get; set; }
    public TrailRenderer RainbowTrail { get; set; }

    public void Initialize(CosmicCatRunner runner, Rigidbody rigidbody)
    {
        game = runner;
        body = rigidbody;
    }

    private void Update()
    {
        if (game == null || game.IsGameOver)
            return;

        horizontal = 0f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferedUntil = Time.time + 0.14f;

        if (VisualRoot != null)
        {
            float lean = -horizontal * 9f;
            VisualRoot.localRotation = Quaternion.Lerp(VisualRoot.localRotation, Quaternion.Euler(0f, 0f, lean), Time.deltaTime * 8f);
            Vector3 tailBase = new Vector3(0f, 0.63f, -0.5f);
            for (int i = 0; i < Tail.childCount; i++)
            {
                float t = (i + 1f) / Tail.childCount;
                float wag = Mathf.Sin(Time.time * 5f + t * 2f) * 0.12f * t;
                Tail.GetChild(i).localPosition = tailBase + new Vector3(wag, Mathf.Sin(t * Mathf.PI) * 0.47f, -0.69f * t);
            }
            float bob = Mathf.Sin(Time.time * 12f) * 0.035f;
            VisualRoot.localPosition = new Vector3(0f, bob, 0f);
        }
    }

    private void FixedUpdate()
    {
        if (game == null || game.IsGameOver || body == null)
            return;

        Vector3 velocity = body.velocity;
        velocity.x = horizontal * 6.7f;
        velocity.z = ForwardSpeed;

        bool grounded = Physics.SphereCast(transform.position + Vector3.up * 0.25f, 0.2f, Vector3.down, out RaycastHit hit, 0.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.45f;
        if (grounded)
            lastGroundedTime = Time.time;

        if (jumpBufferedUntil >= Time.time && Time.time - lastGroundedTime < 0.16f)
        {
            velocity.y = 8.4f;
            jumpBufferedUntil = -1f;
            lastGroundedTime = -10f;
        }

        velocity.x = Mathf.Clamp(velocity.x, -7f, 7f);
        body.velocity = velocity;
    }

    public void EnableRainbowTrail()
    {
        if (RainbowTrail != null)
            RainbowTrail.emitting = true;
    }

    public void Freeze()
    {
        if (body == null)
            return;
        body.velocity = Vector3.zero;
        body.isKinematic = true;
    }
}

public sealed class CosmicPlatform : MonoBehaviour
{
    private bool collapsing;
    private float collapseStarted;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Light landingGlow;
    private Material baseMaterial;
    private Renderer baseRenderer;

    public void Initialize(Light glow, Material material)
    {
        landingGlow = glow;
        baseMaterial = material;
        baseRenderer = GetComponent<Renderer>();
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collapsing || !collision.collider.CompareTag("Player"))
            return;

        bool landedOnTop = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Mathf.Abs(collision.GetContact(i).normal.y) > 0.48f)
            {
                landedOnTop = true;
                break;
            }
        }
        if (landedOnTop)
            BeginCollapse();
    }

    private void BeginCollapse()
    {
        collapsing = true;
        collapseStarted = Time.time;
        if (landingGlow != null)
            landingGlow.enabled = true;
    }

    private void Update()
    {
        if (!collapsing)
            return;

        float t = Mathf.Clamp01((Time.time - collapseStarted) / 0.78f);
        float pulse = 0.7f + Mathf.Sin(Time.time * 36f) * 0.28f;
        if (landingGlow != null)
            landingGlow.intensity = 2.3f * (1f - t) * pulse;
        if (baseRenderer != null && baseMaterial != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_EmissionColor", Color.Lerp(new Color(0.06f, 0.28f, 0.78f), new Color(0.42f, 0.85f, 1f), pulse));
            baseRenderer.SetPropertyBlock(block);
        }
        transform.localScale = new Vector3(originalScale.x, Mathf.Lerp(originalScale.y, 0.012f, t), originalScale.z);
        transform.localPosition = originalPosition + Vector3.down * (0.24f * t);
        if (t >= 1f)
            Destroy(gameObject);
    }
}

public sealed class CosmicStar : MonoBehaviour
{
    private CosmicCatRunner game;
    private Transform shape;
    private float startY;
    private float phase;

    public void Initialize(CosmicCatRunner runner)
    {
        game = runner;
        startY = transform.position.y;
        phase = (float)Random.Range(0f, 6.28f);
        shape = transform.Find("Five Point Star");
    }

    private void Update()
    {
        if (shape == null)
            return;
        transform.position = new Vector3(transform.position.x, startY + Mathf.Sin(Time.time * 2.5f + phase) * 0.12f, transform.position.z);
        shape.Rotate(0f, 65f * Time.deltaTime, 22f * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        if (game != null)
            game.OnStarCollected();
        Destroy(gameObject);
    }
}
