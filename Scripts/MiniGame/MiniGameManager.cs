using System;
using System.Collections.Generic;
using ThenDie.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class MiniGameManager : MonoBehaviour
{
    [Serializable]
    private class MiniGameEntry
    {
        public MiniGameType type;
        public BaseMiniGame prefab;
    }

    public static MiniGameManager Instance { get; private set; }

    [Header("MiniGame Prefabs")]
    [SerializeField] private List<MiniGameEntry> miniGames = new();

    [Header("Debug")]
    [SerializeField] private MiniGameType debugMiniGameType = MiniGameType.None;
    [FormerlySerializedAs("miniGamePrefab")]
    [SerializeField] private BaseMiniGame debugMiniGamePrefab;

    [Header("UI Root")]
    [SerializeField] private Transform uiRoot;
    [SerializeField] private GameObject popupRoot;

    private readonly Dictionary<MiniGameType, BaseMiniGame> miniGamePrefabs = new();

    private BaseMiniGame currentMiniGame;
    private Action currentSuccessCallback;
    private Action currentFailCallback;

    private bool isMiniGamePlaying = false;

    public event Action OnMiniGameSuccess;
    public event Action OnMiniGameFail;

    // 씬 안에서 MiniGameManager가 하나만 존재하도록,
    // 인스펙터에 등록한 미니게임 목록을 빠르게 찾을 수 있게 저장
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveUIRoot();
        SetPopupRootActive(false);
        RegisterMiniGames();
    }

    // 테스트용 실행 코드
    // F키를 누르면 Debug에 지정한 미니게임 타입 또는 프리팹을 실행
    private void Update()
    {
        if (GameplayInputBlocker.ShouldIgnoreGameplayInput())
            return;

        if (Keyboard.current != null
            && isMiniGamePlaying
            && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseMiniGame();
            return;
        }

        if (Keyboard.current != null
            && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (debugMiniGameType != MiniGameType.None)
                OpenMiniGame(debugMiniGameType);
            else
                OpenMiniGame(debugMiniGamePrefab);
        }
    }

    // 매니저가 사라질 때 싱글톤 참조를 비워서
    // 다른 씬에서 새 매니저를 사용할 수 있게 한다.
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // 미니게임 타입만으로 미니게임을 실행합니다.
    // 성공/실패 후 추가로 실행할 동작이 없을 때 사용합니다.
    public bool OpenMiniGame(MiniGameType miniGameType)
    {
        return OpenMiniGame(
            miniGameType,
            null,
            null
        );
    }

    // 미니게임 타입으로 등록된 프리팹을 찾아 실행합니다.
    // 성공/실패 시 호출할 콜백을 함께 넘길 수 있습니다.
    public bool OpenMiniGame(
        MiniGameType miniGameType,
        Action onSuccess,
        Action onFail = null)
    {
        if (!miniGamePrefabs.TryGetValue(miniGameType, out BaseMiniGame prefab))
        {
            Debug.LogWarning($"등록되지 않은 미니게임입니다: {miniGameType}");
            return false;
        }

        return OpenMiniGame(
            prefab,
            onSuccess,
            onFail
        );
    }

    // 프리팹을 직접 넘겨 미니게임을 실행합니다.
    // 기존 테스트 씬이나 임시 테스트용으로 남겨둔 실행 방식입니다.
    public void OpenMiniGame(BaseMiniGame miniGamePrefab)
    {
        OpenMiniGame(
            miniGamePrefab,
            null,
            null
        );
    }

    // 실제 미니게임 프리팹을 생성하고 시작하는 공통 실행 함수입니다.
    // 이미 다른 미니게임이 실행 중이면 새 미니게임을 열지 않습니다.
    private bool OpenMiniGame(
        BaseMiniGame miniGamePrefab,
        Action onSuccess,
        Action onFail)
    {
        if (isMiniGamePlaying)
            return false;

        if (miniGamePrefab == null)
            return false;

        ResolveUIRoot();

        if (uiRoot == null)
        {
            Debug.LogWarning("미니게임 UI Root가 설정되지 않았습니다.");
            return false;
        }

        isMiniGamePlaying = true;
        currentSuccessCallback = onSuccess;
        currentFailCallback = onFail;

        SetPopupRootActive(true);
        BaseMiniGame miniGame = Instantiate(miniGamePrefab, uiRoot);

        miniGame.onSuccess = MiniGameSuccess;
        miniGame.onFail = MiniGameFail;

        currentMiniGame = miniGame;

        currentMiniGame.StartMiniGame();

        return true;
    }

    // 현재 실행 중인 미니게임을 닫고 실행 상태를 초기화합니다.
    public void CloseMiniGame()
    {
        if (currentMiniGame != null)
        {
            currentMiniGame.CloseMiniGame();
        }

        currentMiniGame = null;
        currentSuccessCallback = null;
        currentFailCallback = null;

        isMiniGamePlaying = false;
        SetPopupRootActive(false);
    }

    // 미니게임 성공 시 호출됩니다.
    // 런처에서 넘긴 성공 콜백을 실행한 뒤 미니게임을 닫습니다.
    public void MiniGameSuccess()
    {
        Debug.Log("미니게임 성공");
        currentSuccessCallback?.Invoke();
        OnMiniGameSuccess?.Invoke();
        CloseMiniGame();
    }

    // 미니게임 실패 시 호출됩니다.
    // 실패 콜백과 이벤트를 실행한 뒤 미니게임을 닫습니다.
    public void MiniGameFail()
    {
        Debug.Log("미니게임 실패");
        currentFailCallback?.Invoke();
        OnMiniGameFail?.Invoke();
        CloseMiniGame();
    }

    // 인스펙터의 미니게임 목록을 딕셔너리로 옮겨
    // MiniGameType만으로 프리팹을 찾을 수 있게 합니다.
    private void RegisterMiniGames()
    {
        miniGamePrefabs.Clear();

        foreach (MiniGameEntry miniGame in miniGames)
        {
            if (miniGame == null
                || miniGame.type == MiniGameType.None
                || miniGame.prefab == null)
            {
                continue;
            }

            miniGamePrefabs[miniGame.type] = miniGame.prefab;
        }
    }

    private void ResolveUIRoot()
    {
        if (uiRoot != null)
        {
            if (popupRoot == null && uiRoot.parent != null)
                popupRoot = uiRoot.parent.gameObject;

            return;
        }

        Transform foundRoot = FindSceneTransform("MiniGameRoot");
        if (foundRoot == null)
            return;

        uiRoot = foundRoot;

        if (popupRoot == null && foundRoot.parent != null)
            popupRoot = foundRoot.parent.gameObject;
    }

    private static Transform FindSceneTransform(string targetName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform transform in transforms)
        {
            if (transform.name != targetName
                || !transform.gameObject.scene.IsValid()
                || transform.hideFlags != HideFlags.None)
            {
                continue;
            }

            return transform;
        }

        return null;
    }

    private void SetPopupRootActive(bool active)
    {
        if (popupRoot != null)
            popupRoot.SetActive(active);
    }
}
