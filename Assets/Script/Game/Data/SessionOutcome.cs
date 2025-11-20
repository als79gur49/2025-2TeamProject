using System;

namespace Game.Data
{
    /// <summary>
    /// 게임 세션의 결과 상태
    /// </summary>
    [Serializable]
    public enum SessionOutcome
    {
        Unknown = 0,
        Victory = 1,
        Defeat = 2,
        Aborted = 3
    }
}

