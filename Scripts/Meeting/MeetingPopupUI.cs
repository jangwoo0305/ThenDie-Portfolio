using System;
using System.Collections.Generic;
using Mirror;
using ThenDie.Ingame;
using ThenDie.Player;
using ThenDie.Sound;
using UnityEngine;
using UnityEngine.UI;

public class MeetingPopupUI : MonoBehaviour
{
    [Header("Optional")]
    [SerializeField] private GameObject meetingPopup;
    [SerializeField] private MeetingVoteUI meetingVoteUI;
    [SerializeField] private MeetingChatUI meetingChatUI;
    [SerializeField] private MeetingResultPopupUI meetingResultPopupUI;
    [SerializeField] private Button miniMapButton;
    [SerializeField] private Button speakButton;
    [SerializeField] private Button listenButton;
    [SerializeField] private Image speakIcon;
    [SerializeField] private Image listenIcon;

    private MiniMapToggleUI miniMapToggleUI;
    private GameObject speakMutedOverlay;
    private GameObject listenMutedOverlay;

    private void Awake()
    {
        CollectReferences();
    }

    private void OnEnable()
    {
        CollectReferences();
        BindServerEvents();
        RefreshVoiceControls();
    }

    private void OnDisable()
    {
        UnbindServerEvents();
    }

    private void Update()
    {
        if (meetingPopup != null && meetingPopup.activeInHierarchy)
            RefreshVoiceControls();
    }

    // 서버에서 회의 시작을 통보했을 때 호출합니다.
    public void OpenMeeting()
    {
        CollectReferences();

        if (meetingPopup != null)
        {
            meetingPopup.SetActive(true);
        }

        if (meetingVoteUI != null)
        {
            meetingVoteUI.StartMeeting();
        }

        if (meetingResultPopupUI != null)
        {
            meetingResultPopupUI.Hide();
        }

        if (meetingChatUI != null)
        {
            meetingChatUI.ClearMessages();
            meetingChatUI.SetServerAuthoritative(true);
            meetingChatUI.SetInputInteractable(false);
        }

        miniMapToggleUI?.HideMiniMap();
        RefreshVoiceControls();
    }

    public void OpenMeeting(
        uint reporterNetId,
        uint victimNetId,
        uint[] playerNetIds,
        string[] playerNames,
        bool[] playerAliveStates,
        int[] playerColorIndices,
        int[] revealedRoleIds)
    {
        OpenMeeting();

        meetingVoteUI?.SetServerPlayers(
            playerNetIds,
            playerNames,
            playerAliveStates,
            playerColorIndices,
            revealedRoleIds
        );
        meetingVoteUI?.SetMeetingContext(reporterNetId, victimNetId);
        meetingVoteUI?.StartMeeting();
        ApplyMeetingStage(MeetingStage.CallerStatement, reporterNetId);

        if (meetingChatUI == null || NetworkClient.localPlayer == null)
            return;

        int localIndex = Array.IndexOf(playerNetIds, NetworkClient.localPlayer.netId);
        string localName = localIndex >= 0 && playerNames != null && localIndex < playerNames.Length
            ? playerNames[localIndex]
            : "Player";
        bool localDead = localIndex >= 0 && playerAliveStates != null &&
                         localIndex < playerAliveStates.Length && !playerAliveStates[localIndex];
        meetingChatUI.SetCurrentPlayer(localName, localDead);
    }

    public void StartVoting()
    {
        meetingVoteUI?.StartVoting();
        ApplyMeetingStage(MeetingStage.DiscussionAndVoting, 0u);
    }

    public void StartVoteClosing()
    {
        meetingVoteUI?.StartVoteClosing();
        ApplyMeetingStage(MeetingStage.VoteClosing, 0u);
        miniMapToggleUI?.HideMiniMap();
    }

    public void StartResultPhase()
    {
        meetingVoteUI?.StartResultPhase();
        ApplyMeetingStage(MeetingStage.Result, 0u);
        miniMapToggleUI?.HideMiniMap();
    }

