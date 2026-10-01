namespace BunDotNet.Tests;

public class BunVersionTests
{
    private static BunVersion Version(int major, int minor, int patch) =>
        new()
        {
            Major = major,
            Minor = minor,
            Patch = patch,
        };

    [Test]
    [Arguments("1.3.6")]
    [Arguments("v1.3.6")]
    [Arguments("V1.3.6")]
    [Arguments("bun-v1.3.6")]
    [Arguments("BUN-V1.3.6")]
    [Arguments("bun-1.3.6")]
    public async Task Parse_ValidVersion_ReturnsVersion(string input)
    {
        var version = BunVersion.Parse(input);

        await Assert.That(version).IsNotNull();
        await Assert.That(version!.Major).IsEqualTo(1);
        await Assert.That(version.Minor).IsEqualTo(3);
        await Assert.That(version.Patch).IsEqualTo(6);
    }

    [Test]
    public async Task Parse_MultiDigitParts_ReturnsVersion()
    {
        var version = BunVersion.Parse("10.200.3000");

        await Assert.That(version).IsEqualTo(Version(10, 200, 3000));
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("latest")]
    [Arguments("LATEST")]
    [Arguments("Latest")]
    public async Task Parse_EmptyOrLatest_ReturnsNull(string? input)
    {
        var version = BunVersion.Parse(input);

        await Assert.That(version).IsNull();
    }

    [Test]
    [Arguments("1")]
    [Arguments("1.3")]
    [Arguments("1.3.6.1")]
    [Arguments("bun-v")]
    [Arguments("v")]
    [Arguments("1.3.")]
    [Arguments("..")]
    public async Task Parse_WrongNumberOfParts_ThrowsFormatException(string input)
    {
        await Assert.That(() => BunVersion.Parse(input)).Throws<FormatException>();
    }

    [Test]
    [Arguments("a.b.c")]
    [Arguments("1.3.x")]
    [Arguments("1.3.6-beta")]
    [Arguments("1..6.")]
    [Arguments("99999999999.0.0")]
    [Arguments("1.-3.6")]
    [Arguments("-1.3.6")]
    [Arguments("+1.3.6")]
    [Arguments("1. 3.6")]
    [Arguments("1.3.6 ")]
    [Arguments(" 1.3.6")]
    [Arguments("1,000.3.6")]
    public async Task Parse_NonIntegerParts_ThrowsFormatException(string input)
    {
        await Assert.That(() => BunVersion.Parse(input)).Throws<FormatException>();
    }

    [Test]
    public async Task ToString_ReturnsDottedVersion()
    {
        await Assert.That(Version(1, 3, 6).ToString()).IsEqualTo("1.3.6");
    }

    [Test]
    public async Task ToGitTag_ReturnsBunTag()
    {
        await Assert.That(Version(1, 3, 6).ToGitTag()).IsEqualTo("bun-v1.3.6");
    }

    [Test]
    [Arguments("1.3.6")]
    [Arguments("bun-v1.3.6")]
    public async Task ToGitTag_RoundTripsThroughParse(string input)
    {
        var version = BunVersion.Parse(input)!;

        await Assert.That(BunVersion.Parse(version.ToGitTag())).IsEqualTo(version);
        await Assert.That(BunVersion.Parse(version.ToString())).IsEqualTo(version);
    }

    [Test]
    [Arguments(1, 3, 6, 1, 3, 6, 0)]
    [Arguments(1, 3, 6, 1, 3, 7, -1)]
    [Arguments(1, 3, 7, 1, 3, 6, 1)]
    [Arguments(1, 3, 6, 1, 4, 0, -1)]
    [Arguments(1, 4, 0, 1, 3, 6, 1)]
    [Arguments(1, 9, 9, 2, 0, 0, -1)]
    [Arguments(2, 0, 0, 1, 9, 9, 1)]
    [Arguments(1, 10, 0, 1, 9, 0, 1)]
    public async Task CompareTo_ComparesMajorMinorPatch(
        int major1,
        int minor1,
        int patch1,
        int major2,
        int minor2,
        int patch2,
        int expectedSign
    )
    {
        var left = Version(major1, minor1, patch1);
        var right = Version(major2, minor2, patch2);

        await Assert.That(Math.Sign(left.CompareTo(right))).IsEqualTo(expectedSign);
        await Assert.That(Math.Sign(left.CompareTo((object)right))).IsEqualTo(expectedSign);
    }

