namespace EbookManager.Application.Metadata;

public static class IsbnValidator
{
    public static bool TryNormalize(string? value, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var characters = value
            .Where(character => character is not '-' and not ' ' && character != '\t')
            .Select(char.ToUpperInvariant)
            .ToArray();
        var candidate = new string(characters);
        if (candidate.Length == 10 && IsValidIsbn10(candidate) ||
            candidate.Length == 13 && IsValidIsbn13(candidate))
        {
            normalized = candidate;
            return true;
        }

        return false;
    }

    private static bool IsValidIsbn10(string value)
    {
        var sum = 0;
        for (var index = 0; index < value.Length; index++)
        {
            var digit = index == 9 && value[index] == 'X'
                ? 10
                : char.IsAsciiDigit(value[index]) ? value[index] - '0' : -1;
            if (digit < 0)
            {
                return false;
            }

            sum += digit * (10 - index);
        }

        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string value)
    {
        if (!value.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        for (var index = 0; index < 12; index++)
        {
            sum += (value[index] - '0') * (index % 2 == 0 ? 1 : 3);
        }

        var checkDigit = (10 - sum % 10) % 10;
        return checkDigit == value[12] - '0';
    }
}
