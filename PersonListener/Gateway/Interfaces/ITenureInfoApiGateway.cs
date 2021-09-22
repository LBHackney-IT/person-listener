using PersonListener.Domain.TenureInformation;
using System;
using System.Threading.Tasks;

namespace PersonListener.Gateway.Interfaces
{
    public interface ITenureInfoApiGateway
    {
        Task<TenureResponseObject> GetTenureInfoByIdAsync(Guid id, Guid correlationId);
    }
}
