using DeSanti.Application.DTOs.Cliente;
using DeSanti.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeSanti.API.Controllers;

[ApiController]
[Route("api/v1/clientes")]
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
    public async Task<IActionResult> CreateAsync([FromBody] CreateClienteDto dto)
    {
        if (dto == null) return BadRequest(new { message = "Preencha as informações do cliente." });
        
        var cliente = await _service.AddClienteAsync(dto);

        return Ok(cliente);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateClienteDto dto)
    {
        if (dto == null) return BadRequest(new { message = "Preencha as informações do cliente." });

        var success = await _service.UpdateClienteAsync(dto);

        if (!success) return NotFound(new { message = "Cliente não encontrado." });
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync([FromBody] DesativarClienteDto dto)
    {
        var success = await _service.DeleteClienteAsync(dto);
        
        if (!success) return NotFound(new { message = "Cliente não encontrado." });
        
        return NoContent();
    }
}