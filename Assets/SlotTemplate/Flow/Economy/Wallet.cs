using System;

namespace SlotTemplate.Flow.Economy
{
    /// <summary>
    /// Player balance in credits. Whole numbers only: treat 1 credit as the smallest currency unit.
    /// Only <see cref="Save.RoundJournal"/> should move money during a round, so the save always matches.
    /// </summary>
    public sealed class Wallet
    {
        public long Balance { get; private set; }

        /// <summary>Raised with the new balance whenever it changes.</summary>
        public event Action<long> BalanceChanged;

        public Wallet(long startingBalance)
        {
            if (startingBalance < 0) throw new ArgumentOutOfRangeException(nameof(startingBalance));
            Balance = startingBalance;
        }

        public bool CanAfford(long amount) => amount >= 0 && Balance >= amount;

        public bool TryDebit(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Balance < amount) return false;

            Balance -= amount;
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public void Credit(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return;

            Balance += amount;
            BalanceChanged?.Invoke(Balance);
        }
    }
}
