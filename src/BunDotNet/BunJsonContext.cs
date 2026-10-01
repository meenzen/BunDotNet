using System.Text.Json.Serialization;

namespace BunDotNet;

/// <summary>
/// Source generated serializers, reflection based serialization is not supported with Native AOT.
/// </summary>
[JsonSerializable(typeof(BunInstaller.InstallMetadata))]
[JsonSerializable(typeof(BunVersion))]
internal sealed partial class BunJsonContext : JsonSerializerContext;
