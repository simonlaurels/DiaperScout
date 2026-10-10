using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.Extensions.Configuration;

namespace DiaperScout.Infrastructure;

// Only sourced place attributes enter this snapshot. No Explorer/device coordinates or account data.
public sealed record ProviderPlaceSnapshot(string Provider, string ExternalId, string Datasource,
    string SourceIdentity, string Licence, string Attribution, string LicenceUrl, string Name,
    string AddressLine1, string Locality, string Postcode, string CountryCode,
    decimal Latitude, decimal Longitude, PlaceCategory? Category);

public sealed class GeoapifyPlaces(HttpClient client, IConfiguration configuration)
{
    private static readonly string[] Categories = ["commercial.chemist", "healthcare.pharmacy", "commercial.health_and_beauty.medical_supply", "commercial.supermarket", "commercial"];
    private string Key => configuration["Geoapify:ApiKey"] is { Length: > 0 } key ? key : throw Invalid("Nearby shops are temporarily unavailable. Your scan is safe; try again later.");
    private byte[] SigningKey => SHA256.HashData(Encoding.UTF8.GetBytes("DiaperScout.PlaceSelection.v1|" + Key));
    public async Task<IReadOnlyList<NearbyPlace>> NearbyAsync(NearbyPlaceRequest request, CancellationToken ct)
    {
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180 || request.RadiusMetres is not (1000 or 3000))
            throw Invalid("Choose a valid nearby search position and radius.");
        var key = Key;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(12));
        try
        {
            var responses = await Task.WhenAll(Categories.Select(async category =>
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.geoapify.com/v2/places") {
                    Content = JsonContent.Create(new { categories = new[] {category},
                        filter = new {type = "circle", lon = request.Longitude, lat = request.Latitude, radius = request.RadiusMetres},
                        bias = new {type = "proximity", lon = request.Longitude, lat = request.Latitude}, limit = 20 })
                };
                message.Headers.Add("x-api-key", key);
                using var response = await client.SendAsync(message, timeout.Token);
                response.EnsureSuccessStatusCode();
                using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token), cancellationToken: timeout.Token);
                if (!document.RootElement.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array) throw new JsonException();
                return features.EnumerateArray().Select(Map).Where(p => p is not null).Cast<ProviderPlaceSnapshot>().ToArray();
            }));
            return responses.SelectMany(p => p).GroupBy(p => p.SourceIdentity, StringComparer.Ordinal)
                .Select(g => g.OrderByDescending(p => p.AddressLine1.Length + p.Postcode.Length + p.Locality.Length).ThenBy(p => p.ExternalId, StringComparer.Ordinal).First())
                .Select(p => new NearbyPlace(new PlaceItem(SelectionId(p), p.Name, p.AddressLine1, p.Locality, p.Postcode,
                    p.CountryCode, p.Latitude, p.Longitude, p.Category, Sign(p), p.Attribution), Distance(request, p)))
                .Where(p => p.DistanceMetres <= request.RadiusMetres).OrderBy(p => p.DistanceMetres).ThenBy(p => p.Place.Name).Take(30).ToArray();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        { throw Invalid("Nearby shops are temporarily unavailable. Your scan is safe; try again later."); }
    }
    public static ProviderPlaceSnapshot? Map(JsonElement feature)
    {
        try
        {
            var p = feature.GetProperty("properties"); var source = p.GetProperty("datasource"); var raw = source.GetProperty("raw");
            if (Text(source, "sourcename") != "openstreetmap" || Text(source, "license") != "Open Database License") return null;
            var name = Text(p, "name"); var id = Text(p, "place_id"); var country = Text(p, "country_code").ToUpperInvariant();
            var sourceId = Text(raw, "osm_type") + ":" + Text(raw, "osm_id");
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200 || string.IsNullOrWhiteSpace(id) || id.Length > 2048 || country.Length != 2 || sourceId.StartsWith(':') || sourceId.EndsWith(':')) return null;
            var lat = decimal.Round(p.GetProperty("lat").GetDecimal(), 6); var lon = decimal.Round(p.GetProperty("lon").GetDecimal(), 6);
            if (lat is < -90 or > 90 || lon is < -180 or > 180) return null;
            var address = string.Join(" ", new[] { Text(p, "housenumber"), Text(p, "street") }.Where(s => s.Length > 0));
            if (string.IsNullOrWhiteSpace(address) || address.Length > 250) return null;
            var city = new[] { "city", "town", "village", "municipality" }.Select(k => Text(p, k)).FirstOrDefault(v => v.Length > 0) ?? "";
            var postcode = Text(p, "postcode"); if (city.Length > 150 || postcode.Length > 32) return null;
            var categories = p.GetProperty("categories").EnumerateArray().Select(v => v.GetString() ?? "").ToArray();
            PlaceCategory category = categories.Any(v => v.Contains("pharmacy") || v == "commercial.chemist") ? PlaceCategory.Pharmacy
                : categories.Contains("commercial.supermarket") ? PlaceCategory.Supermarket
                : categories.Any(v => v.Contains("medical_supply")) ? PlaceCategory.SpecialistRetailer : PlaceCategory.GeneralRetailer;
            return new("Geoapify", id, "openstreetmap", sourceId, "ODbL-1.0", "© OpenStreetMap contributors · Powered by Geoapify",
                "https://www.openstreetmap.org/copyright", name, address, city, postcode, country, lat, lon, category);
        }
        catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException or FormatException or OverflowException) { return null; }
    }
    private static string Text(JsonElement element, string name) => element.TryGetProperty(name, out var value) ? value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.ValueKind == JsonValueKind.Number ? value.ToString() : "" : "";
    private sealed record Selection(ProviderPlaceSnapshot Place, DateTimeOffset Expires);
    private string Sign(ProviderPlaceSnapshot place)
    {
        var payload = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new Selection(place, DateTimeOffset.UtcNow.AddMinutes(30))));
        return payload + "." + Convert.ToBase64String(HMACSHA256.HashData(SigningKey, Encoding.UTF8.GetBytes(payload)));
    }
    public ProviderPlaceSnapshot Verify(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 12000) throw new FormatException();
            var parts = token.Split('.'); if (parts.Length != 2) throw new FormatException();
            var expected = HMACSHA256.HashData(SigningKey, Encoding.UTF8.GetBytes(parts[0]));
            if (!CryptographicOperations.FixedTimeEquals(expected, Convert.FromBase64String(parts[1]))) throw new FormatException();
            var selection = JsonSerializer.Deserialize<Selection>(Convert.FromBase64String(parts[0]));
            if (selection is null || selection.Expires < DateTimeOffset.UtcNow) throw new FormatException();
            return selection.Place;
        }
        catch (Exception e) when (e is FormatException or JsonException) { throw Invalid("Choose a nearby shop again; this selection has expired or could not be verified."); }
    }
    private static Guid SelectionId(ProviderPlaceSnapshot p) => new(SHA256.HashData(Encoding.UTF8.GetBytes(p.SourceIdentity)).AsSpan(0,16));
    private static double Distance(NearbyPlaceRequest r, ProviderPlaceSnapshot p)
    {
        var lat = (double)r.Latitude * Math.PI / 180; var lat2 = (double)p.Latitude * Math.PI / 180;
        var d = Math.Pow(Math.Sin((lat2-lat)/2),2) + Math.Cos(lat)*Math.Cos(lat2)*Math.Pow(Math.Sin((double)(p.Longitude-r.Longitude)*Math.PI/360),2);
        return 6371000*2*Math.Asin(Math.Sqrt(Math.Clamp(d,0,1)));
    }
    private static CatalogueValidationException Invalid(string message) => new("place", message);
}
