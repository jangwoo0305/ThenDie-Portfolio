using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThenDie.Ingame
{
    public class OwlMailSabotageUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text deliverProgressText;
        [SerializeField] private TMP_Text carryLetterText;
        [SerializeField] private GameObject[] deliverySuccessOrbs;

        private Graphic blockerGraphic;
        private SabotageManager sabotageManager;
        private OwlMailPlayerCarry localPlayerCarry;
        private OwlMailSabotageState lastState = (OwlMailSabotageState)byte.MaxValue;

        private void Awake()
        {
            blockerGraphic = GetComponent<Graphic>();
            Hide();
        }

        private void Update()
        {
            ResolveReferences();

            if (sabotageManager == null)
                return;

            OwlMailSabotageState currentState = sabotageManager.OwlMailState;
            if (currentState != lastState)
            {
                ApplyState(currentState);
                lastState = currentState;
            }

            if (currentState == OwlMailSabotageState.Running)
                UpdateRunningUI();
        }

        private void ResolveReferences()
        {
            if (sabotageManager == null)
                sabotageManager = SabotageManager.Instance;

            if (localPlayerCarry == null && NetworkClient.localPlayer != null)
            {
                localPlayerCarry =
                    NetworkClient.localPlayer.GetComponent<OwlMailPlayerCarry>();
            }
        }

        private void ApplyState(OwlMailSabotageState state)
        {
            switch (state)
            {
                case OwlMailSabotageState.Running:
                    Show();
                    UpdateDeliverySuccessOrbs(0);
                    break;

                case OwlMailSabotageState.Success:
                    Hide();
                    break;

                case OwlMailSabotageState.Failed:
                    Hide();
                    break;

                default:
                    Hide();
                    break;
            }
        }

        private void UpdateRunningUI()
        {
            int remainTime =
                Mathf.CeilToInt((float)sabotageManager.OwlMailRemainingTime);
            int minutes = remainTime / 60;
            int seconds = remainTime % 60;

            timerText.text = $"{minutes:00}:{seconds:00}";
            deliverProgressText.text =
                $"{sabotageManager.CurrentDeliveredLetterCount} / " +
                $"{sabotageManager.RequiredLetterCount}";
            UpdateDeliverySuccessOrbs(sabotageManager.CurrentDeliveredLetterCount);

            int carryCount =
                localPlayerCarry != null ? localPlayerCarry.CarryLetterCount : 0;
            carryLetterText.text = $"보유편지 : {carryCount}";
        }

        private void Hide()
        {
            SetPanelVisible(false);
            UpdateDeliverySuccessOrbs(0);
        }

        private void Show()
        {
            SetPanelVisible(true);
        }

        private void SetPanelVisible(bool isVisible)
        {
            if (panel != null)
                panel.SetActive(isVisible);

            if (blockerGraphic != null)
                blockerGraphic.raycastTarget = isVisible;
        }

        private void UpdateDeliverySuccessOrbs(int deliveredCount)
        {
            if (deliverySuccessOrbs == null)
                return;

            for (int i = 0; i < deliverySuccessOrbs.Length; i++)
            {
                if (deliverySuccessOrbs[i] != null)
                    deliverySuccessOrbs[i].SetActive(i < deliveredCount);
            }
        }
    }
}
