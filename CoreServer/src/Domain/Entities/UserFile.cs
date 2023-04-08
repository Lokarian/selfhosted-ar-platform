namespace CoreServer.Domain.Entities;

public class UserFile : BaseEntity
{
    public string InStorageFileName { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string MimeType { get; set; } = null!;
}