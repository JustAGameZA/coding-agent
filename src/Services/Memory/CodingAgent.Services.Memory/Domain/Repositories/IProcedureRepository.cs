using CodingAgent.Services.Memory.Domain.Entities;

namespace CodingAgent.Services.Memory.Domain.Repositories;

/// <summary>
/// Repository for procedural memory operations
/// </summary>
public interface IProcedureRepository
{
    Task<Procedure?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Procedure> AddAsync(Procedure procedure, CancellationToken ct = default);
    Task UpdateAsync(Procedure procedure, CancellationToken ct = default);
    Task<Procedure?> GetByContextAsync(Dictionary<string, object> contextPattern, CancellationToken ct = default);
    Task<IEnumerable<Procedure>> GetTopProceduresAsync(int limit = 10, CancellationToken ct = default);
    Task<IEnumerable<Procedure>> SearchByNameAsync(string searchTerm, int limit = 10, CancellationToken ct = default);
}

