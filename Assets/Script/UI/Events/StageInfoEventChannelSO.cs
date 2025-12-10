using UnityEngine;
using Game.Data;

/// <summary>
/// Event channel for broadcasting stage information.
/// Used to request opening StageDetailInfoPanel with a given StageDataSO.
/// </summary>
[CreateAssetMenu(fileName = "StageInfoEventChannel", menuName = "Events/UI/Stage Info Event Channel")]
public class StageInfoEventChannelSO : GameEventChannelSO<StageDataSO>
{
    /// <summary>
    /// Convenience method to show stage information.
    /// Validates StageDataSO before raising the event.
    /// </summary>
    /// <param name="stageData">The stage data to display.</param>
    public void ShowStageInfo(StageDataSO stageData)
    {
        if (stageData == null)
        {
            Debug.LogWarning("StageInfoEventChannelSO: Attempted to show null StageDataSO");
            return;
        }

        RaiseEvent(stageData);
    }
}

