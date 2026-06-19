namespace RhinoImageStudio.Backend.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string BasePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RhinoImageStudio",
        "data"
    );
}
