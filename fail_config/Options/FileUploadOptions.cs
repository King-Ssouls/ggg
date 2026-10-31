namespace FailConfig.Options;

// Хранит лимит файла и разрешённые расширения из настроек
public sealed class FileUploadOptions
{
    public long MaxSizeBytes { get; set; }
    public string[] AllowedExtensions { get; set; } = [];
}
