using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PotionButton : MonoBehaviour
{
    private static readonly Color32 completedColor = new(128, 128, 128, 255);

    [SerializeField] private PotionType potionType;
    [SerializeField] private Image potionImage;

    private Button button;
    private BaseMiniGame game; 
    private RectTransform rectTransform;

    public PotionType PotionType => potionType;
    public bool IsCompleted { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);

        if (potionImage == null)
            potionImage = FindPotionImage();
    }
    
    public void Initialize(BaseMiniGame game)
    {
        this.game = game;
    }

    public void OnClick()
    {
        if (IsCompleted)
            return;
        
        (game as PotionMiniGame)?.OnPotionClicked(this);
    }

    public void Complete()
    {
        IsCompleted = true;
        
        if (potionImage != null)
            potionImage.color = completedColor;

        button.interactable = false;
    }

    private Image FindPotionImage()
    {
        Image rootImage = GetComponent<Image>();
        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image != rootImage)
                return image;
        }

        return rootImage;
    }

    public void PlayShake()
    {
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Vector2 originalPos = rectTransform.anchoredPosition;

        float duration = 0.2f;
        float strength = 10f;

        float timer = 0f;

        while (timer < duration)
        {
            float offsetX = Random.Range(-strength, strength);

            rectTransform.anchoredPosition =
                originalPos + new Vector2(offsetX, 0f);

            timer += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = originalPos;
    }
}
