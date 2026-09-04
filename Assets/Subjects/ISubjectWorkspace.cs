using UnityEngine;

public interface ISubjectWorkspace
{
    public void AssignWorker(LegionSubject worker);
    public Vector2 GetBounds();
    public Profession GetProfession();
}