    public void ReceiveChatMessage(string senderName, string message, bool senderIsDead)
    {
        meetingChatUI?.ReceiveMessage(senderName, message, senderIsDead);
    }

    // 서버에서 회의 시작 데이터까지 함께 받은 뒤 호출합니다.
    public void OpenMeeting(int playerCount, IEnumerable<int> deadNumbers, bool isCurrentPlayerDead)
    {
        OpenMeeting();

        if (meetingVoteUI != null)
        {
            meetingVoteUI.SetPlayers(playerCount, deadNumbers, isCurrentPlayerDead);
        }
    }

    // 서버에서 회의 시작 데이터와 현재 플레이어 닉네임을 함께 받은 뒤 호출합니다.
    public void OpenMeeting(string currentPlayerName, int playerCount, IEnumerable<int> deadNumbers, bool isCurrentPlayerDead)
    {
        OpenMeeting(playerCount, deadNumbers, isCurrentPlayerDead);

        if (meetingChatUI != null)
        {
            meetingChatUI.SetCurrentPlayer(currentPlayerName, isCurrentPlayerDead);
        }
    }

    // 서버에서 투표 종료 후 추방된 플레이어의 닉네임을 받았을 때 호출합니다.
    public void ShowEjectionResult(string playerName)
    {
        if (meetingResultPopupUI != null)
        {
            meetingResultPopupUI.ShowEjectionResult(playerName);
        }
    }

    public void ShowEjectionResult(string playerName, RoleId role)
    {
        meetingResultPopupUI?.ShowEjectionResult(playerName, role);
    }

    // 서버에서 동률 또는 스킵 처리로 추방자가 없다고 판단했을 때 호출합니다.
    public void ShowNoEjectionResult()
    {
        if (meetingResultPopupUI != null)
        {
            meetingResultPopupUI.ShowNoEjectionResult();
        }
    }

    // 결과 팝업을 닫고 회의 화면으로 돌아갈 때 호출합니다.
    public void HideEjectionResult()
    {
        if (meetingResultPopupUI != null)
        {
            meetingResultPopupUI.Hide();
        }
    }

    // 서버에서 회의 종료를 통보했을 때 호출합니다.
    public void CloseMeeting()
    {
        if (meetingResultPopupUI != null)
        {
            meetingResultPopupUI.Hide();
        }

        if (meetingChatUI != null)
        {
            meetingChatUI.SetChatVisible(false);
            meetingChatUI.SetInputInteractable(false);
        }

        if (meetingPopup != null)
        {
            meetingPopup.SetActive(false);
        }

        miniMapToggleUI?.HideMiniMap();
    }

    private void BindServerEvents()
    {
        if (meetingVoteUI != null)
        {
            meetingVoteUI.TargetConfirmed -= HandleTargetConfirmed;
            meetingVoteUI.TargetConfirmed += HandleTargetConfirmed;
            meetingVoteUI.VoteSkipped -= HandleVoteSkipped;
            meetingVoteUI.VoteSkipped += HandleVoteSkipped;
        }

        if (meetingChatUI != null)
        {
            meetingChatUI.MessageSubmitted -= HandleChatSubmitted;
            meetingChatUI.MessageSubmitted += HandleChatSubmitted;
        }

        if (miniMapButton != null)
        {
            miniMapButton.onClick.RemoveListener(ToggleMeetingMiniMap);
            miniMapButton.onClick.AddListener(ToggleMeetingMiniMap);
        }

        if (speakButton != null)
        {
            speakButton.onClick.RemoveListener(ToggleLocalMicrophone);
            speakButton.onClick.AddListener(ToggleLocalMicrophone);
        }

        if (listenButton != null)
        {
            listenButton.onClick.RemoveListener(ToggleLocalSpeaker);
            listenButton.onClick.AddListener(ToggleLocalSpeaker);
        }

        VoteManager.OnVoteSubmitted -= HandleVoteSubmitted;
        VoteManager.OnVoteSubmitted += HandleVoteSubmitted;
        VoteManager.OnVoteResultRevealed -= HandleVoteResult;
        VoteManager.OnVoteResultRevealed += HandleVoteResult;
    }

