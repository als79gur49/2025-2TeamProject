using UnityEngine;
using UnityEditor;
using Game.Data;
using Game.Card.Effects;
using System.Collections.Generic;

namespace Game.Editor
{
    /// <summary>
    /// CardData 생성 및 테스트를 위한 Editor 유틸리티
    /// </summary>
    public class CardDataCreator : EditorWindow
    {
        [MenuItem("Tools/Card Data Creator")]
        static void ShowWindow()
        {
            GetWindow<CardDataCreator>("Card Data Creator");
        }

        void OnGUI()
        {
            GUILayout.Label("CardData 생성 테스트", EditorStyles.boldLabel);

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "이 도구는 CardData와 EffectData가 정상적으로 작동하는지 테스트합니다.\n" +
                "버튼을 클릭하면 Assets/SO/ 폴더에 카드가 생성됩니다.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            if (GUILayout.Button("테스트 1: 기본 데미지 카드 생성", GUILayout.Height(40)))
            {
                CreateTestDamageCard();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("테스트 2: 복합 효과 카드 생성", GUILayout.Height(40)))
            {
                CreateTestMultiEffectCard();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("테스트 3: 빈 카드 생성 (Inspector 수동 편집용)", GUILayout.Height(40)))
            {
                CreateEmptyCard();
            }
        }

        void CreateTestDamageCard()
        {
            var card = CardData.CreateDamageCard(
                name: "코드생성_파이어볼",
                desc: "적에게 3 피해를 줍니다",
                manaCost: 2,
                damageValue: 3,
                affectedType: AffectedType.Enemy,
                affectedRange: 1
            );

            string path = "Assets/SO/CodeCreated_Fireball.asset";
            AssetDatabase.CreateAsset(card, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = card;

            Debug.Log($"✅ 데미지 카드 생성 완료: {path}");
            EditorUtility.DisplayDialog("성공",
                $"카드가 생성되었습니다!\n\n경로: {path}\n\nInspector에서 effectDataList를 확인하세요.",
                "확인");
        }

        void CreateTestMultiEffectCard()
        {
            var effects = new EffectData[]
            {
                new EffectData(EffectType.Damage, 2, AffectedType.Enemy, 0),
                new EffectData(EffectType.Heal, 1, AffectedType.Ally, 0)
            };

            var card = CardData.CreateMultiEffectCard(
                name: "코드생성_흡혈",
                desc: "적에게 2 피해를 주고 자신을 1 회복합니다",
                manaCost: 2,
                effects
            );

            string path = "Assets/SO/CodeCreated_Lifesteal.asset";
            AssetDatabase.CreateAsset(card, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = card;

            Debug.Log($"✅ 복합 효과 카드 생성 완료: {path}");
            EditorUtility.DisplayDialog("성공",
                $"카드가 생성되었습니다!\n\n경로: {path}\n\nInspector에서 effectDataList Size: 2를 확인하세요.",
                "확인");
        }

        void CreateEmptyCard()
        {
            var card = ScriptableObject.CreateInstance<CardData>();

            // Reflection으로 private 필드 초기화
            var cardNameField = typeof(CardData).GetField("cardName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cardNameField?.SetValue(card, "빈카드_Inspector편집용");

            var descField = typeof(CardData).GetField("description",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            descField?.SetValue(card, "Inspector에서 직접 편집하세요");

            string path = "Assets/SO/Empty_ForInspectorEdit.asset";
            AssetDatabase.CreateAsset(card, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = card;

            Debug.Log($"✅ 빈 카드 생성 완료: {path}");
            EditorUtility.DisplayDialog("성공",
                $"빈 카드가 생성되었습니다!\n\n경로: {path}\n\n" +
                "Inspector에서 effectDataList의 Size를 변경하거나 + 버튼을 눌러보세요.\n\n" +
                "만약 여전히 추가가 안된다면 Unity Console 에러를 확인하세요!",
                "확인");
        }
    }
}