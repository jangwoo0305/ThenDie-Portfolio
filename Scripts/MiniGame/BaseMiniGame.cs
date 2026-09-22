using System;
using UnityEngine;

public abstract class BaseMiniGame : MonoBehaviour
{
    public Action onSuccess;
    public Action onFail;

    public abstract void StartMiniGame();

    public abstract void CloseMiniGame();
}