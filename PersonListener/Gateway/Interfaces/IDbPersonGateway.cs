using Hackney.Shared.Person;
using System;
using System.Threading.Tasks;

namespace PersonListener.Gateway.Interfaces
{
    public interface IDbPersonGateway
    {
        Task<Person> GetPersonByIdAsync(Guid id);
        Task SavePersonAsync(Person person);
    }
}
