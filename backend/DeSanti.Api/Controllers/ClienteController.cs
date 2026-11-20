using DeSanti.Application.DTOs.Cliente;
using DeSanti.Application.Interfaces.Services;
using DeSanti.Application.Validators.Clientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeSanti.API.Controllers;

[ApiController]
[Route("api/v1/clientes")]
[Authorize]
public class ClienteController : ControllerBase
{
    private readonly IClienteService _service;
    
    public ClienteController(IClienteService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<List<ResponseClienteDto>> GetAllAsync()
    {
        return await _service.GetAllAsync();
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] long id)
    {
        var cliente = await _service.GetByIdAsync(id);

        if (cliente == null) return NotFound(new { message = "Cliente não encontrado." });
        
        return Ok(cliente);
    }

    [HttpPost]
    [ProducesResponseType<ResponseClienteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateClienteDto? dto)
    {
        var erros = CriarClienteValidator.Validar(dto);
        if (erros.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(erros)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Confira os dados do cliente."
            });
        }
        
        ResponseClienteDto cliente;
        try { cliente = await _service.AddClienteAsync(dto!); }
        catch (DeSanti.Application.DocumentoDuplicadoException) { return DocumentoDuplicado(); }

        return StatusCode(StatusCodes.Status201Created, cliente);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateClienteDto? dto)
    {
        var erros = CriarClienteValidator.ValidarAtualizacao(dto);
        if (erros.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(erros)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Confira os dados do cliente."
            });
        }

        bool success;
        try { success = await _service.UpdateClienteAsync(dto!); }
        catch (DeSanti.Application.DocumentoDuplicadoException) { return DocumentoDuplicado(); }

        if (!success) return NotFound(new { message = "Cliente não encontrado." });
        return NoContent();
    }

    private IActionResult DocumentoDuplicado() => Conflict(new ValidationProblemDetails(
        new Dictionary<string, string[]> { ["documento"] = ["Já existe um cliente com este CPF/CNPJ. Consulte o cadastro existente."] })
        { Status = 409, Title = "CPF/CNPJ já cadastrado." });

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync([FromBody] DesativarClienteDto dto)
    {
        var success = await _service.DeleteClienteAsync(dto);
        
        if (!success) return NotFound(new { message = "Cliente não encontrado." });
        
        return NoContent();
    }
}