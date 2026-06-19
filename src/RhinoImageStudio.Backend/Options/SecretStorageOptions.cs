namespace RhinoImageStudio.Backend.Options;

public class SecretStorageOptions
{
    public const string SectionName = "SecretStorage";

    public string StoragePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RhinoImageStudio",
        "secrets"
    );
}
