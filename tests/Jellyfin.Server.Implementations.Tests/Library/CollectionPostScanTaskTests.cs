using System;
using System.Collections.Generic;
using Emby.Server.Implementations.Library.Validators;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class CollectionPostScanTaskTests
{
    [Fact]
    public void GetCollectionName_Movie_ReturnsCollectionName()
    {
        var movie = new Movie { CollectionName = "Alien Collection" };

        Assert.Equal("Alien Collection", CollectionPostScanTask.GetCollectionName(movie, []));
    }

    [Fact]
    public void GetCollectionName_MovieAlternateVersion_ReturnsNull()
    {
        var movie = new Movie { CollectionName = "Alien Collection", PrimaryVersionId = Guid.NewGuid() };

        Assert.Null(CollectionPostScanTask.GetCollectionName(movie, []));
    }

    [Fact]
    public void GetCollectionName_AudioBookWithSeries_ReturnsSeriesName()
    {
        var book = new AudioBook { SeriesName = "Example Saga", Album = "Book Two", ParentId = Guid.NewGuid() };

        Assert.Equal("Example Saga", CollectionPostScanTask.GetCollectionName(book, []));
    }

    [Fact]
    public void GetCollectionName_AudioBookWithoutSeries_ReturnsNull()
    {
        var book = new AudioBook { Album = "Book Two", ParentId = Guid.NewGuid() };

        Assert.Null(CollectionPostScanTask.GetCollectionName(book, []));
    }

    [Fact]
    public void GetCollectionName_FilesOfOneBook_OnlyFirstReturnsSeriesName()
    {
        var folder = Guid.NewGuid();
        var seen = new HashSet<(Guid ParentId, string Album)>();
        var part1 = new AudioBook { SeriesName = "Example Saga", Album = "Book Two", ParentId = folder };
        var part2 = new AudioBook { SeriesName = "Example Saga", Album = "Book Two", ParentId = folder };

        Assert.Equal("Example Saga", CollectionPostScanTask.GetCollectionName(part1, seen));
        Assert.Null(CollectionPostScanTask.GetCollectionName(part2, seen));
    }

    [Fact]
    public void GetCollectionName_BooksOfSeriesInOneFolder_AllReturnSeriesName()
    {
        var folder = Guid.NewGuid();
        var seen = new HashSet<(Guid ParentId, string Album)>();
        var book3 = new AudioBook { SeriesName = "Example Saga", Album = "Book One", ParentId = folder };
        var book4 = new AudioBook { SeriesName = "Example Saga", Album = "Book Two", ParentId = folder };

        Assert.Equal("Example Saga", CollectionPostScanTask.GetCollectionName(book3, seen));
        Assert.Equal("Example Saga", CollectionPostScanTask.GetCollectionName(book4, seen));
    }
}
