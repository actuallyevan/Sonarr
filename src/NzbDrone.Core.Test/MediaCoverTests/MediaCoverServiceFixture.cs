using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.Test.MediaCoverTests
{
    [TestFixture]
    [NonParallelizable]
    public class MediaCoverServiceFixture : CoreTest<MediaCoverService>
    {
        private const string DisableMediaCoverCacheEnvironmentVariable = "DISABLE_MEDIA_COVER_CACHE";

        private Series _series;
        private string _previousDisableMediaCoverCacheValue;

        [SetUp]
        public void Setup()
        {
            _previousDisableMediaCoverCacheValue = Environment.GetEnvironmentVariable(DisableMediaCoverCacheEnvironmentVariable);
            Environment.SetEnvironmentVariable(DisableMediaCoverCacheEnvironmentVariable, null);

            Mocker.SetConstant<IAppFolderInfo>(new AppFolderInfo(Mocker.Resolve<IStartupContext>()));

            _series = Builder<Series>.CreateNew()
                .With(v => v.Id = 2)
                .With(v => v.Images = new List<MediaCover.MediaCover> { new MediaCover.MediaCover(MediaCoverTypes.Poster, "") })
                .Build();
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(DisableMediaCoverCacheEnvironmentVariable, _previousDisableMediaCoverCacheValue);
        }

        [Test]
        public void should_convert_cover_urls_to_local()
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover { CoverType = MediaCoverTypes.Banner }
                };

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileGetLastWrite(It.IsAny<string>()))
                  .Returns(new DateTime(1234));

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.ConvertToLocalUrls(12, covers);

            covers.Single().Url.Should().Be("/MediaCover/12/banner.jpg?lastWrite=1234");
        }

        [Test]
        public void should_convert_media_urls_to_local_without_time_if_file_doesnt_exist()
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover { CoverType = MediaCoverTypes.Banner }
                };

            Subject.ConvertToLocalUrls(12, covers);

            covers.Single().Url.Should().Be("/MediaCover/12/banner.jpg");
        }

        [Test]
        public void should_use_remote_cover_urls_when_cache_is_disabled()
        {
            Environment.SetEnvironmentVariable(DisableMediaCoverCacheEnvironmentVariable, "true");
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover { CoverType = MediaCoverTypes.Banner, RemoteUrl = "https://example.com/banner.jpg" }
                };

            Subject.ConvertToLocalUrls(12, covers);

            covers.Single().Url.Should().Be("https://example.com/banner.jpg");
            Mocker.GetMock<IDiskProvider>().Verify(v => v.FileExists(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_not_download_or_resize_covers_when_cache_is_disabled()
        {
            Environment.SetEnvironmentVariable(DisableMediaCoverCacheEnvironmentVariable, "true");

            Subject.HandleAsync(new SeriesUpdatedEvent(_series));

            Mocker.GetMock<ICoverExistsSpecification>().Verify(v => v.AlreadyExists(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            Mocker.GetMock<IHttpClient>().Verify(v => v.DownloadFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            Mocker.GetMock<IImageResizer>().Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never());
            Mocker.GetMock<IEventAggregator>().Verify(v => v.PublishEvent(It.IsAny<MediaCoversUpdatedEvent>()), Times.Once());
        }

        [Test]
        public void should_resize_covers_if_main_downloaded()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.HandleAsync(new SeriesUpdatedEvent(_series));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void should_resize_covers_if_missing()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(false);

            Subject.HandleAsync(new SeriesUpdatedEvent(_series));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void should_not_resize_covers_if_exists()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetFileSize(It.IsAny<string>()))
                  .Returns(1000);

            Subject.HandleAsync(new SeriesUpdatedEvent(_series));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_resize_covers_if_existing_is_empty()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetFileSize(It.IsAny<string>()))
                  .Returns(0);

            Subject.HandleAsync(new SeriesUpdatedEvent(_series));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void should_log_error_if_resize_failed()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IImageResizer>()
                  .Setup(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                  .Throws<ApplicationException>();

            Subject.HandleAsync(new SeriesUpdatedEvent(_series));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }
    }
}
