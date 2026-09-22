using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ThenDie.Utils;

namespace ThenDie.Ingame
{
    public class MiniMapToggleUI : MonoBehaviour
    {
        private const string MarkerLayerName = "DiagramMarkers";
        private const float TargetRefreshInterval = 0.25f;

        [Header("Mini Map")]
        [SerializeField] private GameObject miniMapRoot;
        [SerializeField] private bool visibleOnStart;

        [Header("World Projection")]
        [Tooltip("Map_Layer0/1의 중앙 월드 좌표입니다.")]
        [SerializeField] private Vector2 worldCenter = Vector2.zero;
        [Tooltip("현재 맵 스프라이트의 월드 크기(11520x12960, PPU 100)입니다.")]
        [SerializeField] private Vector2 worldSize = new Vector2(115.2f, 129.6f);
        [Tooltip("도식 이미지에서 실제 맵이 시작되는 정규화 좌표입니다.")]
        [SerializeField] private Vector2 normalizedMapMin = new Vector2(0.04f, 0.035f);
        [Tooltip("도식 이미지에서 실제 맵이 끝나는 정규화 좌표입니다.")]
        [SerializeField] private Vector2 normalizedMapMax = new Vector2(0.96f, 0.965f);

        [Header("Diagram Marker Colors")]
        [SerializeField] private Color localPlayerColor = new Color(0.1f, 0.9f, 1f, 1f);
        [SerializeField] private Color missionColor = new Color(1f, 0.86f, 0.12f, 1f);
        [SerializeField] private Color sabotageColor = new Color(1f, 0.18f, 0.12f, 1f);
        [SerializeField] private Color sabotageGoalColor = new Color(1f, 0.55f, 0.08f, 1f);

        private readonly Dictionary<MiniGameLauncher, RectTransform> missionMarkers = new();
        private readonly Dictionary<OwlMailLetter, RectTransform> sabotageMarkers = new();
        private readonly List<MiniGameLauncher> staleMissionLaunchers = new();
        private readonly List<OwlMailLetter> staleLetters = new();

        private RectTransform markerLayer;
        private RectTransform localPlayerMarker;
        private RectTransform incubatorMarker;
        private RectTransform owlMailMailboxMarker;
        private IncubatorStabilizationStation incubatorStation;
        private OwlMailMailbox owlMailMailbox;
        private Canvas overlayCanvas;
        private Button closeSurfaceButton;
        private Image mapLayer1Image;
        private float nextTargetRefreshTime;

        private void Awake()
        {
            InitializeDiagram();
            SetMiniMapVisible(visibleOnStart);
        }

        private void Update()
        {
            bool canShowMap = CanShowMap();

            if (!canShowMap)
            {
                if (miniMapRoot != null && miniMapRoot.activeSelf)
                    SetMiniMapVisible(false);

                return;
            }

            Keyboard keyboard = Keyboard.current;
            bool ignoreGameplayInput = GameplayInputBlocker.ShouldIgnoreGameplayInput();

            if (!ignoreGameplayInput && keyboard != null && keyboard.mKey.wasPressedThisFrame)
                ToggleMiniMap();

            if (!ignoreGameplayInput &&
                keyboard != null && miniMapRoot != null && miniMapRoot.activeSelf &&
                keyboard.escapeKey.wasPressedThisFrame)
                SetMiniMapVisible(false);

            if (miniMapRoot == null || !miniMapRoot.activeSelf)
                return;

            UpdateLocalPlayerMarker();

            if (Time.unscaledTime >= nextTargetRefreshTime)
            {
                RefreshTargetMarkers();
                nextTargetRefreshTime = Time.unscaledTime + TargetRefreshInterval;
            }

            AnimateMarkers();
        }

        public void ToggleMiniMap()
        {
            if (miniMapRoot == null)
                return;

            SetMiniMapVisible(!miniMapRoot.activeSelf);
        }

        public void ToggleFromMeeting(Canvas meetingCanvas)
        {
            if (!CanShowMap())
                return;

            PrepareMeetingOverlay(meetingCanvas);
            ToggleMiniMap();
        }

        public void ShowMiniMap()
        {
            SetMiniMapVisible(true);
        }

        public void HideMiniMap()
        {
            SetMiniMapVisible(false);
        }

        public void SetMiniMapVisible(bool visible)
        {
            if (miniMapRoot == null)
                return;

            visible &= CanShowMap();
            miniMapRoot.SetActive(visible);

            if (!visible)
                return;

            // 회의 팝업과 동일한 Canvas에 있어도 열린 미니맵이 HUD 뒤로 숨지 않게 합니다.
            miniMapRoot.transform.SetAsLastSibling();
            RefreshTargetMarkers();
            UpdateLocalPlayerMarker();
            nextTargetRefreshTime = Time.unscaledTime + TargetRefreshInterval;
        }

