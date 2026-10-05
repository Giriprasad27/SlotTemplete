using System;
using System.Collections.Generic;
using SlotTemplate.Core.Model;
using SlotTemplate.Data;
using SlotTemplate.Game;
using SlotTemplate.Presentation.Reels;
using SlotTemplate.Presentation.Wins;
using SlotTemplate.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SlotTemplate.Editor
{
    /// <summary>
    /// Generates a playable 5x3 sample: symbol assets, machine config, reel prefab and a wired-up scene.
    /// Safe to run again; it overwrites the generated assets in place.
    /// </summary>
    public static class SampleSceneBuilder
    {
        private const string GeneratedFolder = "Assets/_Project/Generated";
        private const string SymbolsFolder = GeneratedFolder + "/Symbols";
        private const string MaterialsFolder = GeneratedFolder + "/Materials";
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string ScenePath = ScenesFolder + "/SlotSample.unity";

        private const int ReelCount = 5;
        private const int RowCount = 3;
        private const float ReelSpacing = 1.5f;

        private struct SymbolSpec
        {
            public string Name;
            public SymbolKind Kind;
            public PrimitiveType Shape;
            public Color Color;
            public int Weight;
            public long[] Pays; // multipliers for 3, 4 and 5 of a kind

            public SymbolSpec(string name, SymbolKind kind, PrimitiveType shape, Color color, int weight, params long[] pays)
            {
                Name = name;
                Kind = kind;
                Shape = shape;
                Color = color;
                Weight = weight;
                Pays = pays;
            }
        }

        private static readonly SymbolSpec[] Specs =
        {
            new SymbolSpec("Cherry", SymbolKind.Regular, PrimitiveType.Sphere, new Color(0.9f, 0.1f, 0.15f), 7, 5, 20, 50),
            new SymbolSpec("Lemon", SymbolKind.Regular, PrimitiveType.Capsule, new Color(1f, 0.9f, 0.2f), 7, 5, 20, 50),
            new SymbolSpec("Plum", SymbolKind.Regular, PrimitiveType.Sphere, new Color(0.55f, 0.2f, 0.75f), 6, 10, 30, 75),
            new SymbolSpec("Bell", SymbolKind.Regular, PrimitiveType.Cylinder, new Color(1f, 0.75f, 0.1f), 5, 15, 40, 100),
            new SymbolSpec("Bar", SymbolKind.Regular, PrimitiveType.Cube, new Color(0.2f, 0.45f, 1f), 4, 20, 60, 150),
            new SymbolSpec("Seven", SymbolKind.Regular, PrimitiveType.Cylinder, new Color(1f, 0.35f, 0.1f), 3, 50, 150, 500),
            new SymbolSpec("Wild", SymbolKind.Wild, PrimitiveType.Cube, new Color(0.95f, 0.95f, 0.95f), 2, 50, 200, 1000),
            new SymbolSpec("Scatter", SymbolKind.Scatter, PrimitiveType.Sphere, new Color(0.15f, 0.85f, 0.4f), 2, 2, 10, 50),
        };

        // Classic 10-line layout for a 5x3 grid. Row 0 is the top.
        private static readonly int[][] PaylineRows =
        {
            new[] { 1, 1, 1, 1, 1 },
            new[] { 0, 0, 0, 0, 0 },
            new[] { 2, 2, 2, 2, 2 },
            new[] { 0, 1, 2, 1, 0 },
            new[] { 2, 1, 0, 1, 2 },
            new[] { 0, 0, 1, 2, 2 },
            new[] { 2, 2, 1, 0, 0 },
            new[] { 1, 0, 0, 0, 1 },
            new[] { 1, 2, 2, 2, 1 },
            new[] { 1, 0, 1, 2, 1 },
        };

        [MenuItem("Tools/Slot Template/Build Sample Scene")]
        public static void Build()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog("Slot Template",
                    "TextMeshPro essentials are not imported yet.\n\n" +
                    "Use Window > TextMeshPro > Import TMP Essential Resources, then run this again.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(GeneratedFolder);
            EnsureFolder(SymbolsFolder);
            EnsureFolder(MaterialsFolder);
            EnsureFolder(ScenesFolder);

            var symbols = CreateSymbols();
            var config = CreateConfig(symbols);
            var animation = CreateAsset<ReelAnimationSettings>(GeneratedFolder + "/ReelAnimationSettings.asset");
            var symbolPrefab = CreateSymbolPrefab();

            BuildScene(config, animation, symbolPrefab);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Slot Template] Sample scene built at {ScenePath}. Press Play to spin.");
        }

        // ---------- Assets ----------

        private static List<SymbolDefinition> CreateSymbols()
        {
            var symbols = new List<SymbolDefinition>();

            foreach (var spec in Specs)
            {
                var material = CreateMaterial(MaterialsFolder + $"/{spec.Name}.mat", spec.Color, spec.Kind == SymbolKind.Wild);
                var symbol = CreateAsset<SymbolDefinition>(SymbolsFolder + $"/{spec.Name}.asset");

                var payouts = new SymbolDefinition.Payout[spec.Pays.Length];
                for (int i = 0; i < spec.Pays.Length; i++)
                    payouts[i] = new SymbolDefinition.Payout { count = i + 3, multiplier = spec.Pays[i] };

                symbol.EditorSetup(spec.Name, spec.Kind, GetPrimitiveMesh(spec.Shape), material, payouts);
                EditorUtility.SetDirty(symbol);
                symbols.Add(symbol);
            }

            return symbols;
        }

        private static SlotMachineConfig CreateConfig(List<SymbolDefinition> symbols)
        {
            var config = CreateAsset<SlotMachineConfig>(GeneratedFolder + "/SlotMachineConfig.asset");

            var reels = new List<SlotMachineConfig.ReelStripData>();
            for (int reel = 0; reel < ReelCount; reel++)
            {
                var strip = new List<SymbolDefinition>();
                for (int s = 0; s < Specs.Length; s++)
                for (int w = 0; w < Specs[s].Weight; w++)
                    strip.Add(symbols[s]);

                Shuffle(strip, new System.Random(1000 + reel));
                reels.Add(new SlotMachineConfig.ReelStripData { symbols = strip });
            }

            var paylines = new List<SlotMachineConfig.PaylineData>();
            for (int i = 0; i < PaylineRows.Length; i++)
            {
                paylines.Add(new SlotMachineConfig.PaylineData
                {
                    rows = (int[])PaylineRows[i].Clone(),
                    color = Color.HSVToRGB((float)i / PaylineRows.Length, 0.8f, 1f),
                });
            }

            config.EditorSetup(RowCount, symbols, reels, paylines, new long[] { 1, 2, 5, 10, 25 }, 1000);
            EditorUtility.SetDirty(config);
            return config;
        }

        private static SymbolView CreateSymbolPrefab()
        {
            var root = new GameObject("SymbolView");
            var view = root.AddComponent<SymbolView>();

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.6f;
            visual.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

            var so = new SerializedObject(view);
            so.FindProperty("meshFilter").objectReferenceValue = visual.GetComponent<MeshFilter>();
            so.FindProperty("meshRenderer").objectReferenceValue = visual.GetComponent<MeshRenderer>();
            so.FindProperty("idleRotationSpeed").floatValue = 30f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, GeneratedFolder + "/SymbolView.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<SymbolView>();
        }

        // ---------- Scene ----------

        private static void BuildScene(SlotMachineConfig config, ReelAnimationSettings animation, SymbolView symbolPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 0f, -9f);
                camera.transform.rotation = Quaternion.identity;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.06f, 0.05f, 0.12f);
            }

            var machine = new GameObject("SlotMachine");
            machine.transform.position = new Vector3(0f, 0.6f, 0f);

            BuildCabinet(machine.transform, animation);

            var reelsRoot = new GameObject("Reels");
            reelsRoot.transform.SetParent(machine.transform, false);
            var reelsPresenter = reelsRoot.AddComponent<ReelsPresenter>();

            var reelViews = new List<Object>();
            for (int i = 0; i < ReelCount; i++)
            {
                var reel = new GameObject($"Reel_{i}");
                reel.transform.SetParent(reelsRoot.transform, false);
                reel.transform.localPosition = new Vector3((i - (ReelCount - 1) * 0.5f) * ReelSpacing, 0f, 0f);
                var reelView = reel.AddComponent<ReelView>();
                SetReference(reelView, "symbolPrefab", symbolPrefab);
                reelViews.Add(reelView);
            }

            SetReferenceList(reelsPresenter, "reels", reelViews);
            SetReference(reelsPresenter, "settings", animation);

            var winLine = new GameObject("WinLine");
            winLine.transform.SetParent(machine.transform, false);
            var lineRenderer = winLine.AddComponent<LineRenderer>();
            lineRenderer.widthMultiplier = 0.08f;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
            var lineShader = Shader.Find("Sprites/Default");
            if (lineShader != null) lineRenderer.sharedMaterial = SaveMaterial(new Material(lineShader), MaterialsFolder + "/WinLine.mat");

            var winPresenter = winLine.AddComponent<WinPresenter>();
            SetReference(winPresenter, "reels", reelsPresenter);
            SetReference(winPresenter, "config", config);
            SetReference(winPresenter, "lineRenderer", lineRenderer);

            var hud = BuildHud();

            var game = new GameObject("Game");
            var controller = game.AddComponent<SlotGameController>();
            SetReference(controller, "config", config);
            SetReference(controller, "reels", reelsPresenter);
            SetReference(controller, "wins", winPresenter);
            SetReference(controller, "hud", hud);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
        }

        private static void BuildCabinet(Transform parent, ReelAnimationSettings animation)
        {
            var frameMaterial = CreateMaterial(MaterialsFolder + "/Frame.mat", new Color(0.35f, 0.08f, 0.12f), false);
            var backMaterial = CreateMaterial(MaterialsFolder + "/Back.mat", new Color(0.1f, 0.1f, 0.16f), false);

            float windowWidth = ReelCount * ReelSpacing;
            float windowHeight = RowCount * animation.symbolSpacing;
            const float border = 0.5f;

            var cabinet = new GameObject("Cabinet").transform;
            cabinet.SetParent(parent, false);

            CreateBlock(cabinet, "Back", backMaterial, new Vector3(0f, 0f, 0.8f), new Vector3(windowWidth, windowHeight, 0.1f));
            CreateBlock(cabinet, "Top", frameMaterial, new Vector3(0f, (windowHeight + border) * 0.5f, 0f), new Vector3(windowWidth + border * 2f, border, 1.6f));
            CreateBlock(cabinet, "Bottom", frameMaterial, new Vector3(0f, -(windowHeight + border) * 0.5f, 0f), new Vector3(windowWidth + border * 2f, border, 1.6f));
            CreateBlock(cabinet, "Left", frameMaterial, new Vector3(-(windowWidth + border) * 0.5f, 0f, 0f), new Vector3(border, windowHeight, 1.6f));
            CreateBlock(cabinet, "Right", frameMaterial, new Vector3((windowWidth + border) * 0.5f, 0f, 0f), new Vector3(border, windowHeight, 1.6f));
        }

        private static SlotHud BuildHud()
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var hud = canvasGo.AddComponent<SlotHud>();

            var resources = new TMP_DefaultControls.Resources();
            var balance = CreateLabel(canvasGo.transform, resources, "Balance", new Vector2(0f, 0f), new Vector2(180f, 90f));
            var bet = CreateLabel(canvasGo.transform, resources, "Bet", new Vector2(0.38f, 0f), new Vector2(0f, 90f));
            var win = CreateLabel(canvasGo.transform, resources, "Win", new Vector2(0.62f, 0f), new Vector2(0f, 90f));
            var message = CreateLabel(canvasGo.transform, resources, "Message", new Vector2(0.5f, 1f), new Vector2(0f, -80f));
            message.color = new Color(1f, 0.85f, 0.3f);

            var betDown = CreateButton(canvasGo.transform, resources, "BetDown", "-", new Vector2(0.38f, 0f), new Vector2(-150f, 90f), new Vector2(80f, 80f));
            var betUp = CreateButton(canvasGo.transform, resources, "BetUp", "+", new Vector2(0.38f, 0f), new Vector2(150f, 90f), new Vector2(80f, 80f));
            var spin = CreateButton(canvasGo.transform, resources, "Spin", "SPIN", new Vector2(1f, 0f), new Vector2(-200f, 100f), new Vector2(260f, 120f));
            spin.GetComponent<Image>().color = new Color(0.2f, 0.75f, 0.3f);

            SetReference(hud, "spinButton", spin);
            SetReference(hud, "betUpButton", betUp);
            SetReference(hud, "betDownButton", betDown);
            SetReference(hud, "balanceLabel", balance);
            SetReference(hud, "betLabel", bet);
            SetReference(hud, "winLabel", win);
            SetReference(hud, "messageLabel", message);
            SetReference(hud, "spinButtonLabel", spin.GetComponentInChildren<TMP_Text>());

            CreateEventSystem();
            return hud;
        }

        private static TMP_Text CreateLabel(Transform parent, TMP_DefaultControls.Resources resources, string name, Vector2 anchor, Vector2 position)
        {
            var go = TMP_DefaultControls.CreateText(resources);
            go.name = name;
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(320f, 110f);

            var text = go.GetComponent<TMP_Text>();
            text.text = name.ToUpperInvariant();
            text.fontSize = 40f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return text;
        }

        private static Button CreateButton(Transform parent, TMP_DefaultControls.Resources resources, string name, string label,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = TMP_DefaultControls.CreateButton(resources);
            go.name = name;
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = go.GetComponentInChildren<TMP_Text>();
            text.text = label;
            text.fontSize = 44f;
            text.color = Color.black;
            return go.GetComponent<Button>();
        }

        private static void CreateEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));

            // Prefer the Input System UI module when that package is active; fall back to the legacy module.
            var inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null) go.AddComponent(inputSystemModule);
            else go.AddComponent<StandaloneInputModule>();
        }

        // ---------- Helpers ----------

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Material CreateMaterial(string path, Color color, bool emissive)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);

            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.6f);
            }

            return SaveMaterial(material, path);
        }

        private static Material SaveMaterial(Material material, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, path);
                return material;
            }

            existing.shader = material.shader;
            existing.CopyPropertiesFromMaterial(material);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(material);
            return existing;
        }

        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            var temp = GameObject.CreatePrimitive(type);
            var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }

        private static void CreateBlock(Transform parent, string name, Material material, Vector3 position, Vector3 scale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null) throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'.");

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReferenceList(Object target, string field, List<Object> values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;

            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void Shuffle<T>(IList<T> list, System.Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
