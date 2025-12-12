using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 카드팩 오픈 연출 패널
/// - CardPackPresentationRequestEventChannelSO를 구독하여 카드팩 보상을 표시
/// - 연출이 끝나면 CardPackPresentationFinishedEventChannelSO를 통해 완료 신호 발행
/// </summary>
public class PackOpenPanel : UIPanel
{
    [Header("Event Channels")]
    [SerializeField] private CardPackPresentationRequestEventChannelSO presentationRequestChannel;
    [SerializeField] private CardPackPresentationFinishedEventChannelSO presentationFinishedChannel;

    [Header("UI References")]
    [SerializeField] private GameObject packClosedRoot;
    [SerializeField] private GameObject cardsRoot;
    [SerializeField] private Image packClosedImage;
    [SerializeField] private Transform cardSlotContainer;
    [SerializeField] private PackOpenCardSlot cardSlotPrefab;
    [SerializeField] private Button openButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Presentation Root")]
    [SerializeField] private RectTransform presentationRoot;

    private readonly List<PackOpenCardSlot> _slots = new List<PackOpenCardSlot>();
    private bool _isPresenting;

    protected override void OnInitializeWithDependencies()
    {
        base.OnInitializeWithDependencies();

        // 패널 활성화 여부와 상관없이 이벤트를 수신할 수 있도록
        // 초기화 시점에 구독하고, Cleanup에서 해제한다.
        if (presentationRequestChannel != null)
        {
            presentationRequestChannel.Subscribe(OnPresentationRequested);
        }

        // Panel 자체는 항상 활성 상태로 두고,
        // 실제 표시/숨김은 presentationRoot로만 제어한다.
        hideOnStart = false;

        if (presentationRoot != null)
        {
            presentationRoot.gameObject.SetActive(false);
        }
    }

    protected override void OnCleanup()
    {
        if (presentationRequestChannel != null)
        {
            presentationRequestChannel.Unsubscribe(OnPresentationRequested);
        }

        base.OnCleanup();
    }

    private void OnPresentationRequested(CardPackPresentationData data)
    {
        if (data == null || data.result == null)
        {
            Debug.LogWarning("[PackOpenPanel] Received invalid CardPackPresentationData");
            return;
        }

        if (_isPresenting)
        {
            Debug.LogWarning("[PackOpenPanel] Already presenting a pack - ignoring new request");
            return;
        }

        // 요청된 팩의 Sprite 적용 (나무 상자/금 상자 등)
        if (data.packSprite != null)
        {
            if (packClosedImage != null)
            {
                packClosedImage.sprite = data.packSprite;
            }
            else if (packClosedRoot != null)
            {
                var image = packClosedRoot.GetComponentInChildren<Image>();
                if (image != null)
                {
                    image.sprite = data.packSprite;
                }
            }
        }

        StartCoroutine(PresentRoutine(data.result));
    }

    private IEnumerator PresentRoutine(CardPackOpenResult result)
    {
        _isPresenting = true;

        // 전체 프레젠테이션 루트 활성화
        if (presentationRoot != null)
        {
            presentationRoot.gameObject.SetActive(true);
        }

        // 슬롯 초기화
        ClearSlots();

        if (packClosedRoot != null)
            packClosedRoot.SetActive(true);
        if (cardsRoot != null)
            cardsRoot.SetActive(false);

        if (titleText != null)
        {
            titleText.text = "Card Pack Opened";
        }

        // 버튼 상태 초기화
        if (openButton != null)
        {
            openButton.interactable = true;
            openButton.onClick.RemoveAllListeners();
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.onClick.RemoveAllListeners();
        }

        // 오픈 버튼 대기 (없으면 바로 열기)
        bool openClicked = false;
        if (openButton != null)
        {
            openButton.onClick.AddListener(() => openClicked = true);
        }
        else
        {
            openClicked = true;
        }

        while (!openClicked)
        {
            yield return null;
        }

        // 카드 슬롯 생성 및 표시
        if (packClosedRoot != null)
            packClosedRoot.SetActive(false);
        if (cardsRoot != null)
            cardsRoot.SetActive(true);

        if (result.Entries != null && cardSlotPrefab != null && cardSlotContainer != null)
        {
            foreach (var entry in result.Entries)
            {
                var slot = Object.Instantiate(cardSlotPrefab, cardSlotContainer);
                slot.Setup(entry.Card);
                _slots.Add(slot);

                // 필요 시 약간의 딜레이를 줄 수도 있음
                yield return null;
            }
        }

        // 확인 버튼 대기 (없으면 자동 종료)
        bool confirmClicked = false;
        if (confirmButton != null)
        {
            confirmButton.interactable = true;
            confirmButton.onClick.AddListener(() => confirmClicked = true);
        }
        else
        {
            confirmClicked = true;
        }

        while (!confirmClicked)
        {
            yield return null;
        }

        // 현재 프레젠테이션 종료 상태로 전환
        _isPresenting = false;

        // 프레젠테이션 루트 비활성화
        if (presentationRoot != null)
        {
            presentationRoot.gameObject.SetActive(false);
        }

        // 완료 이벤트 발행
        if (presentationFinishedChannel != null)
        {
            presentationFinishedChannel.RaiseEvent();
        }
    }

    private void ClearSlots()
    {
        if (_slots.Count == 0)
            return;

        foreach (var slot in _slots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }

        _slots.Clear();
    }
}