    [Test]
    public async Task CompareTo_SameInstance_ReturnsZero()
    {
        var version = Version(1, 3, 6);

        await Assert.That(version.CompareTo(version)).IsEqualTo(0);
        await Assert.That(version.CompareTo((object)version)).IsEqualTo(0);
    }

    [Test]
    public async Task CompareTo_Null_ReturnsPositive()
    {
        var version = Version(1, 3, 6);

        await Assert.That(version.CompareTo((BunVersion?)null)).IsGreaterThan(0);
        await Assert.That(version.CompareTo((object?)null)).IsGreaterThan(0);
    }

    [Test]
    public async Task CompareTo_OtherType_ThrowsArgumentException()
    {
        var version = Version(1, 3, 6);

        await Assert.That(() => version.CompareTo("1.3.6")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Sort_OrdersVersionsAscending()
    {
        List<BunVersion> versions = [Version(1, 10, 0), Version(0, 9, 9), Version(1, 2, 3), Version(1, 2, 0)];

        versions.Sort();

        await Assert
            .That(versions)
            .IsEquivalentTo(
                [Version(0, 9, 9), Version(1, 2, 0), Version(1, 2, 3), Version(1, 10, 0)],
                TUnit.Assertions.Enums.CollectionOrdering.Matching
            );
    }

    [Test]
    public async Task ComparisonOperators_SmallerAndLargerVersion()
    {
        var smaller = Version(1, 3, 6);
        var larger = Version(1, 3, 10);

        await Assert.That(smaller < larger).IsTrue();
        await Assert.That(smaller <= larger).IsTrue();
        await Assert.That(smaller > larger).IsFalse();
        await Assert.That(smaller >= larger).IsFalse();
        await Assert.That(larger > smaller).IsTrue();
        await Assert.That(larger >= smaller).IsTrue();
        await Assert.That(larger < smaller).IsFalse();
        await Assert.That(larger <= smaller).IsFalse();
    }

    [Test]
    public async Task ComparisonOperators_EqualVersions()
    {
        var left = Version(1, 3, 6);
        var right = Version(1, 3, 6);

        await Assert.That(left <= right).IsTrue();
        await Assert.That(left >= right).IsTrue();
        await Assert.That(left < right).IsFalse();
        await Assert.That(left > right).IsFalse();
    }

    [Test]
    public async Task Equality_SameValues_AreEqual()
    {
        var left = Version(1, 3, 6);
        var right = Version(1, 3, 6);

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.Equals((object)right)).IsTrue();
        await Assert.That(left == right).IsTrue();
        await Assert.That(left != right).IsFalse();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
    }

    [Test]
    [Arguments(2, 3, 6)]
    [Arguments(1, 4, 6)]
    [Arguments(1, 3, 7)]
    public async Task Equality_DifferentValues_AreNotEqual(int major, int minor, int patch)
    {
        var left = Version(1, 3, 6);
        var right = Version(major, minor, patch);

        await Assert.That(left.Equals(right)).IsFalse();
        await Assert.That(left.Equals((object)right)).IsFalse();
        await Assert.That(left == right).IsFalse();
        await Assert.That(left != right).IsTrue();
    }

    [Test]
    public async Task Equals_NullOrOtherType_ReturnsFalse()
    {
        var version = Version(1, 3, 6);
        BunVersion? nullVersion = null;
        object? nullObject = null;

        await Assert.That(version.Equals(nullVersion)).IsFalse();
        await Assert.That(version.Equals(nullObject)).IsFalse();
        await Assert.That(version.Equals("1.3.6")).IsFalse();
    }

    [Test]
    public async Task Equals_SameInstance_ReturnsTrue()
    {
        var version = Version(1, 3, 6);

        await Assert.That(version.Equals(version)).IsTrue();
        await Assert.That(version.Equals((object)version)).IsTrue();
    }

    [Test]
    public async Task HashSet_DeduplicatesEqualVersions()
    {
        var set = new HashSet<BunVersion> { Version(1, 3, 6), Version(1, 3, 6), Version(1, 3, 7) };

        await Assert.That(set.Count).IsEqualTo(2);
    }

    [Test]
    public async Task EqualityOperators_WithNull_DoNotThrow()
    {
        var version = Version(1, 3, 6);
        BunVersion? nullVersion = null;

        await Assert.That(nullVersion == version).IsFalse();
        await Assert.That(version == nullVersion).IsFalse();
        await Assert.That(nullVersion == null).IsTrue();
        await Assert.That(nullVersion != version).IsTrue();
        await Assert.That(version != nullVersion).IsTrue();
        await Assert.That(nullVersion != null).IsFalse();
    }

    [Test]
    public async Task ComparisonOperators_WithNull_TreatNullAsSmallest()
    {
        var version = Version(1, 3, 6);
        BunVersion? nullVersion = null;

        await Assert.That(nullVersion < version).IsTrue();
        await Assert.That(nullVersion <= version).IsTrue();
        await Assert.That(nullVersion > version).IsFalse();
        await Assert.That(nullVersion >= version).IsFalse();
        await Assert.That(version > nullVersion).IsTrue();
        await Assert.That(version < nullVersion).IsFalse();
        await Assert.That(nullVersion <= null).IsTrue();
        await Assert.That(nullVersion < null).IsFalse();
    }

    [Test]
    [Arguments("canary")]
    [Arguments("CANARY")]
    [Arguments("Canary")]
    public async Task Parse_Canary_ReturnsCanary(string input)
    {
        var version = BunVersion.Parse(input);

        await Assert.That(version).IsEqualTo(BunVersion.Canary);
        await Assert.That(version!.IsCanary).IsTrue();
    }

    [Test]
    [Arguments("bun-canary")]
    [Arguments("vcanary")]
    [Arguments("canary-1.3.6")]
    public async Task Parse_InvalidCanary_ThrowsFormatException(string input)
    {
        await Assert.That(() => BunVersion.Parse(input)).Throws<FormatException>();
    }

    [Test]
    public async Task Canary_ToStringAndGitTag()
    {
        await Assert.That(BunVersion.Canary.ToString()).IsEqualTo("canary");
        await Assert.That(BunVersion.Canary.ToGitTag()).IsEqualTo("canary");
        await Assert.That(BunVersion.Parse(BunVersion.Canary.ToString())).IsEqualTo(BunVersion.Canary);
    }

    [Test]
    public async Task Canary_IsNotEqualToStableVersion()
    {
        var stable = Version(0, 0, 0);

        await Assert.That(BunVersion.Canary == stable).IsFalse();
        await Assert.That(stable.Equals(BunVersion.Canary)).IsFalse();
        await Assert.That(stable.IsCanary).IsFalse();
    }

    [Test]
    public async Task Canary_EqualsOtherCanaryInstance()
    {
        var other = new BunVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            IsCanary = true,
        };

        await Assert.That(other == BunVersion.Canary).IsTrue();
        await Assert.That(other.GetHashCode()).IsEqualTo(BunVersion.Canary.GetHashCode());
        await Assert.That(other.CompareTo(BunVersion.Canary)).IsEqualTo(0);
    }

