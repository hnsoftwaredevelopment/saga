namespace EbookManager.Presentation.ViewModels;

public sealed record LibraryColumnKey(string Value)
{
    public const string CustomPrefix = "custom:";

    public bool IsCustom => Value.StartsWith(CustomPrefix, StringComparison.OrdinalIgnoreCase);

    public Guid? CustomFieldId =>
        IsCustom && Guid.TryParse(Value[CustomPrefix.Length..], out var fieldId)
            ? fieldId
            : null;

    public LibraryColumnOption? StandardOption =>
        !IsCustom &&
        Enum.TryParse<LibraryColumnOption>(Value, ignoreCase: true, out var option) &&
        Enum.IsDefined(option)
            ? option
            : null;

    public static LibraryColumnKey FromStandard(LibraryColumnOption option) => new(option.ToString());

    public static LibraryColumnKey FromCustom(Guid fieldId) => new($"{CustomPrefix}{fieldId:D}");

    public static implicit operator LibraryColumnKey(LibraryColumnOption option) => FromStandard(option);

    public bool Equals(LibraryColumnKey? other) =>
        other is not null &&
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public override string ToString() => Value;
}
