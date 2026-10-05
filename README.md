# Slot Template

A Unity 3D slot machine foundation with the game math kept separate from everything Unity draws.

## Getting started

1. Open the folder in **Unity 6.3 LTS** (6000.3.x) from Unity Hub. 6.3 is the minimum for Unity AI (Assistant and Generators); the code itself also runs on 6.0+.
2. Import TextMeshPro essentials: **Window > TextMeshPro > Import TMP Essential Resources**.
3. Run **Tools > Slot Template > Build Sample Scene**. This generates symbols, a 5x3 / 10-line machine config and `Assets/_Project/Scenes/SlotSample.unity`.
4. Press **Play**, then **SPIN** (or Space with the legacy input manager). Press again while spinning to slam-stop.

Run the logic tests from **Window > General > Test Runner > EditMode**.

## Structure

```
Assets/_Project/
  Scripts/
    Core/          SlotTemplate.Core          Pure C#, no UnityEngine. Rules, RNG, spin, win evaluation, wallet, bets.
    Data/          SlotTemplate.Data          ScriptableObjects designers edit, and the factory that turns them into Core rules.
    Presentation/  SlotTemplate.Presentation  3D reels, symbol views, win highlighting and payline drawing.
    UI/            SlotTemplate.UI            Passive HUD (buttons and labels). Raises events, holds no state.
    Game/          SlotTemplate.Game          SlotGameController: composition root and spin flow.
    Editor/        SlotTemplate.Editor        Sample scene builder.
  Tests/EditMode/  Unit tests for Core.
```

Dependencies only point one way: `Game -> Presentation/UI/Data -> Core`. Core can be unit tested, or moved to a server for real-money play, without touching any Unity code.

### Spin flow

1. `SlotHud` raises `SpinPressed`.
2. `SlotGameController` calls `SlotMachine.TrySpin`, which debits the bet, picks reel stops with the `IRandomNumberGenerator` and evaluates wins with an `IWinEvaluator`.
3. `ReelsPresenter` spins the `ReelView`s and lands them on the outcome's stops, left to right.
4. When every reel has settled, the controller calls `SlotMachine.CollectWin`, updates the HUD and hands the outcome to `WinPresenter`.

The result is decided before the reels move; the animation only shows it.

## Customising

| To change | Edit |
| --- | --- |
| Symbols, pays, art | `SymbolDefinition` assets (mesh + material per symbol) |
| Reel strips, rows, paylines, bet levels, starting balance | `SlotMachineConfig` asset. Invalid configs are reported in the console. |
| Spin speed, stop timing, bounce, win display time | `ReelAnimationSettings` asset |
| Win rules (ways, cluster pays, multipliers) | Implement `IWinEvaluator` and pass it to `SlotMachine` |
| RNG (certified, server-side, scripted) | Implement `IRandomNumberGenerator` |
| Sounds, effects, analytics | Subscribe to `SlotGameController.SpinStarted` / `SpinCompleted` and `ReelsPresenter.ReelStopped` |

### Pay rules

- Line wins pay left to right on active paylines; payout is `multiplier x line bet`.
- Wilds substitute for regular symbols. A run of leading wilds can also pay as wilds if that is worth more.
- Scatters pay anywhere on the grid; payout is `multiplier x total bet`. A scatter breaks a payline run.
- Each payline pays only its best combination.
- Credits are whole numbers (`long`) to avoid floating point rounding.
