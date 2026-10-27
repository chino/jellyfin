using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Validators;

/// <summary>
/// Class CollectionPostScanTask.
/// </summary>
public class CollectionPostScanTask : ILibraryPostScanTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ICollectionManager _collectionManager;
    private readonly ILogger<CollectionPostScanTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionPostScanTask" /> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="collectionManager">The collection manager.</param>
    /// <param name="logger">The logger.</param>
    public CollectionPostScanTask(
        ILibraryManager libraryManager,
        ICollectionManager collectionManager,
        ILogger<CollectionPostScanTask> logger)
    {
        _libraryManager = libraryManager;
        _collectionManager = collectionManager;
        _logger = logger;
    }

    /// <summary>
    /// Runs the specified progress.
    /// </summary>
    /// <param name="progress">The progress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var collectionNameMoviesMap = new Dictionary<string, HashSet<Guid>>();

        foreach (var library in _libraryManager.RootFolder.Children)
        {
            if (!_libraryManager.GetLibraryOptions(library).AutomaticallyAddToCollection)
            {
                continue;
            }

            var startIndex = 0;
            var pagesize = 1000;
            var seenAudioBooks = new HashSet<(Guid ParentId, string Album)>();

            while (true)
            {
                var movies = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.AudioBook],
                    IsVirtualItem = false,
                    OrderBy = [(ItemSortBy.SortName, SortOrder.Ascending)],
                    Parent = library,
                    StartIndex = startIndex,
                    Limit = pagesize,
                    Recursive = true
                });

                foreach (var m in movies)
                {
                    var collectionName = GetCollectionName(m, seenAudioBooks);
                    if (!string.IsNullOrEmpty(collectionName))
                    {
                        if (collectionNameMoviesMap.TryGetValue(collectionName, out var movieList))
                        {
                            movieList.Add(m.Id);
                        }
                        else
                        {
                            collectionNameMoviesMap[collectionName] = new HashSet<Guid> { m.Id };
                        }
                    }
                }

                if (movies.Count < pagesize)
                {
                    break;
                }

                startIndex += pagesize;
            }
        }

        var numComplete = 0;
        var count = collectionNameMoviesMap.Count;

        if (count == 0)
        {
            progress.Report(100);
            return;
        }

        var boxSets = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.BoxSet],
            CollapseBoxSetItems = false,
            Recursive = true
        });

        foreach (var (collectionName, movieIds) in collectionNameMoviesMap)
        {
            try
            {
                var boxSet = boxSets.FirstOrDefault(b => b?.Name == collectionName) as BoxSet;
                if (boxSet is null)
                {
                    // won't automatically create collection if only one movie in it
                    if (movieIds.Count >= 2)
                    {
                        boxSet = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                        {
                            Name = collectionName,
                        }).ConfigureAwait(false);

                        await _collectionManager.AddToCollectionAsync(boxSet.Id, movieIds).ConfigureAwait(false);
                    }
                }
                else
                {
                    await _collectionManager.AddToCollectionAsync(boxSet.Id, movieIds).ConfigureAwait(false);
                }

                numComplete++;
                double percent = numComplete;
                percent /= count;
                percent *= 100;

                progress.Report(percent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing {CollectionName} with {@MovieIds}", collectionName, movieIds);
            }
        }

        progress.Report(100);
    }

    /// <summary>
    /// Gets the name of the collection an item belongs in.
    /// </summary>
    /// <param name="item">A movie or audiobook.</param>
    /// <param name="seenAudioBooks">Audiobooks already added, by folder and album.</param>
    /// <returns>The collection name, or <c>null</c> if the item belongs in none.</returns>
    internal static string? GetCollectionName(BaseItem item, HashSet<(Guid ParentId, string Album)> seenAudioBooks)
    {
        if (item is Movie movie)
        {
            return movie.PrimaryVersionId.HasValue ? null : movie.CollectionName;
        }

        if (item is AudioBook audioBook && !string.IsNullOrEmpty(audioBook.SeriesName))
        {
            // A book split into several files is one AudioBook per file, all sharing an album and folder.
            // Only the first file goes into the collection, so the book appears there once.
            return seenAudioBooks.Add((audioBook.ParentId, audioBook.Album ?? string.Empty)) ? audioBook.SeriesName : null;
        }

        return null;
    }
}
