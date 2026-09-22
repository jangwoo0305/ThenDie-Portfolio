using System;
using System.Collections.Generic;
using Mirror;
using ThenDie.Ingame;
using ThenDie.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MeetingVoteUI : MonoBehaviour
{
    // 플레이어가 투표 대상을 확정했을 때 서버로 전달할 실제 네트워크 ID입니다.
    public event Action<uint> TargetConfirmed;
    // 플레이어가 투표 건너뛰기를 선택했을 때 서버로 전달할 신호입니다.
    public event Action VoteSkipped;

    [Header("Optional")]
    [SerializeField] private Transform profileRoot;
    [SerializeField] private Sprite deadProfileSprite;
    [SerializeField] private Button selectTargetButton;
    [SerializeField] private Button skipVoteButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text bottomGuideText;
    [SerializeField] private MeetingChatUI meetingChatUI;

    private static readonly Color MeetingTextColor = new Color(0.12f, 0.08f, 0.03f);
    private static readonly Color VotingTextColor = new Color(0.6f, 0.02f, 0.03f);
    private static readonly Color SelectedProfileColor = new Color(0.62f, 0.62f, 0.62f, 1f);

    private readonly List<ProfileView> profiles = new List<ProfileView>();
    private readonly List<int> deadPlayerNumbers = new List<int>();
    private readonly List<int> completedVotePlayerNumbers = new List<int>();
    private readonly List<uint> playerNetIds = new List<uint>();
    private readonly List<string> playerNames = new List<string>();
    private readonly List<int> revealedRoleIds = new List<int>();

    private int selectedIndex = -1;
    private int playerCount;
    private bool isVoting;
    private bool hasConfirmedVote;
    private bool currentPlayerDead;
    private float remainingTime;
    private Phase currentPhase;
    private string meetingReasonText = string.Empty;

    private enum Phase
    {
        CallerStatement,
        DiscussionAndVoting,
        VoteClosing,
        Result
    }

    private void Awake()
    {
        CollectPhaseTexts();
        CollectProfiles();
        CollectMeetingChatUI();
        SetPlayers(0, null, false);
        BeginMeetingPhase();
    }

    private void Update()
    {
        UpdateServerTimer();
    }

    // 서버에서 회의 참가 인원, 사망자 번호 목록, 현재 클라이언트의 사망 여부를 동기화할 때 호출합니다.
    public void SetPlayers(int count, IEnumerable<int> deadNumbers, bool isCurrentPlayerDead)
    {
        int maxPlayerCount = profiles.Count > 0 ? profiles.Count : 12;

        playerCount = Mathf.Clamp(count, 0, maxPlayerCount);
        currentPlayerDead = isCurrentPlayerDead;
        SyncChatPlayerState();

        deadPlayerNumbers.Clear();

        if (deadNumbers != null)
        {
            foreach (int deadNumber in deadNumbers)
            {
                deadPlayerNumbers.Add(Mathf.Clamp(deadNumber, 1, maxPlayerCount));
            }
        }

        ApplyPlayerState();
        SetVotingState(isVoting);
    }

    public void SetServerPlayers(
        uint[] netIds,
        string[] names,
        bool[] aliveStates,
        int[] colorIndices,
        int[] serverRevealedRoleIds)
    {
        playerNetIds.Clear();
        playerNames.Clear();
        revealedRoleIds.Clear();

        int serverCount = netIds != null ? netIds.Length : 0;
        int visibleCount = Mathf.Min(serverCount, profiles.Count);
        List<int> deadNumbers = new List<int>();
        bool localPlayerDead = false;
        uint localNetId = NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.netId
            : 0u;

        for (int i = 0; i < visibleCount; i++)
        {
            bool alive = aliveStates != null && i < aliveStates.Length && aliveStates[i];
            string playerName = names != null && i < names.Length &&
                                !string.IsNullOrWhiteSpace(names[i])
                ? names[i]
                : $"Player {netIds[i]}";

            playerNetIds.Add(netIds[i]);
            playerNames.Add(playerName);
            revealedRoleIds.Add(serverRevealedRoleIds != null &&
                                i < serverRevealedRoleIds.Length
                ? serverRevealedRoleIds[i]
                : -1);

            if (!alive)
                deadNumbers.Add(i + 1);
            if (netIds[i] == localNetId)
                localPlayerDead = !alive;
        }

        SetPlayers(visibleCount, deadNumbers, localPlayerDead);
        SetPlayerNames(playerNames);

        List<int> displayNumbers = new List<int>(visibleCount);
        for (int i = 0; i < visibleCount; i++)
            displayNumbers.Add(i + 1);
        SetPlayerNumbers(displayNumbers);

        ApplyPlayerColors(colorIndices, visibleCount);
    }

    public void SetMeetingContext(uint reporterNetId, uint victimNetId)
    {
        string reporterName = GetPlayerName(reporterNetId);
        if (victimNetId == MeetingManager.NoVictim)
        {
            meetingReasonText = $"{reporterName}님이 긴급 회의를 소집했습니다.";
        }
        else
        {
            string victimName = GetPlayerName(victimNetId);
            meetingReasonText = $"{reporterName}님이 {victimName}님의 시체를 발견했습니다.";
        }

        if (currentPhase == Phase.CallerStatement)
            ApplyCallerStatementVisuals();
    }

    public void MarkVoteCompleted(uint voterNetId)
    {
        int playerIndex = playerNetIds.IndexOf(voterNetId);
        if (playerIndex < 0)
            return;

        int playerNumber = playerIndex + 1;
        if (!completedVotePlayerNumbers.Contains(playerNumber))
            completedVotePlayerNumbers.Add(playerNumber);
        ApplyPlayerState();
    }

    public string GetPlayerName(uint netId)
    {
        int index = playerNetIds.IndexOf(netId);
        return index >= 0 && index < playerNames.Count
            ? playerNames[index]
            : $"Player {netId}";
    }

    // 서버에서 투표를 완료한 플레이어 번호 목록을 동기화할 때 호출합니다.
    public void SetCompletedVotes(IEnumerable<int> completedPlayerNumbers)
    {
        int maxPlayerCount = profiles.Count > 0 ? profiles.Count : 12;

        completedVotePlayerNumbers.Clear();

        if (completedPlayerNumbers != null)
        {
            foreach (int completedPlayerNumber in completedPlayerNumbers)
            {
                completedVotePlayerNumbers.Add(Mathf.Clamp(completedPlayerNumber, 1, maxPlayerCount));
            }
        }

        ApplyPlayerState();
    }

    // 서버에서 플레이어 번호 순서에 맞는 닉네임 목록을 받은 뒤 호출합니다.
    public void SetPlayerNames(IList<string> playerNames)
    {
        for (int i = 0; i < profiles.Count; i++)
        {
            string playerName = playerNames != null && i < playerNames.Count ? playerNames[i] : string.Empty;
            SetText(profiles[i].NameText, playerName);
        }
    }

    // 서버에서 고정 플레이어 번호를 받은 뒤 호출합니다. 게임 시작 시 배정된 번호가 회의마다 같은 자리에 보이도록 사용합니다.
    public void SetPlayerNumbers(IList<int> playerNumbers)
    {
        for (int i = 0; i < profiles.Count; i++)
        {
            int playerNumber = playerNumbers != null && i < playerNumbers.Count ? playerNumbers[i] : i + 1;
            SetText(profiles[i].PlayerNumberText, playerNumber.ToString());
        }
    }

    // 서버 또는 계정 데이터에서 플레이어 프로필 이미지를 받은 뒤 호출합니다.
    public void SetPlayerAvatars(IList<Sprite> playerAvatars)
    {
        if (playerAvatars == null)
            return;

        for (int i = 0; i < profiles.Count && i < playerAvatars.Count; i++)
        {
            if (profiles[i].UserImage != null && playerAvatars[i] != null)
            {
                profiles[i].UserImage.sprite = playerAvatars[i];
            }
        }
    }

    private void ApplyPlayerColors(int[] colorIndices, int visibleCount)
    {
        ColorDatabase database = null;

        foreach (uint netId in playerNetIds)
        {
            if (!NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity) ||
                identity == null)
                continue;

            PlayerAppearance appearance = identity.GetComponent<PlayerAppearance>();
            if (appearance?.Database != null)
            {
                database = appearance.Database;
                break;
            }
        }

        for (int i = 0; i < profiles.Count; i++)
        {
            if (profiles[i].UserImage == null)
                continue;

            if (i < visibleCount && database != null && colorIndices != null &&
                i < colorIndices.Length && database.IsValid(colorIndices[i]))
            {
                Sprite profileSprite = database.GetProfileSprite(colorIndices[i]);
                if (profileSprite != null)
                {
                    profiles[i].UserImage.sprite = profileSprite;
                    profiles[i].UserImage.color = Color.white;
                }
                else
                {
                    profiles[i].UserImage.color = database.GetUiTint(colorIndices[i]);
                }
            }
            else
            {
                profiles[i].UserImage.color = Color.white;
            }
        }
    }

    // 서버에서 현재 회의 단계가 투표 가능 상태인지 내려줄 때 호출합니다.
    public void SetVotingState(bool voting)
    {
        isVoting = voting;

        for (int i = 0; i < profiles.Count; i++)
        {
            bool visible = i < playerCount;
            bool dead = IsDeadProfile(i);

            ApplyProfileVisual(i, visible, dead);
            profiles[i].Button.interactable = visible && !dead && isVoting &&
                                              !hasConfirmedVote && !currentPlayerDead;
        }

        if (skipVoteButton != null)
        {
            skipVoteButton.interactable = isVoting && !hasConfirmedVote && !currentPlayerDead;
        }

        if (!hasConfirmedVote || currentPlayerDead)
        {
            ClearSelection();
        }
    }

    // 서버에서 회의 토론 단계 시작을 통보했을 때 호출합니다.
    public void StartMeeting()
    {
        BeginMeetingPhase();
    }

    // 서버에서 투표 단계 시작을 통보했을 때 호출합니다.
    public void StartVoting()
    {
        BeginVotingPhase();
    }

    public void StartVoteClosing()
    {
        BeginVoteClosingPhase();
    }

    public void StartResultPhase()
    {
        BeginResultPhase();
    }

    // 서버에서 투표 단계 종료를 통보했을 때 호출합니다.
    public void EndVoting()
    {
        EndVotingPhase();
    }

    // 서버에서 받은 남은 시간을 UI에 표시할 때 호출합니다.
    public void SetRemainingTime(float seconds)
    {
        remainingTime = Mathf.Max(0f, seconds);
        UpdateTimerText();
    }

    private void SelectProfile(int index)
    {
        if (!isVoting || hasConfirmedVote || currentPlayerDead)
            return;

        if (index < 0 || index >= profiles.Count || index >= playerCount)
            return;

        if (IsDeadProfile(index))
            return;

        if (selectedIndex >= 0)
        {
            SetProfileSelected(selectedIndex, false);
        }

        selectedIndex = index;
        SetProfileSelected(selectedIndex, true);

        if (selectTargetButton != null)
        {
            selectTargetButton.interactable = true;
        }
    }

    private void ClearSelection()
    {
        selectedIndex = -1;

        foreach (ProfileView profile in profiles)
        {
            if (profile.ProfileImage != null)
            {
                profile.ProfileImage.color = profile.AliveColor;
            }
        }

        if (selectTargetButton != null)
        {
            selectTargetButton.interactable = false;
        }
    }

    // 대상 선택 버튼에서 호출됩니다. 서버가 내려준 실제 netId를 TargetConfirmed로 전달합니다.
    public void ConfirmTarget()
    {
        if (!isVoting || hasConfirmedVote || currentPlayerDead || selectedIndex < 0 ||
            selectedIndex >= playerNetIds.Count || IsDeadProfile(selectedIndex))
            return;

        hasConfirmedVote = true;
        uint selectedPlayerNetId = playerNetIds[selectedIndex];

        foreach (ProfileView profile in profiles)
        {
            profile.Button.interactable = false;
        }

        if (selectTargetButton != null)
        {
            selectTargetButton.interactable = false;
        }

        if (skipVoteButton != null)
        {
            skipVoteButton.interactable = false;
        }

        if (TargetConfirmed != null)
        {
            TargetConfirmed.Invoke(selectedPlayerNetId);
        }

        Debug.Log($"[MeetingVoteUI] 투표 대상 netId: {selectedPlayerNetId}");
    }

    // 투표 건너뛰기 버튼에서 호출됩니다. 건너뛰기 요청은 VoteSkipped 이벤트를 통해 서버로 전달합니다.
    public void SkipVote()
    {
        if (!isVoting || hasConfirmedVote || currentPlayerDead)
            return;

        hasConfirmedVote = true;
        ClearSelection();

        foreach (ProfileView profile in profiles)
        {
            profile.Button.interactable = false;
        }

        if (skipVoteButton != null)
        {
            skipVoteButton.interactable = false;
        }

        if (VoteSkipped != null)
        {
            VoteSkipped.Invoke();
        }

        Debug.Log("투표를 건너뛰었습니다.");
    }

    private void BeginMeetingPhase()
    {
        currentPhase = Phase.CallerStatement;
        remainingTime = 0f;
        hasConfirmedVote = false;
        completedVotePlayerNumbers.Clear();

        SetVotingState(false);
        ApplyCallerStatementVisuals();
        UpdateTimerText();
    }

    private void BeginVotingPhase()
    {
        currentPhase = Phase.DiscussionAndVoting;
        remainingTime = 0f;
        hasConfirmedVote = false;
        completedVotePlayerNumbers.Clear();

        SetVotingState(true);
        ApplyVotingVisuals();
        UpdateTimerText();
    }

    private void EndVotingPhase()
    {
        BeginResultPhase();
    }

    private void BeginVoteClosingPhase()
    {
        currentPhase = Phase.VoteClosing;
        remainingTime = 0f;
        hasConfirmedVote = true;

        SetVotingState(false);
        SetText(titleText, "투표 마감");
        SetText(descriptionText, "투표가 마감되었습니다. 서버에서 결과를 집계하고 있습니다.");
        SetText(bottomGuideText, "잠시만 기다려 주세요.");
        SetTextColor(titleText, VotingTextColor);
        SetTextColor(timerText, VotingTextColor);
        UpdateTimerText();
    }

    private void BeginResultPhase()
    {
        currentPhase = Phase.Result;
        remainingTime = 0f;
        hasConfirmedVote = true;

        SetVotingState(false);
        SetText(titleText, "투표 결과");
        SetText(descriptionText, "투표 결과가 공개되었습니다.");
        SetText(bottomGuideText, string.Empty);
        UpdateTimerText();
    }

    private void ApplyCallerStatementVisuals()
    {
        SetText(titleText, "호출자 발언 시간");
        SetText(descriptionText, string.IsNullOrEmpty(meetingReasonText)
            ? "호출자만 발언할 수 있습니다."
            : $"{meetingReasonText}\n호출자만 발언할 수 있습니다.");
        SetText(bottomGuideText, "호출자의 발언을 들어주세요.");

        SetTextColor(titleText, MeetingTextColor);
        SetTextColor(timerText, MeetingTextColor);
    }

    private void ApplyVotingVisuals()
    {
        SetText(titleText, "투표 및 발언 시간");
        SetText(descriptionText, "모두 발언할 수 있습니다.\n의심되는 참가자에게 투표하세요.");
        SetText(bottomGuideText, "투표 혹은 건너뛰기를 선택하세요.");

        SetTextColor(titleText, VotingTextColor);
        SetTextColor(timerText, VotingTextColor);
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
        int minutes = seconds / 60;
        int secondsOnly = seconds % 60;

        timerText.text = string.Format("{0:00}:{1:00}", minutes, secondsOnly);
    }

    private void UpdateServerTimer()
    {
        double endTime = 0d;
        if (currentPhase == Phase.CallerStatement && MeetingManager.Instance != null)
            endTime = MeetingManager.Instance.discussionEndTime;
        else if (currentPhase == Phase.DiscussionAndVoting && VoteManager.Instance != null)
            endTime = VoteManager.Instance.votingEndTime;
        else if (currentPhase == Phase.VoteClosing && VoteManager.Instance != null)
            endTime = VoteManager.Instance.voteClosingEndTime;
        else if (currentPhase == Phase.Result && MeetingManager.Instance != null)
            endTime = MeetingManager.Instance.stageEndTime;

        remainingTime = endTime > 0d
            ? (float)Math.Max(0d, endTime - NetworkTime.time)
            : 0f;
        UpdateTimerText();
    }

    private void SyncChatPlayerState()
    {
        if (meetingChatUI == null)
        {
            CollectMeetingChatUI();
        }

        if (meetingChatUI != null)
        {
            meetingChatUI.SetCurrentPlayerDead(currentPlayerDead);
        }
    }

    private void ApplyPlayerState()
    {
        if (profiles.Count == 0)
            return;

        playerCount = Mathf.Clamp(playerCount, 0, profiles.Count);

        for (int i = 0; i < profiles.Count; i++)
        {
            bool visible = i < playerCount;
            bool dead = visible && IsDeadProfile(i);
            ApplyProfileVisual(i, visible, dead);
            profiles[i].Button.interactable = visible && !dead && isVoting &&
                                              !hasConfirmedVote && !currentPlayerDead;
        }
    }

    private void CollectPhaseTexts()
    {
        if (titleText == null)
            titleText = FindTextByObjectName(transform, "TitleText");

        if (timerText == null)
            timerText = FindTextByObjectName(transform, "TimerText");

        if (bottomGuideText == null)
            bottomGuideText = FindTextByObjectName(transform, "Text (TMP)");

        if (descriptionText == null)
            descriptionText = FindDescriptionText();
    }

    private void CollectProfiles()
    {
        profiles.Clear();

        Transform root = IsValidProfileRoot(profileRoot) ? profileRoot : transform;
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        List<Transform> profileTransforms = new List<Transform>();

        foreach (Transform child in children)
        {
            int profileNumber;
            if (TryGetProfileNumber(child.name, out profileNumber))
            {
                profileTransforms.Add(child);
            }
        }

        profileTransforms.Sort((a, b) =>
        {
            int numberA;
            int numberB;

            TryGetProfileNumber(a.name, out numberA);
            TryGetProfileNumber(b.name, out numberB);

            return numberA.CompareTo(numberB);
        });

        foreach (Transform profileTransform in profileTransforms)
        {
            Button button = profileTransform.GetComponent<Button>();

            if (button == null)
                continue;

            int index = profiles.Count;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectProfile(index));

            GameObject deathOverlay = FindChildByName(profileTransform, "DeathOverlay");

            if (deathOverlay == null)
                continue;

            GameObject completeVote = FindChildByName(profileTransform, "CompleteVote");
            GameObject beforeVote = FindChildByName(profileTransform, "BeforeVote");
            GameObject cantVote = FindChildByName(profileTransform, "CantVote");
            if (cantVote == null)
            {
                cantVote = FindChildByName(profileTransform, "Cantvote");
            }

            TMP_Text nameText = FindTextByObjectName(profileTransform, "NameText");
            TMP_Text playerNumberText = FindTextUnderChild(profileTransform, "PlayerNumber");
            TMP_Text stateText = FindTextByObjectName(profileTransform, "StateText");
            Image userImage = FindImageByObjectName(profileTransform, "Userimage");
            Image profileImage = profileTransform.GetComponent<Image>();
            Sprite aliveSprite = profileImage != null ? profileImage.sprite : null;
            Color aliveColor = profileImage != null ? profileImage.color : Color.white;

            deathOverlay.SetActive(false);
            SetText(playerNumberText, (index + 1).ToString());
            profiles.Add(new ProfileView(profileTransform.gameObject, button, deathOverlay, completeVote, beforeVote, cantVote, nameText, playerNumberText, stateText, userImage, profileImage, aliveSprite, aliveColor));
        }

        if (selectTargetButton == null)
        {
            GameObject voteButtonObject = FindChildByName(transform, "VoteButton");
            if (voteButtonObject != null)
            {
                selectTargetButton = voteButtonObject.GetComponent<Button>();
            }
        }

        if (selectTargetButton != null)
        {
            selectTargetButton.onClick.AddListener(ConfirmTarget);
        }

        if (skipVoteButton == null)
        {
            GameObject skipButtonObject = FindChildByName(transform, "SkipButton");
            if (skipButtonObject != null)
            {
                skipVoteButton = skipButtonObject.GetComponent<Button>();
            }
        }

        if (skipVoteButton != null)
        {
            skipVoteButton.onClick.AddListener(SkipVote);
        }
    }

    private void CollectMeetingChatUI()
    {
        if (meetingChatUI == null)
        {
            meetingChatUI = FindFirstObjectByType<MeetingChatUI>();
        }
    }

    private static bool TryGetProfileNumber(string objectName, out int number)
    {
        number = 0;

        if (!objectName.StartsWith("Profile_"))
            return false;

        string numberText = objectName.Substring("Profile_".Length);
        return int.TryParse(numberText, out number);
    }

    private bool IsValidProfileRoot(Transform root)
    {
        return root != null && (root == transform || root.IsChildOf(transform));
    }

    private bool IsDeadProfile(int profileIndex)
    {
        int playerNumber = profileIndex + 1;

        foreach (int deadPlayerNumber in deadPlayerNumbers)
        {
            if (deadPlayerNumber == playerNumber)
                return true;
        }

        return false;
    }

    private bool HasCompletedVote(int profileIndex)
    {
        int playerNumber = profileIndex + 1;

        foreach (int completedVotePlayerNumber in completedVotePlayerNumbers)
        {
            if (completedVotePlayerNumber == playerNumber)
                return true;
        }

        return false;
    }

    private void ApplyProfileVisual(int profileIndex, bool visible, bool dead)
    {
        ProfileView profile = profiles[profileIndex];
        profile.Root.SetActive(visible);

        bool canUseDeadSprite = profile.ProfileImage != null && deadProfileSprite != null;
        if (profile.ProfileImage != null)
        {
            profile.ProfileImage.sprite = dead && canUseDeadSprite ? deadProfileSprite : profile.AliveSprite;
            profile.ProfileImage.color = profileIndex == selectedIndex ? SelectedProfileColor : profile.AliveColor;
        }

        profile.DeathOverlay.SetActive(visible && dead && !canUseDeadSprite);

        bool completedVote = visible && !dead && HasCompletedVote(profileIndex);
        SetActiveIfExists(profile.CompleteVote, completedVote);
        SetActiveIfExists(profile.BeforeVote, visible && !dead && !completedVote);
        SetActiveIfExists(profile.CantVote, visible && dead);
        SetText(profile.StateText, dead ? GetDeadStateText(profileIndex) : "생존");
    }

    private string GetDeadStateText(int profileIndex)
    {
        if (profileIndex < 0 || profileIndex >= revealedRoleIds.Count ||
            revealedRoleIds[profileIndex] < 0)
            return "사망";

        return $"사망 · {GetRoleDisplayName((RoleId)revealedRoleIds[profileIndex])}";
    }

    private static string GetRoleDisplayName(RoleId role)
    {
        return role switch
        {
            RoleId.Mafia => "마피아",
            RoleId.Detective => "탐정",
            _ => "시민",
        };
    }

    private void SetProfileSelected(int profileIndex, bool selected)
    {
        if (profileIndex < 0 || profileIndex >= profiles.Count)
            return;

        ProfileView profile = profiles[profileIndex];

        if (profile.ProfileImage != null)
        {
            profile.ProfileImage.color = selected ? SelectedProfileColor : profile.AliveColor;
        }
    }

    private TMP_Text FindDescriptionText()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            if (text.gameObject.name != "LabelText")
                continue;

            if (string.IsNullOrEmpty(text.text))
                continue;

            if (text.text.Contains("의견") || text.text.Contains("투표"))
                return text;
        }

        return null;
    }

    private static TMP_Text FindTextByObjectName(Transform root, string objectName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name != objectName)
                continue;

            return child.GetComponent<TMP_Text>();
        }

        return null;
    }

    private static TMP_Text FindTextUnderChild(Transform root, string childName)
    {
        GameObject child = FindChildByName(root, childName);
        return child != null ? child.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private static Image FindImageByObjectName(Transform root, string objectName)
    {
        GameObject child = FindChildByName(root, objectName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static void SetTextColor(TMP_Text text, Color color)
    {
        if (text != null)
        {
            text.color = color;
        }
    }

    private static void SetActiveIfExists(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private static GameObject FindChildByName(Transform root, string childName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    private struct ProfileView
    {
        public readonly GameObject Root;
        public readonly Button Button;
        public readonly GameObject DeathOverlay;
        public readonly GameObject CompleteVote;
        public readonly GameObject BeforeVote;
        public readonly GameObject CantVote;
        public readonly TMP_Text NameText;
        public readonly TMP_Text PlayerNumberText;
        public readonly TMP_Text StateText;
        public readonly Image UserImage;
        public readonly Image ProfileImage;
        public readonly Sprite AliveSprite;
        public readonly Color AliveColor;

        public ProfileView(GameObject root, Button button, GameObject deathOverlay, GameObject completeVote, GameObject beforeVote, GameObject cantVote, TMP_Text nameText, TMP_Text playerNumberText, TMP_Text stateText, Image userImage, Image profileImage, Sprite aliveSprite, Color aliveColor)
        {
            Root = root;
            Button = button;
            DeathOverlay = deathOverlay;
            CompleteVote = completeVote;
            BeforeVote = beforeVote;
            CantVote = cantVote;
            NameText = nameText;
            PlayerNumberText = playerNumberText;
            StateText = stateText;
            UserImage = userImage;
            ProfileImage = profileImage;
            AliveSprite = aliveSprite;
            AliveColor = aliveColor;
        }
    }
}
