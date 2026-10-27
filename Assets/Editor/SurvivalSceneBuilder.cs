using System;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = System.Random;

// Construit toute la scene du jeu (materiaux, prefabs, monde, joueur, systemes) en un clic.
// Menu Unity : Survie > Construire la scene du jeu
// Ligne de commande : Unity -batchmode -projectPath . -executeMethod SurvivalSceneBuilder.BuildFromCommandLine -quit
public static class SurvivalSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Survie.unity";
    const string NavMeshPath = "Assets/Scenes/Survie_NavMesh.asset";
    const int IgnoreRaycastLayer = 2;
    const int WaterLayer = 4;
    const float WorldHalfSize = 100f;

    static Material ground, trunk, leaves, rock, bush, berry, water, playerMat, dark, backpack, toolMat,
        enemyMat, eyes, planks, roofMat, fire, stoneMat, ghostValid, ghostInvalid;

    [MenuItem("Survie/Construire la scene du jeu")]
    public static void BuildScene()
    {
        if (!Application.isBatchMode &&
            !EditorUtility.DisplayDialog("Survie", "Reconstruire la scene " + ScenePath + " ? Les changements faits a la main dans cette scene seront perdus.", "Oui", "Annuler"))
            return;

        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Scenes");

        CreateMaterials();
        var prefabs = CreatePrefabs();
        BuildWorld(prefabs);
        ConfigureProject();
        Debug.Log("[Survie] Scene construite : " + ScenePath);
    }

    public static void BuildFromCommandLine()
    {
        try
        {
            BuildScene();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    // ---------- Materiaux ----------

    static void CreateMaterials()
    {
        ground = Mat("Herbe", new Color(0.32f, 0.52f, 0.24f), 0.1f);
        trunk = Mat("Tronc", new Color(0.4f, 0.27f, 0.15f));
        leaves = Mat("Feuillage", new Color(0.18f, 0.45f, 0.2f));
        rock = Mat("Rocher", new Color(0.5f, 0.5f, 0.52f), 0.3f);
        bush = Mat("Buisson", new Color(0.22f, 0.5f, 0.22f));
        berry = Mat("Baie", new Color(0.8f, 0.1f, 0.2f), 0.6f);
        water = Mat("Eau", new Color(0.15f, 0.45f, 0.75f), 0.95f);
        playerMat = Mat("Joueur", new Color(0.2f, 0.45f, 0.85f));
        dark = Mat("Sombre", new Color(0.1f, 0.1f, 0.12f), 0.6f);
        backpack = Mat("Sac", new Color(0.45f, 0.3f, 0.15f));
        toolMat = Mat("Outil", new Color(0.55f, 0.4f, 0.25f));
        enemyMat = Mat("Ennemi", new Color(0.55f, 0.12f, 0.12f));
        eyes = Mat("Yeux", new Color(1f, 0.85f, 0.1f), 0.5f, true);
        planks = Mat("Planches", new Color(0.62f, 0.45f, 0.26f));
        roofMat = Mat("Toit", new Color(0.45f, 0.3f, 0.18f));
        fire = Mat("Feu", new Color(1f, 0.5f, 0.1f), 0.5f, true);
        stoneMat = Mat("Pierres", new Color(0.4f, 0.4f, 0.42f));
        ghostValid = TransparentMat("FantomeValide", new Color(0.3f, 1f, 0.4f, 0.4f));
        ghostInvalid = TransparentMat("FantomeInvalide", new Color(1f, 0.25f, 0.2f, 0.4f));
        AssetDatabase.SaveAssets();
    }

    static Material Mat(string name, Color color, float smoothness = 0.15f, bool emissive = false)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = shader;
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smoothness);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * 1.5f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material TransparentMat(string name, Color color)
    {
        var m = Mat(name, color, 0.5f);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------- Prefabs ----------

    class Prefabs
    {
        public GameObject tree, rock, bush, enemy;
        public BuildPiece[] pieces;
    }

    static Prefabs CreatePrefabs()
    {
        var p = new Prefabs
        {
            tree = SavePrefab(MakeTree(), "Arbre"),
            rock = SavePrefab(MakeRock(), "Rocher"),
            bush = SavePrefab(MakeBush(), "Buisson"),
            enemy = SavePrefab(MakeEnemy(), "Ennemi"),
        };
        p.pieces = new[]
        {
            SavePrefab(MakeFloor(), "Piece_Sol").GetComponent<BuildPiece>(),
            SavePrefab(MakeWall(), "Piece_Mur").GetComponent<BuildPiece>(),
            SavePrefab(MakeDoorway(), "Piece_Porte").GetComponent<BuildPiece>(),
            SavePrefab(MakeRoof(), "Piece_Toit").GetComponent<BuildPiece>(),
            SavePrefab(MakeCampfire(), "Piece_FeuDeCamp").GetComponent<BuildPiece>(),
        };
        return p;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/" + name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat,
        bool collider = false, Vector3 euler = default)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.transform.localEulerAngles = euler;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static GameObject ResourceRoot(string name, ResourceKind kind, int hits, float respawn, out Transform visual)
    {
        var root = new GameObject(name);
        visual = new GameObject("Visuel").transform;
        visual.SetParent(root.transform, false);
        var node = root.AddComponent<ResourceNode>();
        node.kind = kind;
        node.hitsToDeplete = hits;
        node.respawnTime = respawn;
        node.visual = visual.gameObject;
        return root;
    }

    static GameObject MakeTree()
    {
        var root = ResourceRoot("Arbre", ResourceKind.Tree, 5, 150f, out var v);
        Prim(PrimitiveType.Cylinder, "Tronc", v, new Vector3(0f, 1.5f, 0f), new Vector3(0.5f, 1.5f, 0.5f), trunk, true);
        Prim(PrimitiveType.Sphere, "Feuillage", v, new Vector3(0f, 3.6f, 0f), new Vector3(2.6f, 2.8f, 2.6f), leaves);
        Prim(PrimitiveType.Sphere, "Feuillage2", v, new Vector3(0.2f, 4.7f, 0.1f), new Vector3(1.8f, 1.8f, 1.8f), leaves);
        return root;
    }

    static GameObject MakeRock()
    {
        var root = ResourceRoot("Rocher", ResourceKind.Rock, 5, 200f, out var v);
        Prim(PrimitiveType.Cube, "Bloc", v, new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 1.1f, 1.4f), rock, true, new Vector3(10f, 25f, 5f));
        Prim(PrimitiveType.Cube, "Bloc2", v, new Vector3(0.7f, 0.3f, 0.45f), new Vector3(0.8f, 0.6f, 0.7f), rock, false, new Vector3(0f, 60f, 15f));
        return root;
    }

    static GameObject MakeBush()
    {
        var root = ResourceRoot("Buisson", ResourceKind.Bush, 3, 90f, out var v);
        Prim(PrimitiveType.Sphere, "Feuilles", v, new Vector3(0f, 0.55f, 0f), new Vector3(1.5f, 1.1f, 1.5f), bush, true);
        Vector3[] spots = { new Vector3(0.5f, 0.8f, 0.4f), new Vector3(-0.45f, 0.9f, 0.35f), new Vector3(0.1f, 1f, -0.55f), new Vector3(-0.3f, 0.6f, -0.5f), new Vector3(0.6f, 0.5f, -0.2f) };
        for (int i = 0; i < spots.Length; i++)
            Prim(PrimitiveType.Sphere, "Baie" + i, v, spots[i], Vector3.one * 0.22f, berry);
        return root;
    }

    static GameObject MakeEnemy()
    {
        var root = new GameObject("Ennemi");
        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.9f, 0f);
        col.height = 1.8f;
        col.radius = 0.4f;
        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.4f;
        agent.height = 1.8f;
        agent.speed = 3.4f;
        agent.angularSpeed = 400f;
        agent.acceleration = 12f;
        agent.stoppingDistance = 1.3f;
        root.AddComponent<EnemyAI>();

        Prim(PrimitiveType.Capsule, "Corps", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.85f, 0.9f, 0.85f), enemyMat);
        Prim(PrimitiveType.Cube, "OeilG", root.transform, new Vector3(-0.15f, 1.45f, 0.34f), new Vector3(0.12f, 0.08f, 0.05f), eyes);
        Prim(PrimitiveType.Cube, "OeilD", root.transform, new Vector3(0.15f, 1.45f, 0.34f), new Vector3(0.12f, 0.08f, 0.05f), eyes);
        Prim(PrimitiveType.Cube, "BrasG", root.transform, new Vector3(-0.5f, 1f, 0.2f), new Vector3(0.15f, 0.15f, 0.7f), enemyMat);
        Prim(PrimitiveType.Cube, "BrasD", root.transform, new Vector3(0.5f, 1f, 0.2f), new Vector3(0.15f, 0.15f, 0.7f), enemyMat);
        SetLayer(root, IgnoreRaycastLayer);
        return root;
    }

    static GameObject PieceRoot(string name, BuildPieceType type)
    {
        var root = new GameObject(name);
        root.AddComponent<BuildPiece>().type = type;
        return root;
    }

    static void Carve(GameObject go)
    {
        var o = go.AddComponent<NavMeshObstacle>();
        o.shape = NavMeshObstacleShape.Box;
        o.size = Vector3.one;
        o.carving = true;
    }

    static GameObject MakeFloor()
    {
        var root = PieceRoot("Sol", BuildPieceType.Floor);
        Prim(PrimitiveType.Cube, "Plancher", root.transform, new Vector3(0f, 0.1f, 0f), new Vector3(3f, 0.2f, 3f), planks, true);
        return root;
    }

    static GameObject MakeWall()
    {
        var root = PieceRoot("Mur", BuildPieceType.Wall);
        Carve(Prim(PrimitiveType.Cube, "Mur", root.transform, new Vector3(0f, 1.35f, 0f), new Vector3(3f, 2.7f, 0.2f), planks, true));
        return root;
    }

    static GameObject MakeDoorway()
    {
        var root = PieceRoot("Porte", BuildPieceType.Doorway);
        Carve(Prim(PrimitiveType.Cube, "MontantG", root.transform, new Vector3(-1.05f, 1.35f, 0f), new Vector3(0.9f, 2.7f, 0.2f), planks, true));
        Carve(Prim(PrimitiveType.Cube, "MontantD", root.transform, new Vector3(1.05f, 1.35f, 0f), new Vector3(0.9f, 2.7f, 0.2f), planks, true));
        Prim(PrimitiveType.Cube, "Linteau", root.transform, new Vector3(0f, 2.45f, 0f), new Vector3(1.2f, 0.5f, 0.2f), planks, true);
        return root;
    }

    static GameObject MakeRoof()
    {
        var root = PieceRoot("Toit", BuildPieceType.Roof);
        Prim(PrimitiveType.Cube, "Toit", root.transform, new Vector3(0f, 2.8f, 0f), new Vector3(3.2f, 0.2f, 3.2f), roofMat, true);
        return root;
    }

    static GameObject MakeCampfire()
    {
        var root = PieceRoot("FeuDeCamp", BuildPieceType.Campfire);
        var box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.25f, 0f);
        box.size = new Vector3(1f, 0.5f, 1f);
        var obstacle = root.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = box.center;
        obstacle.size = box.size;
        obstacle.carving = true;

        for (int i = 0; i < 3; i++)
            Prim(PrimitiveType.Cylinder, "Buche" + i, root.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.14f, 0.45f, 0.14f), trunk, false, new Vector3(90f, i * 60f, 0f));
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * Mathf.PI * 2f;
            Prim(PrimitiveType.Cube, "Pierre" + i, root.transform, new Vector3(Mathf.Cos(a) * 0.6f, 0.08f, Mathf.Sin(a) * 0.6f), new Vector3(0.22f, 0.16f, 0.2f), stoneMat, false, new Vector3(0f, a * Mathf.Rad2Deg, 0f));
        }
        Prim(PrimitiveType.Sphere, "Flamme", root.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.45f, 0.65f, 0.45f), fire);

        var lightGo = new GameObject("Lumiere");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 1f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.25f);
        light.range = 9f;
        light.intensity = 3f;
        light.shadows = LightShadows.None;
        var flicker = root.AddComponent<CampfireFlicker>();
        flicker.fireLight = light;
        return root;
    }

    // ---------- Monde ----------

    static void BuildWorld(Prefabs prefabs)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.65f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 35f;
        RenderSettings.fogEndDistance = 140f;
        RenderSettings.fogColor = new Color(0.65f, 0.75f, 0.85f);

        // Lumieres
        var sun = new GameObject("Soleil").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.intensity = 1.3f;
        sun.transform.rotation = Quaternion.Euler(50f, 170f, 0f);
        RenderSettings.sun = sun;
        var moon = new GameObject("Lune").AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.shadows = LightShadows.None;
        moon.color = new Color(0.55f, 0.65f, 1f);
        moon.intensity = 0f;

        // Sol et limites
        var world = new GameObject("Monde").transform;
        Prim(PrimitiveType.Cube, "Sol", world, new Vector3(0f, -0.5f, 0f), new Vector3(WorldHalfSize * 2f, 1f, WorldHalfSize * 2f), ground, true);
        for (int i = 0; i < 4; i++)
        {
            var wall = new GameObject("Limite" + i);
            wall.transform.SetParent(world, false);
            bool alongX = i < 2;
            float sign = i % 2 == 0 ? 1f : -1f;
            wall.transform.position = alongX ? new Vector3(0f, 5f, sign * (WorldHalfSize + 0.5f)) : new Vector3(sign * (WorldHalfSize + 0.5f), 5f, 0f);
            wall.AddComponent<BoxCollider>().size = alongX ? new Vector3(WorldHalfSize * 2f, 10f, 1f) : new Vector3(1f, 10f, WorldHalfSize * 2f);
        }

        var lakes = new List<(Vector3 center, float radius)>
        {
            (new Vector3(22f, 0f, 20f), 9f),
            (new Vector3(-40f, 0f, -32f), 7f),
            (new Vector3(-30f, 0f, 45f), 6f),
        };
        foreach (var lake in lakes) MakeLake(world, lake.center, lake.radius);

        // Ressources
        var resources = new GameObject("Ressources").transform;
        var rng = new Random(42);
        var used = new List<Vector3>();
        int id = 0;
        id = Scatter(prefabs.tree, 90, resources, rng, used, lakes, id, 3.5f);
        id = Scatter(prefabs.rock, 45, resources, rng, used, lakes, id, 3f);
        Scatter(prefabs.bush, 40, resources, rng, used, lakes, id, 2.5f);

        // Joueur et camera
        var player = MakePlayer(out var toolHolder, out var tool);
        var spawn = new GameObject("PointDeDepart").transform;
        spawn.position = new Vector3(0f, 0.05f, 0f);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        cam.farClipPlane = 160f;
        cam.fieldOfView = 60f;
        camGo.AddComponent<AudioListener>();
        var tpc = camGo.AddComponent<ThirdPersonCamera>();
        tpc.target = player.transform;
        camGo.transform.position = new Vector3(0f, 3.5f, -6f);

        var controller = player.GetComponent<PlayerController>();
        controller.cam = tpc;
        var actions = player.GetComponent<PlayerActions>();
        actions.toolHolder = toolHolder;
        var stats = player.GetComponent<PlayerStats>();
        var inventory = player.GetComponent<Inventory>();
        var equip = player.GetComponent<EquipmentVisual>();
        equip.inventory = inventory;
        equip.tool = tool.transform;
        equip.toolRenderer = tool.GetComponent<Renderer>();

        // Core
        var core = new GameObject("Core");
        core.AddComponent<InputHub>();
        var surface = core.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~((1 << IgnoreRaycastLayer) | (1 << WaterLayer));

        // P1
        var p1 = new GameObject("P1 - Personnage (Gyroscope)");
        p1.AddComponent<GyroCameraInput>();
        p1.AddComponent<ControlsUI>().player = actions;

        // P2
        var p2 = new GameObject("P2 - Inventaire (Proximite)");
        var invUI = p2.AddComponent<InventoryUI>();
        invUI.inventory = inventory;
        invUI.stats = stats;
        p2.AddComponent<ProximityInventoryToggle>();

        // P3
        var p3 = new GameObject("P3 - Craft (Magnetometre)");
        p3.AddComponent<MagnetometerCompass>();
        p3.AddComponent<CraftingUI>().inventory = inventory;
        var compass = p3.AddComponent<CompassUI>();
        compass.inventory = inventory;
        compass.player = player.transform;
        compass.cameraTransform = camGo.transform;

        // P4
        var p4 = new GameObject("P4 - Survie et combat (Accelerometre)");
        p4.AddComponent<ShakeDetector>().player = actions;
        p4.AddComponent<StatsHUD>().stats = stats;
        var spawner = p4.AddComponent<EnemySpawner>();
        spawner.enemyPrefab = prefabs.enemy.GetComponent<EnemyAI>();
        spawner.player = player.transform;
        spawner.worldHalfSize = WorldHalfSize - 5f;

        // P5
        var p5 = new GameObject("P5 - Construction et sauvegarde (Lumiere)");
        p5.AddComponent<AmbientLightSensor>();
        var cycle = p5.AddComponent<DayNightCycle>();
        cycle.sun = sun;
        cycle.moon = moon;
        var build = p5.AddComponent<BuildSystem>();
        build.prefabs = prefabs.pieces;
        build.player = player.transform;
        build.inventory = inventory;
        build.stats = stats;
        build.ghostValid = ghostValid;
        build.ghostInvalid = ghostInvalid;
        var buildUI = p5.AddComponent<BuildUI>();
        buildUI.build = build;
        buildUI.inventory = inventory;
        p5.AddComponent<MenuUI>();

        // Bonus
        new GameObject("Bonus - Podometre").AddComponent<Pedometer>();

        var gm = core.AddComponent<GameManager>();
        gm.player = controller;
        gm.stats = stats;
        gm.inventory = inventory;
        gm.cam = tpc;
        gm.spawnPoint = spawn;
        gm.dayNight = cycle;
        gm.build = build;
        gm.spawner = spawner;
        gm.navMesh = surface;

        // NavMesh pour l'IA des ennemis
        surface.BuildNavMesh();
        AssetDatabase.DeleteAsset(NavMeshPath);
        AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }

    static void MakeLake(Transform parent, Vector3 center, float radius)
    {
        var lake = Prim(PrimitiveType.Cylinder, "Lac", parent, center + Vector3.up * 0.02f, new Vector3(radius * 2f, 0.02f, radius * 2f), water);
        lake.layer = WaterLayer;
        var trigger = lake.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(0.75f, 100f, 0.75f);
        lake.AddComponent<WaterSource>();
    }

    static int Scatter(GameObject prefab, int count, Transform parent, Random rng, List<Vector3> used,
        List<(Vector3 center, float radius)> lakes, int nextId, float spacing)
    {
        int placed = 0;
        for (int attempt = 0; attempt < count * 30 && placed < count; attempt++)
        {
            var p = new Vector3((float)(rng.NextDouble() * 2 - 1) * (WorldHalfSize - 8f), 0f, (float)(rng.NextDouble() * 2 - 1) * (WorldHalfSize - 8f));
            if (p.magnitude < 9f) continue;
            bool ok = true;
            foreach (var lake in lakes)
                if (Vector3.Distance(p, lake.center) < lake.radius + 2.5f) { ok = false; break; }
            if (ok)
                foreach (var u in used)
                    if (Vector3.Distance(p, u) < spacing) { ok = false; break; }
            if (!ok) continue;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            go.transform.localScale = Vector3.one * (0.8f + (float)rng.NextDouble() * 0.5f);
            go.GetComponent<ResourceNode>().id = nextId++;
            used.Add(p);
            placed++;
        }
        return nextId;
    }

    static GameObject MakePlayer(out Transform toolHolder, out GameObject tool)
    {
        var player = new GameObject("Joueur");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0.05f, 0f);
        var cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1f, 0f);
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.stepOffset = 0.4f;
        cc.skinWidth = 0.05f;
        player.AddComponent<PlayerStats>();
        player.AddComponent<Inventory>();
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerActions>();
        player.AddComponent<EquipmentVisual>();

        Prim(PrimitiveType.Capsule, "Corps", player.transform, new Vector3(0f, 1f, 0f), Vector3.one, playerMat);
        Prim(PrimitiveType.Cube, "Visiere", player.transform, new Vector3(0f, 1.55f, 0.33f), new Vector3(0.55f, 0.15f, 0.12f), dark);
        Prim(PrimitiveType.Cube, "Sac", player.transform, new Vector3(0f, 1.1f, -0.38f), new Vector3(0.5f, 0.6f, 0.25f), backpack);

        toolHolder = new GameObject("Main").transform;
        toolHolder.SetParent(player.transform, false);
        toolHolder.localPosition = new Vector3(0.5f, 1.05f, 0.15f);
        tool = Prim(PrimitiveType.Cube, "Outil", toolHolder, new Vector3(0f, 0f, 0.45f), new Vector3(0.12f, 0.12f, 0.9f), toolMat);
        tool.SetActive(false);

        SetLayer(player, IgnoreRaycastLayer);
        return player;
    }

    static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayer(t.gameObject, layer);
    }

    // ---------- Reglages Android ----------

    static void ConfigureProject()
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.productName = "Survie";
        PlayerSettings.companyName = "EquipeSurvie";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.equipesurvie.survie");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        AssetDatabase.SaveAssets();
    }
}