        private static bool CanShowMap()
        {
            if (SceneManager.GetActiveScene().name == "03_LobbyScene")
                return true;

            return GameFlowManager.Instance != null &&
                   (GameFlowManager.Instance.phase == GamePhase.FreeRoam ||
                    GameFlowManager.Instance.phase == GamePhase.Meeting ||
                    GameFlowManager.Instance.phase == GamePhase.Voting);
        }

        private void PrepareMeetingOverlay(Canvas meetingCanvas)
        {
            if (miniMapRoot == null)
                return;

            if (overlayCanvas == null)
                overlayCanvas = miniMapRoot.GetComponent<Canvas>();
            if (overlayCanvas == null)
                overlayCanvas = miniMapRoot.AddComponent<Canvas>();

            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = meetingCanvas != null
                ? meetingCanvas.sortingOrder + 1
                : 101;

            if (miniMapRoot.GetComponent<GraphicRaycaster>() == null)
                miniMapRoot.AddComponent<GraphicRaycaster>();
        }

        private void InitializeDiagram()
        {
            if (miniMapRoot == null)
                return;

            RectTransform mapRect = miniMapRoot.transform as RectTransform;
            if (mapRect == null)
                return;

            ApplySceneMapArtwork(mapRect);

            closeSurfaceButton = miniMapRoot.GetComponent<Button>();
            if (closeSurfaceButton == null)
                closeSurfaceButton = miniMapRoot.AddComponent<Button>();
            closeSurfaceButton.transition = Selectable.Transition.None;
            closeSurfaceButton.targetGraphic = miniMapRoot.GetComponent<Image>();
            closeSurfaceButton.onClick.RemoveListener(HideMiniMap);
            closeSurfaceButton.onClick.AddListener(HideMiniMap);

            Transform existingLayer = mapRect.Find(MarkerLayerName);
            markerLayer = existingLayer as RectTransform;

            if (markerLayer == null)
            {
                GameObject layerObject = new GameObject(MarkerLayerName, typeof(RectTransform));
                layerObject.layer = miniMapRoot.layer;
                markerLayer = layerObject.GetComponent<RectTransform>();
                markerLayer.SetParent(mapRect, false);
                markerLayer.anchorMin = Vector2.zero;
                markerLayer.anchorMax = Vector2.one;
                markerLayer.offsetMin = Vector2.zero;
                markerLayer.offsetMax = Vector2.zero;
            }

            localPlayerMarker = CreateMarker(
                "LocalPlayerMarker",
                new Vector2(22f, 22f),
                localPlayerColor,
                45f);
            localPlayerMarker.SetAsLastSibling();
        }

        private void ApplySceneMapArtwork(RectTransform mapRect)
        {
            Sprite layer0 = null;
            Sprite layer1 = null;
            SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || renderer.sprite == null)
                    continue;

                if (renderer.gameObject.name == "Map_Layer0")
                    layer0 = renderer.sprite;
                else if (renderer.gameObject.name == "Map_Layer1")
                    layer1 = renderer.sprite;
            }

            Image baseImage = miniMapRoot.GetComponent<Image>();
            if (baseImage != null)
            {
                if (layer0 != null)
                    baseImage.sprite = layer0;
                baseImage.preserveAspect = true;
            }

            Transform existingLayer1 = mapRect.Find("MapLayer1Artwork");
            if (existingLayer1 != null)
                mapLayer1Image = existingLayer1.GetComponent<Image>();

            if (mapLayer1Image == null)
            {
                GameObject layerObject = new GameObject(
                    "MapLayer1Artwork",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                layerObject.layer = miniMapRoot.layer;

                RectTransform layerRect = layerObject.GetComponent<RectTransform>();
                layerRect.SetParent(mapRect, false);
                layerRect.anchorMin = Vector2.zero;
                layerRect.anchorMax = Vector2.one;
                layerRect.offsetMin = Vector2.zero;
                layerRect.offsetMax = Vector2.zero;
                mapLayer1Image = layerObject.GetComponent<Image>();
            }

            mapLayer1Image.sprite = layer1;
            mapLayer1Image.preserveAspect = true;
            mapLayer1Image.raycastTarget = false;
            mapLayer1Image.gameObject.SetActive(layer1 != null);
            mapLayer1Image.transform.SetAsFirstSibling();
        }

        private void RefreshTargetMarkers()
        {
            if (markerLayer == null)
                return;

            RefreshMissionMarkers();
            RefreshSabotageMarkers();
            RefreshStaticPointMarkers();

            if (localPlayerMarker != null)
                localPlayerMarker.SetAsLastSibling();
        }

