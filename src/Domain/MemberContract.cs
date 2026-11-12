namespace Domain;

public record MemberContract(
    Guid MemberId,
    string FullName,
    string Role
);

public class Team
    {
        private readonly List<MemberContract> _members = new();
        public string Name { get; private set; }

        public Team(string name)
        {
            Name = name;
        }

        public void RemoveMember(Guid studentId)
        {
            var member = _members.FirstOrDefault(m => m.MemberId == studentId);
            if (member == null)
                throw new InvalidOperationException("Student nije clan u timu");

            _members.Remove(member);
        }
    }
