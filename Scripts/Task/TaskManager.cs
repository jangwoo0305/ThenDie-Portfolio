using System;
using System.Collections.Generic;
using UnityEngine;

// 게임 전체 미션 진행도를 관리하는 중앙 매니저입니다.
// 각 TaskType마다 필요한 ObjectiveType 목록을 등록하고 완료 여부를 추적합니다.
public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    private readonly Dictionary<TaskType, TaskData> tasks = new();

    public event Action<TaskType, ObjectiveType> OnObjectiveCompleted;
    public event Action<TaskType> OnTaskCompleted;
    public event Action OnTasksReset;

    // 씬 안에서 TaskManager가 하나만 존재하도록 준비하고,
    // 기본 미션 목록을 등록합니다.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        RegisterDefaultTasks();
    }

    // 현재 게임에서 사용하는 기본 미션과 목표 목록을 등록합니다.
    // 여러 단계가 필요한 미션은 ObjectiveType을 여러 개 넣습니다.
    private void RegisterDefaultTasks()
    {
        RegisterTask(new TaskData(
            TaskType.Potion,
            new[] { ObjectiveType.CompleteMiniGame }
        ));

        RegisterTask(new TaskData(
            TaskType.StarAlign,
            new[] { ObjectiveType.CompleteMiniGame }
        ));

        RegisterTask(new TaskData(
            TaskType.Picture,
            new[] { ObjectiveType.CompleteMiniGame }
        ));

        RegisterTask(new TaskData(
            TaskType.Stabilize,
            new[]
            {
                ObjectiveType.StabilizeControl,
                ObjectiveType.IncubatorCheck
            }
        ));
    }

    // 새 미션 데이터를 등록하거나 같은 TaskType의 기존 데이터를 교체합니다.
    public void RegisterTask(TaskData taskData)
    {
        if (taskData == null || taskData.taskType == TaskType.None)
            return;

        tasks[taskData.taskType] = taskData;
    }

    // 미니게임 성공, 상호작용 완료, 서버 판정 완료 시 호출합니다.
    // 해당 미션의 모든 ObjectiveType이 완료되면 OnTaskCompleted가 발생하고 CommonHUD 미션 UI가 갱신됩니다.
    public void CompleteObjective(TaskType taskType, ObjectiveType objective)
    {
        if (!tasks.TryGetValue(taskType, out TaskData task))
        {
            Debug.LogWarning($"등록되지 않은 작업입니다: {taskType}");
            return;
        }

        bool newlyCompleted = task.CompleteObjective(objective);

        if (!newlyCompleted)
            return;

        Debug.Log($"Objective 완료: {taskType} / {objective}");
        OnObjectiveCompleted?.Invoke(taskType, objective);

        if (task.IsCompleted)
        {
            Debug.Log($"Task 완료: {taskType}");
            OnTaskCompleted?.Invoke(taskType);
        }
    }

    // 해당 미션이 모든 목표를 끝냈는지 확인합니다.
    public bool IsTaskCompleted(TaskType taskType)
    {
        return tasks.TryGetValue(taskType, out TaskData task) && task.IsCompleted;
    }

    public int GetTotalTaskCount()
    {
        return tasks.Count;
    }

    public int GetCompletedTaskCount()
    {
        int completedCount = 0;

        foreach (TaskData task in tasks.Values)
        {
            if (task.IsCompleted)
                completedCount++;
        }

        return completedCount;
    }

    public void ResetAllTasks()
    {
        foreach (TaskData task in tasks.Values)
            task.ResetCompletion();

        OnTasksReset?.Invoke();
    }

    // 해당 미션 안의 특정 목표가 완료됐는지 확인합니다.
    public bool IsObjectiveCompleted(TaskType taskType, ObjectiveType objective)
    {
        return tasks.TryGetValue(taskType, out TaskData task)
               && task.IsObjectiveCompleted(objective);
    }
}
