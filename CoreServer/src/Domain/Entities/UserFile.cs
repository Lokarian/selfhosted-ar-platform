namespace CoreServer.Domain.Entities;

public class UserFile : BaseEntity
{

    public string FileName { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public FileType FileType { get; set; }
}

public enum FileType
{
    UserImage
}