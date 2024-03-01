using Ocurrences.Domain.Core.Entities;
using Ocurrences.Infra.Data.Context;
using Ocurrences.Infra.Data.Interfaces;

namespace Ocurrences.Infra.Data;

public class OcurrenceRepository(ApplicationContext context) : IOcurrenceRepository
{
    private readonly ApplicationContext _context = context;

    public async Task CreateAsync(Ocurrence ocurrence)
    {
        _context.Add(ocurrence);
        await _context.SaveChangesAsync();
    }
}
