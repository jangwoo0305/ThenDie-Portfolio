using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PotionUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goalText;
    [SerializeField] private TMP_Text progressText;


    public void SetGoalText(string text)
    {
        goalText.text = text;
    }

    public void UpdateProgress(int current, int target)
    {
        progressText.text =
            $"진행도 : {current} / {target}";
    }
}