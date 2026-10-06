using DeSanti.Domain;

namespace DeSanti.Application.Interfaces.Repositories;

public interface IClienteRepository
{
    Task AddAsync(Cliente cliente);
    Task UpdateAsync(Cliente cliente);
    Task DeleteAsync(Cliente cliente);
    Task<List<Cliente>> GetAllAsync();
    Task<Cliente?> GetByIdAsync(long id);
}