namespace LibraryManagement.DTOs;

public class AuthorResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
}