using TMPro;
using ThenDie.Ingame;
using UnityEngine;

public class MeetingResultPopupUI : MonoBehaviour
{
    [Header("Optional")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text resultText;

    private const string NoEjectionMessage = "투표 결과, 추방된 참가자가 없습니다.";
    private const int ResultSortingOrder = 1000;

    private void Awake()
    {
        CollectReferences();
    }

    // 서버에서 투표 결과로 추방된 플레이어의 닉네임을 받은 뒤 호출합니다.
    public void ShowEjectionResult(string playerName, RoleId role)
    {
        string displayName = string.IsNullOrWhiteSpace(playerName) ? "참가자" : playerName;
        SetMessage($"{GetRoleDisplayName(role)} {displayName} 님이 추방되었습니다.");
        Show();
    }

    public void ShowEjectionResult(string playerName)
    {
        ShowEjectionResult(playerName, RoleId.Citizen);
    }

    // 서버에서 동률 또는 스킵 처리로 추방자가 없다고 판단했을 때 호출합니다.
    public void ShowNoEjectionResult()
    {
        SetMessage(NoEjectionMessage);
        Show();
    }

    // 회의 결과 팝업을 닫을 때 호출합니다.
    public void Hide()
    {
        CollectReferences();

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void Show()
    {
        CollectReferences();

        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
            popupRoot.transform.SetAsLastSibling();
            EnsureResultDrawsOnTop();
        }
    }

    private void SetMessage(string message)
    {
        CollectReferences();

        if (resultText != null)
        {
            resultText.text = message;
        }
    }

    private void EnsureResultDrawsOnTop()
    {
        if (popupRoot == null)
            return;

        Canvas resultCanvas = popupRoot.GetComponent<Canvas>();
        if (resultCanvas == null)
            resultCanvas = popupRoot.AddComponent<Canvas>();

        resultCanvas.overrideSorting = true;
        resultCanvas.sortingOrder = ResultSortingOrder;

        if (resultText != null)
            resultText.transform.SetAsLastSibling();
    }

    private void CollectReferences()
    {
        if (popupRoot == null)
        {
            popupRoot = gameObject;
        }

        if (resultText == null)
        {
            resultText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private static string GetRoleDisplayName(RoleId role)
    {
        return role switch
        {
            RoleId.Mafia => "마피아",
            RoleId.Detective => "탐정",
            _ => "시민",
        };
    }
}
