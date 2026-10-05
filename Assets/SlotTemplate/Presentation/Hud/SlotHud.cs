using System;
using SlotTemplate.Flow.Presentation;
using SlotTemplate.Flow.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotTemplate.Presentation.Hud
{
    /// <summary>Buttons and labels. Raises input and displays values; holds no game state.</summary>
    public sealed class SlotHud : MonoBehaviour, IHud
    {
        [Header("Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button betUpButton;
        [SerializeField] private Button betDownButton;
        [SerializeField] private Button autoplayButton;

        [Header("Labels")]
        [SerializeField] private TMP_Text balanceLabel;
        [SerializeField] private TMP_Text betLabel;
        [SerializeField] private TMP_Text winLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text spinButtonLabel;
        [SerializeField] private TMP_Text autoplayButtonLabel;

        [Header("Text")]
        [SerializeField] private string spinText = "SPIN";
        [SerializeField] private string stopText = "STOP";
        [SerializeField] private float messageDuration = 2f;

        private float _messageHideTime;

        public event Action SpinPressed;
        public event Action BetUpPressed;
        public event Action BetDownPressed;
        public event Action AutoplayPressed;

        private void Awake()
        {
            if (spinButton != null) spinButton.onClick.AddListener(() => SpinPressed?.Invoke());
            if (betUpButton != null) betUpButton.onClick.AddListener(() => BetUpPressed?.Invoke());
            if (betDownButton != null) betDownButton.onClick.AddListener(() => BetDownPressed?.Invoke());
            if (autoplayButton != null) autoplayButton.onClick.AddListener(() => AutoplayPressed?.Invoke());
            ShowMessage(string.Empty);
        }

        private void Update()
        {
            if (messageLabel != null && messageLabel.gameObject.activeSelf && Time.time >= _messageHideTime)
                messageLabel.gameObject.SetActive(false);

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space)) SpinPressed?.Invoke();
#endif
        }

        public void SetBalance(long credits) => SetText(balanceLabel, $"BALANCE\n{credits:N0}");
        public void SetBet(long credits) => SetText(betLabel, $"BET\n{credits:N0}");
        public void SetWin(long credits) => SetText(winLabel, credits > 0 ? $"WIN\n{credits:N0}" : "WIN\n-");

        public void SetState(GameState state, bool canChangeBet)
        {
            bool busy = state == GameState.InRound || state == GameState.InFeature;
            SetText(spinButtonLabel, busy ? stopText : spinText);
            if (spinButton != null) spinButton.interactable = state != GameState.Boot;
            if (betUpButton != null) betUpButton.interactable = canChangeBet;
            if (betDownButton != null) betDownButton.interactable = canChangeBet;
        }

        public void SetAutoplay(bool running) => SetText(autoplayButtonLabel, running ? "STOP\nAUTO" : "AUTO");

        public void ShowMessage(string message)
        {
            if (messageLabel == null) return;

            messageLabel.text = message;
            messageLabel.gameObject.SetActive(!string.IsNullOrEmpty(message));
            _messageHideTime = Time.time + messageDuration;
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
