using EbookManager.Infrastructure.Settings;
using FluentAssertions;

namespace EbookManager.Tests.Settings;

public sealed class SyncfusionLicenseKeyResolverTests
{
    [Fact]
    public void Resolve_prefers_environment_key()
    {
        var key = SyncfusionLicenseKeyResolver.Resolve("from-env", ["from-file"]);

        key.Should().Be("from-env");
    }

    [Fact]
    public void Resolve_uses_first_non_empty_file_key_when_environment_is_empty()
    {
        var key = SyncfusionLicenseKeyResolver.Resolve(null, ["", "  from-file  "]);

        key.Should().Be("from-file");
    }
}
