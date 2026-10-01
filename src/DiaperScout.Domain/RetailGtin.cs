namespace DiaperScout.Domain;

public static class RetailGtin
{
    public static string? Normalise(string? value)
    {
        var digits = value?.Replace(" ", "").Replace("-", "");
        if (digits is null || digits.Length is not (8 or 12 or 13 or 14) || digits.Any(c => c is < '0' or > '9') || digits.All(c => c == '0')) return null;
        var sum = 0;
        for (var i = digits.Length - 2; i >= 0; i--) sum += (digits[i] - '0') * ((digits.Length - i) % 2 == 0 ? 3 : 1);
        return (10 - sum % 10) % 10 == digits[^1] - '0' ? digits : null;
    }
}
