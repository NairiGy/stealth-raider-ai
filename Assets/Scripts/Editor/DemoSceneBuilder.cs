using System.Collections.Generic;
using StealthRaider.AI;
using StealthRaider.Combat;
using StealthRaider.Debugging;
using StealthRaider.Player;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace StealthRaider.EditorTools
{
    /// <summary>
    /// Builds the test scene from code: a junkyard yard with cover, the hero, three raiders with
    /// patrol routes, two suns, and a baked NavMesh. Stealth Raider → Build Demo Scene.
    /// Characters are placeholders made of primitives on purpose: the task is about the AI.
    /// </summary>
    public static class DemoSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/RaiderDemo.unity";
        const string NavMeshPath = "Assets/Scenes/RaiderDemo_NavMesh.asset";
        const string ConfigPath = "Assets/Settings/RaiderConfig.asset";
        const string MaterialFolder = "Assets/Art/Materials";
        const int CharacterLayer = 8;
        const string CharacterLayerName = "Character";

        static int ObstacleMask => ~(1 << CharacterLayer);

        class Palette
        {
            public Material Ground, Metal, Ochre, Rust, Suit, Stripe, Visor, RaiderArmor, Threat, Debug;
        }

        [MenuItem("Stealth Raider/Build Demo Scene")]
        public static void Build()
        {
            EnsureCharacterLayer();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var palette = CreatePalette();
            RaiderConfig config = LoadOrCreateConfig();

            SetupLighting();
            NavMeshSurface surface = BuildLevel(palette);

            GameObject hero = BuildHero(palette, new Vector3(0f, 0f, -24f));
            Camera camera = BuildCamera(hero.transform);
            SetRef(hero.GetComponent<HeroController>(), "cameraRig", camera.GetComponent<ThirdPersonCamera>());
            SetRef(hero.GetComponent<HeroShooter>(), "aimCamera", camera);

            var routes = new GameObject("Patrol Routes").transform;
            BuildRaider("Raider A", palette, config, hero.transform, routes,
                new Vector3(-5f, 0f, -5f), new Vector3(5f, 0f, -5f), new Vector3(5f, 0f, 5f), new Vector3(-5f, 0f, 5f));
            BuildRaider("Raider B", palette, config, hero.transform, routes,
                new Vector3(-12f, 0f, 11f), new Vector3(12f, 0f, 11f));
            BuildRaider("Raider C", palette, config, hero.transform, routes,
                new Vector3(19f, 0f, -12f), new Vector3(19f, 0f, 10f), new Vector3(24f, 0f, 22f));

            var systems = new GameObject("Systems");
            SetRef(systems.AddComponent<GameHud>(), "hero", hero.GetComponent<Health>());
            SetRef(systems.AddComponent<NoiseDebugView>(), "lineMaterial", palette.Debug);

            BakeNavMesh(surface);

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"Stealth Raider: demo scene built at {ScenePath}");
        }

        // ---------------- Level ----------------

        static NavMeshSurface BuildLevel(Palette p)
        {
            var level = new GameObject("Level");

            var ground = Box("Ground", level.transform, new Vector3(0f, -0.5f, 0f), new Vector3(64f, 1f, 64f), p.Ground);
            ground.isStatic = true;

            // Perimeter: a scrap fence.
            Box("Wall N", level.transform, new Vector3(0f, 1.5f, 31f), new Vector3(62f, 3f, 0.6f), p.Metal);
            Box("Wall S", level.transform, new Vector3(0f, 1.5f, -31f), new Vector3(62f, 3f, 0.6f), p.Metal);
            Box("Wall E", level.transform, new Vector3(31f, 1.5f, 0f), new Vector3(0.6f, 3f, 62f), p.Metal);
            Box("Wall W", level.transform, new Vector3(-31f, 1.5f, 0f), new Vector3(0.6f, 3f, 62f), p.Metal);

            // Shipping containers: full cover that blocks sight.
            Container(level.transform, p, new Vector3(-8f, 0f, -11f), 0f, p.Ochre);
            Container(level.transform, p, new Vector3(9f, 0f, -11f), 0f, p.Rust);
            Container(level.transform, p, new Vector3(0f, 0f, 0f), 90f, p.Metal);
            Container(level.transform, p, new Vector3(-14f, 0f, 6f), 90f, p.Rust);
            Container(level.transform, p, new Vector3(14f, 0f, 4f), 90f, p.Ochre);
            Container(level.transform, p, new Vector3(-6f, 0f, 15f), 0f, p.Metal);
            Container(level.transform, p, new Vector3(7f, 0f, 16f), 0f, p.Rust);
            Container(level.transform, p, new Vector3(0f, 0f, 24f), 0f, p.Ochre);
            Container(level.transform, p, new Vector3(-22f, 0f, -20f), 30f, p.Rust);

            // Crates: low cover. The hero's head shows over them when standing close.
            var crates = new[]
            {
                new Vector3(-3f, 0f, -17f), new Vector3(3.5f, 0f, -18f), new Vector3(-12f, 0f, -2f),
                new Vector3(10f, 0f, -1f), new Vector3(-2.5f, 0f, 9f), new Vector3(3.5f, 0f, 8.5f),
                new Vector3(-18f, 0f, 18f), new Vector3(16f, 0f, 19f), new Vector3(-20f, 0f, -12f),
                new Vector3(22f, 0f, -16f), new Vector3(24f, 0f, 2f),
            };
            for (int i = 0; i < crates.Length; i++)
            {
                Box($"Crate {i + 1}", level.transform, crates[i] + Vector3.up * 0.6f, Vector3.one * 1.2f, p.Ochre)
                    .transform.rotation = Quaternion.Euler(0f, i * 23f, 0f);
            }

            // Low walls.
            Box("Low Wall 1", level.transform, new Vector3(-5f, 0.55f, -4f), new Vector3(4f, 1.1f, 0.4f), p.Metal);
            Box("Low Wall 2", level.transform, new Vector3(9f, 0.55f, 9f), new Vector3(0.4f, 1.1f, 4f), p.Metal);
            Box("Low Wall 3", level.transform, new Vector3(-10f, 0.55f, 22f), new Vector3(5f, 1.1f, 0.4f), p.Metal);

            // Two old tanks for silhouette.
            Tank(level.transform, new Vector3(-21f, 0f, 1f), 2.6f, 5f, p.Rust);
            Tank(level.transform, new Vector3(23f, 0f, -6f), 2.2f, 4f, p.Metal);

            foreach (Transform child in level.GetComponentsInChildren<Transform>()) child.gameObject.isStatic = true;

            var surface = level.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ObstacleMask;
            return surface;
        }

        static void Container(Transform parent, Palette p, Vector3 position, float yaw, Material body)
        {
            var root = new GameObject("Container").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Box("Body", root, new Vector3(0f, 1.3f, 0f), new Vector3(6f, 2.6f, 2.4f), body);
            // Ribs and a faded stripe, no collider.
            for (int i = -2; i <= 2; i++)
                Visual(PrimitiveType.Cube, root, new Vector3(i * 1.4f, 1.3f, 0f), new Vector3(0.12f, 2.62f, 2.44f), p.Metal);
            Visual(PrimitiveType.Cube, root, new Vector3(0f, 2.2f, 0f), new Vector3(6.02f, 0.25f, 2.42f), p.Stripe);
        }

        static void Tank(Transform parent, Vector3 position, float radius, float height, Material material)
        {
            var tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.name = "Tank";
            tank.transform.SetParent(parent, false);
            tank.transform.position = position + Vector3.up * height * 0.5f;
            tank.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            tank.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void BakeNavMesh(NavMeshSurface surface)
        {
            surface.BuildNavMesh();
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshPath) != null) AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
            EditorUtility.SetDirty(surface);
        }

        // ---------------- Lighting ----------------

        static void SetupLighting()
        {
            // Two suns: the white one leads with hard shadows, the red one warms the shadows.
            var white = new GameObject("Sun (White)").AddComponent<Light>();
            white.type = LightType.Directional;
            white.color = new Color(1f, 0.96f, 0.9f);
            white.intensity = 1.3f;
            white.shadows = LightShadows.Hard;
            white.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            var red = new GameObject("Sun (Red)").AddComponent<Light>();
            red.type = LightType.Directional;
            red.color = new Color(1f, 0.38f, 0.25f);
            red.intensity = 0.5f;
            red.shadows = LightShadows.None;
            red.transform.rotation = Quaternion.Euler(18f, 145f, 0f);

            RenderSettings.sun = white;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.27f, 0.21f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.55f, 0.38f, 0.28f);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 110f;
        }

        // ---------------- Characters ----------------

        static GameObject BuildHero(Palette p, Vector3 position)
        {
            var hero = new GameObject("Hero");
            hero.transform.position = position;

            var controller = hero.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            var health = hero.AddComponent<Health>();
            SetFloat(health, "max", 100f);
            SetEnum(health, "team", (int)Team.Hero);

            // Bulky suit, half-closed helmet with a strip of glass, oxygen tank on the back.
            Transform body = BuildBody(hero.transform, p.Suit, p.Stripe, p.Metal, p.Visor,
                out Transform leftArm, out Transform rightArm, out Transform gunModel, out Transform muzzle);
            Visual(PrimitiveType.Cylinder, body, new Vector3(0f, 1.15f, -0.36f), new Vector3(0.3f, 0.3f, 0.3f), p.Metal);
            Visual(PrimitiveType.Cylinder, body, new Vector3(0f, 1.15f, -0.36f), new Vector3(0.31f, 0.05f, 0.31f), p.Stripe);

            var gun = hero.AddComponent<Gun>();
            SetRef(gun, "muzzle", muzzle);
            SetFloat(gun, "damage", 25f);
            SetFloat(gun, "fireInterval", 0.25f);
            SetFloat(gun, "spreadDegrees", 0.4f);
            SetFloat(gun, "noiseRadius", 40f);
            SetRef(gun, "tracerMaterial", p.Debug);

            var move = hero.AddComponent<HeroController>();
            SetRef(move, "body", body);
            SetRef(hero.AddComponent<HeroShooter>(), "gun", gun);

            SetLayerRecursively(hero, CharacterLayer);
            return hero;
        }

        static Camera BuildCamera(Transform hero)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.38f, 0.28f);
            camera.nearClipPlane = 0.1f;
            camera.fieldOfView = 60f;
            go.AddComponent<AudioListener>();
            var rig = go.AddComponent<ThirdPersonCamera>();
            SetRef(rig, "target", hero);
            SetInt(rig, "collisionMask", ObstacleMask);
            go.transform.position = hero.position + new Vector3(0.6f, 1.8f, -3.4f);
            return camera;
        }

        static void BuildRaider(string raiderName, Palette p, RaiderConfig config, Transform hero, Transform routes,
            params Vector3[] route)
        {
            var routeRoot = new GameObject($"{raiderName} Route").transform;
            routeRoot.SetParent(routes, false);
            var points = new List<Object>();
            for (int i = 0; i < route.Length; i++)
            {
                var point = new GameObject($"Point {i + 1}").transform;
                point.SetParent(routeRoot, false);
                point.position = route[i];
                points.Add(point);
            }

            var raider = new GameObject(raiderName);
            raider.transform.position = route[0];
            if (route.Length > 1) raider.transform.rotation = Quaternion.LookRotation(route[1] - route[0]);

            var agent = raider.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.8f;
            agent.speed = config.patrolSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.3f;

            var collider = raider.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.height = 1.8f;
            collider.radius = 0.35f;

            var health = raider.AddComponent<Health>();
            SetFloat(health, "max", 100f);
            SetEnum(health, "team", (int)Team.Raiders);

            // Raiders wear dark scrap armour with red marks: red means threat in this game.
            Transform body = BuildBody(raider.transform, p.RaiderArmor, p.Threat, p.Metal, p.Visor,
                out Transform leftArm, out Transform rightArm, out Transform gunModel, out Transform muzzle);
            Visual(PrimitiveType.Cube, body, new Vector3(0f, 1.95f, -0.05f), new Vector3(0.1f, 0.18f, 0.42f), p.Threat);

            var gun = raider.AddComponent<Gun>();
            SetRef(gun, "muzzle", muzzle);
            SetFloat(gun, "damage", 8f);
            SetFloat(gun, "fireInterval", 0.9f);
            SetFloat(gun, "spreadDegrees", 4f);
            SetFloat(gun, "noiseRadius", 35f);
            SetRef(gun, "tracerMaterial", p.Debug);
            SetColor(gun, "tracerColor", new Color(0.84f, 0.24f, 0.17f));

            var visual = raider.AddComponent<RaiderVisual>();
            SetRef(visual, "body", body);
            SetRef(visual, "leftArm", leftArm);
            SetRef(visual, "rightArm", rightArm);
            SetRef(visual, "gunModel", gunModel);

            var brain = raider.AddComponent<RaiderBrain>();
            SetRef(brain, "config", config);
            SetRef(brain, "target", hero);
            SetRefArray(brain, "waypoints", points);
            SetRef(brain, "gun", gun);
            SetRef(brain, "visual", visual);

            var debugView = raider.AddComponent<RaiderDebugView>();
            SetRef(debugView, "coneMaterial", p.Debug);
            SetRef(debugView, "lineMaterial", p.Debug);

            SetLayerRecursively(raider, CharacterLayer);
        }

        /// <summary>Placeholder humanoid from primitives. Arms pivot at the shoulders; the gun sits in the right hand.</summary>
        static Transform BuildBody(Transform root, Material main, Material stripe, Material metal, Material visor,
            out Transform leftArm, out Transform rightArm, out Transform gunModel, out Transform muzzle)
        {
            var body = new GameObject("Body").transform;
            body.SetParent(root, false);

            Visual(PrimitiveType.Capsule, body, new Vector3(0f, 0.9f, 0f), new Vector3(0.72f, 0.9f, 0.6f), main);
            Visual(PrimitiveType.Cube, body, new Vector3(0f, 1.25f, 0.02f), new Vector3(0.74f, 0.08f, 0.62f), stripe);
            Visual(PrimitiveType.Sphere, body, new Vector3(0f, 1.72f, 0f), new Vector3(0.5f, 0.5f, 0.5f), metal);
            Visual(PrimitiveType.Cube, body, new Vector3(0f, 1.74f, 0.2f), new Vector3(0.34f, 0.12f, 0.14f), visor);

            leftArm = Arm("Left Arm", body, new Vector3(-0.42f, 1.42f, 0f), main, Quaternion.Euler(-70f, 0f, 0f));
            rightArm = Arm("Right Arm", body, new Vector3(0.42f, 1.42f, 0f), main, Quaternion.Euler(-80f, 0f, 0f));

            gunModel = new GameObject("Gun Model").transform;
            gunModel.SetParent(rightArm, false);
            gunModel.localPosition = new Vector3(-0.08f, -0.75f, 0.05f);
            Visual(PrimitiveType.Cube, gunModel, Vector3.zero, new Vector3(0.08f, 0.62f, 0.12f), metal);
            Visual(PrimitiveType.Cube, gunModel, new Vector3(0f, 0.12f, 0.1f), new Vector3(0.06f, 0.08f, 0.14f), metal);
            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(gunModel, false);
            muzzle.localPosition = new Vector3(0f, -0.33f, 0f);
            return body;
        }

        static Transform Arm(string armName, Transform body, Vector3 shoulder, Material material, Quaternion rest)
        {
            var pivot = new GameObject(armName).transform;
            pivot.SetParent(body, false);
            pivot.localPosition = shoulder;
            pivot.localRotation = rest;
            Visual(PrimitiveType.Capsule, pivot, new Vector3(0f, -0.32f, 0f), new Vector3(0.17f, 0.34f, 0.17f), material);
            return pivot;
        }

        // ---------------- Assets ----------------

        static Palette CreatePalette()
        {
            EnsureFolder(MaterialFolder);
            return new Palette
            {
                Ground = Lit("Ground", 0x6E3A26, 0.05f),
                Metal = Lit("Metal", 0x5F5B56, 0.3f),
                Ochre = Lit("Ochre", 0xA06A2C, 0.1f),
                Rust = Lit("Rust", 0x7C3420, 0.1f),
                Suit = Lit("Suit", 0xD6CFC0, 0.2f),
                Stripe = Lit("Faded Orange", 0xC0672E, 0.1f),
                Visor = Lit("Visor", 0x1A1816, 0.6f),
                RaiderArmor = Lit("Raider Armour", 0x3B3430, 0.15f),
                Threat = Lit("Threat Red", 0xD63D2B, 0.2f),
                Debug = DebugMaterial(),
            };
        }

        static Material Lit(string materialName, int rgb, float smoothness)
        {
            string path = $"{MaterialFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            var color = new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material DebugMaterial()
        {
            string path = $"{MaterialFolder}/Debug Lines.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            // Unlit, transparent and tinted by vertex colour: right for cones, lines and tracers.
            material = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static RaiderConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<RaiderConfig>(ConfigPath);
            if (config == null)
            {
                EnsureFolder("Assets/Settings");
                config = ScriptableObject.CreateInstance<RaiderConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            config.obstacleMask = ObstacleMask;
            EditorUtility.SetDirty(config);
            return config;
        }

        static void EnsureCharacterLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layer = tagManager.FindProperty("layers").GetArrayElementAtIndex(CharacterLayer);
            if (layer.stringValue == CharacterLayerName) return;
            if (!string.IsNullOrEmpty(layer.stringValue))
                Debug.LogWarning($"Layer {CharacterLayer} was '{layer.stringValue}', renaming to '{CharacterLayerName}'.");
            layer.stringValue = CharacterLayerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        // ---------------- Small helpers ----------------

        static GameObject Box(string boxName, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = boxName;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        /// <summary>A primitive for looks only: its collider is removed.</summary>
        static GameObject Visual(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        static void SetRef(Object target, string field, Object value) =>
            Edit(target, field, p => p.objectReferenceValue = value);

        static void SetFloat(Object target, string field, float value) =>
            Edit(target, field, p => p.floatValue = value);

        static void SetInt(Object target, string field, int value) =>
            Edit(target, field, p => p.intValue = value);

        static void SetEnum(Object target, string field, int index) =>
            Edit(target, field, p => p.enumValueIndex = index);

        static void SetColor(Object target, string field, Color value) =>
            Edit(target, field, p => p.colorValue = value);

        static void SetRefArray(Object target, string field, IReadOnlyList<Object> values) =>
            Edit(target, field, p =>
            {
                p.arraySize = values.Count;
                for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            });

        static void Edit(Object target, string field, System.Action<SerializedProperty> apply)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"{target.GetType().Name} has no serialized field '{field}'.");
                return;
            }
            apply(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
