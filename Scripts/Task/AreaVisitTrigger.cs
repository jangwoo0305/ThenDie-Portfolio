using UnityEngine;

// 플레이어가 특정 영역에 들어왔을 때 VisitAreas 미션의 방문 목표를 완료합니다.
public class AreaVisitTrigger : MonoBehaviour
{
    [SerializeField] private ObjectiveType visitObjective;

    // Player 태그를 가진 오브젝트가 트리거에 들어오면 방문 목표를 완료 처리합니다.
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (TaskManager.Instance == null)
        {
            Debug.LogWarning("TaskManager가 씬에 없습니다.");
            return;
        }

        TaskManager.Instance.CompleteObjective(
            TaskType.VisitAreas,
            visitObjective
        );
    }
}
