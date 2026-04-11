using System.Text;
using System.Text.Json;

namespace Evolver.Frontend.Services;

/// <summary>从 JWT payload 解析当前用户 Id（NameIdentifier / sub / nameid）。</summary>
public static class JwtUserIdReader
{
    public static long? TryGetUserId(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return null;

        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return null;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (p.Value.ValueKind != JsonValueKind.String && p.Value.ValueKind != JsonValueKind.Number)
                    continue;

                var s = p.Value.ValueKind == JsonValueKind.Number
                    ? p.Value.GetInt64().ToString()
                    : p.Value.GetString();

                if (!long.TryParse(s, out var id))
                    continue;

                if (p.Name.Equals("sub", StringComparison.OrdinalIgnoreCase)
                    || p.Name.Equals("nameid", StringComparison.OrdinalIgnoreCase)
                    || p.Name.EndsWith("/nameidentifier", StringComparison.OrdinalIgnoreCase))
                    return id;
            }
        }
        catch
        {
            // ignore malformed token
        }

        return null;
    }
}
