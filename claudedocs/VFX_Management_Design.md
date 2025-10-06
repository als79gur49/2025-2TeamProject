# VFX 관리 시스템 설계 개선안

## 📋 개요

**목적**: VFX를 재사용 가능한 ScriptableObject로 관리하여 유지보수성과 확장성 향상

**현재 문제점** (설계서 Version 1.1 기준):
- VFX 프리팹을 EffectData에 직접 참조 → 중복, 관리 어려움
- VFX 메타데이터가 여러 곳에 분산 (EffectData, VFXEventTrigger)
- 같은 VFX를 여러 효과에서 사용 시 중복 설정

---

## 🏗️ 개선 아키텍처

### 방식 1: VFXData ScriptableObject (권장)

**구조**:
```
CardData (SO)
└─> EffectData (클래스)
    └─> VFXData (SO) → GameObject + 메타데이터
```

**장점**:
- ✅ VFX 재사용 용이 (드래그앤드롭으로 할당)
- ✅ 메타데이터 중앙 관리 (duration, triggerTime 등)
- ✅ 프리팹과 설정을 하나의 에셋으로 관리
- ✅ 프로젝트 에셋으로 버전 관리 가능

**구현**:

```csharp
// VFXData.cs - ScriptableObject
using UnityEngine;

namespace Game.VFX
{
    /// <summary>
    /// VFX 프리팹과 메타데이터를 관리하는 ScriptableObject
    /// 재사용 가능한 VFX 에셋으로 여러 카드/효과에서 공유 가능
    /// </summary>
    [CreateAssetMenu(fileName = "New VFX", menuName = "Game/VFX/VFX Data", order = 1)]
    public class VFXData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string vfxName = "New VFX";
        [TextArea(2, 4)]
        [SerializeField] private string description = "";

        [Header("VFX 프리팹")]
        [Tooltip("실제 VFX GameObject 프리팹 (ParticleSystem, Animator 등)")]
        [SerializeField] private GameObject vfxPrefab;

        [Header("타이밍 설정")]
        [Tooltip("VFX 전체 재생 시간 (초). 0이면 자동 계산")]
        [SerializeField] private float duration = 0f;

        [Tooltip("효과 트리거 타이밍 (0-1 정규화). 0.7 = VFX 70% 지점")]
        [Range(0f, 1f)]
        [SerializeField] private float triggerNormalizedTime = 0.7f;

        [Header("고급 설정")]
        [Tooltip("VFX가 루프인 경우 수동으로 duration 설정 필요")]
        [SerializeField] private bool isLooping = false;

        [Tooltip("VFX 풀링 사용 여부 (추후 확장)")]
        [SerializeField] private bool usePooling = false;

        [Tooltip("풀 크기 (usePooling = true일 때)")]
        [SerializeField] private int poolSize = 5;

        #region Properties

        public string VFXName => vfxName;
        public string Description => description;
        public GameObject VFXPrefab => vfxPrefab;
        public float Duration => duration;
        public float TriggerNormalizedTime => triggerNormalizedTime;
        public bool IsLooping => isLooping;
        public bool UsePooling => usePooling;
        public int PoolSize => poolSize;

        #endregion

        #region Validation

        /// <summary>
        /// VFXData 유효성 검증
        /// </summary>
        public bool IsValid()
        {
            if (vfxPrefab == null)
            {
                Debug.LogError($"VFXData[{vfxName}]: VFX Prefab이 할당되지 않았습니다.");
                return false;
            }

            if (isLooping && duration <= 0f)
            {
                Debug.LogWarning($"VFXData[{vfxName}]: 루프 VFX는 duration을 수동으로 설정해야 합니다.");
            }

            return true;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnValidate()
        {
            triggerNormalizedTime = Mathf.Clamp01(triggerNormalizedTime);
            duration = Mathf.Max(0f, duration);
            poolSize = Mathf.Max(1, poolSize);

            if (string.IsNullOrEmpty(vfxName) && vfxPrefab != null)
            {
                vfxName = vfxPrefab.name;
            }
        }
#endif

        #endregion

        public override string ToString()
        {
            return $"VFXData[{vfxName}, Duration: {duration}s, Trigger: {triggerNormalizedTime:P0}]";
        }
    }
}
```

