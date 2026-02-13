using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Discosaur.Models;

namespace Discosaur.Services;

public class LibraryService
{
    private static readonly string[] SupportedExtensions = [".mp3", ".flac", ".wav", ".m4a", ".ogg", ".wma", ".aac"];

    private static readonly string[] CoverArtFileNames =
        ["cover", "artwork", "folder", "front"];

    private static readonly string[] CoverArtExtensions = [".jpg", ".jpeg", ".png"];

    public List<Album> ScanFolder(string folderPath)
    {
        var files = Directory.GetFiles(folderPath)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

        var tracks = new List<Track>();

        foreach (var file in files)
        {
            var track = ParseTrack(file);
            tracks.Add(track);
        }

        var albums = GroupIntoAlbums(tracks);

        var coverArtPath = FindCoverArt(folderPath);
        if (coverArtPath != null)
        {
            foreach (var album in albums)
            {
                album.CoverArtPath ??= coverArtPath;
            }
        }

        return albums;
    }

    private static Track ParseTrack(string filePath)
    {
        var track = new Track
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            Title = Path.GetFileNameWithoutExtension(filePath)
        };

        try
        {
            using var tagFile = TagLib.File.Create(filePath);
            var tag = tagFile.Tag;

            if (!string.IsNullOrWhiteSpace(tag.Title))
                track.Title = tag.Title;

            if (!string.IsNullOrWhiteSpace(tag.FirstPerformer))
                track.Artist = tag.FirstPerformer;

            if (!string.IsNullOrWhiteSpace(tag.Album))
                track.AlbumTitle = tag.Album;

            if (tag.Track > 0)
                track.TrackNumber = tag.Track;

            if (tag.Year > 0)
                track.Year = tag.Year;

            if (!string.IsNullOrWhiteSpace(tag.FirstGenre))
                track.Genre = tag.FirstGenre;

            if (tagFile.Properties?.Duration > TimeSpan.Zero)
                track.Duration = tagFile.Properties.Duration;
        }
        catch
        {
            // Metadata parsing failed — keep defaults (Title = filename, no album)
        }

        return track;
    }

    private static List<Album> GroupIntoAlbums(List<Track> tracks)
    {
        var albums = new List<Album>();

        var grouped = tracks.GroupBy(t => t.AlbumTitle ?? string.Empty);

        foreach (var group in grouped)
        {
            var withNumber = group
                .Where(t => t.TrackNumber.HasValue)
                .OrderBy(t => t.TrackNumber!.Value)
                .ThenBy(t => t.FileName);
            var withoutNumber = group.Where(t => !t.TrackNumber.HasValue);
            var sortedTracks = withNumber.Concat(withoutNumber).ToList();

            if (string.IsNullOrEmpty(group.Key))
            {
                var uncategorized = new Album { Name = Album.UncategorizedName };
                foreach (var track in sortedTracks)
                    uncategorized.Tracks.Add(track);
                albums.Add(uncategorized);
            }
            else
            {
                var firstTrack = sortedTracks.First();
                var album = new Album
                {
                    Name = group.Key,
                    Artist = firstTrack.Artist,
                    Year = firstTrack.Year,
                };

                foreach (var track in sortedTracks)
                    album.Tracks.Add(track);

                albums.Add(album);
            }
        }

        return albums;
    }

    private static string? FindCoverArt(string folderPath)
    {
        var filesInFolder = Directory.GetFiles(folderPath);

        foreach (var file in filesInFolder)
        {
            var name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            var ext = Path.GetExtension(file).ToLowerInvariant();

            if (CoverArtFileNames.Contains(name) && CoverArtExtensions.Contains(ext))
                return file;
        }

        return null;
    }

    public static Track? FindNextTrack(Track? current, IReadOnlyList<Album> library)
    {
        if (current == null || library.Count == 0) return null;

        for (int albumIdx = 0; albumIdx < library.Count; albumIdx++)
        {
            var album = library[albumIdx];
            var trackIdx = album.Tracks.IndexOf(current);
            if (trackIdx < 0) continue;

            if (trackIdx + 1 < album.Tracks.Count)
                return album.Tracks[trackIdx + 1];

            if (albumIdx + 1 < library.Count && library[albumIdx + 1].Tracks.Count > 0)
                return library[albumIdx + 1].Tracks[0];

            return null;
        }
        return null;
    }

    public static Track? FindPreviousTrack(Track? current, IReadOnlyList<Album> library)
    {
        if (current == null || library.Count == 0) return null;

        for (int albumIdx = 0; albumIdx < library.Count; albumIdx++)
        {
            var album = library[albumIdx];
            var trackIdx = album.Tracks.IndexOf(current);
            if (trackIdx < 0) continue;

            if (trackIdx - 1 >= 0)
                return album.Tracks[trackIdx - 1];

            if (albumIdx - 1 >= 0)
            {
                var prevAlbum = library[albumIdx - 1];
                if (prevAlbum.Tracks.Count > 0)
                    return prevAlbum.Tracks[^1];
            }

            return null;
        }
        return null;
    }
}
