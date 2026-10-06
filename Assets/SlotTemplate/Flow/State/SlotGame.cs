using System;

namespace SlotTemplate.Flow.State
{
    /// <summary>
    /// The game's state machine. It only controls input and recovery. Any transition not listed in
    /// <see cref="CanEnter"/> throws, so a bug shows up immediately instead of corrupting the round.
    /// </summary>
    public sealed class SlotGame
    {
        public GameState Current { get; private set; } = GameState.Boot;

        /// <summary>Raised with (from, to) after every transition.</summary>
        public event Action<GameState, GameState> StateChanged;

        public bool CanEnter(GameState next)
        {
            switch (Current)
            {
                case GameState.Boot: return next == GameState.Idle || next == GameState.InRound;
                case GameState.Idle: return next == GameState.InRound;
                case GameState.InRound: return next == GameState.Idle || next == GameState.InFeature;
                case GameState.InFeature: return next == GameState.Idle;
                default: return false;
            }
        }

        public void Enter(GameState next)
        {
            if (!CanEnter(next))
                throw new InvalidOperationException($"Illegal game state transition {Current} -> {next}.");

            var previous = Current;
            Current = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}
