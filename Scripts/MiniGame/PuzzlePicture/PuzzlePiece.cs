using UnityEngine;
using UnityEngine.UI;

public class PuzzlePiece : MonoBehaviour
{
    [SerializeField] private RawImage pieceImage;

    private static readonly Color TargetImageColor = new Color(1f, 0.55f, 0.55f, 1f);
    private static readonly Color TargetBackgroundColor = new Color(1f, 0.82f, 0.82f, 1f);
    private static readonly Color NormalImageColor = new Color(0.8f, 0.654902f, 0.372549f, 1f);
    private static readonly Color NormalBackgroundColor = new Color(0.8f, 0.654902f, 0.372549f, 1f);

    private RectTransform rectTransform;
    private RectTransform imageRectTransform;
    private Image backgroundImage;
    private Button button;

    private int currentRotation; // 현재 회전 상태 저장
    private bool isTargetPiece;

    private const int RotationAmount = 90;
    
    private PuzzleGridManager puzzleGridManager;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        imageRectTransform = pieceImage.GetComponent<RectTransform>();
        backgroundImage = GetComponent<Image>();

        button = GetComponent<Button>();
        button.onClick.AddListener(RotatePiece);
    }

    public void Setup(Texture texture, Vector2 uvPosition, Vector2 uvSize, PuzzleGridManager manager)
    {
        puzzleGridManager = manager;
        
        pieceImage.texture = texture;

        pieceImage.uvRect =
            new Rect(uvPosition, uvSize);

        currentRotation = 0;
        isTargetPiece = false;
        imageRectTransform.localRotation = Quaternion.identity;
        UpdateVisualState();
    }

    private void RotatePiece()
    {
        currentRotation = (currentRotation + RotationAmount) % 360;
        imageRectTransform.localRotation = Quaternion.Euler(0, 0, -currentRotation);

        Debug.Log($"{currentRotation}도 회전");

        UpdateVisualState();
    }
    
    public void SetRandomRotation()
    {
        int randomRotation = Random.Range(1, 4) * 90;
        
        currentRotation = randomRotation;
        
        imageRectTransform.localRotation = Quaternion.Euler(0, 0, -currentRotation);
    }
    
    public void SetTargetState()
    {
        isTargetPiece = true;
        UpdateVisualState();
    }
    
    public void SetSolvedState()
    {
        SetNormalColor();

        SetInteractable(false);
    }

    private void UpdateVisualState()
    {
        rectTransform.localRotation = Quaternion.identity;

        if (!isTargetPiece)
        {
            SetNormalColor();
            return;
        }

        if (IsCorrectRotation())
        {
            SetSolvedState();
            return;
        }

        pieceImage.color = TargetImageColor;
        backgroundImage.color = TargetBackgroundColor;
        SetInteractable(true);
    }

    private void SetNormalColor()
    {
        pieceImage.color = NormalImageColor;
        backgroundImage.color = NormalBackgroundColor;
    }
    
    public bool IsCorrectRotation()
    {
        return currentRotation == 0;
    }
    
    public void SetInteractable(bool value)
    {
        button.interactable = value;
    }
}
