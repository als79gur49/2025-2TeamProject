using System;
using System.Collections.Generic;
using Game.Core.Effects;
using Game.Interfaces;
using Game.ScriptableObjects;
using Game.VFX;
using UnityEngine;

namespace Game.Components
{
    /// <summary>
    /// Unit에 부착된 지속형 상태 VFX를 관리하는 컴포넌트입니다.
    /// EffectManager의 이벤트를 통해 VFX를 생성/제거합니다.
    /// </summary>
    public class UnitVFXController : MonoBehaviour
    {
        [Header("Owner")]
        [SerializeField] private Unit owner;

        [Header("Status VFX Config (Global)")]
        [SerializeField] private StatusVFXConfig statusVfxConfig;

        [Header("VFX Roots")]
        [SerializeField] private Transform vfxRootDefault;
        [SerializeField] private Transform vfxRootHead;
        [SerializeField] private Transform vfxRootBody;
        [SerializeField] private Transform vfxRootFeet;
        [SerializeField] private Transform vfxRootWeapon;

        // 타입별 Effect 개수
        private readonly Dictionary<Type, int> effectTypeCounts = new Dictionary<Type, int>();

        // 타입별 활성 VFX 인스턴스
        private readonly Dictionary<Type, GameObject> activeVfxByType = new Dictionary<Type, GameObject>();

        public void Initialize(Unit unit)
        {
            owner = unit;

            if (vfxRootDefault == null && owner != null)
            {
                vfxRootDefault = owner.transform;
            }
        }

        public void OnEffectAdded(IEffect effect)
        {
            if (effect is not IPersistentVFXEffect persistent)
                return;

            var type = effect.GetType();

            if (!effectTypeCounts.TryGetValue(type, out var count))
            {
                count = 0;
            }

            count++;
            effectTypeCounts[type] = count;

            // 이미 이 타입의 VFX가 활성화되어 있으면 VFX는 하나만 유지합니다.
            if (activeVfxByType.ContainsKey(type))
                return;

            var instance = SpawnPersistentVFX(persistent);
            if (instance != null)
            {
                activeVfxByType[type] = instance;
            }
        }

        public void OnEffectRemoved(IEffect effect)
        {
            if (effect is not IPersistentVFXEffect)
                return;

            var type = effect.GetType();

            if (!effectTypeCounts.TryGetValue(type, out var count))
                return;

            count = Mathf.Max(0, count - 1);
            effectTypeCounts[type] = count;

            // 같은 타입의 Effect가 아직 남아 있으면 VFX를 유지합니다.
            if (count > 0)
                return;

            if (activeVfxByType.TryGetValue(type, out var go) && go != null)
            {
                Destroy(go);
            }

            activeVfxByType.Remove(type);
        }

        public void CleanupAllVFX()
        {
            foreach (var kv in activeVfxByType)
            {
                if (kv.Value != null)
                {
                    Destroy(kv.Value);
                }
            }

            activeVfxByType.Clear();
            effectTypeCounts.Clear();
        }

        private GameObject SpawnPersistentVFX(IPersistentVFXEffect persistent)
        {
            if (statusVfxConfig == null && persistent.GetVFXOverrideOrNull() == null)
            {
                return null;
            }

            var vfxOverride = persistent.GetVFXOverrideOrNull();

            VFXData vfxData = null;
            VFXAnchorType configAnchor = VFXAnchorType.Default;
            Vector3 configOffset = Vector3.zero;

            if (vfxOverride != null)
            {
                vfxData = vfxOverride;

                if (statusVfxConfig != null &&
                    statusVfxConfig.TryGet(persistent.GetPersistentVFXId(), out var entry))
                {
                    configAnchor = entry.anchor;
                    configOffset = entry.offset;
                }
            }
            else
            {
                if (!statusVfxConfig.TryGet(persistent.GetPersistentVFXId(), out var entry) ||
                    entry.vfxData == null)
                {
                    return null;
                }

                vfxData = entry.vfxData;
                configAnchor = entry.anchor;
                configOffset = entry.offset;
            }

            if (vfxData == null || !vfxData.IsValid())
            {
                return null;
            }

            var anchorFromEffect = persistent.GetVFXAnchor();
            var finalAnchor = anchorFromEffect != VFXAnchorType.Default ? anchorFromEffect : configAnchor;

            var root = ResolveRoot(finalAnchor);
            if (root == null)
            {
                return null;
            }

            var finalOffset = configOffset + persistent.GetVFXOffset();

            var go = Instantiate(vfxData.VFXPrefab, root);
            go.transform.localPosition = finalOffset;

            // 프리팹의 기본 회전을 기준으로 팀별 회전 보정 적용
            ApplyTeamRotation(go);
            ApplyPlaybackSpeed(go, vfxData.PlaybackSpeed);

            return go;
        }

        private Transform ResolveRoot(VFXAnchorType anchor)
        {
            return anchor switch
            {
                VFXAnchorType.Head => vfxRootHead ?? vfxRootDefault ?? owner?.transform,
                VFXAnchorType.Body => vfxRootBody ?? vfxRootDefault ?? owner?.transform,
                VFXAnchorType.Feet => vfxRootFeet ?? vfxRootDefault ?? owner?.transform,
                VFXAnchorType.Weapon => vfxRootWeapon ?? vfxRootDefault ?? owner?.transform,
                _ => vfxRootDefault ?? owner?.transform
            };
        }

        private static void ApplyPlaybackSpeed(GameObject go, float speed)
        {
            speed = Mathf.Clamp(speed, 0.1f, 3.0f);

            var particles = go.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                var main = ps.main;
                main.simulationSpeed = speed;
            }

            var animator = go.GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = speed;
            }
        }

        private void ApplyTeamRotation(GameObject go)
        {
            if (go == null || owner == null)
            {
                return;
            }

            TeamType teamType = TeamType.None;

            var teamComponent = owner.GetComponent<ITeamComponent>();
            if (teamComponent != null)
            {
                teamType = teamComponent.Team;
            }
            else
            {
                teamType = owner.IsPlayerUnit ? TeamType.Player : TeamType.Enemy;
            }

            float additionalY = 0f;

            switch (teamType)
            {
                case TeamType.Player:
                case TeamType.Ally:
                    additionalY = 0f;
                    break;
                case TeamType.Enemy:
                    additionalY = 180f;
                    break;
                default:
                    additionalY = 0f;
                    break;
            }

            var t = go.transform;
            var euler = t.localEulerAngles;
            euler.y += additionalY;
            t.localEulerAngles = euler;
        }
    }
}
