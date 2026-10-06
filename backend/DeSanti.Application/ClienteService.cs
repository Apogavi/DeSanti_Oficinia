using DeSanti.Application.DTOs.Cliente;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Application.Interfaces.Services;
using DeSanti.Domain;

namespace DeSanti.Application;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _repository;

    public ClienteService(IClienteRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<ResponseClienteDto> AddClienteAsync(CreateClienteDto dto)
    {
        Cliente cliente = new()
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone,
            Documento = dto.Documento,
            DataCriacao = DateTime.UtcNow
        };

        await _repository.AddAsync(cliente);
        return MapDto(cliente);
    }

    public async Task<bool> UpdateClienteAsync(UpdateClienteDto dto)
    {
        Cliente? cliente = await _repository.GetByIdAsync(dto.Id);

        if (cliente == null) return false;
        
        if (!string.IsNullOrWhiteSpace(dto.Nome)) cliente.Nome = dto.Nome;
        if (!string.IsNullOrWhiteSpace(dto.Email)) cliente.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Telefone)) cliente.Telefone = dto.Telefone;
        if (!string.IsNullOrWhiteSpace(dto.Documento)) cliente.Documento = dto.Documento;
        cliente.DataAlteracao = DateTime.UtcNow;
        cliente.Ativo = dto.Ativo;

        await _repository.UpdateAsync(cliente);

        return true;
    }

    public async Task<bool> DeleteClienteAsync(DesativarClienteDto dto)
    {
        Cliente? cliente = await _repository.GetByIdAsync(dto.Id);
        
        if (cliente == null) return false;
        
        cliente.Ativo = dto.Ativo;
        
        await _repository.UpdateAsync(cliente);

        return true;
    }

    public async Task<List<ResponseClienteDto>> GetAllAsync()
    {
        List<ResponseClienteDto> lstResponse = new();
        List<Cliente> lstClientes = await _repository.GetAllAsync();

        foreach (var cliente in lstClientes)
        {
            lstResponse.Add(MapDto(cliente));
        }

        return lstResponse;
    }

    public async Task<ResponseClienteDto?> GetByIdAsync(long id)
    {
        Cliente? cliente = await _repository.GetByIdAsync(id);
        
        if (cliente == null) return null;
        
        return MapDto(cliente);
    }

    private static ResponseClienteDto MapDto(Cliente cliente)
    {
        return new ResponseClienteDto()
        {
            Id = cliente.Id,
            Nome = cliente.Nome,
            Email = cliente.Email,
            Telefone = cliente.Telefone,
            Documento = cliente.Documento,
            DataCriacao = cliente.DataCriacao,
            DataAlteracao = cliente.DataAlteracao,
            Ativo = cliente.Ativo
        };
    }
}