---

### 방식 2: VFXDatabase ScriptableObject (대규모 프로젝트용)

**구조**:
```
CardData (SO)
└─> EffectData (클래스)
    └─> string vfxId → VFXDatabase (SO) → VFXData (SO) → GameObject
```

**장점**:
- ✅ 모든 VFX 중앙 집중 관리
- ✅ ID 기반 참조로 타입 안전성 향상
- ✅ 풀링 시스템 통합 용이
- ✅ 런타임 VFX 교체 가능

**단점**:
- ❌ 추가 시스템 복잡도
- ❌ Database 초기화 필요

**구현** (선택적):

```csharp
// VFXDatabase.cs - ScriptableObject
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Game.VFX
{
    /// <summary>
    /// 모든 VFX를 중앙에서 관리하는 데이터베이스
    /// 대규모 프로젝트에서 VFX를 ID 기반으로 관리
    /// </summary>
    [CreateAssetMenu(fileName = "VFXDatabase", menuName = "Game/VFX/VFX Database", order = 0)]
    public class VFXDatabase : ScriptableObject
    {
        [SerializeField] private List<VFXData> vfxDataList = new List<VFXData>();

        private Dictionary<string, VFXData> vfxDictionary;

        #region Initialization

        private void OnEnable()
        {
            BuildDictionary();
        }

        private void BuildDictionary()
        {
            vfxDictionary = new Dictionary<string, VFXData>();

            foreach (var vfxData in vfxDataList)
            {
                if (vfxData == null) continue;

                string id = vfxData.name; // ScriptableObject의 파일명을 ID로 사용
                if (vfxDictionary.ContainsKey(id))
                {
                    Debug.LogWarning($"VFXDatabase: 중복된 VFX ID '{id}' 발견");
                    continue;
                }

                vfxDictionary[id] = vfxData;
            }

            Debug.Log($"VFXDatabase: {vfxDictionary.Count}개 VFX 로드 완료");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// VFX ID로 VFXData 가져오기
        /// </summary>
        public VFXData GetVFXData(string vfxId)
        {
            if (vfxDictionary == null || vfxDictionary.Count == 0)
            {
                BuildDictionary();
            }

            if (string.IsNullOrEmpty(vfxId))
            {
                Debug.LogWarning("VFXDatabase: VFX ID가 null 또는 빈 문자열입니다.");
                return null;
            }

            if (vfxDictionary.TryGetValue(vfxId, out VFXData vfxData))
            {
                return vfxData;
            }

            Debug.LogWarning($"VFXDatabase: VFX ID '{vfxId}'를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// VFX 프리팹 직접 가져오기
        /// </summary>
        public GameObject GetVFXPrefab(string vfxId)
        {
            var vfxData = GetVFXData(vfxId);
            return vfxData?.VFXPrefab;
        }

        /// <summary>
        /// 모든 VFX 목록 반환
        /// </summary>
        public IReadOnlyList<VFXData> GetAllVFX()
        {
            return vfxDataList.AsReadOnly();
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        [ContextMenu("Rebuild Database")]
        private void RebuildDatabase()
        {
            BuildDictionary();
            Debug.Log("VFXDatabase: 데이터베이스 재구축 완료");
        }

        [ContextMenu("Validate All VFX")]
        private void ValidateAllVFX()
        {
            int validCount = 0;
            int invalidCount = 0;

            foreach (var vfxData in vfxDataList)
            {
                if (vfxData == null)
                {
                    Debug.LogError("VFXDatabase: null VFXData 발견");
                    invalidCount++;
                    continue;
                }

                if (vfxData.IsValid())
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                }
            }

            Debug.Log($"VFXDatabase 검증 완료: 유효 {validCount}개, 무효 {invalidCount}개");
        }
#endif

        #endregion
    }
}
```

---

## 🔄 EffectData 수정 (VFXData 통합)

### Option 1: VFXData 직접 참조 (권장)

