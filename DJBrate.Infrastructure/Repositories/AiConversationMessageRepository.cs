using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;

namespace DJBrate.Infrastructure.Repositories;

public class AiConversationMessageRepository : Repository<AiConversationMessage>, IAiConversationMessageRepository
{
    public AiConversationMessageRepository(AppDbContext context) : base(context) { }
}
