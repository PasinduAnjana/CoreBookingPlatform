using System.Net;
using System.Net.Http.Json;

static class Check
{
    public static int Count { get; private set; }

    public static void That(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        Count++;
        Console.WriteLine("PASS " + description);
    }

    public static async Task<T> Throws<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T ex) { return ex; }
        throw new Exception($"Expected {typeof(T).Name}.");
    }
}

sealed class Responses(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(respond(request));

    public static HttpClient Client(Func<HttpRequestMessage, HttpResponseMessage> respond)
        => new(new Responses(respond)) { BaseAddress = new Uri("http://fixture/") };

    public static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = JsonContent.Create(body) };
}
