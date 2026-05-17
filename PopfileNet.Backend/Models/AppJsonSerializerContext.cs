using System.Text.Json.Serialization;
using PopfileNet.Common;

namespace PopfileNet.Backend.Models;

[JsonSerializable(typeof(Email))]
[JsonSerializable(typeof(MailFolder))]
[JsonSerializable(typeof(IEnumerable<Email>))]
[JsonSerializable(typeof(IEnumerable<MailFolder>))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(IEnumerable<UserDto>))]
[JsonSerializable(typeof(CreateUserRequest))]
[JsonSerializable(typeof(UpdateUserRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}