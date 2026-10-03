using DiaperScout.Application;

namespace DiaperScout.Web.Services;

public sealed record ExplorePosition(double Latitude, double Longitude, double Accuracy)
{
    public bool IsUsable => double.IsFinite(Latitude) && double.IsFinite(Longitude) && double.IsFinite(Accuracy)
        && Latitude is >= -90 and <= 90 && Longitude is >= -180 and <= 180 && Accuracy is >= 0 and <= 5000;
}

public sealed record ExploreDiscovery(PlaceItem Place, PlaceProductObservation Observation, double Miles)
{
    public static IReadOnlyList<ExploreDiscovery> Select(IReadOnlyList<AtlasPlace> places, ExplorePosition position)
    {
        if (!position.IsUsable) return [];
        return places.Where(p => p.Observations.Count > 0)
            .Select(p => new { p, Distance = DistanceMiles(position, p.Place) })
            .Where(p => p.Distance <= 25)
            .SelectMany(p => p.p.Observations.Select(o => new ExploreDiscovery(p.p.Place, o, p.Distance)))
            .Where(d => d.Observation.ObservedAtUtc <= DateTimeOffset.UtcNow)
            .OrderByDescending(d => d.Observation.ObservedAtUtc).ThenBy(d => d.Miles).ThenBy(d => d.Observation.ObservationId)
            .Take(3).ToArray();
    }
    private static double DistanceMiles(ExplorePosition origin, PlaceItem destination)
    {
        const double radians = Math.PI / 180;
        var lat = ((double)destination.Latitude - origin.Latitude) * radians;
        var lng = ((double)destination.Longitude - origin.Longitude) * radians;
        var a = Math.Pow(Math.Sin(lat / 2), 2) + Math.Cos(origin.Latitude * radians) * Math.Cos((double)destination.Latitude * radians) * Math.Pow(Math.Sin(lng / 2), 2);
        return 3958.7613 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }
}
