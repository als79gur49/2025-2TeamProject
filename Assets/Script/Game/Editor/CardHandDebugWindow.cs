using UnityEngine;
using UnityEditor;
using Game.Core;
using Game.Data;
using Game.Interfaces;
using Game.Managers;

namespace Game.Editor
{
    /// <summary>
    /// 전투 중 플레이어 손패를 디버그/조작하기 위한 에디터 창
    /// - CardServiceManager를 ServiceLocator에서 가져와 CardHandManager에 접근
    /// - CardData 직접 참조 기반 추가
    /// - Card ID + ICardRegistry 기반 추가
    /// </summary>
    public class CardHandDebugWindow : EditorWindow
    {
        private const int SlotCount = 3;

        // 슬롯별 CardData / CardID 설정
        private CardData[] _slotCardData = new CardData[SlotCount];
        private string[] _slotCardIds = new string[SlotCount];

        // 캐시된 서비스 레퍼런스 (OnGUI마다 갱신)
        private ICardHandManager _handManager;
        private ICardRegistry _cardRegistry;

        [MenuItem("Tools/Card Hand Debug")]
        public static void ShowWindow()
        {
            var window = GetWindow<CardHandDebugWindow>("Card Hand Debug");
            window.minSize = new Vector2(420, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Card Hand Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawRuntimeStatus();
            EditorGUILayout.Space(10);

            if (!Application.isPlaying)
            {
                // Play 모드가 아니면 카드 조작은 허용하지 않음
                return;
            }

            // Play 모드일 때만 ServiceLocator에서 서비스 갱신
            RefreshServiceReferences();

            DrawCardSlots();
        }

        /// <summary>
        /// 런타임/서비스 상태 표시
        /// </summary>
        private void DrawRuntimeStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Runtime / Service Status", EditorStyles.boldLabel);

            // Play 모드 여부
            EditorGUILayout.LabelField("Play Mode", Application.isPlaying ? "✅ Playing" : "❌ Not Playing");

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Play 모드에서만 손패를 조작할 수 있습니다.\n" +
                    "전투 씬을 실행한 후 이 창을 사용하세요.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // CardServiceManager 및 하위 서비스 상태 표시
            var cardService = ServiceLocator.Get<ICardServiceManager>();
            bool hasCardService = cardService != null;
            EditorGUILayout.LabelField("ICardServiceManager", hasCardService ? "✅ Found" : "❌ Not Found");

            ICardHandManager hand = hasCardService ? cardService.GetCardHandManager() : null;
            bool handReady = hand != null && hand.IsInitialized;
            EditorGUILayout.LabelField("CardHandManager (Player)", handReady ? "✅ Ready" : "❌ Null / Not Initialized");

            bool registryRegistered = ServiceLocator.IsRegistered<ICardRegistry>();
            EditorGUILayout.LabelField("ICardRegistry", registryRegistered ? "✅ Registered" : "❌ Not Registered");

            if (!hasCardService)
            {
                EditorGUILayout.HelpBox(
                    "ICardServiceManager가 ServiceLocator에 등록되어 있지 않습니다.\n" +
                    "GameInitializer에서 CardServiceManager.InitializeAndRegisterServices가 호출되는지 확인하세요.",
                    MessageType.Warning);
            }
            else if (!handReady)
            {
                EditorGUILayout.HelpBox(
                    "CardHandManager가 아직 초기화되지 않았습니다.\n" +
                    "전투 흐름에서 CardServiceManager가 Init을 완료했는지 확인하세요.",
                    MessageType.Warning);
            }

            if (!registryRegistered)
            {
                EditorGUILayout.HelpBox(
                    "ICardRegistry(CardDatabase)가 ServiceLocator에 등록되어 있지 않습니다.\n" +
                    "ServiceBootstrap에서 카드 데이터베이스 초기화가 수행되는지 확인하세요.",
                    MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// ServiceLocator를 통해 ICardHandManager / ICardRegistry 레퍼런스를 갱신
        /// </summary>
        private void RefreshServiceReferences()
        {
            var cardService = ServiceLocator.Get<ICardServiceManager>();
            _handManager = cardService != null ? cardService.GetCardHandManager() : null;

            _cardRegistry = ServiceLocator.IsRegistered<ICardRegistry>()
                ? ServiceLocator.Get<ICardRegistry>()
                : null;
        }

        /// <summary>
        /// 슬롯별 CardData / CardID 설정 및 액션
        /// </summary>
        private void DrawCardSlots()
        {
            bool handReady = _handManager != null && _handManager.IsInitialized;
            bool registryReady = _cardRegistry != null;

            for (int i = 0; i < SlotCount; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Slot {i + 1}", EditorStyles.boldLabel);

                // CardData 기반 설정
                _slotCardData[i] = (CardData)EditorGUILayout.ObjectField(
                    "CardData Asset",
                    _slotCardData[i],
                    typeof(CardData),
                    false);

                if (_slotCardData[i] != null)
                {
                    EditorGUILayout.LabelField("Card Name", _slotCardData[i].CardName);
                    EditorGUILayout.LabelField("Card ID (from asset)",
                        string.IsNullOrEmpty(_slotCardData[i].CardID) ? "(빈 ID)" : _slotCardData[i].CardID);
                }
                else
                {
                    EditorGUILayout.LabelField("Card Name", "(none)");
                    EditorGUILayout.LabelField("Card ID (from asset)", "(none)");
                }

                EditorGUILayout.Space(4);

                // Card ID 기반 설정
                string currentId = _slotCardIds[i] ?? string.Empty;
                _slotCardIds[i] = EditorGUILayout.TextField("Card ID", currentId);

                if (_cardRegistry == null)
                {
                    EditorGUILayout.HelpBox(
                        "ICardRegistry가 아직 ServiceLocator에 등록되지 않았습니다.\n" +
                        "CardID 기반 조회는 CardDatabase가 초기화된 이후에만 사용할 수 있습니다.",
                        MessageType.Info);
                }
                else if (!string.IsNullOrEmpty(_slotCardIds[i]))
                {
                    bool exists = _cardRegistry.HasCard(_slotCardIds[i]);
                    EditorGUILayout.LabelField("Registry Lookup", exists ? "✅ Found" : "❌ Not Found");

                    if (!exists)
                    {
                        EditorGUILayout.HelpBox(
                            "입력한 ID에 해당하는 카드가 CardDatabase에 없습니다.\n" +
                            "CardData.CardID 값 또는 Resources/Cards 경로를 확인하세요.",
                            MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "CardID 문자열을 입력하면 CardDatabase(ICardRegistry)를 통해\n" +
                        "Resources.LoadAll로 캐싱된 카드 데이터를 조회합니다.",
                        MessageType.None);
                }

                EditorGUILayout.Space(6);

                // CardData 기반 추가 버튼
                using (new EditorGUI.DisabledScope(!handReady || _slotCardData[i] == null))
                {
                    if (GUILayout.Button("Add CardData To Player Hand", GUILayout.Height(26)))
                    {
                        AddCardUsingCardData(_slotCardData[i], i);
                    }
                }

                // ID 기반 추가 버튼
                using (new EditorGUI.DisabledScope(!handReady || !registryReady || string.IsNullOrEmpty(_slotCardIds[i])))
                {
                    if (GUILayout.Button("Add Card By ID To Player Hand", GUILayout.Height(26)))
                    {
                        AddCardUsingId(_slotCardIds[i], i);
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6);
            }
        }

        /// <summary>
        /// 선택된 CardData를 그대로 손패에 추가
        /// </summary>
        private void AddCardUsingCardData(CardData cardData, int slotIndex)
        {
            if (_handManager == null || !_handManager.IsInitialized || cardData == null)
            {
                Debug.LogWarning("[CardHandDebugWindow] Cannot add card using CardData - hand manager or card is not ready");
                return;
            }

            bool success = _handManager.AddCardToHand(cardData);
            if (success)
            {
                Debug.Log($"[CardHandDebugWindow] [Slot {slotIndex + 1}] Added card to hand via CardData: {cardData.CardName} ({cardData.CardID})");
            }
            else
            {
                Debug.LogWarning($"[CardHandDebugWindow] [Slot {slotIndex + 1}] AddCardToHand failed for CardData: {cardData.CardName} ({cardData.CardID})");
            }
        }

        /// <summary>
        /// CardID + ICardRegistry 기반으로 CardData를 찾아 손패에 추가
        /// </summary>
        private void AddCardUsingId(string cardId, int slotIndex)
        {
            if (_handManager == null || !_handManager.IsInitialized || _cardRegistry == null)
            {
                Debug.LogWarning("[CardHandDebugWindow] Cannot add card using ID - hand manager or registry is not ready");
                return;
            }

            if (string.IsNullOrEmpty(cardId))
            {
                Debug.LogWarning("[CardHandDebugWindow] Card ID is empty");
                return;
            }

            CardData card = _cardRegistry.GetCardByID(cardId);
            if (card == null)
            {
                Debug.LogWarning($"[CardHandDebugWindow] [Slot {slotIndex + 1}] Card not found in registry for ID: {cardId}");
                return;
            }

            bool success = _handManager.AddCardToHand(card);
            if (success)
            {
                Debug.Log($"[CardHandDebugWindow] [Slot {slotIndex + 1}] Added card to hand via ID lookup: {card.CardName} ({cardId})");
            }
            else
            {
                Debug.LogWarning($"[CardHandDebugWindow] [Slot {slotIndex + 1}] AddCardToHand failed for ID: {cardId} (Card: {card.CardName})");
            }
        }
    }
}
