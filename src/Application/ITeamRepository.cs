using System;
using System.Threading.Tasks;
using Domain.Activity;

namespace Interface.Repositories;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid id);
    Task UpdateAsync(Team team);
}