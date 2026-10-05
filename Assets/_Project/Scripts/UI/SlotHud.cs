using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotTemplate.UI
{
    /// <summary>
    /// Passive view for the machine's controls and readouts. It raises input events and displays
    /// whatever it is told; it holds no game state of its own.
    /// </summary>
    public sealed class SlotHud : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button betUpButton;
        [SerializeField] private Button betDownButton;

        [Header("Labels")]
        [SerializeField] private TMP_Text balanceLabel;
        [SerializeField] private TMP_Text betLabel;
        [SerializeField] private TMP_Text winLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text spinButtonLabel;

        [Header("Text")]
        [SerializeField] private string spinText = "SPIN";
        [SerializeField] private string stopText = "STOP";
        [SerializeField] private float messageDuration = 2f;

        private float _messageHideTime;

        public event Action SpinPressed;
        public event Action BetUpPressed;
        public event Action BetDownPressed;

        private void Awake()
        {
            if (spinButton != null) spinButton.onClick.AddListener(() => SpinPressed?.Invoke());
            if (betUpButton != null) betUpButton.onClick.AddListener(() => BetUpPressed?.Invoke());
            if (betDownButton != null) betDownButton.onClick.AddListener(() => BetDownPressed?.Invoke());
            ShowMessage(string.Empty);
        }

        private void Update()
        {
            if (messageLabel != null && messageLabel.gameObject.activeSelf && Time.time >= _messageHideTime)
                messageLabel.gameObject.SetActive(false);
        }

        public void SetBalance(long credits) => SetText(balanceLabel, $"BALANCE\n{credits:N0}");
        public void SetBet(long credits) => SetText(betLabel, $"BET\n{credits:N0}");
        public void SetWin(long credits) => SetText(winLabel, credits > 0 ? $"WIN\n{credits:N0}" : "WIN\n-");

        public void SetBetButtons(bool canDecrease, bool canIncrease)
        {
            if (betDownButton != null) betDownButton.interactable = canDecrease;
            if (betUpButton != null) betUpButton.interactable = canIncrease;
        }

        /// <summary>Switches the spin button between "spin" and "stop" (slam stop) modes.</summary>
        public void SetSpinning(bool spinning)
        {
            SetText(spinButtonLabel, spinning ? stopText : spinText);
        }

        public void ShowMessage(string message)
        {
            if (messageLabel == null) return;

            bool show = !string.IsNullOrEmpty(message);
            messageLabel.text = message;
            messageLabel.gameObject.SetActive(show);
            _messageHideTime = Time.time + messageDuration;
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
