using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using ThenDie.Player;
using ThenDie.Scenes.Main;
using ThenDie.Sound;
using ThenDie.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ThenDie.Ingame
{
    public class CommonHUDController : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] private TMP_Text playerRoleText;
        [SerializeField] private TMP_Text statusText;

        [Header("Mission Dropdown")]
        [SerializeField] private RectTransform missionDropdownPanel;
        [SerializeField] private GameObject missionListArea;
        [SerializeField] private Button missionDropdownButton;
        [SerializeField] private TMP_Text missionProgressText;
        [SerializeField] private TMP_Text missionArrowText;
        [SerializeField] private RectTransform missionProgressBarFill;
        [SerializeField] private GameObject missionItemPrefab;
        [SerializeField] private float collapsedHeight = 80f;
        [SerializeField] private float expandedHeight = 260f;
        [SerializeField] private float dropdownDuration = 0.18f;
        [SerializeField] private string collapsedArrow = "▼";
        [SerializeField] private string expandedArrow = "▲";
        [SerializeField] private float missionListTopOffset = 78f;
        [SerializeField] private float missionItemHeight = 44f;
        [SerializeField] private float missionItemSpacing = 12f;
        [SerializeField] private float missionListBottomPadding = 22f;

        [Header("Utility Buttons")]
        [SerializeField] private Button soundButton;
        [SerializeField] private Button micButton;
        [SerializeField] private Button chatButton;
        [SerializeField] private Button settingButton;
        [SerializeField] private Button chatCloseButton;
        [SerializeField] private Button chatSubmitButton;
        [SerializeField] private GameObject soundMutedIndicator;
        [SerializeField] private GameObject micMutedIndicator;
        [SerializeField] private GameObject chatPopup;
        [SerializeField] private TMP_InputField chatMessageInput;
        [SerializeField] private Transform chatMessageContent;
        [SerializeField] private GameObject chatMessageItemPrefab;
        [SerializeField] private MeetingChatUI meetingChatUI;
        [SerializeField] private SettingsPanel settingsPanel;

        private PlayerMove localPlayerMove;
        private PlayerSecret localPlayerSecret;
        private PlayerState localPlayerState;
        private TaskManager taskManager;
        private Coroutine dropdownCoroutine;
        private bool missionDropdownExpanded;
        private float missionProgressBarMaxWidth;
        private readonly List<MissionItemView> missionItemViews = new();
        private string lastNickname;
        private string lastRenderedPlayerRoleText = string.Empty;
        private RoleId lastRole;
        private bool hasRole;
        private bool outputMuted;
        private bool micMuted;
        private bool localPlayerAlive = true;
        private int lastPersonalMissionProgress = -1;
        private int lastPersonalMissionGoal = -1;

        private static readonly TaskType[] MissionItemTaskOrder =
        {
            TaskType.Potion,
            TaskType.StarAlign,
            TaskType.Picture,
            TaskType.Stabilize
        };

        private void Awake()
        {
            EnsureMissionDropdownMask();
            ConfigureMissionDropdown();
            CacheMissionProgressBarWidth();
            RebuildMissionItems();
            ApplyMissionDropdown(false, true);
            UpdateStatus(true);
            SetDeadChatAvailable(false);
            UpdatePlayerRoleText();
            TryBindTaskManager();
            UpdateMissionProgress();
            ConfigureUtilityButtons();
            ApplyUtilityButtonStates();
        }

        private void OnDestroy()
        {
            UnbindLocalPlayer();

            if (missionDropdownButton != null)
                missionDropdownButton.onClick.RemoveListener(ToggleMissionDropdown);

            UnregisterUtilityButtons();
            UnbindTaskManager();
        }

        private void Update()
        {
            if (localPlayerState == null)
                TryBindLocalPlayer();

            if (taskManager == null)
                TryBindTaskManager();

            if (taskManager != null &&
                (lastPersonalMissionProgress != GetPersonalMissionCompletedCount() ||
                 lastPersonalMissionGoal != GetVisibleMissionItemCount()))
            {
                UpdateMissionProgress();
            }

            if (localPlayerMove != null && localPlayerMove.Nickname != lastNickname)
                UpdatePlayerRoleText();

            if (playerRoleText != null && playerRoleText.text != lastRenderedPlayerRoleText)
                UpdatePlayerRoleText();
        }

        private void ConfigureMissionDropdown()
        {
            if (missionDropdownButton != null)
            {
                missionDropdownButton.onClick.RemoveListener(ToggleMissionDropdown);
                missionDropdownButton.onClick.AddListener(ToggleMissionDropdown);
            }
        }

        private void ConfigureUtilityButtons()
        {
            if (soundButton != null)
            {
                soundButton.onClick.RemoveListener(ToggleSoundMute);
                soundButton.onClick.AddListener(ToggleSoundMute);
            }

            if (micButton != null)
            {
                micButton.onClick.RemoveListener(ToggleMicMute);
                micButton.onClick.AddListener(ToggleMicMute);
            }

            if (chatButton != null)
            {
                chatButton.onClick.RemoveListener(ToggleChat);
                chatButton.onClick.AddListener(ToggleChat);
            }

            if (settingButton != null)
            {
                settingButton.onClick.RemoveListener(OpenSettings);
                settingButton.onClick.AddListener(OpenSettings);
            }

            if (chatCloseButton != null)
            {
                chatCloseButton.onClick.RemoveListener(CloseChat);
                chatCloseButton.onClick.AddListener(CloseChat);
            }

            if (chatSubmitButton != null)
            {
                chatSubmitButton.onClick.RemoveListener(SubmitIngameChatMessage);
                chatSubmitButton.onClick.AddListener(SubmitIngameChatMessage);
            }

            if (chatMessageInput != null)
            {
                chatMessageInput.onSubmit.RemoveListener(SubmitIngameChatMessageFromInput);
                chatMessageInput.onSubmit.AddListener(SubmitIngameChatMessageFromInput);
            }
        }

        private void UnregisterUtilityButtons()
        {
            if (soundButton != null)
                soundButton.onClick.RemoveListener(ToggleSoundMute);

            if (micButton != null)
                micButton.onClick.RemoveListener(ToggleMicMute);

            if (chatButton != null)
                chatButton.onClick.RemoveListener(ToggleChat);

            if (settingButton != null)
                settingButton.onClick.RemoveListener(OpenSettings);

            if (chatCloseButton != null)
                chatCloseButton.onClick.RemoveListener(CloseChat);

            if (chatSubmitButton != null)
                chatSubmitButton.onClick.RemoveListener(SubmitIngameChatMessage);

            if (chatMessageInput != null)
                chatMessageInput.onSubmit.RemoveListener(SubmitIngameChatMessageFromInput);
        }

        // SoundButton 클릭 시 호출됩니다. Vivox 로컬 출력 장치를 음소거하거나 다시 켭니다.
        public void ToggleSoundMute()
        {
            outputMuted = !outputMuted;

            if (ProximityVoiceManager.Instance != null)
                ProximityVoiceManager.Instance.SetLocalOutputMuted(outputMuted);

            ApplyUtilityButtonStates();
        }

        // MicButton 클릭 시 호출됩니다. Vivox 로컬 입력 장치를 음소거하거나 다시 켭니다.
        public void ToggleMicMute()
        {
            micMuted = !micMuted;

            if (ProximityVoiceManager.Instance != null)
                ProximityVoiceManager.Instance.SetLocalInputMuted(micMuted);

            ApplyUtilityButtonStates();
        }

        // ChatButton 클릭 시 호출됩니다. 회의/채팅 UI가 씬에 있으면 채팅창을 열고 닫습니다.
        public void ToggleChat()
        {
            if (localPlayerAlive)
            {
                CloseChat();
                return;
            }

            if (chatPopup != null)
            {
                chatPopup.SetActive(!chatPopup.activeSelf);
                if (chatPopup.activeSelf && chatMessageInput != null)
                    chatMessageInput.ActivateInputField();
                return;
            }

            if (meetingChatUI == null)
                meetingChatUI = FindFirstObjectByType<MeetingChatUI>(
                    FindObjectsInactive.Include
                );

            if (meetingChatUI != null)
                meetingChatUI.ToggleChat();
        }

        // ChatPopup 안의 CloseButton 클릭 시 호출됩니다.
        public void CloseChat()
        {
            if (chatPopup != null)
                chatPopup.SetActive(false);
        }

        // 죽은 플레이어가 인게임 채팅 메시지를 전송할 때 호출됩니다.
        public void SubmitIngameChatMessage()
        {
            if (localPlayerAlive || chatMessageInput == null)
                return;

            string message = chatMessageInput.text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            GetLocalAbilities()?.CmdSendIngameChat(message);
            chatMessageInput.text = string.Empty;
            chatMessageInput.ActivateInputField();
        }

        private void SubmitIngameChatMessageFromInput(string _)
        {
            SubmitIngameChatMessage();
        }

        public void ReceiveIngameChatMessage(string speakerName, string message, bool senderIsDead)
        {
            if (localPlayerAlive != !senderIsDead)
                return;

            AddIngameChatMessage(speakerName, message, senderIsDead);
        }

        private void AddIngameChatMessage(string speakerName, string message, bool senderIsDead)
        {
            if (chatMessageContent == null || chatMessageItemPrefab == null)
                return;

            GameObject item = Instantiate(chatMessageItemPrefab, chatMessageContent);
            item.name = chatMessageItemPrefab.name;

            SetChildText(item.transform, "NameText", string.IsNullOrWhiteSpace(speakerName) ? "나" : speakerName);
            SetChildText(item.transform, "MessageText", message);
            SetChildText(item.transform, "InitialText", GetInitialText(speakerName));
            ApplyChatBubbleColor(item.transform, senderIsDead);
        }

        private static PlayerAbilities GetLocalAbilities()
        {
            return NetworkClient.localPlayer != null
                ? NetworkClient.localPlayer.GetComponent<PlayerAbilities>()
                : null;
        }

        private static void SetChildText(Transform root, string childName, string text)
        {
            TMP_Text targetText = FindChildComponent<TMP_Text>(root, childName);
            if (targetText != null)
                targetText.text = text;
        }

        private static string GetInitialText(string speakerName)
        {
            if (string.IsNullOrWhiteSpace(speakerName))
                return "나";

            return speakerName.Trim()[0].ToString();
        }

        private static void ApplyChatBubbleColor(Transform itemRoot, bool senderIsDead)
        {
            Transform bubble = FindChildTransform(itemRoot, "Bubble");
            if (bubble == null || !bubble.TryGetComponent(out Image bubbleImage))
                return;

            bubbleImage.color = senderIsDead
                ? new Color32(0x96, 0xAA, 0xFF, 0xFF)
                : new Color32(0xFF, 0xF3, 0xC5, 0xFF);
        }

        private void SetDeadChatAvailable(bool available)
        {
            if (chatButton != null)
                chatButton.interactable = available;

            if (chatMessageInput != null)
                chatMessageInput.interactable = available;

            if (chatSubmitButton != null)
                chatSubmitButton.interactable = available;

            if (!available)
                CloseChat();
        }

        // SettingButton 클릭 시 호출됩니다. 설정 패널이 씬에 있으면 표시합니다.
        public void OpenSettings()
        {
            if (settingsPanel == null)
                settingsPanel = FindFirstObjectByType<SettingsPanel>(
                    FindObjectsInactive.Include
                );

            if (settingsPanel != null)
                settingsPanel.Show();
            else
                RuntimeUtilityMenu.ShowSettingsForActiveScene();
        }

        private void ApplyUtilityButtonStates()
        {
            if (ProximityVoiceManager.Instance != null)
            {
                micMuted = ProximityVoiceManager.Instance.IsLocalInputMuted;
                outputMuted = ProximityVoiceManager.Instance.IsLocalOutputMuted;
            }

            if (micMutedIndicator != null)
                micMutedIndicator.SetActive(micMuted);

            if (soundMutedIndicator != null)
                soundMutedIndicator.SetActive(outputMuted);
        }

        private void EnsureMissionDropdownMask()
        {
            if (missionDropdownPanel == null)
                return;

            if (missionDropdownPanel.GetComponent<RectMask2D>() == null)
                missionDropdownPanel.gameObject.AddComponent<RectMask2D>();
        }

        private void CacheMissionProgressBarWidth()
        {
            if (missionProgressBarFill == null)
                return;

            RectTransform parentRect = missionProgressBarFill.parent as RectTransform;
            missionProgressBarMaxWidth = parentRect != null
                ? parentRect.rect.width
                : missionProgressBarFill.rect.width;
        }

        private void TryBindTaskManager()
        {
            if (TaskManager.Instance == null || TaskManager.Instance == taskManager)
                return;

            UnbindTaskManager();
            taskManager = TaskManager.Instance;
            // TaskManager.CompleteObjective 호출 후 미션 전체가 완료되면 미션 목록과 진행도를 갱신합니다.
            taskManager.OnTaskCompleted += HandleTaskCompleted;
            taskManager.OnTasksReset += HandleTasksReset;
            RebuildMissionItems();
            UpdateMissionProgress();
        }

        private void UnbindTaskManager()
        {
            if (taskManager != null)
            {
                taskManager.OnTaskCompleted -= HandleTaskCompleted;
                taskManager.OnTasksReset -= HandleTasksReset;
            }

            taskManager = null;
        }

        private void HandleTaskCompleted(TaskType taskType)
        {
            UpdateMissionProgress();
        }

        private void HandleTasksReset()
        {
            UpdateMissionProgress();
        }

        private void TryBindLocalPlayer()
        {
            if (NetworkClient.localPlayer == null)
                return;

            GameObject localPlayer = NetworkClient.localPlayer.gameObject;
            PlayerState playerState = localPlayer.GetComponent<PlayerState>();
            if (playerState == null || playerState == localPlayerState)
                return;

            UnbindLocalPlayer();

            localPlayerState = playerState;
            localPlayerMove = localPlayer.GetComponent<PlayerMove>();
            localPlayerSecret = localPlayer.GetComponent<PlayerSecret>();

            // PlayerState.Kill 또는 Revive로 로컬 플레이어 생존 상태가 바뀌면 상태 텍스트를 갱신합니다.
            localPlayerState.AliveChanged += HandleAliveChanged;

            // 서버가 PlayerSecret.TargetRpcSetRole로 역할을 내려주면 역할 텍스트를 갱신합니다.
            if (localPlayerSecret != null)
                localPlayerSecret.RoleAssigned += HandleRoleAssigned;

            hasRole = localPlayerSecret != null && localPlayerSecret.HasLocalRole;
            if (hasRole)
                lastRole = localPlayerSecret.LocalRole;

            UpdateStatus(localPlayerState.isAlive);
            UpdatePlayerRoleText();
        }

        private void UnbindLocalPlayer()
        {
            if (localPlayerState != null)
                localPlayerState.AliveChanged -= HandleAliveChanged;

            if (localPlayerSecret != null)
                localPlayerSecret.RoleAssigned -= HandleRoleAssigned;

            localPlayerState = null;
            localPlayerMove = null;
            localPlayerSecret = null;
        }

        private void HandleAliveChanged(PlayerState playerState, bool isAlive, bool isLocalPlayer)
        {
            if (!isLocalPlayer)
                return;

            UpdateStatus(isAlive);
        }

        private void HandleRoleAssigned(RoleId role)
        {
            hasRole = true;
            lastRole = role;
            UpdatePlayerRoleText();
        }

        private void UpdatePlayerRoleText()
        {
            if (playerRoleText == null)
                return;

            lastNickname = localPlayerMove != null && !string.IsNullOrWhiteSpace(localPlayerMove.Nickname)
                ? localPlayerMove.Nickname
                : "Player";

            string roleText = hasRole ? GetRoleDisplayName(lastRole) : "역할 대기";
            lastRenderedPlayerRoleText = $"{lastNickname} - {roleText}";
            playerRoleText.text = lastRenderedPlayerRoleText;
        }

        private void UpdateStatus(bool isAlive)
        {
            localPlayerAlive = isAlive;
            SetDeadChatAvailable(!isAlive);

            if (statusText != null)
            {
                statusText.text = isAlive
                    ? "상태 - <color=#65D65D>생존</color>"
                    : "상태 - <color=#FF4D4D>사망</color>";
            }
        }

        private static string GetRoleDisplayName(RoleId role)
        {
            switch (role)
            {
                case RoleId.Mafia:
                    return "마피아";
                case RoleId.Detective:
                    return "탐정";
                default:
                    return "시민";
            }
        }

        private void ToggleMissionDropdown()
        {
            ApplyMissionDropdown(!missionDropdownExpanded, false);
        }

        private void ApplyMissionDropdown(bool expanded, bool immediate)
        {
            missionDropdownExpanded = expanded;
            UpdateMissionArrow();

            if (missionListArea != null && expanded)
                missionListArea.SetActive(true);

            if (missionDropdownPanel == null)
                return;

            float targetHeight = expanded ? expandedHeight : collapsedHeight;

            if (dropdownCoroutine != null)
                StopCoroutine(dropdownCoroutine);

            if (immediate)
            {
                SetDropdownHeight(targetHeight);

                if (missionListArea != null)
                    missionListArea.SetActive(expanded);
                return;
            }

            dropdownCoroutine = StartCoroutine(AnimateDropdown(targetHeight, expanded));
        }

        private IEnumerator AnimateDropdown(float targetHeight, bool expanded)
        {
            float startHeight = missionDropdownPanel.rect.height;
            float elapsed = 0f;

            while (elapsed < dropdownDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dropdownDuration);
                SetDropdownHeight(Mathf.Lerp(startHeight, targetHeight, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }

            SetDropdownHeight(targetHeight);

            if (missionListArea != null)
                missionListArea.SetActive(expanded);

            dropdownCoroutine = null;
        }

        private void SetDropdownHeight(float height)
        {
            missionDropdownPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private void UpdateMissionArrow()
        {
            if (missionArrowText != null)
                missionArrowText.text = missionDropdownExpanded ? expandedArrow : collapsedArrow;
        }

        private void UpdateMissionProgress()
        {
            int totalCount = GetVisibleMissionItemCount();
            int completedCount = GetPersonalMissionCompletedCount();
            completedCount = Mathf.Clamp(completedCount, 0, totalCount);

            lastPersonalMissionProgress = completedCount;
            lastPersonalMissionGoal = totalCount;

            if (missionProgressText != null)
                missionProgressText.text = $"{completedCount}/{totalCount}";

            if (missionProgressBarFill == null)
            {
                UpdateMissionItems();
                return;
            }

            if (missionProgressBarMaxWidth <= 0f)
                CacheMissionProgressBarWidth();

            float progress = totalCount > 0 ? Mathf.Clamp01((float)completedCount / totalCount) : 0f;
            missionProgressBarFill.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                missionProgressBarMaxWidth * progress
            );

            UpdateMissionItems();
        }

        private int GetPersonalMissionCompletedCount()
        {
            if (taskManager == null)
                return 0;

            int completedCount = 0;
            int visibleCount = GetVisibleMissionItemCount();
            for (int i = 0; i < visibleCount && i < MissionItemTaskOrder.Length; i++)
            {
                if (taskManager.IsTaskCompleted(MissionItemTaskOrder[i]))
                    completedCount++;
            }

            return completedCount;
        }

        private void RebuildMissionItems()
        {
            EnsureMissionItemCount(GetVisibleMissionItemCount());
            CacheMissionItemViews();
            LayoutMissionItems();
            UpdateExpandedHeight();
        }

        private int GetVisibleMissionItemCount()
        {
            int taskCount = taskManager != null ? taskManager.GetTotalTaskCount() : MissionItemTaskOrder.Length;
            return Mathf.Clamp(taskCount, 0, MissionItemTaskOrder.Length);
        }

        private void EnsureMissionItemCount(int visibleCount)
        {
            if (missionListArea == null)
                return;

            Transform listRoot = missionListArea.transform;
            List<Transform> itemTransforms = GetMissionItemTransforms();
            if (itemTransforms.Count == 0 && missionItemPrefab == null)
                return;

            while (itemTransforms.Count < visibleCount)
            {
                GameObject sourcePrefab = missionItemPrefab != null
                    ? missionItemPrefab
                    : itemTransforms[0].gameObject;

                Transform clonedItem = Instantiate(sourcePrefab, listRoot).transform;
                clonedItem.name = $"MissionItem_{itemTransforms.Count + 1:00}";
                itemTransforms.Add(clonedItem);
            }

            for (int i = 0; i < itemTransforms.Count; i++)
            {
                Transform item = itemTransforms[i];
                item.SetSiblingIndex(i);
                item.gameObject.SetActive(i < visibleCount);
            }
        }

        private List<Transform> GetMissionItemTransforms()
        {
            List<Transform> itemTransforms = new();

            if (missionListArea == null)
                return itemTransforms;

            Transform listRoot = missionListArea.transform;
            for (int i = 0; i < listRoot.childCount; i++)
            {
                Transform child = listRoot.GetChild(i);
                if (child.name.StartsWith("MissionItem_"))
                    itemTransforms.Add(child);
            }

            return itemTransforms;
        }

        private void CacheMissionItemViews()
        {
            missionItemViews.Clear();

            if (missionListArea == null)
                return;

            Transform listRoot = missionListArea.transform;
            for (int i = 0; i < listRoot.childCount; i++)
            {
                Transform child = listRoot.GetChild(i);
                if (!child.name.StartsWith("MissionItem_"))
                    continue;

                int missionItemIndex = missionItemViews.Count;
                if (missionItemIndex >= GetVisibleMissionItemCount())
                    continue;

                TaskType taskType = missionItemIndex < MissionItemTaskOrder.Length
                    ? MissionItemTaskOrder[missionItemIndex]
                    : TaskType.None;

                MissionItemView itemView = new MissionItemView(child, taskType);
                itemView.SetText(GetMissionDisplayName(taskType), GetMissionLocationName(taskType));
                missionItemViews.Add(itemView);
            }

        }

        private void LayoutMissionItems()
        {
            for (int i = 0; i < missionItemViews.Count; i++)
            {
                MissionItemView itemView = missionItemViews[i];
                if (itemView.RectTransform == null)
                    continue;

                itemView.RectTransform.anchorMin = new Vector2(0f, 1f);
                itemView.RectTransform.anchorMax = new Vector2(1f, 1f);
                itemView.RectTransform.pivot = new Vector2(0.5f, 1f);
                itemView.RectTransform.anchoredPosition = new Vector2(
                    0f,
                    -i * (missionItemHeight + missionItemSpacing)
                );
                itemView.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, missionItemHeight);
            }

            if (missionListArea == null)
                return;

            RectTransform listAreaRect = missionListArea.transform as RectTransform;
            if (listAreaRect == null)
                return;

            listAreaRect.anchoredPosition = new Vector2(listAreaRect.anchoredPosition.x, -missionListTopOffset);
            listAreaRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, GetMissionListHeight());
        }

        private void UpdateExpandedHeight()
        {
            expandedHeight = Mathf.Max(collapsedHeight, missionListTopOffset + GetMissionListHeight());

            if (missionDropdownExpanded && missionDropdownPanel != null)
                SetDropdownHeight(expandedHeight);
        }

        private float GetMissionListHeight()
        {
            int visibleCount = GetVisibleMissionItemCount();
            if (visibleCount <= 0)
                return missionListBottomPadding;

            return visibleCount * missionItemHeight
                   + (visibleCount - 1) * missionItemSpacing
                   + missionListBottomPadding;
        }

        private void UpdateMissionItems()
        {
            if (missionItemViews.Count == 0)
                return;

            foreach (MissionItemView itemView in missionItemViews)
            {
                bool isCompleted = taskManager != null && taskManager.IsTaskCompleted(itemView.TaskType);
                itemView.ApplyCompleted(isCompleted);
            }
        }

        private static string GetMissionDisplayName(TaskType taskType)
        {
            switch (taskType)
            {
                case TaskType.Potion:
                    return "시약정리";
                case TaskType.StarAlign:
                    return "별빛정렬";
                case TaskType.Picture:
                    return "유물복원";
                case TaskType.Stabilize:
                    return "촉매 안정화";
                default:
                    return "미션 대기";
            }
        }

        private static string GetMissionLocationName(TaskType taskType)
        {
            switch (taskType)
            {
                case TaskType.Potion:
                    return "시약창고";
                case TaskType.StarAlign:
                    return "천문탑";
                case TaskType.Picture:
                    return "금지된 유물보관소";
                case TaskType.Stabilize:
                    return "연금술 실험실";
                default:
                    return "";
            }
        }

        private static T FindChildComponent<T>(Transform root, string childName) where T : Component
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName && child.TryGetComponent(out T component))
                    return component;

                T nestedComponent = FindChildComponent<T>(child, childName);
                if (nestedComponent != null)
                    return nestedComponent;
            }

            return null;
        }

        private static Transform FindChildTransform(Transform root, string childName)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                    return child;

                Transform nestedChild = FindChildTransform(child, childName);
                if (nestedChild != null)
                    return nestedChild;
            }

            return null;
        }

        private sealed class MissionItemView
        {
            private readonly MissionItemUI missionItemUI;

            public MissionItemView(Transform root, TaskType taskType)
            {
                Root = root;
                TaskType = taskType;
                RectTransform = root as RectTransform;
                missionItemUI = root.GetComponent<MissionItemUI>();
            }

            public Transform Root { get; }
            public TaskType TaskType { get; }
            public RectTransform RectTransform { get; }

            public void SetText(string missionName, string locationName)
            {
                if (missionItemUI != null)
                    missionItemUI.SetText(missionName, locationName);
            }

            public void ApplyCompleted(bool isCompleted)
            {
                if (TaskType == TaskType.None)
                    return;

                if (missionItemUI != null)
                    missionItemUI.SetCompleted(isCompleted);
            }
        }
    }
}
