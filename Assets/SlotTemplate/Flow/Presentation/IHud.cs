using System;
using SlotTemplate.Flow.State;

namespace SlotTemplate.Flow.Presentation
{
    /// <summary>Passive view for controls and readouts. Raises input, displays what it is told.</summary>
    public interface IHud
    {
        event Action SpinPressed;
        event Action BetUpPressed;
        event Action BetDownPressed;
        event Action AutoplayPressed;

        void SetBalance(long credits);
        void SetBet(long credits);
        void SetWin(long credits);
        void SetState(GameState state, bool canChangeBet);
        void SetAutoplay(bool running);
        void ShowMessage(string message);
    }
}
