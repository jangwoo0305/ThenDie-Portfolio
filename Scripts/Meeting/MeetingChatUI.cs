using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum MeetingChatChannel
{
    Alive,
    Dead
}

// 서버에서 받은 채팅 기록을 UI에 다시 표시할 때 사용하는 데이터 형식입니다.
public struct MeetingChatMessage
{
    public string SpeakerName;
    public string Text;
    public MeetingChatChannel Channel;

    public MeetingChatMessage(string speakerName, string text, MeetingChatChannel channel)
    {
        SpeakerName = speakerName;
        Text = text;
        Channel = channel;
    }
}

public class MeetingChatUI : MonoBehaviour
{
    // 기존 단일 문자열 이벤트가 필요한 코드와의 호환용입니다. 서버 연동에서는 MessageSubmittedWithSender 사용을 권장합니다.
    public event Action<string> MessageSubmitted;
    // 채팅 채널만 필요한 서버 코드에서 사용할 수 있습니다.
    public event Action<string, MeetingChatChannel> MessageSubmittedWithChannel;
    // 플레이어가 채팅을 전송했을 때 서버로 전달할 닉네임, 메시지, 채널 정보입니다.
    public event Action<string, string, MeetingChatChannel> MessageSubmittedWithSender;

    [Header("Optional")]
    [SerializeField] private Button chatButton;
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private GameObject chatPanelPrefab;
    [SerializeField] private Transform chatPanelParent;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Transform messageContent;
    [SerializeField] private GameObject chatMessageItemPrefab;

    private MeetingChatChannel currentPlayerChannel = MeetingChatChannel.Alive;
    private string currentPlayerName = "나";
    private bool serverAuthoritative;
    private readonly List<GameObject> runtimeMessages = new List<GameObject>();

    private static readonly Color32 AliveBubbleColor = new Color32(0xFF, 0xF3, 0xC5, 0xFF);
    private static readonly Color32 DeadBubbleColor = new Color32(0x96, 0xAA, 0xFF, 0xFF);

    private void Awake()
    {
        CollectReferences();
        EnsureChatPanelReference();
        CollectChatPanelReferences();
        RegisterEvents();
        SetChatVisible(false);

    }