        private void RefreshMissionMarkers()
        {
            MiniGameLauncher[] launchers =
                FindObjectsByType<MiniGameLauncher>(FindObjectsSortMode.None);
            HashSet<MiniGameLauncher> currentLaunchers = new();
            TaskManager taskManager = TaskManager.Instance;

            foreach (MiniGameLauncher launcher in launchers)
            {
                if (launcher == null ||
                    !launcher.TryGetMissionObjective(
                        out TaskType taskType,
                        out ObjectiveType objectiveType))
                    continue;

                currentLaunchers.Add(launcher);

                if (!missionMarkers.TryGetValue(launcher, out RectTransform marker))
                {
                    marker = CreateMarker(
                        $"MissionMarker_{taskType}",
                        new Vector2(16f, 16f),
                        missionColor,
                        45f);
                    missionMarkers.Add(launcher, marker);
                }

                bool objectiveCompleted = taskManager != null &&
                                          taskManager.IsObjectiveCompleted(taskType, objectiveType);
                bool taskCompleted = taskManager != null && taskManager.IsTaskCompleted(taskType);
                bool shouldShow = !objectiveCompleted && !taskCompleted;

                marker.gameObject.SetActive(shouldShow);
                if (shouldShow)
                    SetMarkerPosition(marker, launcher.transform);
            }

            staleMissionLaunchers.Clear();
            foreach (KeyValuePair<MiniGameLauncher, RectTransform> pair in missionMarkers)
            {
                if (pair.Key == null || !currentLaunchers.Contains(pair.Key))
                    staleMissionLaunchers.Add(pair.Key);
            }

            foreach (MiniGameLauncher staleLauncher in staleMissionLaunchers)
            {
                if (missionMarkers.TryGetValue(staleLauncher, out RectTransform marker) && marker != null)
                    Destroy(marker.gameObject);

                missionMarkers.Remove(staleLauncher);
            }

        }

        private void RefreshStaticPointMarkers()
        {
            RefreshIncubatorMarker();
            RefreshOwlMailMailboxMarker();
        }

        private void RefreshIncubatorMarker()
        {
            if (incubatorStation == null)
                incubatorStation = FindFirstObjectByType<IncubatorStabilizationStation>();

            bool shouldShow = incubatorStation != null
                              && TaskManager.Instance != null
                              && TaskManager.Instance.IsObjectiveCompleted(
                                  TaskType.Stabilize,
                                  ObjectiveType.StabilizeControl)
                              && !TaskManager.Instance.IsObjectiveCompleted(
                                  TaskType.Stabilize,
                                  ObjectiveType.IncubatorCheck);

            if (!shouldShow)
            {
                if (incubatorMarker != null)
                    incubatorMarker.gameObject.SetActive(false);
                return;
            }

            if (incubatorMarker == null)
            {
                incubatorMarker = CreateMarker(
                    "MissionMarker_IncubatorCheck",
                    new Vector2(16f, 16f),
                    missionColor,
                    45f);
            }

            incubatorMarker.gameObject.SetActive(true);
            SetMarkerPosition(incubatorMarker, incubatorStation.transform);
        }

        private void RefreshOwlMailMailboxMarker()
        {
            if (owlMailMailbox == null)
                owlMailMailbox = FindFirstObjectByType<OwlMailMailbox>();

            if (owlMailMailbox == null)
            {
                if (owlMailMailboxMarker != null)
                    owlMailMailboxMarker.gameObject.SetActive(false);
                return;
            }

            if (owlMailMailboxMarker == null)
            {
                owlMailMailboxMarker = CreateMarker(
                    "OwlMailMailboxMarker",
                    new Vector2(18f, 18f),
                    sabotageGoalColor,
                    45f);
            }

            owlMailMailboxMarker.gameObject.SetActive(true);
            SetMarkerPosition(owlMailMailboxMarker, owlMailMailbox.transform);
        }

