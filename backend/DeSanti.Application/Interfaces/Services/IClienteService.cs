using DeSanti.Application.DTOs.Cliente;
using DeSanti.Domain;

namespace DeSanti.Application.Interfaces.Services;

public interface IClienteService
{
    Task<ResponseClienteDto> AddClienteAsync(CreateClienteDto dto);
    Task<bool> UpdateClienteAsync(UpdateClienteDto dto);
    Task<bool> DeleteClienteAsync(DesativarClienteDto dto);
    Task<List<ResponseClienteDto>> GetAllAsync();
    Task<ResponseClienteDto?> GetByIdAsync(long id);
}