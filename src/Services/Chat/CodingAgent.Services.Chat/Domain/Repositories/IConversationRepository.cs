using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.SharedKernel.Results;

namespace CodingAgent.Services.Chat.Domain.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Conversation>> GetAllAsync(CancellationToken ct = default);
    Task<PagedResult<Conversation>> GetPagedAsync(Guid userId, PaginationParameters pagination, CancellationToken ct = default);
    Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Conversation>> SearchAsync(Guid userId, string query, CancellationToken ct = default);
    Task<PagedResult<Conversation>> SearchPagedAsync(Guid userId, string query, PaginationParameters pagination, CancellationToken ct = default);
    Task<Conversation> CreateAsync(Conversation conversation, CancellationToken ct = default);
    Task UpdateAsync(Conversation conversation, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
