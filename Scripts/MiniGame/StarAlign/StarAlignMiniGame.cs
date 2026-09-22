using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class StarAlignMiniGame : BaseMiniGame
{
    [Header("Moving Star")]
    [Tooltip("이동 스타의 트랜스폼")]
    [SerializeField] private RectTransform movingStar;
    [Tooltip("이동 스타의 속력")]
    [SerializeField] private float movingStarSpeed = 300f;
    [Tooltip("마커와 이동 스타 연동")]
    [SerializeField] private RectTransform marker;
    
    [Header("Goal Stars")]
    [SerializeField] private RectTransform[] goalStars;
    [SerializeField] private Sprite successStarSprite;
    [SerializeField] private Sprite failStarSprite;
    [SerializeField] private Sprite defaultStarSprite;

    [Header("successZone")]
    [SerializeField] private RectTransform successZone;
    
    [Header("Button")]
    [SerializeField] private Button alignButton;
    private InputAction alignAction;
    
    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;

    [SerializeField] private TMPro.TMP_Text titleText;
    [SerializeField] private TMPro.TMP_Text descriptionText;

    [SerializeField] private TMPro.TMP_Text tryCountText;
    [SerializeField] private TMPro.TMP_Text successCountText;
    [SerializeField] private TMPro.TMP_Text failCountText;

    [SerializeField] private Button retryButton;
    [SerializeField] private TMPro.TMP_Text retryButtonText;
    
    private float startX = -260f;
    private float endX = 260f;

    private int currentGoalIndex = 0;
    private int successCount = 0;
    private bool isGameEnded;

    #region Unity Lifecycle

    private void Awake()
    {
        alignAction = new InputAction(
            name: "Align",
            type: InputActionType.Button,
            binding: "<Keyboard>/space");
    }
    private void OnEnable()
    {
        alignAction?.Enable();
    }
    private void OnDisable()
    {
        alignAction?.Disable();
    }
    private void OnDestroy()
    {
        alignAction?.Dispose();
    }
    
    void Start()
    {
        alignButton.onClick.AddListener(OnAlignButton);
        retryButton.onClick.AddListener(OnRetryButton);

        // InitGame();
    }

    void Update()
    {
        if (isGameEnded)
            return;
        
        MoveStar();
        UpdateMarker();
        
        if (alignAction.WasPressedThisFrame())
        {
            OnAlignButton();
        }
    }

    #endregion

    #region BaseMiniGame

    public override void StartMiniGame()
    {
        gameObject.SetActive(true);
        InitGame();
    }

    public override void CloseMiniGame()
    {
        Destroy(gameObject);
    }

    #endregion

    #region Game Flow

    private void InitGame()
    {
        resultPanel.SetActive(false);
        
        isGameEnded = false;
        
        Canvas.ForceUpdateCanvases();
        successCount = 0;
        currentGoalIndex = 0;
        
        RandomizeGoalPositions();

        foreach (RectTransform goalStar in goalStars)
        {
            Image starImage = goalStar.Find("StarImage").GetComponent<Image>();
            SetGoalStarSprite(starImage, defaultStarSprite);
        }
        MoveStarToCurrentGoal();
    }
    
    private void GameEnd()
    {
        isGameEnded = true;

        if (successCount >= 4)
        {
            onSuccess?.Invoke();
            return;
        }

        resultPanel.SetActive(true);

        int failCount = goalStars.Length - successCount;

        tryCountText.text = $"정렬 시도 횟수 : {goalStars.Length}";
        successCountText.text = $"정렬 성공 별 : {successCount}";
        failCountText.text = $"정렬 실패 별 : {failCount}";

        titleText.text = "정렬 실패";
        descriptionText.text = "별들의 정렬 의식이 불안정하게 종료되었습니다.";
        retryButtonText.text = "재도전";
        
    }

    private void OnRetryButton()
    {
        InitGame();
    }

    #endregion

    #region Star Movement

    private void ResetStar()
    {
        Vector2 Pos = movingStar.anchoredPosition;
        Pos.x = startX;
        movingStar.anchoredPosition = Pos;
    }

    private void MoveStar()
    {
        Vector2 Pos = movingStar.anchoredPosition;
        
        Pos.x += movingStarSpeed * Time.deltaTime;

        if (Pos.x > endX)
        {
            Pos.x = startX;
        }
        movingStar.anchoredPosition = Pos; 
    }
    
    private void UpdateMarker()
    {
        float progress = Mathf.InverseLerp(startX, endX, movingStar.anchoredPosition.x);

        float markerStartX = -260f;
        float markerEndX = 260f;
        
        Vector2 markerPos = marker.anchoredPosition;
        
        markerPos.x = Mathf.Lerp(markerStartX, markerEndX, progress);
        
        marker.anchoredPosition = markerPos;
    }

    private void MoveStarToCurrentGoal()
    {
        RectTransform currentGoal = goalStars[currentGoalIndex];
        Vector2 pos = movingStar.anchoredPosition;

        pos.x = startX;
        pos.y = currentGoal.anchoredPosition.y;

        movingStar.anchoredPosition = pos;
        MoveSuccessZone(currentGoal);
    }

    private void MoveSuccessZone(RectTransform currentGoal)
    {
        
        Debug.Log($"Goal X : {currentGoal.anchoredPosition.x}");
        
        Vector2 zonePos = successZone.anchoredPosition;
        zonePos.x = currentGoal.anchoredPosition.x;
        successZone.anchoredPosition = zonePos;
        
        Debug.Log($"SuccessZone X : {successZone.anchoredPosition.x}");
    }
    
    private void RandomizeGoalPositions()
    {
        float[] positions = { -200f, -100f, 0f, 100f, 200f };

        for (int i = 0; i < positions.Length; i++)
        {
            int randomIndex = Random.Range(i, positions.Length);

            (positions[i], positions[randomIndex]) =
                (positions[randomIndex], positions[i]);
        }

        for (int i = 0; i < goalStars.Length; i++)
        {
            Vector2 pos = goalStars[i].anchoredPosition;
            pos.x = positions[i];
            goalStars[i].anchoredPosition = pos;
        }
    }

    #endregion

    #region Goal Logic

    private void OnAlignButton()
    {
        RectTransform currentGoal = goalStars[currentGoalIndex];
        
        float distance = Mathf.Abs(movingStar.anchoredPosition.x - successZone.anchoredPosition.x);
        
        Image starImage = currentGoal.Find("StarImage").GetComponent<Image>();

        float successRange = successZone.rect.width * 0.5f;
        if (distance <= successRange)
        {
            // Debug.Log($"거리 : {distance}");
            Debug.Log("성공");
            SetGoalStarSprite(starImage, successStarSprite);
            successCount ++;
        }
        else
        {
            // Debug.Log($"거리 : {distance}");
            Debug.Log("실패");         
            SetGoalStarSprite(starImage, failStarSprite);
        }

        MoveToNextGoal();
    }

    private void SetGoalStarSprite(Image starImage, Sprite sprite)
    {
        if (sprite != null)
        {
            starImage.sprite = sprite;
        }

        starImage.color = Color.white;
    }

    private void MoveToNextGoal()
    {
        currentGoalIndex++;
        if (currentGoalIndex >= goalStars.Length)
        {
            GameEnd();
            return;
        }

        MoveStarToCurrentGoal();
    }

    #endregion
}
