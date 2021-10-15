using PersonListener.Domain.Account;
using System;
using System.Threading.Tasks;

namespace PersonListener.Gateway.Interfaces
{
    public interface IAccountApi
    {
        Task<AccountResponseObject> GetAccountByIdAsync(Guid id, Guid correlationId);
    }
}
