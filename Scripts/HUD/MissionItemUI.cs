using TMPro;
using UnityEngine;

namespace ThenDie.Ingame
{
    public class MissionItemUI : MonoBehaviour
    {
        [Header("State Roots")]
        [SerializeField] private GameObject incompleteState;
        [SerializeField] private GameObject completedState;

        [Header("Text")]
        [SerializeField] private TMP_Text[] missionNameTexts;
        [SerializeField] private TMP_Text[] missionLocationTexts;

        public void SetText(string missionName, string locationName)
        {
            foreach (TMP_Text missionNameText in missionNameTexts ?? System.Array.Empty<TMP_Text>())
            {
                if (missionNameText != null)
                    missionNameText.text = missionName;
            }

            foreach (TMP_Text missionLocationText in missionLocationTexts ?? System.Array.Empty<TMP_Text>())
            {
                if (missionLocationText != null)
                    missionLocationText.text = locationName;
            }
        }

        public void SetCompleted(bool isCompleted)
        {
            if (incompleteState != null)
                incompleteState.SetActive(!isCompleted);

            if (completedState != null)
                completedState.SetActive(isCompleted);
        }
    }
}
