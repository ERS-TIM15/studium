using System.Collections.ObjectModel;

namespace Studium.Domain.Entities;

public enum TeamCompositionStatus
{
    Open,
    Locked
}

/// <summary>
/// Tim studenata vezan za izvođenje predmeta i projektnu aktivnost.
/// Neispravni podaci se ne signaliziraju izuzetkom, već rezultatom
/// <see cref="TeamCreation"/>.
/// Stanje tima se menja samo kroz domenske operacije: sva svojstva imaju privatan
/// seter, a liste članova i poziva se izlažu samo kao kolekcije samo za čitanje.
/// </summary>
public class StudentTeam
{
    private readonly List<Guid> _memberStudentIds = new();
    private readonly List<Guid> _pendingInvitationStudentIds = new();
    private readonly ReadOnlyCollection<Guid> _memberStudentIdsView;
    private readonly ReadOnlyCollection<Guid> _pendingInvitationStudentIdsView;

    private StudentTeam()
    {
        _memberStudentIdsView = _memberStudentIds.AsReadOnly();
        _pendingInvitationStudentIdsView = _pendingInvitationStudentIds.AsReadOnly();
    }

    /// <summary>
    /// Identifikator tima. Dodeljuje se pri kreiranju (ili zadaje pri ubacivanju demo
    /// podataka) i posle se ne menja.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CourseOfferingId { get; private set; }

    public Guid ProjectActivityId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int MinSize { get; private set; }

    public int MaxSize { get; private set; }

    /// <summary>
    /// Članovi tima, uključujući tvorca. Kolekcija je samo za čitanje; članstvo se menja
    /// isključivo domenskim operacijama koje poštuju pravila tima.
    /// </summary>
    public IReadOnlyCollection<Guid> MemberStudentIds => _memberStudentIdsView;

    /// <summary>
    /// Studenti koji su pozvani u tim, a još nisu članovi. Kolekcija je samo za čitanje.
    /// </summary>
    public IReadOnlyCollection<Guid> PendingInvitationStudentIds => _pendingInvitationStudentIdsView;

    public TeamCompositionStatus Status { get; private set; } = TeamCompositionStatus.Open;

    /// <summary>
    /// Predstavnik tima. Dok ne postoji posebna dodela predstavnika, to je student koji je osnovao tim.
    /// </summary>
    public Guid RepresentativeStudentId { get; private set; }

    /// <summary>
    /// Kada je uključeno, timu mogu pristupiti samo studenti koji su u njega pozvani.
    /// </summary>
    public bool IsInviteOnly { get; private set; }

    /// <summary>
    /// Verzija koju je adapter pročitao iz skladišta. Adapter je uvećava pri upisu, pa se
    /// izmena između čitanja i pisanja prepoznaje kao konflikt, a ne kao tiha zamena stanja.
    /// </summary>
    public int Version { get; private set; }

    public bool IsMember(Guid studentId) =>
        studentId != Guid.Empty && _memberStudentIds.Contains(studentId);

    public bool IsRepresentative(Guid studentId) =>
        studentId != Guid.Empty && studentId == RepresentativeStudentId;

    /// <summary>
    /// Nezavisna kopija tima, uključujući listu članova i poziva. Adapteri njome čuvaju i
    /// vraćaju snimke, pa pozivalac ne može da menja sačuvano stanje kroz svoj objekat.
    /// </summary>
    public StudentTeam Copy()
    {
        var copy = new StudentTeam
        {
            Id = Id,
            CourseOfferingId = CourseOfferingId,
            ProjectActivityId = ProjectActivityId,
            Name = Name,
            MinSize = MinSize,
            MaxSize = MaxSize,
            Status = Status,
            RepresentativeStudentId = RepresentativeStudentId,
            IsInviteOnly = IsInviteOnly,
            Version = Version
        };
        copy._memberStudentIds.AddRange(_memberStudentIds);
        copy._pendingInvitationStudentIds.AddRange(_pendingInvitationStudentIds);

        return copy;
    }

    /// <summary>
    /// Postavlja verziju pri upisu u skladište; poziva je isključivo adapter.
    /// </summary>
    public void MarkStored(int version) => Version = version;

    /// <summary>
    /// Zatvara tim samo za pozvane studente ili ga ponovo otvara. Postojeći članovi i
    /// već poslati pozivi ostaju nepromenjeni. Proveru da li je pozivalac predstavnik
    /// tima vrši slučaj upotrebe.
    /// </summary>
    public void SetInviteOnly(bool inviteOnly)
    {
        IsInviteOnly = inviteOnly;
    }

    /// <summary>
    /// Dodaje poziv studentu. Vraća <c>false</c> bez izmene stanja za prazan identifikator,
    /// postojećeg člana i studenta koji je već pozvan. Provere zaključanog sastava i
    /// popunjenosti tima pripadaju slučaju upotrebe pozivanja.
    /// </summary>
    public bool InviteStudent(Guid studentId)
    {
        if (studentId == Guid.Empty || IsMember(studentId) || _pendingInvitationStudentIds.Contains(studentId))
        {
            return false;
        }

        _pendingInvitationStudentIds.Add(studentId);
        return true;
    }

    /// <summary>
    /// Proverava samo pravilo pristupa: otvoren tim prima svakoga, a tim samo po pozivu
    /// samo pozvane studente. Ostala pravila pristupanja (zaključan sastav, najveća
    /// veličina, jedan tim po aktivnosti) pripadaju slučaju upotrebe pristupanja timu,
    /// koji još nije implementiran.
    /// </summary>
    public bool AllowsJoinBy(Guid studentId) =>
        !IsInviteOnly || _pendingInvitationStudentIds.Contains(studentId);

    public static TeamCreation TryCreate(
        Guid courseOfferingId,
        Guid projectActivityId,
        string? name,
        Guid creatorStudentId,
        int minSize,
        int maxSize,
        Guid? teamId = null)
    {
        if (teamId == Guid.Empty)
        {
            return TeamCreation.Failure("Identifikator tima ne može biti prazan");
        }

        if (courseOfferingId == Guid.Empty)
        {
            return TeamCreation.Failure("Izvođenje predmeta mora biti zadato");
        }

        if (projectActivityId == Guid.Empty)
        {
            return TeamCreation.Failure("Projektna aktivnost mora biti zadata");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return TeamCreation.Failure("Naziv tima mora biti zadat");
        }

        if (creatorStudentId == Guid.Empty)
        {
            return TeamCreation.Failure("Tvorac tima mora biti zadat");
        }

        if (minSize < 1)
        {
            return TeamCreation.Failure("Najmanji broj članova tima mora biti najmanje 1");
        }

        if (maxSize < minSize)
        {
            return TeamCreation.Failure("Najveći broj članova tima ne sme biti manji od najmanjeg");
        }

        var team = new StudentTeam
        {
            Id = teamId ?? Guid.NewGuid(),
            CourseOfferingId = courseOfferingId,
            ProjectActivityId = projectActivityId,
            Name = name.Trim(),
            MinSize = minSize,
            MaxSize = maxSize,
            Status = TeamCompositionStatus.Open,
            RepresentativeStudentId = creatorStudentId,
            IsInviteOnly = false,
            Version = 0
        };
        team._memberStudentIds.Add(creatorStudentId);

        return TeamCreation.Success(team);
    }
}
