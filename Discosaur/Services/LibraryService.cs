using System.IO;
using System.Linq;
using Discosaur.Models;

namespace Discosaur.Services;

public class LibraryService
{
    private static readonly string[] SupportedExtensions = [".mp3", ".flac", ".wav", ".m4a", ".ogg", ".wma", ".aac"];

    public Album ScanFolder(string folderPath)
    {
        var folderName = Path.GetFileName(folderPath) ?? folderPath;

        var album = new Album { Name = folderName };

        var files = Directory.GetFiles(folderPath)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f);

        foreach (var file in files)
        {
            album.Tracks.Add(new Track
            {
                FilePath = file,
                FileName = Path.GetFileName(file)
            });
        }

        return album;
    }
}