    private void Update()
    {
        if (chatPanel == null || !chatPanel.activeSelf)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            SetChatVisible(false);
        }
    }

    public void SetChatVisible(bool visible)
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(visible);
        }

        if (visible && messageInput != null)
        {
            messageInput.ActivateInputField();
        }
    }

    public void ToggleChat()
    {
        bool nextVisible = chatPanel == null || !chatPanel.activeSelf;
        SetChatVisible(nextVisible);
    }

    public void SubmitMessage()
    {
        if (messageInput == null)
            return;

        string message = messageInput.text.Trim();
        if (string.IsNullOrEmpty(message))
            return;

        if (!serverAuthoritative)
            AddMessage(currentPlayerName, message, currentPlayerChannel);
        messageInput.text = string.Empty;
        messageInput.ActivateInputField();

        if (MessageSubmitted != null)
        {
            MessageSubmitted.Invoke(message);
        }

        if (MessageSubmittedWithChannel != null)
        {
            MessageSubmittedWithChannel.Invoke(message, currentPlayerChannel);
        }

        if (MessageSubmittedWithSender != null)
        {
            MessageSubmittedWithSender.Invoke(currentPlayerName, message, currentPlayerChannel);
        }
    }

    // UI에 메시지를 직접 표시할 때 사용합니다. 서버에서 수신한 메시지는 ReceiveMessage 사용을 권장합니다.
    public void AddMessage(string speakerName, string message)
    {
        AddMessage(speakerName, message, currentPlayerChannel);
    }

    // UI에 메시지를 직접 표시할 때 사용합니다. 서버에서 수신한 메시지는 ReceiveMessage 사용을 권장합니다.
    public void AddMessage(string speakerName, string message, bool isDeadChat)
    {
        AddMessage(speakerName, message, isDeadChat ? MeetingChatChannel.Dead : MeetingChatChannel.Alive);
    }

    // UI에 메시지를 직접 표시할 때 사용합니다. 현재 클라이언트와 다른 채널의 메시지는 표시하지 않습니다.
    public void AddMessage(string speakerName, string message, MeetingChatChannel channel)
    {
        if (messageContent == null)
            return;

        if (channel != currentPlayerChannel)
            return;

        if (chatMessageItemPrefab != null)
        {
            GameObject item = Instantiate(chatMessageItemPrefab, messageContent);
            item.name = chatMessageItemPrefab.name;
            SetChatMessageItem(item.transform, speakerName, message, channel);
            runtimeMessages.Add(item);
            return;
        }

        AddGeneratedMessage(speakerName, message, channel);
    }

    // 서버에서 현재 클라이언트의 생존 상태가 변경되었을 때 호출합니다. 채널이 바뀌면 기존 채팅은 섞이지 않도록 초기화합니다.
    public void SetCurrentPlayerDead(bool isDead)
    {
        MeetingChatChannel nextChannel = isDead ? MeetingChatChannel.Dead : MeetingChatChannel.Alive;
        if (currentPlayerChannel == nextChannel)
            return;

        currentPlayerChannel = nextChannel;
        ClearMessages();
    }

    // 서버에서 현재 클라이언트의 플레이어 닉네임을 받은 뒤 호출합니다.
    public void SetCurrentPlayerName(string playerName)
    {
        currentPlayerName = string.IsNullOrWhiteSpace(playerName) ? "나" : playerName.Trim();
    }

    // 서버에서 현재 클라이언트의 닉네임과 생존 상태를 함께 받은 뒤 호출합니다.
    public void SetCurrentPlayer(string playerName, bool isDead)
    {
        SetCurrentPlayerName(playerName);
        SetCurrentPlayerDead(isDead);
    }

    public void SetServerAuthoritative(bool enabled)
    {
        serverAuthoritative = enabled;
    }

    // 서버에서 채팅 한 건을 수신했을 때 호출합니다. 현재 클라이언트와 같은 채널의 메시지만 화면에 표시됩니다.
    public void ReceiveMessage(string speakerName, string message, bool isDeadChat)
    {
        AddMessage(speakerName, message, isDeadChat);
    }

    // 서버에서 채팅 한 건을 수신했을 때 호출합니다. enum 채널을 직접 넘길 수 있는 경우 이 함수를 사용합니다.
    public void ReceiveMessage(string speakerName, string message, MeetingChatChannel channel)
    {
        AddMessage(speakerName, message, channel);
    }

    // 서버에서 현재 회의 채팅 목록을 다시 내려줄 때 호출합니다. 현재 클라이언트와 같은 채널의 메시지만 화면에 표시됩니다.
    public void SetMessages(IEnumerable<MeetingChatMessage> messages)
    {
        ClearMessages();

        if (messages == null)
            return;

        foreach (MeetingChatMessage message in messages)
        {
            AddMessage(message.SpeakerName, message.Text, message.Channel);
        }
    }

    // 서버 상태나 회의 단계에 따라 채팅 입력 가능 여부를 변경할 때 호출합니다.
    public void SetInputInteractable(bool interactable)
    {
        if (messageInput != null)
        {
            messageInput.interactable = interactable;
        }

        if (submitButton != null)
        {
            submitButton.interactable = interactable;
        }
    }

    // 회의가 새로 시작되거나 서버에서 채팅 목록을 다시 동기화하기 전에 호출합니다.
    public void ClearMessages()
    {
        foreach (GameObject message in runtimeMessages)
        {
            if (message != null)
            {
                Destroy(message);
            }
        }

        runtimeMessages.Clear();
    }

    private void AddGeneratedMessage(string speakerName, string message, MeetingChatChannel channel)
    {
        GameObject row = CreateUIObject("ChatMessage", messageContent);
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.spacing = 14f;
        rowLayout.padding = new RectOffset(20, 20, 8, 8);

        CreateAvatar(row.transform, speakerName);

        GameObject body = CreateUIObject("Body", row.transform);
        VerticalLayoutGroup bodyLayout = body.AddComponent<VerticalLayoutGroup>();
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = false;
        bodyLayout.spacing = 6f;

        TextMeshProUGUI nameText = CreateText("NameText", body.transform, speakerName, 22f, FontStyles.Bold);
        nameText.color = new Color(0.1f, 0.06f, 0.02f);

        GameObject bubble = CreateUIObject("Bubble", body.transform);
        Image bubbleImage = bubble.AddComponent<Image>();
        bubbleImage.color = GetBubbleColor(channel);

        LayoutElement bubbleLayout = bubble.AddComponent<LayoutElement>();
        bubbleLayout.preferredWidth = 360f;
        bubbleLayout.minHeight = 64f;

        TextMeshProUGUI messageText = CreateText("MessageText", bubble.transform, message, 24f, FontStyles.Normal);
        messageText.color = new Color(0.15f, 0.09f, 0.04f);
        messageText.textWrappingMode = TextWrappingModes.Normal;
        messageText.margin = new Vector4(18, 12, 18, 12);

        runtimeMessages.Add(row);
    }

    private void CollectReferences()
    {
        if (chatButton == null)
        {
            GameObject chatButtonObject = FindObjectByName("ChatButton");
            if (chatButtonObject != null)
            {
                chatButton = chatButtonObject.GetComponent<Button>();
            }
        }

        if (chatPanel == null)
        {
            chatPanel = FindObjectByName("ChatPopup");
        }
    }

    private void EnsureChatPanelReference()
    {
        if (chatPanel != null)
            return;

        if (chatPanelPrefab == null)
            return;

        Transform parent = chatPanelParent;
        if (parent == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            parent = canvas != null ? canvas.transform : transform;
        }

        chatPanel = Instantiate(chatPanelPrefab, parent);
        chatPanel.name = chatPanelPrefab.name;
    }

    private void CollectChatPanelReferences()
    {
        if (chatPanel == null)
            return;

        Transform panelTransform = chatPanel.transform;

        if (closeButton == null)
        {
            closeButton = FindComponentInChild<Button>(panelTransform, "CloseButton");
        }

        if (messageInput == null)
        {
            messageInput = FindComponentInChild<TMP_InputField>(panelTransform, "MessageInput");
        }

        if (submitButton == null)
        {
            GameObject inputArea = FindChildByName(panelTransform, "InputArea");
            if (inputArea != null)
            {
                submitButton = FindComponentInChild<Button>(inputArea.transform, "SubmitButton");
                if (submitButton == null)
                {
                    submitButton = FindComponentInChild<Button>(inputArea.transform, "Button");
                }
            }
        }

        if (messageContent == null)
        {
            ScrollRect scrollRect = chatPanel.GetComponentInChildren<ScrollRect>(true);
            if (scrollRect != null && scrollRect.content != null)
            {
                messageContent = scrollRect.content;
            }
        }
    }

    private void RegisterEvents()
    {
        if (chatButton != null)
        {
            chatButton.onClick.RemoveListener(ToggleChat);
            chatButton.onClick.AddListener(ToggleChat);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => SetChatVisible(false));
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(SubmitMessage);
        }

        if (messageInput != null)
        {
            messageInput.onSubmit.RemoveListener(SubmitMessageFromInput);
            messageInput.onSubmit.AddListener(SubmitMessageFromInput);
        }
    }

    private void SubmitMessageFromInput(string _)
    {
        SubmitMessage();
    }


    private static GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.layer = parent != null ? parent.gameObject.layer : 5;
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize, FontStyles fontStyle)
    {
        GameObject obj = CreateUIObject(objectName, parent);
        TextMeshProUGUI textComponent = obj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = TextAlignmentOptions.MidlineLeft;
        return textComponent;
    }

    private static void CreateAvatar(Transform parent, string speakerName)
    {
        GameObject avatar = CreateUIObject("Avatar", parent);
        Image avatarImage = avatar.AddComponent<Image>();
        avatarImage.color = new Color(0.78f, 0.72f, 0.62f, 0.75f);

        LayoutElement avatarLayout = avatar.AddComponent<LayoutElement>();
        avatarLayout.preferredWidth = 58f;
        avatarLayout.preferredHeight = 58f;

        string initial = string.IsNullOrEmpty(speakerName) ? "?" : speakerName.Substring(0, 1);
        TextMeshProUGUI avatarText = CreateText("InitialText", avatar.transform, initial, 22f, FontStyles.Bold);
        avatarText.alignment = TextAlignmentOptions.Center;
        avatarText.color = new Color(0.2f, 0.12f, 0.06f);

        RectTransform avatarTextRect = avatarText.GetComponent<RectTransform>();
        avatarTextRect.anchorMin = Vector2.zero;
        avatarTextRect.anchorMax = Vector2.one;
        avatarTextRect.offsetMin = Vector2.zero;
        avatarTextRect.offsetMax = Vector2.zero;
    }

    private static void SetChatMessageItem(Transform itemRoot, string speakerName, string message, MeetingChatChannel channel)
    {
        TextMeshProUGUI nameText = FindComponentInChild<TextMeshProUGUI>(itemRoot, "NameText");
        if (nameText != null)
        {
            nameText.text = speakerName;
        }

        TextMeshProUGUI messageText = null;
        GameObject bubble = FindChildByName(itemRoot, "Bubble");
        if (bubble != null)
        {
            Image bubbleImage = bubble.GetComponent<Image>();
            if (bubbleImage != null)
            {
                bubbleImage.color = GetBubbleColor(channel);
            }

            messageText = FindComponentInChild<TextMeshProUGUI>(bubble.transform, "MessageText");
        }

        if (messageText == null)
        {
            messageText = FindComponentInChild<TextMeshProUGUI>(itemRoot, "MessageText");
        }

        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    private static Color GetBubbleColor(MeetingChatChannel channel)
    {
        return channel == MeetingChatChannel.Dead ? DeadBubbleColor : AliveBubbleColor;
    }

    private static T FindComponentInChild<T>(Transform root, string childName) where T : Component
    {
        GameObject child = FindChildByName(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static GameObject FindChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    private static GameObject FindObjectByName(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform target in transforms)
        {
            if (target.name == objectName && target.gameObject.scene.IsValid())
                return target.gameObject;
        }

        return null;
    }
}