    private void UnbindServerEvents()
    {
        if (meetingVoteUI != null)
        {
            meetingVoteUI.TargetConfirmed -= HandleTargetConfirmed;
            meetingVoteUI.VoteSkipped -= HandleVoteSkipped;
        }

        if (meetingChatUI != null)
            meetingChatUI.MessageSubmitted -= HandleChatSubmitted;

        if (miniMapButton != null)
            miniMapButton.onClick.RemoveListener(ToggleMeetingMiniMap);

        if (speakButton != null)
            speakButton.onClick.RemoveListener(ToggleLocalMicrophone);

        if (listenButton != null)
            listenButton.onClick.RemoveListener(ToggleLocalSpeaker);

        VoteManager.OnVoteSubmitted -= HandleVoteSubmitted;
        VoteManager.OnVoteResultRevealed -= HandleVoteResult;
    }

    private static PlayerAbilities GetLocalAbilities()
    {
        return NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.GetComponent<PlayerAbilities>()
            : null;
    }

    private void HandleTargetConfirmed(uint targetNetId)
    {
        GetLocalAbilities()?.CmdCastVote(targetNetId);
    }

    private void HandleVoteSkipped()
    {
        GetLocalAbilities()?.CmdCastVote(VoteManager.VoteSkip);
    }

    private void HandleChatSubmitted(string message)
    {
        GetLocalAbilities()?.CmdSendMeetingChat(message);
    }

    private void HandleVoteSubmitted(uint voterNetId)
    {
        meetingVoteUI?.MarkVoteCompleted(voterNetId);
    }

    private void HandleVoteResult(int ejectedNetId, RoleId ejectedRole)
    {
        meetingVoteUI?.EndVoting();
        if (ejectedNetId < 0)
        {
            ShowNoEjectionResult();
            return;
        }

        if (ejectedRole != RoleId.Citizen)
            return;

        ShowEjectionResult(
            meetingVoteUI != null
                ? meetingVoteUI.GetPlayerName((uint)ejectedNetId)
                : $"Player {ejectedNetId}",
            ejectedRole);
    }

    private void CollectReferences()
    {
        if (meetingPopup == null)
        {
            meetingPopup = gameObject;
        }

        if (meetingVoteUI == null)
        {
            meetingVoteUI = GetComponentInChildren<MeetingVoteUI>(true);
        }

        if (meetingChatUI == null)
        {
            meetingChatUI = GetComponentInChildren<MeetingChatUI>(true);
        }

        if (meetingResultPopupUI == null)
        {
            meetingResultPopupUI = GetComponentInChildren<MeetingResultPopupUI>(true);
        }

        if (miniMapButton == null || speakButton == null || listenButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (miniMapButton == null && button.name == "MinimapButton")
                    miniMapButton = button;
                else if (speakButton == null && button.name == "SpeakButton")
                    speakButton = button;
                else if (listenButton == null && button.name == "ListenButton")
                    listenButton = button;
            }
        }

        if (speakIcon == null)
            speakIcon = FindButtonIcon(speakButton);
        if (listenIcon == null)
            listenIcon = FindButtonIcon(listenButton);

        ConfigureVoiceButton(speakButton, speakIcon, ref speakMutedOverlay);
        ConfigureVoiceButton(listenButton, listenIcon, ref listenMutedOverlay);

