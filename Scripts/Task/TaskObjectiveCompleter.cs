using UnityEngine;

// 오브젝트 상호작용이나 버튼 이벤트에서 특정 미션 목표를 완료시키는 컴포넌트입니다.
// 미니게임 성공 버튼, 상호작용 완료 이벤트, 테스트용 UI 버튼 등에 연결하면 CommonHUD 미션 UI가 갱신됩니다.
public class TaskObjectiveCompleter : MonoBehaviour
{
    [SerializeField] private TaskType taskType;
    [SerializeField] private ObjectiveType objectiveType;

    // 인스펙터에 지정한 TaskType과 ObjectiveType을 완료 처리합니다.
    public void CompleteObjective()
    {
        if (TaskManager.Instance == null)
        {
            Debug.LogWarning("TaskManager가 씬에 없습니다.");
            return;
        }

        TaskManager.Instance.CompleteObjective(
            taskType,
            objectiveType
        );
    }
}
