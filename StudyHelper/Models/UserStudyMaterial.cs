namespace StudyHelper.Models;

/// <summary>
/// Metadata about a user's uploaded study material file.
/// </summary>
public class UserStudyMaterial
{
    public string Username { get; set; } = string.Empty;
    public StudyMaterialType MaterialType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedDate { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty; // SHA256 for integrity
}
