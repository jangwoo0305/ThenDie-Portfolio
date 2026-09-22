using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThenDie.Ingame
{
    public class DetectiveHUD : MonoBehaviour
    {
        [SerializeField] private Button killButton;
        [SerializeField] private Button revealButton;
        [SerializeField] private GameObject killHintText;
        [SerializeField] private GameObject revealHintText;
        [SerializeField] private int startingAbilityChargeCount;
        [SerializeField] private GameObject chargeBadge;
        [SerializeField] private TMP_Text chargeText;
        [SerializeField] private TMP_Text revealResultText;
        [SerializeField, Min(1f)] private float resultDisplayDuration = 7f;

        private int abilityChargeCount;
        private bool abilitiesAvailable;
        private Coroutine hideResultCoroutine;

        public event Action DetectiveKillRequested;
        public event Action DetectiveRevealRequested;

        private void Awake()
        {
            ResetDetectiveAbilityState();
        }

        private void OnEnable()
        {
            if (killButton != null)
            {
                killButton.onClick.RemoveListener(UseDetectiveKillButton);
                killButton.onClick.AddListener(UseDetectiveKillButton);
            }

            if (revealButton != null)
            {
                revealButton.onClick.RemoveListener(UseDetectiveRevealButton);
                revealButton.onClick.AddListener(UseDetectiveRevealButton);
            }
        }

        private void OnDisable()
        {
            if (killButton != null)
                killButton.onClick.RemoveListener(UseDetectiveKillButton);
            if (revealButton != null)
                revealButton.onClick.RemoveListener(UseDetectiveRevealButton);
        }

        public void UseDetectiveKillButton()
        {
            if (!abilitiesAvailable || abilityChargeCount <= 0)
                return;

            DetectiveKillRequested?.Invoke();
        }

        public void UseDetectiveRevealButton()
        {
            if (!abilitiesAvailable || abilityChargeCount <= 0)
                return;

            DetectiveRevealRequested?.Invoke();
        }

        public void SetDetectiveAbilityVisible(bool visible)
        {
            gameObject.SetActive(visible);

            if (chargeBadge != null)
                chargeBadge.SetActive(visible);

            if (!visible)
            {
                SetDetectiveAbilityAvailable(false);
                if (revealResultText != null)
                    revealResultText.gameObject.SetActive(false);
            }
        }

        public void SetDetectiveAbilityAvailable(bool available)
        {
            abilitiesAvailable = available;
            UpdateAbilityButtons();
        }

        public void SetDetectiveAbilityChargeCount(int count)
        {
            abilityChargeCount = Mathf.Max(0, count);
            UpdateAbilityButtons();
        }

        public void AddDetectiveAbilityCharge(int amount = 1)
        {
            abilityChargeCount = Mathf.Max(0, abilityChargeCount + amount);
            UpdateAbilityButtons();
        }

        public void SpendDetectiveAbilityCharge()
        {
            SetDetectiveAbilityChargeCount(abilityChargeCount - 1);
        }

        public void ResetDetectiveAbilityState()
        {
            abilityChargeCount = Mathf.Max(0, startingAbilityChargeCount);
            abilitiesAvailable = false;
            UpdateAbilityButtons();
        }

        public void ShowNonMafiaIdentity(string nickname)
        {
            string displayName = string.IsNullOrWhiteSpace(nickname)
                ? "알 수 없는 플레이어"
                : nickname.Trim();
            ShowPrivateResult($"탐정 전용 조사 결과\n{displayName}은 마피아가 아닙니다.");
        }

        public void ShowPrivateMessage(string message)
        {
            ShowPrivateResult(message);
        }

        private void ShowPrivateResult(string message)
        {
            if (revealResultText == null)
                return;

            revealResultText.text = message;
            revealResultText.gameObject.SetActive(true);

            if (hideResultCoroutine != null)
                StopCoroutine(hideResultCoroutine);
            hideResultCoroutine = StartCoroutine(HideResultAfterDelay());
        }

        private IEnumerator HideResultAfterDelay()
        {
            yield return new WaitForSeconds(resultDisplayDuration);
            if (revealResultText != null)
                revealResultText.gameObject.SetActive(false);
            hideResultCoroutine = null;
        }

        private void UpdateAbilityButtons()
        {
            bool interactable = abilitiesAvailable && abilityChargeCount > 0;
            if (killButton != null)
                killButton.interactable = interactable;
            if (revealButton != null)
                revealButton.interactable = interactable;

            SetActiveIfExists(killHintText, interactable);
            SetActiveIfExists(revealHintText, interactable);

            if (chargeText != null)
                chargeText.text = abilityChargeCount.ToString();
        }

        private static void SetActiveIfExists(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
