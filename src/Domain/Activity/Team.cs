namespace Domain.Activity;

public enum TeamState
{
    Open,
    Locked
}

public class Team
{
    // Zameni ili dodaj nova svojstva u postojeću klasu
    public TeamState State { get; private set; } = TeamState.Open;

    /// <summary>
    /// Vraća true ako je uspeh, ili false ako je tim već bio zaključan.
    /// </summary>
    public bool LockTeam()
    {
        if (State == TeamState.Locked)
        {
            return false;
        }

        State = TeamState.Locked;
        return true;
    }
}