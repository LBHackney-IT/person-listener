using PersonListener.Boundary;
using System.Threading.Tasks;

namespace PersonListener.UseCase.Interfaces
{
    public interface IMessageProcessing
    {
        Task ProcessMessageAsync(EntityEventSns message);
    }
}
