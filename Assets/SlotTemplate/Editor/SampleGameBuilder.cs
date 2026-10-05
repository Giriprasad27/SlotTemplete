using System;
using System.Collections.Generic;
using SlotTemplate.Bootstrap;
using SlotTemplate.Core.Definition;
using SlotTemplate.Core.Model;
using SlotTemplate.Features;
using SlotTemplate.Features.FreeSpins;
using SlotTemplate.Presentation;
using SlotTemplate.Presentation.Hud;
using SlotTemplate.Presentation.Reels;
using SlotTemplate.Presentation.Skin;
using SlotTemplate.Presentation.Wins;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SlotTemplate.Editor
{
    /// <summary>
    /// Generates a playable 5x3, 10-line sample game with free spins under Assets/Games/ClassicFruits.
    /// Safe to run again; it updates the generated assets in place.
    /// </summary>
    public static class SampleGameBuilder
    {
        private const string GameFolder = "Assets/Games/ClassicFruits";
        private const string MaterialsFolder = GameFolder + "/Materials";
        private const string ScenePath = GameFolder + "/ClassicFruits.unity";

        private const int ReelCount = 5;
        private const int RowCount = 3;
        private const float ReelSpacing = 1.5f;

        private struct SymbolSpec
        {
            public string Id;
            public SymbolKind Kind;
            public PrimitiveType Shape;
            public Color Color;
            public int BaseWeight;
            public int FreeWeight;
            public long[] Pays;

            public SymbolSpec(string id, SymbolKind kind, PrimitiveType shape, Color color, int baseWeight, int freeWeight, params long[] pays)
            {
                Id = id;
                Kind = kind;
                Shape = shape;
                Color = color;
                BaseWeight = baseWeight;
                FreeWeight = freeWeight;
                Pays = pays;
            }
        }

        private static readonly SymbolSpec[] Specs =
        {
            new SymbolSpec("CH", SymbolKind.Regular, PrimitiveType.Sphere, new Color(0.9f, 0.1f, 0.15f), 7, 5, 0, 0, 5, 20, 50),
            new SymbolSpec("LE", SymbolKind.Regular, PrimitiveType.Capsule, new Color(1f, 0.9f, 0.2f), 7, 5, 0, 0, 5, 20, 50),
            new SymbolSpec("PL", SymbolKind.Regular, PrimitiveType.Sphere, new Color(0.55f, 0.2f, 0.75f), 6, 5, 0, 0, 10, 30, 75),
            new SymbolSpec("BE", SymbolKind.Regular, PrimitiveType.Cylinder, new Color(1f, 0.75f, 0.1f), 5, 4, 0, 0, 15, 40, 100),
            new SymbolSpec("BAR", SymbolKind.Regular, PrimitiveType.Cube, new Color(0.2f, 0.45f, 1f), 4, 4, 0, 0, 20, 60, 150),
            new SymbolSpec("SEV", SymbolKind.Regular, PrimitiveType.Cylinder, new Color(1f, 0.35f, 0.1f), 3, 3, 0, 2, 50, 150, 500),
            new SymbolSpec("WILD", SymbolKind.Wild, PrimitiveType.Cube, new Color(0.95f, 0.95f, 0.95f), 2, 4, 0, 0, 50, 200, 1000),
            new SymbolSpec("SC", SymbolKind.Scatter, PrimitiveType.Sphere, new Color(0.15f, 0.85f, 0.4f), 2, 1, 0, 0, 2, 10, 50),
        };

        // Classic 10-line layout for a 5x3 grid. Row 0 is the top.
        private static readonly int[][] PaylineRows =
        {
            new[] { 1, 1, 1, 1, 1 }, new[] { 0, 0, 0, 0, 0 }, new[] { 2, 2, 2, 2, 2 },
            new[] { 0, 1, 2, 1, 0 }, new[] { 2, 1, 0, 1, 2 }, new[] { 0, 0, 1, 2, 2 },
            new[] { 2, 2, 1, 0, 0 }, new[] { 1, 0, 0, 0, 1 }, new[] { 1, 2, 2, 2, 1 },
            new[] { 1, 0, 1, 2, 1 },
        };

        [MenuItem("Tools/Slot Template/Build Sample Game")]
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

            // Open the new scene before creating any assets: NewScene unloads unused assets, which would
            // drop the just-created settings and prefab and leave null references in the scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            EnsureFolder(GameFolder);
            EnsureFolder(MaterialsFolder);

            var skin = CreateSkin();
            var animation = CreateAsset<ReelAnimationSettings>(GameFolder + "/ReelAnimationSettings.asset");
            var freeSpinsPresenter = CreateFreeSpinsPresenterPrefab();
            var freeSpins = CreateAsset<FreeSpinsConfig>(GameFolder + "/FreeSpinsConfig.asset");
            freeSpins.EditorSetup(new FreeSpinsSettings(), freeSpinsPresenter);
            EditorUtility.SetDirty(freeSpins);

            var definition = CreateAsset<SlotDefinition>(GameFolder + "/ClassicFruits.asset");
            definition.EditorSetup(CreateMath(), new long[] { 1, 2, 5, 10, 25 }, 1000,
                new List<SlotFeatureConfig> { freeSpins }, skin, "slot.classicfruits");
            EditorUtility.SetDirty(definition);

            var symbolPrefab = CreateSymbolPrefab();
            BuildScene(scene, definition, animation, symbolPrefab);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Slot Template] Sample game built at {ScenePath}. Press Play to spin.");
        }

        // ---------- Content ----------

        private static SlotDefinitionData CreateMath()
        {
            var data = new SlotDefinitionData { rows = RowCount };
            foreach (var spec in Specs) data.symbols.Add(new SymbolData(spec.Id, spec.Kind, spec.Pays));

            data.reelSets.Add(CreateReelSet("base", s => s.BaseWeight, 1000));
            data.reelSets.Add(CreateReelSet("free", s => s.FreeWeight, 2000));

            foreach (var rows in PaylineRows) data.paylines.Add(new PaylineData((int[])rows.Clone()));
            return data;
        }

        private static ReelSetData CreateReelSet(string id, Func<SymbolSpec, int> weight, int seed)
        {
            var set = new ReelSetData { id = id };
            for (int reel = 0; reel < ReelCount; reel++)
            {
                var strip = new List<string>();
                foreach (var spec in Specs)
                for (int w = 0; w < weight(spec); w++)
                    strip.Add(spec.Id);

                Shuffle(strip, new System.Random(seed + reel));
                set.reels.Add(string.Join(",", strip));
            }
            return set;
        }

        private static SymbolSkin CreateSkin()
        {
            var entries = new List<SymbolSkin.Entry>();
            foreach (var spec in Specs)
            {
                entries.Add(new SymbolSkin.Entry
                {
                    symbolId = spec.Id,
                    mesh = GetPrimitiveMesh(spec.Shape),
                    material = CreateMaterial(MaterialsFolder + $"/{spec.Id}.mat", spec.Color, spec.Kind != SymbolKind.Regular),
                });
            }

            var skin = CreateAsset<SymbolSkin>(GameFolder + "/SymbolSkin.asset");
            skin.EditorSetup(entries);
            EditorUtility.SetDirty(skin);
            return skin;
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

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, GameFolder + "/SymbolView.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<SymbolView>();
        }

        private static FreeSpinsPresenter CreateFreeSpinsPresenterPrefab()
        {
            var root = new GameObject("FreeSpinsPresenter", typeof(RectTransform));
            Stretch((RectTransform)root.transform);
            var presenter = root.AddComponent<FreeSpinsPresenter>();
            var resources = new TMP_DefaultControls.Resources();

            var banner = new GameObject("Banner", typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(root.transform, false);
            var bannerRect = (RectTransform)banner.transform;
            bannerRect.anchorMin = bannerRect.anchorMax = new Vector2(0.5f, 0.55f);
            bannerRect.sizeDelta = new Vector2(900f, 300f);
            banner.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
            var bannerLabel = CreateLabel(banner.transform, resources, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 280f), 72f);
            bannerLabel.color = new Color(1f, 0.85f, 0.3f);

            var counterLabel = CreateLabel(root.transform, resources, "Counter", new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(700f, 120f), 40f);

            var so = new SerializedObject(presenter);
            so.FindProperty("banner").objectReferenceValue = banner;
            so.FindProperty("bannerLabel").objectReferenceValue = bannerLabel;
            so.FindProperty("counter").objectReferenceValue = counterLabel.gameObject;
            so.FindProperty("counterLabel").objectReferenceValue = counterLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, GameFolder + "/FreeSpinsPresenter.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<FreeSpinsPresenter>();
        }

        // ---------- Scene ----------

        private static void BuildScene(Scene scene, SlotDefinition definition, ReelAnimationSettings animation, SymbolView symbolPrefab)
        {
            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -9f), Quaternion.identity);
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
            SetReference(winPresenter, "lineRenderer", lineRenderer);

            var hud = BuildHud(out var featureRoot);

            var bootstrap = new GameObject("SlotBootstrap").AddComponent<SlotBootstrap>();
            SetReference(bootstrap, "definition", definition);
            SetReference(bootstrap, "reels", reelsPresenter);
            SetReference(bootstrap, "wins", winPresenter);
            SetReference(bootstrap, "hud", hud);
            SetReference(bootstrap, "featureUiRoot", featureRoot);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
        }

        private static void BuildCabinet(Transform parent, ReelAnimationSettings animation)
        {
            var frameMaterial = CreateMaterial(MaterialsFolder + "/Frame.mat", new Color(0.35f, 0.08f, 0.12f), false);
            var backMaterial = CreateMaterial(MaterialsFolder + "/Back.mat", new Color(0.1f, 0.1f, 0.16f), false);

            float width = ReelCount * ReelSpacing;
            float height = RowCount * animation.symbolSpacing;
            const float border = 0.5f;

            var cabinet = new GameObject("Cabinet").transform;
            cabinet.SetParent(parent, false);
            CreateBlock(cabinet, "Back", backMaterial, new Vector3(0f, 0f, 0.8f), new Vector3(width, height, 0.1f));
            CreateBlock(cabinet, "Top", frameMaterial, new Vector3(0f, (height + border) * 0.5f, 0f), new Vector3(width + border * 2f, border, 1.6f));
            CreateBlock(cabinet, "Bottom", frameMaterial, new Vector3(0f, -(height + border) * 0.5f, 0f), new Vector3(width + border * 2f, border, 1.6f));
            CreateBlock(cabinet, "Left", frameMaterial, new Vector3(-(width + border) * 0.5f, 0f, 0f), new Vector3(border, height, 1.6f));
            CreateBlock(cabinet, "Right", frameMaterial, new Vector3((width + border) * 0.5f, 0f, 0f), new Vector3(border, height, 1.6f));
        }

        private static SlotHud BuildHud(out Transform featureRoot)
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var hud = canvasGo.AddComponent<SlotHud>();
            var resources = new TMP_DefaultControls.Resources();
            var t = canvasGo.transform;

            // Bottom bar, positioned by fractions of the screen width so it holds up on any aspect ratio.
            var balance = CreateLabel(t, resources, "Balance", new Vector2(0.12f, 0f), new Vector2(0f, 90f), new Vector2(260f, 110f), 40f);
            var bet = CreateLabel(t, resources, "Bet", new Vector2(0.34f, 0f), new Vector2(0f, 90f), new Vector2(180f, 110f), 40f);
            var win = CreateLabel(t, resources, "Win", new Vector2(0.55f, 0f), new Vector2(0f, 90f), new Vector2(260f, 110f), 40f);
            var message = CreateLabel(t, resources, "Message", new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(900f, 90f), 40f);
            message.color = new Color(1f, 0.85f, 0.3f);

            var betDown = CreateButton(t, resources, "BetDown", "-", new Vector2(0.34f, 0f), new Vector2(-130f, 90f), new Vector2(80f, 80f));
            var betUp = CreateButton(t, resources, "BetUp", "+", new Vector2(0.34f, 0f), new Vector2(130f, 90f), new Vector2(80f, 80f));
            var autoplay = CreateButton(t, resources, "Autoplay", "AUTO", new Vector2(0.73f, 0f), new Vector2(0f, 100f), new Vector2(160f, 100f));
            var spin = CreateButton(t, resources, "Spin", "SPIN", new Vector2(0.9f, 0f), new Vector2(0f, 100f), new Vector2(240f, 120f));
            spin.GetComponent<Image>().color = new Color(0.2f, 0.75f, 0.3f);

            SetReference(hud, "spinButton", spin);
            SetReference(hud, "betUpButton", betUp);
            SetReference(hud, "betDownButton", betDown);
            SetReference(hud, "autoplayButton", autoplay);
            SetReference(hud, "balanceLabel", balance);
            SetReference(hud, "betLabel", bet);
            SetReference(hud, "winLabel", win);
            SetReference(hud, "messageLabel", message);
            SetReference(hud, "spinButtonLabel", spin.GetComponentInChildren<TMP_Text>());
            SetReference(hud, "autoplayButtonLabel", autoplay.GetComponentInChildren<TMP_Text>());

            var features = new GameObject("Features", typeof(RectTransform));
            features.transform.SetParent(t, false);
            Stretch((RectTransform)features.transform);
            featureRoot = features.transform;

            CreateEventSystem();
            return hud;
        }

        private static TMP_Text CreateLabel(Transform parent, TMP_DefaultControls.Resources resources, string name,
            Vector2 anchor, Vector2 position, Vector2 size, float fontSize)
        {
            var go = TMP_DefaultControls.CreateText(resources);
            go.name = name;
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = go.GetComponent<TMP_Text>();
            text.text = name.ToUpperInvariant();
            text.fontSize = fontSize;
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
            text.fontSize = 36f;
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

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
            for (int i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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