```csharp
// EffectData.cs 수정
[Serializable]
public class EffectData
{
    // 기존 필드들...

    [Header("시각적 효과 (Version 1.2)")]
    [Tooltip("VFX ScriptableObject. null이면 VFX 없이 즉시 실행")]
    [SerializeField] private VFXData vfxData;

    // [Deprecated] 기존 필드들 (하위 호환성)
    [SerializeField] private GameObject effectPrefab; // ← 제거 예정
    [SerializeField] private string effectAnimation = "";

    /// <summary>VFX 데이터 (Version 1.2)</summary>
    public VFXData VFXData => vfxData;

    /// <summary>효과 프리팹 (하위 호환성)</summary>
    public GameObject EffectPrefab => vfxData?.VFXPrefab ?? effectPrefab;

    /// <summary>VFX 트리거 타이밍 (Version 1.2)</summary>
    public float VFXTriggerNormalizedTime => vfxData?.TriggerNormalizedTime ?? 0.7f;

    /// <summary>VFX 지속 시간 (Version 1.2)</summary>
    public float VFXDuration => vfxData?.Duration ?? 0f;
}
```

### Option 2: VFXDatabase ID 참조 (대규모 프로젝트)

```csharp
// EffectData.cs 수정 (Database 사용)
[Serializable]
public class EffectData
{
    [Header("시각적 효과 (Version 1.2 - Database)")]
    [Tooltip("VFX ID (VFXDatabase에서 조회). 비어있으면 VFX 없음")]
    [SerializeField] private string vfxId = "";

    private VFXData cachedVFXData;

    /// <summary>VFX 데이터 (Database에서 조회)</summary>
    public VFXData VFXData
    {
        get
        {
            if (cachedVFXData == null && !string.IsNullOrEmpty(vfxId))
            {
                cachedVFXData = VFXDatabase.Instance.GetVFXData(vfxId);
            }
            return cachedVFXData;
        }
    }

    public GameObject EffectPrefab => VFXData?.VFXPrefab;
    public float VFXTriggerNormalizedTime => VFXData?.TriggerNormalizedTime ?? 0.7f;
}
```

---

## 📦 SpellEffectExecutor 수정

```csharp
// SpellEffectExecutor.cs - VFXData 사용
private static void ExecuteWithVFXBatch(
    List<EffectData> effectDataList,
    EffectData primaryEffect,
    Vector2Int targetPos,
    GameContext context)
{
    // VFXData 가져오기
    var vfxData = primaryEffect.VFXData;

    if (vfxData == null || !vfxData.IsValid())
    {
        Debug.LogWarning("[SpellEffectExecutor] Invalid VFXData, using immediate execution");
        ExecuteImmediateBatch(effectDataList, targetPos, context);
        return;
    }

    // VFX 생성
    Vector3 worldPos = context.GridController.GridToWorldPosition(targetPos);
    var vfxInstance = Instantiate(vfxData.VFXPrefab, worldPos, Quaternion.identity);

    // VFXEventTrigger 초기화 (VFXData에서 설정 가져오기)
    var vfxTrigger = vfxInstance.GetComponent<VFXEventTrigger>();
    if (vfxTrigger == null)
    {
        vfxTrigger = vfxInstance.AddComponent<VFXEventTrigger>();
    }

    // VFXData의 설정 적용
    vfxTrigger.Initialize(
        vfxData.TriggerNormalizedTime,
        executor.ExecuteBatchEffects
    );

    // Duration 설정 (VFXData에서 가져오기)
    float vfxDuration = vfxData.Duration > 0f
        ? vfxData.Duration
        : vfxTrigger.GetVFXDuration();

    Destroy(vfxInstance, vfxDuration + 1f);
}
```

---

## 🎮 사용 예시

### VFX 에셋 생성

**1. VFXData ScriptableObject 생성**:
```
Project 창:
Assets/VFX/Data/
├─ Fireball_VFX_Data.asset
├─ IceSpike_VFX_Data.asset
├─ HealLight_VFX_Data.asset
└─ Explosion_VFX_Data.asset
```

**2. Inspector 설정**:
```yaml
Fireball_VFX_Data:
  VFX Name: "Fireball"
  Description: "화염구 발사 효과"
  VFX Prefab: Fireball_VFX (프리팹 참조)
  Duration: 1.5
  Trigger Normalized Time: 0.7
  Is Looping: false
```

