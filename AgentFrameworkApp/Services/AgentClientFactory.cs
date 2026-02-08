using Azure.AI.Agents.Persistent;
using Azure.Identity;

namespace AgentFrameworkApp.Services;

/// <summary>
/// Factory that creates and manages the PersistentAgentsClient connection.
/// </summary>
public static class AgentClientFactory
{
    private static PersistentAgentsClient? _client;

    /// <summary>
    /// Gets or creates a singleton <see cref="PersistentAgentsClient"/> using the provided project endpoint.
    /// </summary>
    public static PersistentAgentsClient GetClient(string projectEndpoint)
    {
        _client ??= new PersistentAgentsClient(projectEndpoint, new DefaultAzureCredential());
        return _client;
    }
}
