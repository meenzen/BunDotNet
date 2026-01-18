using System.Text.Json.Serialization;

namespace BunDotNet;

[JsonSerializable(typeof(BunInstaller.InstallMetadata))]
[JsonSerializable(typeof(BunInstaller.VersionMetadata))]
internal partial class BunJsonSerializerContext : JsonSerializerContext;
