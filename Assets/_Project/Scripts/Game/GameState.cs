namespace SlotTemplate.Game
{
    public enum GameState
    {
        /// <summary>Waiting for the player. Wins from the last spin may still be on display.</summary>
        Idle,

        /// <summary>Reels are moving; pressing spin again slam-stops them.</summary>
        Spinning,
    }
}
