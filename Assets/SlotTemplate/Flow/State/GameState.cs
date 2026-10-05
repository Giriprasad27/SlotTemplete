namespace SlotTemplate.Flow.State
{
    /// <summary>
    /// The only states the game has. Steps inside a round are not states: <see cref="Round.RoundRunner"/>
    /// already orders them.
    /// </summary>
    public enum GameState
    {
        /// <summary>Starting up; checking the save for an unfinished round.</summary>
        Boot,

        /// <summary>Waiting for the player. The only state where a spin can start.</summary>
        Idle,

        /// <summary>A paid spin is in progress.</summary>
        InRound,

        /// <summary>A triggered feature (free spins, bonus) is playing.</summary>
        InFeature,
    }
}
