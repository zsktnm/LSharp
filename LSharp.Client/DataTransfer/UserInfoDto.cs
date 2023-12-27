using System.Text.Json.Serialization;

namespace LSharp.Client.DataTransfer
{
    public class UserInfoDto
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public int Exp { get; set; }
        public int Next { get; set; }
        public int Level { get; set; }
    }

}
