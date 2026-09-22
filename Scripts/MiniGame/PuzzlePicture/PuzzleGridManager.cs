using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PuzzleGridManager : BaseMiniGame
{
    [Header("Puzzle")]
    [SerializeField] private PuzzlePiece puzzlePiecePrefab;

    [Tooltip("퍼즐에 사용할 원본 이미지")]
    [SerializeField] private Texture puzzleTexture;

    [Tooltip("Puzzle Texture가 변경되면 자동으로 동일한 이미지로 설정")]
    [SerializeField] private RawImage answerImage;

    [Tooltip("N으로 변경시 N*N으로 변경")]
    [SerializeField] private int gridSize = 4;
    
    [SerializeField] private RectTransform puzzleGrid;
    
    [SerializeField] private Button completeButton;
    [SerializeField] private Button cancelButton;
    
    private GridLayoutGroup gridLayoutGroup;
    private RectTransform rectTransform;
    private List<PuzzlePiece> puzzlePieces = new List<PuzzlePiece>();
    private List<PuzzlePiece> targetPieces = new List<PuzzlePiece>();

    private void Awake()
    {
        gridLayoutGroup = puzzleGrid.GetComponent<GridLayoutGroup>();

        rectTransform = puzzleGrid;
    }

    public override void StartMiniGame()
    {
        gameObject.SetActive(true);
        SetAnswerImage();
        SetCellSize();
        CreatePuzzle();
        
        completeButton.onClick.AddListener(OnCompleteClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void SetAnswerImage()
    {
        answerImage.texture = puzzleTexture;
    }

    private void SetCellSize()
    {
        float width = rectTransform.rect.width;

        float cellSize = width / gridSize;

        gridLayoutGroup.cellSize = new Vector2(cellSize, cellSize);
    }
    
    private void RandomRotatePieces()
    {
        targetPieces.Clear();

        for (int row = 0; row < gridSize; row++)
        {
            int randomColumn = Random.Range(0, gridSize);
            int index = row * gridSize + randomColumn;

            PuzzlePiece targetPiece = puzzlePieces[index];

            targetPiece.SetRandomRotation();
            targetPiece.SetTargetState();
            targetPiece.SetInteractable(true);
            targetPieces.Add(targetPiece);
        }
    }

    private void CreatePuzzle()
    {
        float pieceSize = 1f / gridSize;

        for (int y = gridSize - 1; y >= 0; y--)
        {
            for (int x = 0; x < gridSize; x++)
            {
                PuzzlePiece piece = Instantiate(puzzlePiecePrefab, puzzleGrid);
                Vector2 uvPosition = new Vector2(x * pieceSize, y * pieceSize);
                Vector2 uvSize = new Vector2(pieceSize, pieceSize);

                piece.Setup(puzzleTexture, uvPosition, uvSize, this);
                piece.SetInteractable(false);
                puzzlePieces.Add(piece);
            }
        }
        RandomRotatePieces();
    }
    

    public bool IsPuzzleComplete()
    {
        foreach (PuzzlePiece piece in targetPieces)
        {
            if (!piece.IsCorrectRotation())
            {
                return false;
            }
        }
        return true;
    }
    
    private void OnCompleteClicked()
    {
        if (IsPuzzleComplete())
        {
            Debug.Log("퍼즐 성공");

            onSuccess?.Invoke();
        }
        else
        {
            Debug.Log("아직 정답이 아닙니다.");
        }
    }
    
    private void OnCancelClicked()
    {
        onFail?.Invoke();
    }

    public override void CloseMiniGame()
    {
        Destroy(gameObject);
    }
}
