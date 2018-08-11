﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Images;
using Viewer.UI.Images;

namespace ViewerTest.UI.Images
{
    [TestClass]
    public class LazyImageTest
    {
        private IEntity _entity;
        private Size _thumbnailSize;
        private Mock<IThumbnailLoader> _thumbnailLoader;
        private PhotoThumbnail _thumbnail;

        [TestInitialize]
        public void Setup()
        {
            _entity = new FileEntity("test");
            _thumbnailSize = new Size(100, 100);
            _thumbnailLoader = new Mock<IThumbnailLoader>();
            _thumbnail = new PhotoThumbnail(_thumbnailLoader.Object, _entity, CancellationToken.None);
        }

        [TestMethod]
        public void Current_StartLoadingImage()
        {
            var image = _thumbnail.GetCurrent(_thumbnailSize);
            image = _thumbnail.GetCurrent(_thumbnailSize);

            _thumbnailLoader.Verify(mock => mock.LoadEmbeddedThumbnailAsync(_entity, _thumbnailSize, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void Current_ReplaceLoadedImageWithCurrentImage()
        {
            var image = new Bitmap(1, 1);
            var task = Task.FromResult(new Thumbnail(image, image.Size));
            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, _thumbnailSize, It.IsAny<CancellationToken>()))
                .Returns(task);

            var current = _thumbnail.GetCurrent(_thumbnailSize);
            current = _thumbnail.GetCurrent(_thumbnailSize);

            Assert.AreEqual(image, current);
            _thumbnailLoader.Verify(mock => mock.LoadEmbeddedThumbnailAsync(_entity, It.IsAny<Size>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [TestMethod]
        public void Resize_ResizeThumbnailWithLoadedThumbnail()
        {
            var smallSize = new Size(1, 1);
            var largeSize = new Size(200, 200);
            var image1 = new Bitmap(1, 1);
            var image2 = new Bitmap(1, 1);
            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, smallSize, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new Thumbnail(image1, smallSize)));
            _thumbnailLoader
                .Setup(mock => mock.LoadNativeThumbnailAsync(_entity, largeSize, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new Thumbnail(image2, largeSize)));

            var current = _thumbnail.GetCurrent(smallSize);
            current = _thumbnail.GetCurrent(smallSize);
            Assert.AreEqual(image1, current);
            current = _thumbnail.GetCurrent(largeSize);
            current = _thumbnail.GetCurrent(largeSize);
            Assert.AreEqual(image2, current);

            _thumbnailLoader.Verify(mock => mock.LoadEmbeddedThumbnailAsync(It.IsAny<IEntity>(), smallSize, It.IsAny<CancellationToken>()), Times.Once);
            _thumbnailLoader.Verify(mock => mock.LoadEmbeddedThumbnailAsync(It.IsAny<IEntity>(), largeSize, It.IsAny<CancellationToken>()), Times.Once);
            _thumbnailLoader.Verify(mock => mock.LoadNativeThumbnailAsync(It.IsAny<IEntity>(), It.IsAny<Size>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