    [Test]
    public async Task Canary_SortsAfterStableVersions()
    {
        List<BunVersion> versions = [BunVersion.Canary, Version(99, 0, 0), Version(1, 3, 6)];

        versions.Sort();

        await Assert
            .That(versions)
            .IsEquivalentTo(
                [Version(1, 3, 6), Version(99, 0, 0), BunVersion.Canary],
                TUnit.Assertions.Enums.CollectionOrdering.Matching
            );
        await Assert.That(BunVersion.Canary > Version(99, 0, 0)).IsTrue();
        await Assert.That(BunVersion.Canary > null).IsTrue();
    }

    [Test]
    public async Task Json_RoundTripsCanaryAndStable()
    {
        var canary = System.Text.Json.JsonSerializer.Deserialize<BunVersion>(
            System.Text.Json.JsonSerializer.Serialize(BunVersion.Canary)
        );
        var stable = System.Text.Json.JsonSerializer.Deserialize<BunVersion>(
            System.Text.Json.JsonSerializer.Serialize(Version(1, 3, 6))
        );

        await Assert.That(canary).IsEqualTo(BunVersion.Canary);
        await Assert.That(stable).IsEqualTo(Version(1, 3, 6));
    }

    [Test]
    public async Task Json_MetadataWithoutCanaryField_IsStable()
    {
        // metadata written by older versions of BunDotNet has no IsCanary property
        var version = System.Text.Json.JsonSerializer.Deserialize<BunVersion>("""{"Major":1,"Minor":3,"Patch":6}""");

        await Assert.That(version).IsEqualTo(Version(1, 3, 6));
        await Assert.That(version!.IsCanary).IsFalse();
    }
}
