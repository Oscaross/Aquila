using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SeasonTable", menuName = "Scriptable Objects/Time/SeasonTable")]
public class SeasonTable : ScriptableObject
{
    public static SeasonTable Instance { get; private set; }

    [SerializeField] private SeasonData spring, summer, autumn, winter;

    private void OnEnable() => Instance = this;

    /// <summary>
    /// Gives back the data table for the requested season.
    /// </summary>
    /// <param name="s">The season to get the data table for.</param>
    /// <returns>A SeasonData instance containing the required data.</returns>
    public SeasonData Get(Season s) => s switch
    {
        Season.Spring => spring,
        Season.Summer => summer,
        Season.Autumn => autumn,
        Season.Winter => winter,
        _ => null
    };

    /// <summary>
    /// Returns the data table for the current season.
    /// </summary>
    /// <returns>The current season's data table.</returns>
    public SeasonData GetCurrentSeasonData() => Get(GameTime.CurrentSeason);
}
