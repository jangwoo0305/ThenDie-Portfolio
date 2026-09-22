using System;
using System.Collections.Generic;
using System.Linq;

// 하나의 미션이 어떤 목표들을 필요로 하고,
// 그중 어떤 목표가 완료됐는지 저장하는 데이터 클래스입니다.
[Serializable]
public class TaskData
{
    public TaskType taskType;
    public List<ObjectiveType> requiredObjectives = new();

    private readonly HashSet<ObjectiveType> completedObjectives = new();

    public bool IsCompleted =>
        requiredObjectives.All(objective => completedObjectives.Contains(objective));

    // 외부에서 완료된 목표 목록을 읽을 수 있게 제공하되,
    // 직접 수정하지는 못하게 IReadOnlyCollection으로 노출합니다.
    public IReadOnlyCollection<ObjectiveType> CompletedObjectives => completedObjectives;

    // 미션 타입과 이 미션을 완료하기 위해 필요한 목표 목록을 설정합니다.
    public TaskData(TaskType taskType, IEnumerable<ObjectiveType> requiredObjectives)
    {
        this.taskType = taskType;
        this.requiredObjectives = requiredObjectives.ToList();
    }

    // 목표 하나를 완료 처리합니다.
    // 이미 완료된 목표이거나 이 미션에 속하지 않는 목표면 false를 반환합니다.
    public bool CompleteObjective(ObjectiveType objective)
    {
        if (objective == ObjectiveType.None)
            return false;

        if (!requiredObjectives.Contains(objective))
            return false;

        return completedObjectives.Add(objective);
    }

    // 특정 목표가 이미 완료됐는지 확인합니다.
    public bool IsObjectiveCompleted(ObjectiveType objective)
    {
        return completedObjectives.Contains(objective);
    }

    public void ResetCompletion()
    {
        completedObjectives.Clear();
    }
}
