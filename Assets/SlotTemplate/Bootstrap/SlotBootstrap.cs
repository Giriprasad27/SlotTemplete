using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Random;
using SlotTemplate.Core.Spin;
using SlotTemplate.Features;
using SlotTemplate.Features.FreeSpins;
using SlotTemplate.Flow.Economy;
using SlotTemplate.Flow.Features;
using SlotTemplate.Flow.Outcome;
using SlotTemplate.Flow.Round;
using SlotTemplate.Flow.Save;
using SlotTemplate.Flow.State;
using SlotTemplate.Presentation.Hud;
using SlotTemplate.Presentation.Reels;
using SlotTemplate.Presentation.Wins;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using SlotTemplate.DebugTools;
#endif

namespace SlotTemplate.Bootstrap
{
    /// <summary>
    /// Composition root: reads the SlotDefinition, creates every object and hands each one what it needs.
    /// This is the only class that knows about every layer. No singletons, no container.
    /// </summary>
    public sealed class SlotBootstrap : MonoBehaviour
    {
        [SerializeField] private SlotDefinition definition;
        [SerializeField] private ReelsPresenter reels;
        [SerializeField] private WinPresenter wins;
        [SerializeField] private SlotHud hud;
        [Tooltip("Parent for feature UI such as the free spins banner.")]
        [SerializeField] private Transform featureUiRoot;
        [Tooltip("Editor only: wipe the save on Play for a fresh balance each time.")]
        [SerializeField] private bool resetSaveInEditor;

        private HudBinder _hudBinder;

        public SlotGame Game { get; private set; }
        public RoundRunner Runner { get; private set; }
        public RoundJournal Journal { get; private set; }

        private void Start()
        {
            var errors = definition.Validate();
            if (errors.Count > 0)
            {
                Debug.LogError($"[Slot] '{definition.name}' is invalid:\n- " + string.Join("\n- ", errors), definition);
                enabled = false;
                return;
            }

            var math = definition.BuildMath();
            var featureRules = new List<ISpinRule>();
            var rules = definition.BuildRules(math, featureRules);
            var rng = definition.UseFixedSeed ? new SeededRandom(definition.Seed) : new SeededRandom();
            var engine = new SpinEngine(math, rng, rules);

            IOutcomeProvider outcome = new LocalOutcomeProvider(engine);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var forced = new ForcedOutcomeProvider(outcome, engine);
            outcome = forced;
#endif

            var store = new PlayerPrefsSaveStore(definition.SaveKey);
#if UNITY_EDITOR
            if (resetSaveInEditor) store.Delete();
#endif
            Journal = new RoundJournal(store, definition.StartingBalance, definition.DefaultBetIndex);
            var bet = new BetModel(definition.LineBets, math.Paylines.Count, Journal.SavedBetLevel);

            var buildContext = new FeatureBuildContext(featureUiRoot != null ? featureUiRoot : transform);
            var features = new List<ISlotFeature>();
            for (int i = 0, ruleIndex = 0; i < definition.Features.Count; i++)
            {
                var config = definition.Features[i];
                if (config == null) continue;
                features.Add(config.CreateFeature(featureRules[ruleIndex++], buildContext));
            }

            reels.Initialize(math, definition.Skin, SpinRequest.BaseReelSet);
            wins.Initialize(math);

            Game = new SlotGame();
            Runner = new RoundRunner(Game, outcome, Journal, bet, reels, wins, features);
            var autoplay = new AutoplayController(Runner, Journal.Wallet);
            _hudBinder = new HudBinder(hud, Game, Runner, autoplay, definition.Autoplay, Journal, bet, this.GetCancellationTokenOnDestroy());

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var cheats = GetComponent<DebugCheatMenu>();
            if (cheats == null) cheats = gameObject.AddComponent<DebugCheatMenu>();
            cheats.Initialize(forced, () => new SpinRequest(bet.LineBet, bet.LineCount));
            cheats.AddCheat("Force free spins", r => r.Has<FreeSpinsResult>());
#endif

            Runner.BootAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnDestroy() => _hudBinder?.Dispose();
    }
}
