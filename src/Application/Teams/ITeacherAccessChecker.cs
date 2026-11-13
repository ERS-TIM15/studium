namespace Application.Teams;

public enum TeacherAccess
{
    Allowed,
    Denied,
    ActivityNotFound
}

/// <summary>
/// Port za proveru da li je nastavnik angažovan na izvođenju kome pripada
/// projektna aktivnost (zajednička pravila 2.1 i 2.2). Identitet i uloge još
/// nisu implementirani u projektu; implementaciju treba povezati sa timom
/// zaduženim za ST-001/ST-002/ST-005.
/// </summary>
public interface ITeacherAccessChecker
{
    Task<TeacherAccess> CheckAsync(
        Guid teacherId,
        Guid projectActivityId,
        CancellationToken cancellationToken = default);
}