        if (miniMapToggleUI == null)
        {
            miniMapToggleUI = FindFirstObjectByType<MiniMapToggleUI>(
                FindObjectsInactive.Include);
        }
    }

    private void ToggleMeetingMiniMap()
    {
        miniMapToggleUI?.ToggleFromMeeting(GetComponentInParent<Canvas>());
    }

    private void ToggleLocalMicrophone()
    {
        ProximityVoiceManager voiceManager = ProximityVoiceManager.Instance;
        if (voiceManager == null)
            return;

        voiceManager.SetLocalInputMuted(!voiceManager.IsLocalInputMuted);
        RefreshVoiceControls();
    }

    private void ToggleLocalSpeaker()
    {
        ProximityVoiceManager voiceManager = ProximityVoiceManager.Instance;
        if (voiceManager == null)
            return;

        voiceManager.SetLocalOutputMuted(!voiceManager.IsLocalOutputMuted);
        RefreshVoiceControls();
    }

    private void RefreshVoiceControls()
    {
        ProximityVoiceManager voiceManager = ProximityVoiceManager.Instance;
        bool voiceAvailable = voiceManager != null;

        if (speakButton != null)
            speakButton.interactable = voiceAvailable;
        if (listenButton != null)
            listenButton.interactable = voiceAvailable;

        SetOverlayActive(
            speakMutedOverlay,
            voiceAvailable && voiceManager.IsLocalInputMuted);
        SetOverlayActive(
            listenMutedOverlay,
            voiceAvailable && voiceManager.IsLocalOutputMuted);
    }

    private static Image FindButtonIcon(Button button)
    {
        if (button == null)
            return null;

        foreach (Image image in button.GetComponentsInChildren<Image>(true))
        {
            if (image.transform != button.transform && image.sprite != null)
                return image;
        }

        return null;
    }

    private static void ConfigureVoiceButton(
        Button button,
        Image icon,
        ref GameObject mutedOverlay)
    {
        if (button == null || icon == null)
            return;

        icon.preserveAspect = true;
        icon.raycastTarget = false;
        button.targetGraphic = icon;

        if (mutedOverlay == null)
            mutedOverlay = CreateMutedOverlay(button.transform);
    }

    private static GameObject CreateMutedOverlay(Transform parent)
    {
        Transform existing = parent.Find("MutedOverlay");
        if (existing != null)
            return existing.gameObject;

        GameObject overlay = new GameObject(
            "MutedOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        overlay.layer = parent.gameObject.layer;
        overlay.transform.SetParent(parent, false);

        RectTransform overlayRect = (RectTransform)overlay.transform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image shade = overlay.GetComponent<Image>();
        shade.color = new Color(0.08f, 0.02f, 0.02f, 0.52f);
        shade.raycastTarget = false;

        GameObject slash = new GameObject(
            "Slash",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        slash.layer = parent.gameObject.layer;
        slash.transform.SetParent(overlay.transform, false);

        RectTransform slashRect = (RectTransform)slash.transform;
        slashRect.anchorMin = new Vector2(0.5f, 0.5f);
        slashRect.anchorMax = new Vector2(0.5f, 0.5f);
        slashRect.anchoredPosition = Vector2.zero;
        slashRect.sizeDelta = new Vector2(74f, 8f);
        slashRect.localRotation = Quaternion.Euler(0f, 0f, -45f);

        Image slashImage = slash.GetComponent<Image>();
        slashImage.color = new Color(0.95f, 0.16f, 0.12f, 1f);
        slashImage.raycastTarget = false;

        overlay.SetActive(false);
        return overlay;
    }

    private static void SetOverlayActive(GameObject overlay, bool active)
    {
        if (overlay != null && overlay.activeSelf != active)
            overlay.SetActive(active);
    }

    private void ApplyMeetingStage(MeetingStage meetingStage, uint reporterNetId)
    {
        uint localNetId = NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.netId
            : 0u;
        bool canSpeak = meetingStage == MeetingStage.DiscussionAndVoting ||
                        (meetingStage == MeetingStage.CallerStatement &&
                         localNetId == reporterNetId);

        meetingChatUI?.SetInputInteractable(canSpeak);
        ProximityVoiceManager.Instance?.SetMeetingSpeakingAllowed(canSpeak);
    }
}
