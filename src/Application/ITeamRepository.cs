using System;
using System.Threading.Tasks;
using Studium.Domain.Entities;

namespace Interface.Repositories;

public interface ITeamRepository
{
    Task<bool> AddAsync(StudentTeam team);
    Task<StudentTeam?> GetByIdAsync(Guid id);
    Task UpdateAsync(StudentTeam team);
}