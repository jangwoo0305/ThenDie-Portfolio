using Mirror;
using ThenDie.Interaction;
using ThenDie.Ingame;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkIdentity))]
public class MiniGameLauncher
    : NetworkBehaviour, IInteractable, IInteractionPromptProvider
{
    [Header("Interaction")]
    [SerializeField] private string promptText = "미션 수행";

    [Header("MiniGame")]
    [SerializeField] private MiniGameType miniGameType;

    [Header("Task Objective On Success")]
    [SerializeField] private TaskType successTaskType = TaskType.None;
    [SerializeField] private ObjectiveType successObjectiveType = ObjectiveType.None;

    public string InteractionText => promptText;

    // 개인 미션 미니맵이 이 런처의 실제 완료 목표를 표시할 때 사용합니다.
    public bool TryGetMissionObjective(
        out TaskType taskType,
        out ObjectiveType objectiveType)
    {
        return TryGetSuccessObjective(out taskType, out objectiveType);
    }

    public bool CanInteract(GameObject player)
    {
        if (miniGameType == MiniGameType.None) return false;

        if (isServer && player != null)
        {
            NetworkIdentity identity = player.GetComponent<NetworkIdentity>();
            if (identity?.connectionToClient != null &&
                TryGetSuccessObjective(out TaskType taskType, out _))
                return MissionManager.Instance == null ||
                       MissionManager.Instance.CanStartMission(identity.connectionToClient, taskType);
        }

        return !IsSuccessObjectiveCompleted();
    }

    public bool ShouldShowPrompt(GameObject player)
    {
        return CanInteract(player);
    }

    public string GetPromptText(GameObject player)
    {
        return promptText;
    }

    public bool ShouldShowInteractionKey(GameObject player)
    {
        return true;
    }

    [Server]
    public void Interact(GameObject player)
    {
        if (!CanInteract(player))
            return;

        NetworkIdentity playerIdentity = player != null
            ? player.GetComponent<NetworkIdentity>()
            : null;

        if (playerIdentity == null || playerIdentity.connectionToClient == null)
            return;

        if (TryGetSuccessObjective(out TaskType taskType, out _))
            MissionManager.Instance?.ServerStartMission(playerIdentity.connectionToClient, taskType);

        TargetPlayMiniGame(playerIdentity.connectionToClient);
    }

    [TargetRpc]
    private void TargetPlayMiniGame(NetworkConnectionToClient target)
    {
        PlayMiniGame();
    }

    // 이 오브젝트에서 지정한 타입의 미니게임을 중앙 매니저를 통해 실행합니다.
    // 버튼 클릭이나 상호작용 이벤트에 연결해서 사용합니다.
    public void PlayMiniGame()
    {
        if (!CanInteract(null))
            return;

        if (MiniGameManager.Instance == null)
        {
            Debug.LogWarning("MiniGameManager가 씬에 없습니다.");
            return;
        }

        MiniGameManager.Instance.OpenMiniGame(
            miniGameType,
            CompleteSuccessObjective
        );
    }

    // 미니게임 성공 후 연결된 미션 목표가 있으면 완료 처리합니다.
    // 목표가 None이면 미션 처리 없이 미니게임만 끝납니다.
    private void CompleteSuccessObjective()
    {
        if (!TryGetSuccessObjective(
                out TaskType targetTaskType,
                out ObjectiveType targetObjectiveType))
            return;

        CompleteObjectiveLocally(targetTaskType, targetObjectiveType);

    }

    private void CompleteObjectiveLocally(
        TaskType taskType,
        ObjectiveType objectiveType)
    {
        if (TaskManager.Instance == null)
        {
            Debug.LogWarning("TaskManager가 씬에 없습니다.");
            return;
        }

        TaskManager.Instance.CompleteObjective(taskType, objectiveType);
    }

    private bool IsSuccessObjectiveCompleted()
    {
        if (!TryGetSuccessObjective(
                out TaskType targetTaskType,
                out ObjectiveType targetObjectiveType))
            return false;

        return TaskManager.Instance != null
               && TaskManager.Instance.IsObjectiveCompleted(
                   targetTaskType,
                   targetObjectiveType
               );
    }

    private bool IsConfiguredSuccessObjective(
        TaskType taskType,
        ObjectiveType objectiveType)
    {
        return TryGetSuccessObjective(
                   out TaskType targetTaskType,
                   out ObjectiveType targetObjectiveType)
               && taskType == targetTaskType
               && objectiveType == targetObjectiveType;
    }

    private bool TryGetSuccessObjective(
        out TaskType targetTaskType,
        out ObjectiveType targetObjectiveType)
    {
        targetTaskType = successTaskType;
        targetObjectiveType = successObjectiveType;

        if (miniGameType == MiniGameType.Stabilize
            && targetTaskType == TaskType.None
            && targetObjectiveType == ObjectiveType.None)
        {
            targetTaskType = TaskType.Stabilize;
            targetObjectiveType = ObjectiveType.StabilizeControl;
        }

        return targetTaskType != TaskType.None
               && targetObjectiveType != ObjectiveType.None;
    }
}
