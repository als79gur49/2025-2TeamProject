using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Card.Effects
{
    /// <summary>
    /// Phase 2.8: AffectedRange 필드 추가 후 검증 테스트
    /// Unity 에디터에서 실행할 수 있는 AffectedRange 관련 기능 테스트
    /// </summary>
    public class AffectedRangeValidationTest : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private CardData testCard;
        [SerializeField] private Vector2Int testTargetPosition = Vector2Int.zero;
        [SerializeField] private Vector2Int testCheckPosition = Vector2Int.one;

        [Header("테스트 결과 (읽기 전용)")]
        [SerializeField] private bool isValidationComplete = false;
        [SerializeField] private int maxAffectedRange = -1;
        [SerializeField] private int affectedPositionCount = 0;
        [SerializeField] private bool positionInRange = false;

        /// <summary>
        /// 에디터에서 호출할 수 있는 검증 테스트
        /// </summary>
        [ContextMenu("Run AffectedRange Validation Test")]
        public void RunValidationTest()
        {
            if (testCard == null)
            {
                Debug.LogError("[AffectedRangeValidationTest] 테스트할 CardData가 지정되지 않았습니다.");
                return;
            }

            Debug.Log($"[AffectedRangeValidationTest] 카드 '{testCard.CardName}' AffectedRange 검증 시작");

            // Test 1: 기본 AffectedRange 필드 검증
            TestBasicAffectedRangeField();

            // Test 2: EffectData AffectedRange 검증
            TestEffectDataAffectedRange();

            // Test 3: 범위 계산 검증
            TestRangeCalculations();

            // Test 4: 확장 메서드 검증
            TestExtensionMethods();

            // Test 5: 카드 설명 검증
            TestCardDescription();

            isValidationComplete = true;
            Debug.Log($"[AffectedRangeValidationTest] 카드 '{testCard.CardName}' AffectedRange 검증 완료");
        }

        private void TestBasicAffectedRangeField()
        {
            Debug.Log($"[Test 1] 기본 AffectedRange 필드: {testCard.AffectedRange}");

            if (testCard.AffectedRange < 0)
            {
                Debug.LogWarning("[Test 1] AffectedRange가 음수입니다. 0 이상이어야 합니다.");
            }
            else
            {
                Debug.Log("[Test 1] ✅ 기본 AffectedRange 필드 유효");
            }

            maxAffectedRange = testCard.AffectedRange;
        }

        private void TestEffectDataAffectedRange()
        {
            Debug.Log($"[Test 2] EffectData 시스템 사용 여부: {testCard.IsEffectBasedCard}");

            if (testCard.IsEffectBasedCard)
            {
                foreach (var effectData in testCard.EffectDataList)
                {
                    Debug.Log($"[Test 2] Effect {effectData.Type}: AffectedRange={effectData.AffectedRange}, AffectedType={effectData.AffectedType}");

                    if (effectData.AffectedRange < 0)
                    {
                        Debug.LogWarning($"[Test 2] Effect {effectData.Type}의 AffectedRange가 음수입니다.");
                    }
                }

                maxAffectedRange = testCard.GetMaxAffectedRange();
                Debug.Log($"[Test 2] 최대 AffectedRange: {maxAffectedRange}");
            }
            else
            {
                Debug.Log("[Test 2] 레거시 시스템 사용 중");
            }
        }

        private void TestRangeCalculations()
        {
            Debug.Log($"[Test 3] 범위 계산 테스트 - Target: {testTargetPosition}, Check: {testCheckPosition}");

            // 영향받는 모든 위치 계산
            var allAffectedPositions = testCard.CalculateAllAffectedPositions(testTargetPosition);
            affectedPositionCount = allAffectedPositions.Count;
            Debug.Log($"[Test 3] 전체 영향받는 위치 수: {affectedPositionCount}");

            // 특정 위치가 범위 내에 있는지 확인
            positionInRange = testCard.DoesAnyEffectAffectPosition(testTargetPosition, testCheckPosition);
            Debug.Log($"[Test 3] 위치 {testCheckPosition}이 범위 내인가: {positionInRange}");

            // 각 효과 타입별 테스트
            if (testCard.IsEffectBasedCard)
            {
                foreach (var effectData in testCard.EffectDataList)
                {
                    var effectPositions = testCard.CalculateAffectedPositionsForEffect(testTargetPosition, effectData.Type);
                    Debug.Log($"[Test 3] {effectData.Type} 효과 영향 위치 수: {effectPositions.Count}");
                }
            }
        }

        private void TestExtensionMethods()
        {
            Debug.Log("[Test 4] 확장 메서드 테스트");

            // 검증 메서드 테스트
            bool isValidRange = testCard.ValidateAffectedRangeConfiguration();
            Debug.Log($"[Test 4] AffectedRange 설정 유효성: {isValidRange}");

            if (!isValidRange)
            {
                Debug.LogError("[Test 4] ❌ AffectedRange 설정이 유효하지 않습니다!");
            }
            else
            {
                Debug.Log("[Test 4] ✅ AffectedRange 설정 유효");
            }

            // 플레이어 영향 테스트
            bool canAffectSamePlayer = testCard.CanAffectPlayer(1, 1); // 같은 플레이어
            bool canAffectDifferentPlayer = testCard.CanAffectPlayer(1, 2); // 다른 플레이어

            Debug.Log($"[Test 4] 같은 플레이어에게 영향: {canAffectSamePlayer}");
            Debug.Log($"[Test 4] 다른 플레이어에게 영향: {canAffectDifferentPlayer}");
        }

        private void TestCardDescription()
        {
            Debug.Log("[Test 5] 카드 설명 생성 테스트");

            string description = testCard.GetDetailedDescription();
            bool containsAffectedInfo = description.Contains("효과범위") || description.Contains("대상");

            Debug.Log($"[Test 5] 설명에 AffectedRange 정보 포함: {containsAffectedInfo}");

            if (testCard.IsEffectBasedCard && !containsAffectedInfo)
            {
                Debug.LogWarning("[Test 5] EffectData를 사용하는 카드인데 설명에 영향 정보가 없습니다.");
            }

            Debug.Log($"[Test 5] 생성된 설명:\n{description}");
        }

        #if UNITY_EDITOR
        [ContextMenu("Generate Test Card with AffectedRange")]
        public void GenerateTestCard()
        {
            // 테스트용 카드 생성
            var card = ScriptableObject.CreateInstance<CardData>();

            // EffectData 리스트에 테스트 데이터 추가
            var damageEffect = new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 1);
            var healEffect = new EffectData(EffectType.Heal, 2, AffectedType.Ally, 0);

            // 리플렉션을 사용하여 private 필드에 접근 (테스트 목적)
            var effectDataListField = typeof(CardData).GetField("effectDataList",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (effectDataListField != null)
            {
                var effectList = new List<EffectData> { damageEffect, healEffect };
                effectDataListField.SetValue(card, effectList);
            }

            testCard = card;
            Debug.Log("[GenerateTestCard] AffectedRange 테스트용 카드가 생성되었습니다.");
        }
        #endif
    }
}