**3. CardData에서 사용**:
```yaml
FireballCard:
  Effect Data List:
    - [0] Damage Effect
      Type: Damage
      Value: 5
      VFX Data: Fireball_VFX_Data (SO 참조)  # ← ScriptableObject 할당
```

### 같은 VFX 재사용

```yaml
# 여러 카드에서 같은 VFX 공유
FireballCard:
  Effect Data[0].VFX Data: Fireball_VFX_Data

ExplosionCard:
  Effect Data[1].VFX Data: Fireball_VFX_Data  # 같은 SO 참조

# Fireball_VFX_Data 수정 → 모든 카드에 자동 반영!
```

---

## ✅ 마이그레이션 가이드

### 기존 시스템 → VFXData 전환

**Phase 1: VFXData 생성**
```csharp
// Editor 스크립트로 자동 변환
[MenuItem("Tools/VFX/Convert to VFXData")]
public static void ConvertToVFXData()
{
    var allCards = AssetDatabase.FindAssets("t:CardData");

    Dictionary<GameObject, VFXData> vfxDataMap = new Dictionary<GameObject, VFXData>();

    foreach (var guid in allCards)
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var card = AssetDatabase.LoadAssetAtPath<CardData>(path);

        foreach (var effectData in card.EffectDataList)
        {
            if (effectData.EffectPrefab != null && !vfxDataMap.ContainsKey(effectData.EffectPrefab))
            {
                // VFXData ScriptableObject 생성
                var vfxData = ScriptableObject.CreateInstance<VFXData>();
                vfxData.VFXPrefab = effectData.EffectPrefab;
                vfxData.VFXName = effectData.EffectPrefab.name;

                // 에셋으로 저장
                string savePath = $"Assets/VFX/Data/{vfxData.VFXName}_Data.asset";
                AssetDatabase.CreateAsset(vfxData, savePath);

                vfxDataMap[effectData.EffectPrefab] = vfxData;
            }
        }
    }

    AssetDatabase.SaveAssets();
    Debug.Log($"VFXData 변환 완료: {vfxDataMap.Count}개 생성");
}
```

**Phase 2: EffectData 필드 추가 (하위 호환성 유지)**
```csharp
// 기존 effectPrefab 유지하면서 vfxData 추가
[SerializeField] private VFXData vfxData;           // 새 필드
[SerializeField] private GameObject effectPrefab;   // 기존 필드 (deprecated)

// vfxData 우선, 없으면 effectPrefab 사용
public GameObject EffectPrefab => vfxData?.VFXPrefab ?? effectPrefab;
```

**Phase 3: 점진적 전환**
```
1. 새 카드: VFXData 사용
2. 기존 카드: effectPrefab으로 계속 동작 (하위 호환)
3. 시간 여유 있을 때 기존 카드도 VFXData로 전환
```

---

## 📊 비교 요약

| 항목 | 현재 방식 (직접 참조) | VFXData SO | VFXDatabase |
|------|---------------------|------------|-------------|
| **복잡도** | 단순 | 중간 | 높음 |
| **재사용성** | 낮음 | 높음 | 매우 높음 |
| **유지보수** | 어려움 | 쉬움 | 쉬움 |
| **풀링 지원** | 불가 | 확장 가능 | 쉬움 |
| **권장 규모** | VFX < 20개 | VFX 20~50개 | VFX > 50개 |
| **설정 편의성** | 중간 | 높음 | 높음 |
| **성능** | 동일 | 동일 | 풀링 시 우수 |

---

## 🎯 권장 사항

**현재 프로젝트 (TCG 기반)**: **VFXData ScriptableObject 방식 권장**

**이유**:
1. TCG는 보통 20~100개 VFX 사용 → 중간 규모
2. 카드별로 VFX 재사용 빈번 (같은 속성 카드끼리 공유)
3. VFX 파라미터 조정이 잦음 (밸런싱)
4. Inspector에서 드래그앤드롭으로 직관적

**다음 단계**:
1. Version 1.2로 설계서 업데이트
2. VFXData.cs 구현
3. EffectData에 vfxData 필드 추가 (하위 호환성 유지)
4. 기존 effectPrefab 점진적 전환

---

**작성일**: 2025-10-06
**버전**: 1.0
**관련 설계서**: VFX_Driven_Spell_Card_System_Design.md v1.1
