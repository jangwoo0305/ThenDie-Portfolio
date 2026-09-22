using System;
using UnityEngine;
using UnityEngine.UI;

namespace ThenDie.Ingame
{
    public class CitizenHUD : MonoBehaviour
    {
        [SerializeField] private Button killButton;
        [SerializeField] private GameObject killHintText;

        private int killTokenCount;
        private bool killAvailable;

        // CitizenAbilityHUD의 KillButton이 눌렸을 때 발생합니다.
        // 실제 타깃 선택, 거리 판정, 서버 CmdUseKill 호출은 이 이벤트를 구독한 게임 로직에서 처리합니다.
        public event Action CitizenKillRequested;

        private void Awake()
        {
            //SetCitizenAbilityVisible(false);
            UpdateKillButton();
        }

        private void OnEnable()
        {
            if (killButton != null)
            {
                killButton.onClick.RemoveListener(UseCitizenKillButton);
                killButton.onClick.AddListener(UseCitizenKillButton);
            }
        }

        private void OnDisable()
        {
            if (killButton != null)
                killButton.onClick.RemoveListener(UseCitizenKillButton);
        }

        // CitizenAbilityHUD의 KillButton 클릭 시 호출됩니다.
        // 버튼 사용 가능 상태일 때만 CitizenKillRequested 이벤트를 발생시키고, UI는 즉시 1회 사용 처리합니다.
        public void UseCitizenKillButton()
        {
            if (!killAvailable || killTokenCount <= 0)
                return;

            CitizenKillRequested?.Invoke();
        }

        // 로컬 플레이어의 역할/생존/게임 페이즈에 따라 CitizenAbilityHUD 표시 여부를 갱신할 때 호출합니다.
        // 예: 시민이고 자유 행동 중이면 true, 시민이 아니거나 사망/회의/투표/결과 화면이면 false.
        public void SetCitizenAbilityVisible(bool visible)
        {
            gameObject.SetActive(visible);

            if (!visible)
                SetCitizenKillAvailable(false);
        }

        // 서버/게임 로직에서 시민 킬 사용권, 쿨타임, 거리, 페이즈 조건을 모두 판단한 뒤 호출합니다.
        // true를 넘기면 KillButton이 활성화되고, false를 넘기면 비활성화됩니다.
        public void SetCitizenKillAvailable(bool available)
        {
            killAvailable = available;
            UpdateKillButton();
        }

        public void SetCitizenKillTokenCount(int count)
        {
            killTokenCount = Mathf.Max(0, count);
            UpdateKillButton();
        }

        // 서버에서 킬 사용권이 실제로 소모됐거나, UI 클릭 후 1회 사용 상태로 확정할 때 호출합니다.
        // 호출 후 이번 게임에서는 시민 KillButton이 다시 활성화되지 않습니다.
        public void MarkCitizenKillUsed()
        {
            SetCitizenKillTokenCount(killTokenCount - 1);
        }

        // 새 게임 시작, 재시작, 테스트 초기화 시 시민 킬 UI 상태를 초기값으로 되돌릴 때 호출합니다.
        public void ResetCitizenKillState()
        {
            killTokenCount = 0;
            killAvailable = false;
            UpdateKillButton();
        }

        private void UpdateKillButton()
        {
            bool interactable = killAvailable && killTokenCount > 0;

            if (killButton != null)
                killButton.interactable = interactable;

            SetActiveIfExists(killHintText, interactable);
        }

        private static void SetActiveIfExists(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