        private void RefreshSabotageMarkers()
        {
            bool sabotageRunning = SabotageManager.Instance != null &&
                                   SabotageManager.Instance.IsOwlMailRunning;

            if (!sabotageRunning)
            {
                foreach (RectTransform marker in sabotageMarkers.Values)
                {
                    if (marker != null)
                        marker.gameObject.SetActive(false);
                }

                return;
            }

            OwlMailLetter[] letters = FindObjectsByType<OwlMailLetter>(FindObjectsSortMode.None);
            HashSet<OwlMailLetter> currentLetters = new();

            foreach (OwlMailLetter letter in letters)
            {
                if (letter == null)
                    continue;

                currentLetters.Add(letter);

                if (!sabotageMarkers.TryGetValue(letter, out RectTransform marker))
                {
                    marker = CreateMarker(
                        "SabotageLetterMarker",
                        new Vector2(12f, 16f),
                        sabotageColor,
                        0f);
                    sabotageMarkers.Add(letter, marker);
                }

                marker.gameObject.SetActive(true);
                SetMarkerPosition(marker, letter.transform.position);
            }

            staleLetters.Clear();
            foreach (KeyValuePair<OwlMailLetter, RectTransform> pair in sabotageMarkers)
            {
                if (pair.Key == null || !currentLetters.Contains(pair.Key))
                    staleLetters.Add(pair.Key);
            }

            foreach (OwlMailLetter staleLetter in staleLetters)
            {
                if (sabotageMarkers.TryGetValue(staleLetter, out RectTransform marker) && marker != null)
                    Destroy(marker.gameObject);

                sabotageMarkers.Remove(staleLetter);
            }

        }

        private void UpdateLocalPlayerMarker()
        {
            if (localPlayerMarker == null)
                return;

            NetworkIdentity localPlayer = NetworkClient.localPlayer;
            bool hasLocalPlayer = localPlayer != null;
            localPlayerMarker.gameObject.SetActive(hasLocalPlayer);

            if (!hasLocalPlayer)
                return;

            SetMarkerPosition(localPlayerMarker, localPlayer.transform.position);
            localPlayerMarker.SetAsLastSibling();
        }

        private RectTransform CreateMarker(
            string markerName,
            Vector2 markerSize,
            Color color,
            float zRotation)
        {
            GameObject markerObject = new GameObject(
                markerName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline));
            markerObject.layer = markerLayer.gameObject.layer;

            RectTransform marker = markerObject.GetComponent<RectTransform>();
            marker.SetParent(markerLayer, false);
            marker.anchorMin = new Vector2(0.5f, 0.5f);
            marker.anchorMax = new Vector2(0.5f, 0.5f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.sizeDelta = markerSize;
            marker.localRotation = Quaternion.Euler(0f, 0f, zRotation);

            Image image = markerObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            Outline outline = markerObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.03f, 0.02f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            return marker;
        }

        private void SetMarkerPosition(RectTransform marker, Transform target)
        {
            if (target == null)
                return;

            MiniMapPoint point = target.GetComponent<MiniMapPoint>();
            if (point == null)
            {
                SetMarkerPosition(marker, target.position);
                return;
            }

            SetMarkerNormalizedPosition(marker, point.NormalizedPosition);
        }

        private void SetMarkerPosition(RectTransform marker, Vector3 worldPosition)
        {
            if (marker == null)
                return;

            float safeWidth = Mathf.Max(0.001f, worldSize.x);
            float safeHeight = Mathf.Max(0.001f, worldSize.y);
            Vector2 worldMin = worldCenter - worldSize * 0.5f;
            Vector2 normalized = new Vector2(
                Mathf.Clamp01((worldPosition.x - worldMin.x) / safeWidth),
                Mathf.Clamp01((worldPosition.y - worldMin.y) / safeHeight));
            Vector2 diagramPosition = new Vector2(
                Mathf.Lerp(normalizedMapMin.x, normalizedMapMax.x, normalized.x),
                Mathf.Lerp(normalizedMapMin.y, normalizedMapMax.y, normalized.y));

            SetMarkerNormalizedPosition(marker, diagramPosition);
        }

        private static void SetMarkerNormalizedPosition(RectTransform marker, Vector2 normalizedPosition)
        {
            if (marker == null)
                return;

            Vector2 clampedPosition = new Vector2(
                Mathf.Clamp01(normalizedPosition.x),
                Mathf.Clamp01(normalizedPosition.y));

            marker.anchorMin = clampedPosition;
            marker.anchorMax = clampedPosition;
            marker.anchoredPosition = Vector2.zero;
        }

        private void AnimateMarkers()
        {
            float missionPulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.08f;
            float sabotagePulse = 1f + Mathf.Sin(Time.unscaledTime * 7f) * 0.12f;

            foreach (RectTransform marker in missionMarkers.Values)
            {
                if (marker != null && marker.gameObject.activeSelf)
                    marker.localScale = Vector3.one * missionPulse;
            }

            if (incubatorMarker != null && incubatorMarker.gameObject.activeSelf)
                incubatorMarker.localScale = Vector3.one * missionPulse;

            if (owlMailMailboxMarker != null && owlMailMailboxMarker.gameObject.activeSelf)
                owlMailMailboxMarker.localScale = Vector3.one * sabotagePulse;

            foreach (RectTransform marker in sabotageMarkers.Values)
            {
                if (marker != null && marker.gameObject.activeSelf)
                    marker.localScale = Vector3.one * sabotagePulse;
            }

        }

    }
}
