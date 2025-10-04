using System.Threading;
using System.Threading.Tasks;

namespace IssueAgent.Agent.GraphQL;

public interface IGraphQLClient
{
    Task<T> QueryAsync<T>(string query, CancellationToken cancellationToken);
}
