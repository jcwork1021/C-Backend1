namespace minimalAPILesson1.DTO
{
    public class DTOs
    {
    }

    // A uniform wrapper for all your API responses
    public class ApiResponse<T>
    {
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public T? Data { get; set; }
    }

    public record ResponseDTO<T>(string Message, int StatusCode, T? Data);

    public record PlayerDTO(int ID, string Name, string Email);

    //Note: the "?" means the property is nullable, which means it can be null. This is useful for update operations where you might not want to update every field.
    public record UpdatePlayerDto(
    string? Name,
    string? Email,
    string? Password,
    int? Age,
    int? Year,
    string? Section,
    int? Level
);

    public record LoginDTO(string Email, string Password);

    public record LoginResponseDTO(int ID, string ShortToken, string LongToken, string Name, int Age, int Year, string Section, int Level);

    public class PlayerDTOs
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public PlayerDTOs(string name, string email)
        {
            Name = name;
            Email = email;
        }
    }
}
