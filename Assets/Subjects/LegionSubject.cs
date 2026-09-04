using Unity.VisualScripting;
using UnityEngine;

public class LegionSubject : MonoBehaviour
{
    [SerializeField] private Profession profession = Profession.Unemployed;
    [SerializeField] private Job job;

    public bool IsEmployed => profession != Profession.Unemployed;

    public float Morale
    {
        get;
        private set;
    }

    public void DoMoraleEvent(MoraleEvent moraleEvent)
    {
        Debug.Log(moraleEvent.DisplayName());
    }

    public void SetProfession(Profession profession)
    {
        this.profession = profession;
    }
}


