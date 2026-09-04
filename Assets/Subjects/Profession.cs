using UnityEngine;

public enum Profession
{
    Unemployed,
    Farmer,
    Lumberjack,
    Archer,
}

public struct Job
{
    private Transform location;
    private Profession profession;

    public Job(Profession profession, Transform location)
    {
        this.profession = profession;
        this.location = location;
    }
}