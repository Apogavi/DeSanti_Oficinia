using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Domain;
using DeSanti.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeSanti.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task AddAsync(Cliente cliente)
    {
        _context.Clientes.Add(cliente);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException e) when (e.InnerException is MySql.Data.MySqlClient.MySqlException { Number: 1062 })
        { throw new DeSanti.Application.DocumentoDuplicadoException(); }
    }

    public async Task UpdateAsync(Cliente cliente)
    {
        _context.Clientes.Update(cliente);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException e) when (e.InnerException is MySql.Data.MySqlClient.MySqlException { Number: 1062 })
        { throw new DeSanti.Application.DocumentoDuplicadoException(); }
    }

    public async Task DeleteAsync(Cliente cliente)
    {
        _context.Clientes.Remove(cliente);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException e) when (e.InnerException is MySql.Data.MySqlClient.MySqlException { Number: 1062 })
        { throw new DeSanti.Application.DocumentoDuplicadoException(); }
    }

    public async Task<List<Cliente>> GetAllAsync()
    {
        return await _context.Clientes.ToListAsync();
    }

    public async Task<Cliente?> GetByIdAsync(long id)
    {
        return await _context.Clientes.Where(c => c.Id == id).FirstOrDefaultAsync();
    }

    public Task<bool> ExisteComDocumentoAsync(string documentoNormalizado, long? ignorarId = null)
    {
        return _context.Clientes.AsNoTracking()
            .AnyAsync(c => c.Documento == documentoNormalizado && (!ignorarId.HasValue || c.Id != ignorarId.Value));
    }
}