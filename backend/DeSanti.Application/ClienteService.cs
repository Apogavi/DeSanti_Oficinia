using DeSanti.Application.DTOs.Cliente;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Application.Interfaces.Services;
using DeSanti.Application.Validators.Clientes;
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
        await VerificarDocumentoAsync(CriarClienteValidator.SomenteDigitos(dto.Documento), null);
        Cliente cliente = new()
        {
            Nome = (dto.Nome ?? string.Empty).Trim(),
            Email = (dto.Email ?? string.Empty).Trim(),
            Telefone = CriarClienteValidator.SomenteDigitos(dto.Telefone),
            Documento = CriarClienteValidator.SomenteDigitos(dto.Documento),
            // Alinha a criação ao default true do mapping e do schema de Clientes.
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        await _repository.AddAsync(cliente);
        return MapDto(cliente);
    }

    public async Task<bool> UpdateClienteAsync(UpdateClienteDto dto)
    {
        Cliente? cliente = await _repository.GetByIdAsync(dto.Id);

        if (cliente == null) return false;

        var documento = CriarClienteValidator.SomenteDigitos(dto.Documento);
        await VerificarDocumentoAsync(documento, dto.Id);

        cliente.Nome = (dto.Nome ?? string.Empty).Trim();
        cliente.Email = (dto.Email ?? string.Empty).Trim();
        cliente.Telefone = CriarClienteValidator.SomenteDigitos(dto.Telefone);
        cliente.Documento = documento;
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

    private async Task VerificarDocumentoAsync(string documento, long? ignorarId)
    {
        if (await _repository.ExisteComDocumentoAsync(documento, ignorarId))
            throw new DocumentoDuplicadoException();
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
