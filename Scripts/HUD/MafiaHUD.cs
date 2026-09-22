using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThenDie.Ingame
{
    public class MafiaHUD : MonoBehaviour
    {
        [SerializeField] private Button killButton;
        [SerializeField] private GameObject killHintText;
        [SerializeField] private GameObject killCooldownOverlay;
        [SerializeField] private Image killCooldownFill;
        [SerializeField] private TMP_Text killCooldownText;
        [SerializeField] private Button sabotageButton;
        [SerializeField] private GameObject sabotageHintText;
        [SerializeField] private GameObject sabotageCooldownOverlay;
        [SerializeField] private Image sabotageCooldownFill;

        private bool killAvailable;
        private bool cooldownActive;
        private float cooldownRemaining;
        private float cooldownDuration;
        private float cooldownEndTime;
        private bool sabotageAvailable;
        private bool sabotageCooldownActive;
        private float sabotageCooldownRemaining;
        private float sabotageCooldownDuration;
        private float sabotageCooldownEndTime;

        // KillButton 클릭 시 발생합니다.
        // 게임 로직은 이 이벤트를 구독해 타깃 선택, 거리 판정, 서버 CmdUseKill 호출을 처리합니다.
        public event Action MafiaKillRequested;

        // SabotageButton 클릭 시 발생합니다.
        // 게임 로직은 이 이벤트를 구독해 사보타주 종류 선택, 서버 CmdRequestSabotage 호출을 처리합니다.
        public event Action MafiaSabotageRequested;

        private void Awake()
        {
            //SetMafiaAbilityVisible(false);
            ClearKillCooldown();
            ClearSabotageCooldown();
        }

        private void Update()
        {
            TickKillCooldown();
            TickSabotageCooldown();
        }

        private void OnEnable()
        {
            TickKillCooldown();
            TickSabotageCooldown();

            if (killButton != null)
            {
                killButton.onClick.RemoveListener(UseMafiaKillButton);
                killButton.onClick.AddListener(UseMafiaKillButton);
            }

            if (sabotageButton != null)
            {
                sabotageButton.onClick.RemoveListener(UseMafiaSabotageButton);
                sabotageButton.onClick.AddListener(UseMafiaSabotageButton);
            }
        }

        private void OnDisable()
        {
            if (killButton != null)
                killButton.onClick.RemoveListener(UseMafiaKillButton);

            if (sabotageButton != null)
                sabotageButton.onClick.RemoveListener(UseMafiaSabotageButton);
        }

        // KillButton 클릭 시 호출됩니다.
        // 버튼이 사용 가능하고 쿨타임이 아닐 때만 MafiaKillRequested 이벤트를 발생시킵니다.
        // 실제 킬 성공 여부는 서버가 판단하고, 성공 확정 후 게임 로직에서 StartKillCooldown을 호출해야 합니다.
        public void UseMafiaKillButton()
        {
            if (!killAvailable || cooldownActive)
                return;

            MafiaKillRequested?.Invoke();
        }

        // SabotageButton 클릭 시 호출됩니다.
        // 버튼이 사용 가능하고 쿨타임이 아닐 때만 MafiaSabotageRequested 이벤트를 발생시킵니다.
        // 실제 사보타주 성공 여부는 서버가 판단하고, 성공 확정 후 게임 로직에서 StartSabotageCooldown을 호출해야 합니다.
        public void UseMafiaSabotageButton()
        {
            if (!sabotageAvailable || sabotageCooldownActive)
                return;

            MafiaSabotageRequested?.Invoke();
        }

        // 로컬 플레이어가 마피아 HUD를 볼 수 있는 상태인지 갱신할 때 호출합니다.
        // 예: 마피아이고 자유 행동 중이면 true, 마피아가 아니거나 사망/회의/투표/결과 화면이면 false.
        public void SetMafiaAbilityVisible(bool visible)
        {
            gameObject.SetActive(visible);

            if (!visible)
            {
                SetMafiaKillAvailable(false);
                SetMafiaSabotageAvailable(false);
            }
        }

        // KillButton 사용 가능 여부를 갱신할 때 호출합니다.
        // 게임 로직에서 역할, 쿨타임, 거리, 생존, 페이즈 조건을 판단한 뒤 true/false를 넘깁니다.
        public void SetMafiaKillAvailable(bool available)
        {
            killAvailable = available;
            UpdateKillButton();
        }

        // SabotageButton 사용 가능 여부를 갱신할 때 호출합니다.
        // 게임 로직에서 역할, 사보타주 쿨타임, 진행 중 여부, 생존, 페이즈 조건을 판단한 뒤 true/false를 넘깁니다.
        public void SetMafiaSabotageAvailable(bool available)
        {
            sabotageAvailable = available;
            UpdateSabotageButton();
        }

        // 킬이 서버에서 정상 발동됐다고 확정된 직후 호출합니다.
        // KillButton을 비활성화하고 남은 쿨타임 숫자와 Radial 오버레이를 표시합니다.
        public void StartKillCooldown(float duration)
        {
            SetKillCooldown(duration, duration);
        }

        // 서버에서 KillButton 쿨타임을 동기화할 때 호출합니다.
        // remaining은 남은 시간, duration은 전체 쿨타임입니다. remaining이 0 이하이면 쿨타임 UI를 해제합니다.
        public void SetKillCooldown(float remaining, float duration)
        {
            cooldownDuration = Mathf.Max(0f, duration);
            cooldownRemaining = Mathf.Max(0f, remaining);
            cooldownActive = cooldownDuration > 0f && cooldownRemaining > 0f;
            cooldownEndTime = cooldownActive
                ? Time.unscaledTime + cooldownRemaining
                : 0f;

            UpdateKillButton();
            UpdateCooldownVisual();
        }

        // KillButton 쿨타임 종료, 게임 재시작, 테스트 초기화 시 호출합니다.
        public void ClearKillCooldown()
        {
            cooldownActive = false;
            cooldownRemaining = 0f;
            cooldownDuration = 0f;
            cooldownEndTime = 0f;

            UpdateKillButton();
            UpdateCooldownVisual();
        }

        // 사보타주가 서버에서 정상 발동됐다고 확정된 직후 호출합니다.
        // SabotageButton을 비활성화하고 Radial 오버레이를 표시합니다.
        // 기본 사보타주 쿨타임은 기획 기준 60초입니다.
        public void StartSabotageCooldown(float duration = 60f)
        {
            SetSabotageCooldown(duration, duration);
        }

        // 서버에서 SabotageButton 쿨타임을 동기화할 때 호출합니다.
        // remaining은 남은 시간, duration은 전체 쿨타임입니다. remaining이 0 이하이면 쿨타임 오버레이를 해제합니다.
        public void SetSabotageCooldown(float remaining, float duration)
        {
            sabotageCooldownDuration = Mathf.Max(0f, duration);
            sabotageCooldownRemaining = Mathf.Max(0f, remaining);
            sabotageCooldownActive = sabotageCooldownDuration > 0f && sabotageCooldownRemaining > 0f;
            sabotageCooldownEndTime = sabotageCooldownActive
                ? Time.unscaledTime + sabotageCooldownRemaining
                : 0f;

            UpdateSabotageButton();
            UpdateSabotageCooldownVisual();
        }

        // SabotageButton 쿨타임 종료, 게임 재시작, 테스트 초기화 시 호출합니다.
        public void ClearSabotageCooldown()
        {
            sabotageCooldownActive = false;
            sabotageCooldownRemaining = 0f;
            sabotageCooldownDuration = 0f;
            sabotageCooldownEndTime = 0f;

            UpdateSabotageButton();
            UpdateSabotageCooldownVisual();
        }

        private void TickKillCooldown()
        {
            if (!cooldownActive)
                return;

            cooldownRemaining = Mathf.Max(0f, cooldownEndTime - Time.unscaledTime);

            if (cooldownRemaining <= 0f)
            {
                ClearKillCooldown();
                return;
            }

            UpdateCooldownVisual();
        }

        private void TickSabotageCooldown()
        {
            if (!sabotageCooldownActive)
                return;

            sabotageCooldownRemaining = Mathf.Max(
                0f,
                sabotageCooldownEndTime - Time.unscaledTime
            );

            if (sabotageCooldownRemaining <= 0f)
            {
                ClearSabotageCooldown();
                return;
            }

            UpdateSabotageCooldownVisual();
        }

        private void UpdateKillButton()
        {
            bool interactable = killAvailable && !cooldownActive;

            if (killButton != null)
                killButton.interactable = interactable;

            SetActiveIfExists(killHintText, interactable);
        }

        private void UpdateSabotageButton()
        {
            bool interactable = sabotageAvailable && !sabotageCooldownActive;

            if (sabotageButton != null)
                sabotageButton.interactable = interactable;

            SetActiveIfExists(sabotageHintText, interactable);
        }

        private void UpdateCooldownVisual()
        {
            if (killCooldownOverlay != null)
                killCooldownOverlay.SetActive(cooldownActive);

            if (killCooldownFill != null)
            {
                var fillAmount = cooldownDuration > 0f ? cooldownRemaining / cooldownDuration : 0f;
                killCooldownFill.fillAmount = Mathf.Clamp01(fillAmount);
            }

            if (killCooldownText != null)
            {
                killCooldownText.gameObject.SetActive(cooldownActive);
                killCooldownText.text = cooldownActive ? Mathf.CeilToInt(cooldownRemaining).ToString() : string.Empty;
            }
        }

        private void UpdateSabotageCooldownVisual()
        {
            if (sabotageCooldownOverlay != null)
                sabotageCooldownOverlay.SetActive(sabotageCooldownActive);

            if (sabotageCooldownFill != null)
            {
                var fillAmount = sabotageCooldownDuration > 0f
                    ? sabotageCooldownRemaining / sabotageCooldownDuration
                    : 0f;
                sabotageCooldownFill.fillAmount = Mathf.Clamp01(fillAmount);
            }
        }

        private static void SetActiveIfExists(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
