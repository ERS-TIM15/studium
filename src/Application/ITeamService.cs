using System;
using System.Threading.Tasks;

namespace Application;

public interface ITeamService
{
    Task<(bool IsSuccess, string ErrorMessage)> LockTeamAsync(Guid teamId);
}