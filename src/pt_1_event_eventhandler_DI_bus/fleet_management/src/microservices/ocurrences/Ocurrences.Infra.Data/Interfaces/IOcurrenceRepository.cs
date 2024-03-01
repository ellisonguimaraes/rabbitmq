using Ocurrences.Domain.Core.Entities;

namespace Ocurrences.Infra.Data.Interfaces;

public interface IOcurrenceRepository
{
    Task CreateAsync(Ocurrence ocurrence);
}
