using System;
using System.Collections.Generic;
using Game.Core.Effects;
using Game.VFX;
using UnityEngine;

namespace Game.ScriptableObjects
{
    [Serializable]
    public class StatusVFXEntry
    {
        public string id;
        public VFXData vfxData;
        public VFXAnchorType anchor;
        public Vector3 offset;
    }

    /// <summary>
    /// 상태 ID와 기본 VFX 구성을 매핑하는 전역 설정입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "StatusVFXConfig", menuName = "Game/VFX/StatusVFXConfig")]
    public class StatusVFXConfig : ScriptableObject
    {
        [SerializeField]
        private List<StatusVFXEntry> entries = new List<StatusVFXEntry>();

        private readonly Dictionary<string, StatusVFXEntry> entryById = new Dictionary<string, StatusVFXEntry>();

        private void OnEnable()
        {
            entryById.Clear();
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;

                entryById[entry.id] = entry;
            }
        }

        public bool TryGet(string id, out StatusVFXEntry entry)
        {
            if (string.IsNullOrEmpty(id))
            {
                entry = null;
                return false;
            }

            return entryById.TryGetValue(id, out entry);
        }
    }
}

