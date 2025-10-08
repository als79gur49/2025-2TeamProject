using UnityEngine;

namespace Game.Utilities
{
    /// <summary>
    /// 인스펙터에서 설정한 Material을 자식 오브젝트의 모든 Renderer에 적용하는 유틸리티 클래스
    /// </summary>
    public class MaterialSetter : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("자식 오브젝트의 모든 Renderer에 적용할 Material")]
        private Material targetMaterial;

        private void Awake()
        {
            ApplyMaterialToChildren();
        }

        /// <summary>
        /// 자식 오브젝트의 모든 Renderer에 targetMaterial 적용
        /// </summary>
        private void ApplyMaterialToChildren()
        {
            if (targetMaterial == null)
            {
                Debug.LogWarning($"[MaterialSetter] {gameObject.name}: targetMaterial이 설정되지 않았습니다.");
                return;
            }

            // 자식 오브젝트의 모든 Renderer 컴포넌트 가져오기
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[MaterialSetter] {gameObject.name}: 자식 오브젝트에서 Renderer를 찾을 수 없습니다.");
                return;
            }

            // 각 Renderer에 Material 적용
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.material = targetMaterial;
                }
            }

            Debug.Log($"[MaterialSetter] {gameObject.name}: {renderers.Length}개의 Renderer에 Material 적용 완료");
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 실시간으로 Material 변경 테스트
        /// </summary>
        [ContextMenu("Apply Material")]
        private void ApplyMaterialInEditor()
        {
            ApplyMaterialToChildren();
        }
#endif
    }
}
