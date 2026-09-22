using UnityEngine;
using UnityEngine.EventSystems;

public class TempButtonHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType
    {
        Up,
        Down
    }

    [SerializeField] private ButtonType buttonType;
    [SerializeField] private TemperatureMiniGame miniGame;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (buttonType == ButtonType.Up)
            miniGame.StartHold(+1);
        else
            miniGame.StartHold(-1);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        miniGame.StopHold();
    }
}