namespace BunDotNet;

public sealed class BunVersion : IComparable<BunVersion>, IComparable, IEquatable<BunVersion>
{
    public required int Major { get; init; }
    public required int Minor { get; init; }
    public required int Patch { get; init; }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";

    public string ToGitTag() => $"bun-v{Major}.{Minor}.{Patch}";

    public static BunVersion? Parse(string? versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
        {
            return null;
        }

        if (versionString.Equals("latest", StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        // bun-v1.3.6
        if (versionString.StartsWith("bun-", StringComparison.InvariantCultureIgnoreCase))
        {
            versionString = versionString[4..];
        }

        // v1.3.6
        if (versionString.StartsWith("v", StringComparison.InvariantCultureIgnoreCase))
        {
            versionString = versionString[1..];
        }

        var parts = versionString.Split('.');
        if (parts.Length != 3)
        {
            throw new FormatException("Invalid version format. Expected format: Major.Minor.Patch");
        }

        if (
            !int.TryParse(parts[0], out var major)
            || !int.TryParse(parts[1], out var minor)
            || !int.TryParse(parts[2], out var patch)
        )
        {
            throw new FormatException("Invalid version format. Major, Minor, and Patch must be integers.");
        }

        return new BunVersion
        {
            Major = major,
            Minor = minor,
            Patch = patch,
        };
    }

    public int CompareTo(BunVersion? other)
    {
        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (other is null)
        {
            return 1;
        }

        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0)
        {
            return majorComparison;
        }

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0)
        {
            return minorComparison;
        }

        return Patch.CompareTo(other.Patch);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (ReferenceEquals(this, obj))
        {
            return 0;
        }

        return obj is BunVersion other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(BunVersion)}");
    }

    public static bool operator <(BunVersion left, BunVersion right) => left.CompareTo(right) < 0;

    public static bool operator >(BunVersion left, BunVersion right) => left.CompareTo(right) > 0;

    public static bool operator <=(BunVersion left, BunVersion right) => left.CompareTo(right) <= 0;

    public static bool operator >=(BunVersion left, BunVersion right) => left.CompareTo(right) >= 0;

    public static bool operator ==(BunVersion left, BunVersion right) => left.Equals(right);

    public static bool operator !=(BunVersion left, BunVersion right) => !left.Equals(right);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is BunVersion other && Equals(other);
    }

    public bool Equals(BunVersion? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
    }
}
