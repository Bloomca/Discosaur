using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discosaur.Models;
using Discosaur.Services;

namespace Discosaur.ViewModels
{
    public partial class SelectionViewModel : ObservableObject
    {
        private readonly ObservableCollection<Album> library;

        public ObservableCollection<Track> SelectedTracks { get; } = [];

        public SelectionViewModel(ObservableCollection<Album> _library)
        {
            library = _library;
        }

        public void SelectTrack(Track track)
        {
            SelectedTracks.Clear();
            SelectedTracks.Add(track);
        }

        /// <summary>
        /// Meant to be called when the user clicks with CTRL key pressed
        /// </summary>
        public void SelectExtraTrack(Track track)
        {
            if (SelectedTracks.Contains(track))
            {
                SelectedTracks.Remove(track);
            } else
            {
                SelectedTracks.Add(track);
            }
        }

        public void SelectNextTrack()
        {
            var anchor = SelectedTracks.LastOrDefault();
            if (anchor == null)
            {
                var first = library.FirstOrDefault()?.Tracks.FirstOrDefault();
                if (first != null) SelectTrack(first);
                return;
            }

            var next = LibraryService.FindNextTrack(anchor, library);
            if (next != null) SelectTrack(next);
        }

        public void SelectPreviousTrack()
        {
            var anchor = SelectedTracks.LastOrDefault();
            if (anchor == null)
            {
                var last = library.LastOrDefault()?.Tracks.LastOrDefault();
                if (last != null) SelectTrack(last);
                return;
            }

            var prev = LibraryService.FindPreviousTrack(anchor, library);
            if (prev != null) SelectTrack(prev);
        }

        public void SelectFirstTrackOfNextAlbum()
        {
            var anchor = SelectedTracks.LastOrDefault();
            if (anchor == null)
            {
                var first = library.FirstOrDefault()?.Tracks.FirstOrDefault();
                if (first != null) SelectTrack(first);
                return;
            }

            for (int i = 0; i < library.Count; i++)
            {
                if (library[i].Tracks.Contains(anchor) && i + 1 < library.Count)
                {
                    var target = library[i + 1].Tracks.FirstOrDefault();
                    if (target != null) SelectTrack(target);
                    return;
                }
            }
        }

        public void SelectFirstTrackOfPreviousAlbum()
        {
            var anchor = SelectedTracks.LastOrDefault();
            if (anchor == null) return;

            for (int i = 0; i < library.Count; i++)
            {
                if (library[i].Tracks.Contains(anchor) && i - 1 >= 0)
                {
                    var target = library[i - 1].Tracks.FirstOrDefault();
                    if (target != null) SelectTrack(target);
                    return;
                }
            }
        }

        public void SelectAlbum(Album album)
        {
            // for now, we simply replace any current selection
            // in the future, we will add CTRL key
            SelectedTracks.Clear();

            foreach (var track in album.Tracks)
            {
                SelectedTracks.Add(track);
            }
        }
    }
}
