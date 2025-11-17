using System;
using UnityEngine;
using Game.Interfaces;

namespace Game.Data.Effects
{
    /// <summary>
    /// 배치형 유닛 이펙트에서 대상 팀을 상대적으로 지정하기 위한 설정입니다.
    /// TeamRelation은 항상 이펙트를 가진 유닛의 팀을 기준으로 해석됩니다.
    /// </summary>
    [Serializable]
    public class DeployEffectTeamTargetConfig
    {
        [SerializeField]
        private TeamRelation targetRelation = TeamRelation.Ally;

        public TeamRelation TargetRelation => targetRelation;
    }
}
