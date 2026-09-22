using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PotionMiniGame : BaseMiniGame
{
    [SerializeField] private PotionButton redButtonPrefab;
    [SerializeField] private PotionButton greenButtonPrefab;
    [SerializeField] private PotionButton blueButtonPrefab;
    [SerializeField] private Transform parent;
    [SerializeField] private Transform[] potionSpawnPoints;
    [SerializeField] private PotionUI potionUI;
    [SerializeField] private Button classifyButton;

    private PotionType targetPotionType;

    private int targetCount;
    private int currentCount;
    
    private bool isInputLocked;
    
    private void Awake()
    {
        classifyButton.onClick.RemoveAllListeners();
        classifyButton.onClick.AddListener(OnClassifyButtonClicked);
    }

    public override void StartMiniGame()
    {
        List<PotionType> potionList = new();
        
        currentCount = 0;
        isInputLocked = false;
        classifyButton.interactable = false;

        targetPotionType = (PotionType)Random.Range(0, 3);
        potionUI.SetGoalText(
            $"목표 : {GetPotionName(targetPotionType)} 찾기");

        int[][] patterns =
        {
            new[] { 3, 4, 5 },
            new[] { 3, 5, 4 },
            new[] { 4, 3, 5 },
            new[] { 4, 4, 4 },
            new[] { 4, 5, 3 },
            new[] { 5, 3, 4 },
            new[] { 5, 4, 3 }
        };

        int[] selectedPattern = patterns[Random.Range(0, patterns.Length)];

        switch (targetPotionType)
        {
            case PotionType.Red:
                targetCount = selectedPattern[0];
                break;

            case PotionType.Green:
                targetCount = selectedPattern[1];
                break;

            case PotionType.Blue:
                targetCount = selectedPattern[2];
                break;
        }
        currentCount = 0;

        potionUI.UpdateProgress(
            currentCount,
            targetCount);
        
        AddPotions(potionList, PotionType.Red, selectedPattern[0]);
        AddPotions(potionList, PotionType.Green, selectedPattern[1]);
        AddPotions(potionList, PotionType.Blue, selectedPattern[2]);

        Shuffle(potionList);

        for (int i = 0; i < potionList.Count; i++)
        {
            PotionType potionType = potionList[i];
            PotionButton prefab = null;

            switch (potionType)
            {
                case PotionType.Red:
                    prefab = redButtonPrefab;
                    break;

                case PotionType.Green:
                    prefab = greenButtonPrefab;
                    break;

                case PotionType.Blue:
                    prefab = blueButtonPrefab;
                    break;
            }

            Transform spawnParent = GetSpawnParent(i);

            PotionButton button =
                Instantiate(prefab, spawnParent, false);

            FitToSpawnPoint(button);

            button.Initialize(this);
        }
    }

    private Transform GetSpawnParent(int index)
    {
        if (potionSpawnPoints != null
            && index < potionSpawnPoints.Length
            && potionSpawnPoints[index] != null)
        {
            return potionSpawnPoints[index];
        }

        return parent;
    }

    private static void FitToSpawnPoint(PotionButton button)
    {
        if (button == null
            || !button.TryGetComponent(out RectTransform rectTransform))
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    private void AddPotions(List<PotionType> list,
        PotionType type,
        int count)
    {
        for (int i = 0; i < count; i++)
        {
            list.Add(type);
        }
    }

    private void Shuffle(List<PotionType> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            (list[i], list[randomIndex]) =
                (list[randomIndex], list[i]);
        }
    }

    private string GetPotionName(PotionType type)
    {
        switch (type)
        {
            case PotionType.Red:
                return "빨간 시약";
            
            case PotionType.Blue:
                return "파란 시약";
            
            case PotionType.Green:
                return "초록 시약";

            default:
                return ""; 
        }
    }
    public void OnPotionClicked(PotionButton button)
    {
        if (isInputLocked)
            return;

        if (button.PotionType == targetPotionType)
        {
            button.Complete();

            currentCount++;

            potionUI.UpdateProgress(
                currentCount,
                targetCount);

            if (currentCount >= targetCount)
            {
                classifyButton.interactable = (currentCount >= targetCount);
            }
        }
        else
        {
            Debug.Log("오답");

            button.PlayShake();
            StartCoroutine(InputLockRoutine());
        }
    }
    
    private void OnClassifyButtonClicked()
    {
        if (currentCount >= targetCount)
            onSuccess?.Invoke();
        else
            onFail?.Invoke();
    }
    
    private IEnumerator InputLockRoutine()
    {
        isInputLocked = true;

        Debug.Log("입력 잠금");

        yield return new WaitForSeconds(0.5f);

        isInputLocked = false;

        Debug.Log("입력 해제");
    }
    
    public override void CloseMiniGame()
    {
        Destroy(gameObject);
    }
}
