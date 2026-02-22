using System.Text.Json.Serialization;
using PopfileNet.Common;

namespace PopfileNet.Backend.Models;

[JsonSerializable(typeof(Email))]
[JsonSerializable(typeof(MailFolder))]
[JsonSerializable(typeof(IEnumerable<Email>))]
[JsonSerializable(typeof(IEnumerable<MailFolder>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}