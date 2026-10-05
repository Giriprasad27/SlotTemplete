# Slot Template

A Unity 3D slot game template built to the [Slot Template Architecture](https://claude.ai/artifact/GduUTR5FxTqj84jshxe8Vj) blueprint: game math in plain C#, one readable async round, features as plug-ins, four checked game states and crash-safe saves.

## Getting started

1. Open the folder in **Unity 6.3 LTS** (6000.3.x). Unity downloads [UniTask](https://github.com/Cysharp/UniTask) from Git on first open, so Git must be installed.
2. Import TextMeshPro essentials: **Window > TextMeshPro > Import TMP Essential Resources**.
3. Run **Tools > Slot Template > Build Sample Game**. This creates `Assets/Games/ClassicFruits/` (a 5x3, 10-line game with free spins) and its scene.
4. Press **Play** and **SPIN**. Press again while spinning to quick stop; **AUTO** starts autoplay. In the editor and development builds a **Cheats** button can force free spins or a big win.

Tools in the **Tools > Slot Template** menu:

| Menu | What it does |
| --- | --- |
| RTP Simulator | Plays millions of spins (base game and features) without a scene and reports RTP, hit rate, feature frequency and volatility. |
| Validate Selected Definition | Checks a `SlotDefinition` for unknown symbols, bad paylines, missing reel sets or skin entries. |
| Force Next Spin | Play mode shortcuts for the cheat menu. |

Run tests from **Window > General > Test Runner > EditMode**.

## Layout

```
Assets/
  SlotTemplate/                    the reusable template (later a UPM package)
    Core/          Slot.Core          pure C#, no UnityEngine: definition data, SpinEngine + ISpinRule, SpinResult, seeded PRNG, RTP simulator
    Flow/          Slot.Flow          -> Core: SlotGame states, RoundRunner, RoundJournal (save), wallet, bet, autoplay, IOutcomeProvider,
                                         and the interfaces the layers above implement (IReelPresenter, IWinPresenter, IHud, ISlotFeature)
    Features/      Slot.Features      -> Flow, Core: one folder per feature (FreeSpins: settings, rule, feature, presenter)
    Presentation/  Slot.Presentation  -> Flow, Core: 3D reels, win presenter, HUD, symbol skins
    Bootstrap/     Slot.Bootstrap     -> all: SlotDefinition asset and SlotBootstrap, the composition root
    Debug/         Slot.Debug         editor and development builds only: forced outcomes, cheat menu
    Editor/        Slot.Editor        RTP simulator window, validator, sample game builder
    Tests/EditMode                    Core, feature rule and round flow tests
  Games/
    ClassicFruits/                    generated content only: SlotDefinition, skin, materials, prefabs, scene
```

References only point down, so the compiler rejects, for example, Core using Unity or Flow using a specific feature.

## One round

`RoundRunner.PlayRound()` reads top to bottom:

1. Enter **InRound** before any `await`, so a second tap can't start another round.
2. `RoundJournal.BeginRound` takes the bet and saves it in the same step.
3. `IOutcomeProvider.Spin` decides the result. If it throws, the bet is refunded.
4. `RecordResult` saves the reel stops, so the exact result can be replayed after a crash.
5. Reels spin to the result, wins are shown, and `PayBase` credits and saves in one step.
6. Each triggered feature plays in **InFeature**. Feature spins reuse the same inner step (spin, show, return) without taking a bet. `RoundContext.Pay` credits and saves the feature's progress together.
7. `CompleteRound` clears the save. A `finally` block always returns to **Idle**.

Quick stop raises a `SkipSignal`: presenters jump to their end state, but the round is never cancelled, so money and save stay consistent. On launch, `BootAsync` finishes any saved round: a bet with no result is refunded, a saved result is shown and paid, and a half-played feature resumes where it stopped. Nothing is paid twice.

## Adding a feature

1. Create `Features/MyFeature/`.
2. Put its tunables in a plain `[Serializable]` settings class.
3. `MyFeatureRule : ISpinRule` detects the trigger and writes `MyFeatureResult` with `result.Set(...)`. Implement `IFeatureSimulation` too so the RTP simulator can play it.
4. `MyFeature : ISlotFeature` plays it: use `context.SpinAsync` for extra spins and `context.Pay(amount, state)` for wins.
5. `MyFeatureConfig : SlotFeatureConfig` creates both, plus any presenter prefab.
6. Add the config to the game's `SlotDefinition` features list, run the RTP simulator and add a rule test.

If a step makes you edit `RoundRunner`, `SpinResult` or `SpinEngine`, fix the extension point instead.

## Customising a game

| To change | Edit |
| --- | --- |
| Symbols, pays, reel strips, reel sets, paylines | `SlotDefinition` > Math. Reel strips are comma-separated symbol ids. |
| Bets, starting balance, autoplay limits, fixed seed | `SlotDefinition` |
| Symbol meshes and materials | `SymbolSkin` |
| Spin speed, stop timing, bounce, win display | `ReelAnimationSettings` |
| Where results come from (e.g. a server) | Implement `IOutcomeProvider` and pass it in `SlotBootstrap` |

Credits are whole numbers (`long`). `SeededRandom` is for play money and simulation; real-money games need a certified RNG behind `IOutcomeProvider`.
