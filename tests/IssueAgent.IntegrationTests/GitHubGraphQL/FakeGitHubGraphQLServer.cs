using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace IssueAgent.IntegrationTests.GitHubGraphQL;

public sealed class FakeGitHubGraphQLServer : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;
    private readonly ConcurrentQueue<string> _responses = new();
    private readonly List<string> _receivedQueries = new();

    public FakeGitHubGraphQLServer()
    {
        var port = GetAvailablePort();
        Endpoint = new Uri($"http://127.0.0.1:{port}/graphql");

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Start();

        _processingTask = Task.Run(() => HandleRequestsAsync(_cts.Token));
    }

    public Uri Endpoint { get; }

    public IReadOnlyList<string> ReceivedQueries
    {
        get
        {
            lock (_receivedQueries)
            {
                return _receivedQueries.ToArray();
            }
        }
    }

    public void EnqueueResponse(object payload)
    {
        var json = payload switch
        {
            string text => text,
            JsonDocument doc => doc.RootElement.GetRawText(),
            _ => JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            })
        };

        _responses.Enqueue(json);
    }

    public void Reset()
    {
        while (_responses.TryDequeue(out _))
        {
        }

        lock (_receivedQueries)
        {
            _receivedQueries.Clear();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Close();

        try
        {
            await _processingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (HttpListenerException)
        {
        }
    }

    private async Task HandleRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext? context;

            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (context.Request.Url?.AbsolutePath is not "/graphql")
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
                continue;
            }

            string requestBody;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            var query = ExtractQuery(requestBody);
            lock (_receivedQueries)
            {
                _receivedQueries.Add(query);
            }

            if (!_responses.TryDequeue(out var responseBody))
            {
                responseBody = "{\"errors\":[{\"message\":\"No fake response configured\"}]}";
            }

            var buffer = Encoding.UTF8.GetBytes(responseBody);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            context.Response.Close();
        }
    }

    private static string ExtractQuery(string requestBody)
    {
        if (string.IsNullOrWhiteSpace(requestBody))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(requestBody);
            return document.RootElement.TryGetProperty("query", out var query) ? query.GetString() ?? string.Empty : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
