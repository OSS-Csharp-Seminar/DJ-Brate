using DJBrate.Application.Models.Ai;

namespace DJBrate.Application.Interfaces;

// Definira slanje razgovora i MCP alata odabranom AI modelu.
public interface IAiClient
{
    Task<AiResponse> SendMessageAsync(
        string systemPrompt,
        List<AiMessage> conversationHistory,
        List<AiToolDefinition> tools,
        float? temperature = null,
        int? maxOutputTokens = null);
}
