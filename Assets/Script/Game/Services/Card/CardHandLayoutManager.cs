using UnityEngine;
using System.Collections.Generic;
using Game.Card.UI;
using Game.Card.UI.Refactored;

namespace Game.Services
{
    /// <summary>
    /// 카드 핸드 레이아웃 정렬 유틸리티
    /// Player/Enemy 공통으로 사용하는 정적 레이아웃 메서드 제공
    /// Single Responsibility: 카드 UI 레이아웃 정렬만 담당
    /// </summary>
    public static class CardHandLayoutManager
    {
        /// <summary>
        /// 카드를 수직 중앙 정렬
        /// 카드들이 Y=0 중앙을 기준으로 위아래 균등 배치
        /// 카드 추가 시 전체 그룹이 자동으로 중앙 유지
        /// </summary>
        /// <param name="cards">정렬할 카드 UI 리스트</param>
        /// <param name="spacing">카드 간 간격 (픽셀)</param>
        public static void ArrangeVerticalCentered(List<CardUIRefactored> cards, float spacing)
        {
            int cardCount = cards.Count;
            if (cardCount == 0) return;

            // 전체 높이 계산 및 중앙 기준 시작점 설정
            float totalHeight = (cardCount - 1) * spacing;
            float startY = totalHeight * 0.5f;  // 중앙 기준 시작

            for (int i = 0; i < cardCount; i++)
            {
                if (cards[i] == null) continue;

                var rectTransform = cards[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 수직 배치 (X는 중앙, Y는 중앙 기준 위아래)
                    rectTransform.anchoredPosition = new Vector2(0, startY - i * spacing);
                    rectTransform.rotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// 카드를 호형으로 배치
        /// 중앙을 기준으로 최대 60도 호형으로 카드 배열
        /// </summary>
        /// <param name="cards">정렬할 카드 UI 리스트</param>
        /// <param name="arcRadius">호의 반지름</param>
        public static void ArrangeInArc(List<CardUIRefactored> cards, float arcRadius)
        {
            int cardCount = cards.Count;
            if (cardCount == 0) return;

            float totalAngle = Mathf.Min(60f, cardCount * 8f); // 최대 60도
            float startAngle = -totalAngle * 0.5f;
            float angleStep = cardCount > 1 ? totalAngle / (cardCount - 1) : 0f;

            for (int i = 0; i < cardCount; i++)
            {
                if (cards[i] == null) continue;

                float angle = startAngle + angleStep * i;
                float rad = angle * Mathf.Deg2Rad;

                // 호형 위치 계산
                float x = Mathf.Sin(rad) * arcRadius;
                float y = -Mathf.Cos(rad) * arcRadius * 0.1f; // 살짝 아래로 구부림

                var rectTransform = cards[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(x, y);
                    rectTransform.rotation = Quaternion.Euler(0, 0, angle * 0.5f); // 살짝 회전
                }
            }
        }

        /// <summary>
        /// 카드를 일직선으로 배치
        /// 중앙을 기준으로 좌우 대칭 일직선 배열
        /// </summary>
        /// <param name="cards">정렬할 카드 UI 리스트</param>
        /// <param name="spacing">카드 간 간격 (픽셀)</param>
        public static void ArrangeInLine(List<CardUIRefactored> cards, float spacing)
        {
            int cardCount = cards.Count;
            if (cardCount == 0) return;

            float totalWidth = (cardCount - 1) * spacing;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < cardCount; i++)
            {
                if (cards[i] == null) continue;

                var rectTransform = cards[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(startX + i * spacing, 0);
                    rectTransform.rotation = Quaternion.identity;
                }
            }
        }
    }
}
