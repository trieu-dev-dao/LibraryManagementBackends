using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LibraryManagement.DTOs;

public class BorrowRecordUpdateDto
{
    [Range(1, int.MaxValue)]
    public int BookId { get; set; }

    [Range(1, int.MaxValue)]
    public int MemberId { get; set; }

    [JsonConverter(typeof(DueDateJsonConverter))]
    public DateTime ExpiredDate { get; set; }

    public string Status { get; set; } = string.Empty;
}
