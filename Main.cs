using MelonLoader;
using MelonLoader.Utils;
using System.Reflection;

namespace EmbeddedChart;

public class Main : MelonMod
{
    private const string AlbumsFolderName = "EmbeddedAlbums";
    private const int FileBufferSize = 81920;

    internal const string MelonName = "EmbeddedChart";
    internal const string MelonVersion = "1.0.0";
    internal const string MelonAuthor = "AshtonMemer";

    private static string EmbeddedAlbumsPath => Path.Combine(MelonEnvironment.UserDataDirectory, AlbumsFolderName);

    public override void OnInitializeMelon()
    {
        base.OnInitializeMelon();
        Initialize();
    }

    public override void OnLateInitializeMelon()
    {
        base.OnLateInitializeMelon();
        LoadAlbumsFromDirectory();
    }

    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        CleanupDirectory();
    }

    private static async void Initialize()
    {
        try
        {
            PrepareDirectory();
            await ExtractEmbeddedResourcesAsync();
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Failed to initialize EmbeddedChart: {ex.Message}");
        }
    }

    private static void PrepareDirectory()
    {
        try
        {
            if (!Directory.Exists(EmbeddedAlbumsPath))
            {
                Directory.CreateDirectory(EmbeddedAlbumsPath);
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Failed to prepare directory: {ex.Message}");
            throw;
        }
    }

    private static async Task ExtractEmbeddedResourcesAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        if (resourceNames.Length == 0) return;

        var extractionTasks = resourceNames.Select(resourceName =>
            ExtractResourceAsync(assembly, resourceName));

        await Task.WhenAll(extractionTasks);
    }

    private static async Task ExtractResourceAsync(Assembly assembly, string resourceName)
    {
        try
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return;

            var outputPath = Path.Combine(EmbeddedAlbumsPath, resourceName);
            await WriteStreamToFileAsync(stream, outputPath);
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Failed to extract resource '{resourceName}': {ex.Message}");
        }
    }

    private static async Task WriteStreamToFileAsync(Stream inputStream, string filePath)
    {
        try
        {
            using var outputFileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: FileBufferSize,
                useAsync: true);

            await inputStream.CopyToAsync(outputFileStream);
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Failed to write stream to file '{Path.GetFileName(filePath)}': {ex.Message}");
            throw;
        }
    }

    private static void LoadAlbumsFromDirectory()
    {
        MelonLogger.Msg("Loading albums from directory...");
        try
        {
            if (!Directory.Exists(EmbeddedAlbumsPath))
            {
                MelonLogger.Error($"Albums directory does not exist at: {EmbeddedAlbumsPath}");
                return;
            }

            var albumFiles = Directory.GetFiles(EmbeddedAlbumsPath);
            if (albumFiles.Length == 0)
            {
                MelonLogger.Msg("No album files found in the directory.");
                return;
            }

            MelonLogger.Msg($"Loading {albumFiles.Length} album files...");

            foreach (var file in albumFiles)
            {
                try
                {
                    if (file.EndsWith(".mdm"))
                    {
                        CustomAlbums.Managers.AlbumManager.LoadOne(file);
                    }
                    else if (file.EndsWith(".mdp"))
                    {
                        CustomAlbums.Managers.AlbumManager.LoadPack(file);
                    }
                    else
                    {
                        MelonLogger.Error($"Unsupported file type: {Path.GetFileName(file)}");
                        continue;
                    }

                    MelonLogger.Msg($"Loaded album: {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Failed to load album '{Path.GetFileName(file)}': {ex.Message}");
                }
            }

            MelonLogger.Msg("Album loading completed.");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Failed to load albums from directory: {ex.Message}");
        }
    }

    private static void CleanupDirectory()
    {
        try
        {
            if (!Directory.Exists(EmbeddedAlbumsPath))
            {
                MelonLogger.Msg("Temporary albums directory does not exist - no cleanup needed.");
                return;
            }

            Directory.Delete(EmbeddedAlbumsPath, true);
            MelonLogger.Msg("Temporary albums directory cleaned up successfully.");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Failed to cleanup temporary directory: {ex.Message}");
        }
    }
}