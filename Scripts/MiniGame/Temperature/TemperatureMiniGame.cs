using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TemperatureMiniGame : BaseMiniGame
{
    [Header("UI")]
    [SerializeField] private TMP_Text curTempText;
    [SerializeField] private TMP_Text goalTempText;
    [SerializeField] private Slider tempSlider;

    private int currentTemp;
    private int goalTemp;

    private Coroutine holdCoroutine;
    private int holdDirection;

    public override void StartMiniGame()
    {
        gameObject.SetActive(true);

        currentTemp = Random.Range(0, 100);
        goalTemp = Random.Range(0, 100);

        UpdateUI();
    }

    public override void CloseMiniGame()
    {
        Destroy(gameObject);
    }

    public void StartHold(int direction)
    {
        holdDirection = direction;

        if (holdCoroutine != null)
            StopCoroutine(holdCoroutine);

        holdCoroutine = StartCoroutine(HoldLoop());
    }

    public void StopHold()
    {
        holdDirection = 0;

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    private IEnumerator HoldLoop()
    {
        float delay = 0.2f;
        float minDelay = 0.02f;
        float acceleration = 0.04f;

        while (holdDirection != 0)
        {
            currentTemp += holdDirection;
            currentTemp = Mathf.Clamp(currentTemp, 0, 100);

            UpdateUI();

            yield return new WaitForSeconds(delay);

            delay -= acceleration;
            delay = Mathf.Max(delay, minDelay);
        }
    }

    private void UpdateUI()
    {
        curTempText.text = $"{currentTemp}℃";
        goalTempText.text = $"{goalTemp}℃";
        tempSlider.value = currentTemp;
    }

    public void OnClickStabilization()
    {
        if (currentTemp == goalTemp)
            onSuccess?.Invoke();
        else
            onFail?.Invoke();
    }
}