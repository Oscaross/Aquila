using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SeasonTable", menuName = "Scriptable Objects/Time/SeasonTable")]
public class SeasonTable : ScriptableObject
{
    private static SeasonTable instance;

    public static SeasonTable Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<SeasonTable>("SeasonTable");
            return instance;
        }
    }

    [SerializeField] private SeasonData spring, summer, autumn, winter;

    private void OnEnable() => instance = this;

    /// <summary>
    /// Gives back the data table for the requested season.
    /// </summary>
    /// <param name="s">The season to get the data table for.</param>
    /// <returns>A SeasonData instance containing the required data.</returns>
    public SeasonData Get(Season s)
    {
        switch (s)
        {
            case Season.Spring: return spring;
            case Season.Summer: return summer;
            case Season.Autumn: return autumn;
            case Season.Winter: return winter;
            default:
                Debug.LogError($"SeasonTable: no data for season '{s}'.", this);
                return spring;
        }
    }

    /// <summary>
    /// Returns the data table for the current season.
    /// </summary>
    /// <returns>The current season's data table.</returns>
    public SeasonData GetCurrentSeasonData() => Get(GameClock.CurrentSeason);
}
