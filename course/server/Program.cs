using System.Diagnostics;
using System.Net;
using System.Text;

var root = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Directory.GetCurrentDirectory();

if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"Dossier introuvable : {root}");
    return 1;
}

const string prefix = "http://127.0.0.1:8765/";
using var listener = new HttpListener();
listener.Prefixes.Add(prefix);
listener.Start();

var courseUrl = prefix + "course/index.html";
Console.WriteLine("Formation .NET — cours local hors ligne");
Console.WriteLine($"Racine : {root}");
Console.WriteLine($"Cours  : {courseUrl}");
Console.WriteLine("Ctrl+C pour arrêter le serveur.");

try
{
    Process.Start(new ProcessStartInfo(courseUrl) { UseShellExecute = true });
}
catch
{
    // L'ouverture automatique du navigateur n'est pas indispensable.
}

while (true)
{
    HttpListenerContext context;
    try
    {
        context = await listener.GetContextAsync();
    }
    catch (HttpListenerException)
    {
        break;
    }

    _ = Task.Run(async () =>
    {
        try
        {
            var requestPath = Uri.UnescapeDataString(context.Request.Url?.AbsolutePath ?? "/")
                .TrimStart('/');

            if (string.IsNullOrWhiteSpace(requestPath))
                requestPath = "course/index.html";

            requestPath = requestPath.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(root, requestPath));
            var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 403;
                await WriteText(context.Response, "Accès refusé.");
                return;
            }

            if (Directory.Exists(fullPath))
                fullPath = Path.Combine(fullPath, "index.html");

            if (!File.Exists(fullPath))
            {
                context.Response.StatusCode = 404;
                await WriteText(context.Response, "Fichier introuvable.");
                return;
            }

            var bytes = await File.ReadAllBytesAsync(fullPath);
            context.Response.StatusCode = 200;
            context.Response.ContentType = GetContentType(fullPath);
            context.Response.ContentLength64 = bytes.Length;
            context.Response.Headers["Cache-Control"] = "no-store";
            await context.Response.OutputStream.WriteAsync(bytes);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await WriteText(context.Response, ex.Message);
        }
        finally
        {
            context.Response.OutputStream.Close();
        }
    });
}

return 0;

static async Task WriteText(HttpListenerResponse response, string text)
{
    var bytes = Encoding.UTF8.GetBytes(text);
    response.ContentType = "text/plain; charset=utf-8";
    response.ContentLength64 = bytes.Length;
    await response.OutputStream.WriteAsync(bytes);
}

static string GetContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
{
    ".html" => "text/html; charset=utf-8",
    ".htm" => "text/html; charset=utf-8",
    ".md" => "text/plain; charset=utf-8",
    ".css" => "text/css; charset=utf-8",
    ".js" => "text/javascript; charset=utf-8",
    ".json" => "application/json; charset=utf-8",
    ".svg" => "image/svg+xml",
    ".png" => "image/png",
    ".jpg" or ".jpeg" => "image/jpeg",
    ".gif" => "image/gif",
    ".webp" => "image/webp",
    ".ico" => "image/x-icon",
    _ => "application/octet-stream"
};
