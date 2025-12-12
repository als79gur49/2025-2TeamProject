using UnityEngine;
using Game.Data;
using Game.UI.Panels;

namespace Game.UI.Coordinators
{
    /// <summary>
    /// 스테이지 선택 화면 진입 시 기본 스테이지(Stage1-1 등)의
    /// 적 유닛 카드 프리뷰를 StageEnemyPreviewPanel에 직접 전달하여
    /// StageButton의 단일 클릭 프리뷰 흐름과 동일한 방식으로
    /// 초기 프리뷰를 구성하는 초기화용 컴포넌트.
    ///
    /// 사용 방법:
    /// 1. 스테이지 선택 씬의 적절한 GameObject에 이 스크립트를 추가합니다.
    /// 2. Inspector에서 기본으로 사용할 StageDataSO(예: Stage1-1)를 할당합니다.
    /// 3. 필요하다면 enemyPreviewPanel 슬롯에 StageEnemyPreviewPanel을 직접 연결합니다.
    ///    (비워두면 UIPanelFacade를 통해 자동으로 찾습니다.)
    /// 4. 씬이 시작되면 Start()에서 한 번 자동으로 프리뷰를 요청합니다.
    /// </summary>
    public class StagePreviewInitializer : MonoBehaviour
    {
        [Header("Default Stage")]
        [SerializeField]
        [Tooltip("씬 진입 시 기본으로 프리뷰할 StageDataSO (예: Stage1-1)")]
        private StageDataSO defaultStageData;

        [SerializeField]
        [Tooltip("Start 시 자동으로 기본 스테이지 프리뷰를 요청할지 여부")]
        private bool showOnStart = true;

        [Header("Preview Panel (Optional)")]
        [SerializeField]
        [Tooltip("명시적으로 지정하지 않으면 UIPanelFacade에서 자동으로 찾습니다.")]
        private StageEnemyPreviewPanel enemyPreviewPanel;

        private bool hasShownDefault = false;

        private void Awake()
        {
            // Inspector에서 지정되지 않은 경우, 패널 매니저를 통해 자동 검색
            if (enemyPreviewPanel == null)
            {
                enemyPreviewPanel = UIPanelFacade.GetPanel<StageEnemyPreviewPanel>();
            }
        }

        private void Start()
        {
            if (showOnStart)
            {
                ShowDefaultPreview();
            }
        }

        /// <summary>
        /// 외부에서 수동으로도 호출 가능한 기본 프리뷰 표시 메서드.
        /// StageButton의 onStagePreviewRequested와 동일하게
        /// StageEnemyPreviewPanel의 공개 API를 직접 호출합니다.
        /// </summary>
        public void ShowDefaultPreview()
        {
            if (hasShownDefault)
                return;

            if (defaultStageData == null)
            {
                Debug.LogWarning("[StagePreviewInitializer] Default StageDataSO is not assigned");
                return;
            }

            var previewPanel = enemyPreviewPanel ?? UIPanelFacade.GetPanel<StageEnemyPreviewPanel>();
            if (previewPanel == null)
            {
                Debug.LogWarning("[StagePreviewInitializer] StageEnemyPreviewPanel not found in current UI hierarchy");
                return;
            }

            previewPanel.ShowStagePreview(defaultStageData);
            hasShownDefault = true;

            Debug.Log($"[StagePreviewInitializer] Default enemy preview shown via direct call: {defaultStageData.StageId}");
        }
    }
}
