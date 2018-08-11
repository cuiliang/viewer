﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Images;
using Viewer.IO;

namespace Viewer.UI.Images
{
    public struct Thumbnail
    {
        /// <summary>
        /// Thumbnail picture
        /// </summary>
        public Image ThumbnailImage { get; }

        /// <summary>
        /// Size of the original image from wich <see cref="ThumbnailImage"/> is generated.
        /// </summary>
        public Size OriginalSize { get; }

        public Thumbnail(Image thumbnailImage, Size originalSize)
        {
            ThumbnailImage = thumbnailImage;
            OriginalSize = originalSize;
        }
    }

    /// <summary>
    /// Thumbnail loader loads thumbnails using <see cref="IImageLoader"/> and resizes them using <see cref="IThumbnailGenerator"/>.
    /// </summary>
    public interface IThumbnailLoader
    {
        /// <summary>
        /// Load embedded thumbnail of <paramref name="entity"/> and resize it to fit in <paramref name="thumbnailAreaSize"/> so that it presrves its aspect ratio.
        /// This does not trigger any I/O. It uses thumbnail loaded in <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">Entity for which you want to load an embedded thumbnail</param>
        /// <param name="thumbnailAreaSize">Area for the thumbnail. Generated thumbanil will be scaled so that it fits in this area.</param>
        /// <param name="cancellationToken">Cancellation token of the load operation.</param>
        /// <returns>
        ///     <para>Task finished when the thumbnail is loaded.</para>
        ///     <para>
        ///         If <paramref name="entity"/> does not have an embedded thumbnail, this function
        ///         returns immediately with a completed task where <see cref="Thumbnail.ThumbnailImage"/> is null
        ///         and <see cref="Thumbnail.OriginalSize"/> is <see cref="Size.Empty"/>.
        ///     </para>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null</exception>
        /// <seealso cref="IImageLoader.LoadThumbnailAsync(IEntity, CancellationToken)">
        ///     For the list of possible exceptions returned by the task.
        /// </seealso>
        /// <seealso cref="IThumbnailGenerator.GetThumbnail">
        ///     For the list of possible exceptions returned by the task.
        /// </seealso>
        Task<Thumbnail> LoadEmbeddedThumbnailAsync(IEntity entity, Size thumbnailAreaSize, CancellationToken cancellationToken);

        /// <summary>
        /// Load the original image of <paramref name="entity"/> and and resize it to fit in <paramref name="thumbnailAreaSize"/> so that it presrves its aspect ratio.
        /// This function reads the whole file of <paramref name="entity"/> on a background thread.
        /// </summary>
        /// <param name="entity">Entity whose file will be loaded.</param>
        /// <param name="thumbnailAreaSize">Area for the thumbnail. Generated thumbanil will be scaled so that it fits in this area.</param>
        /// <param name="cancellationToken">Cancellation token of the load operation.</param>
        /// <returns>
        ///     Task finished when the thumbnail is loaded.
        ///     See <see cref="IImageLoader.LoadImage"/> for the list of possible exceptions returned by this task.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null</exception>
        /// <seealso cref="IImageLoader.LoadImage">
        ///     For the list of possible exceptions returned by the task.
        /// </seealso>
        /// <seealso cref="IThumbnailGenerator.GetThumbnail">
        ///     For the list of possible exceptions returned by the task.
        /// </seealso>
        Task<Thumbnail> LoadNativeThumbnailAsync(IEntity entity, Size thumbnailAreaSize, CancellationToken cancellationToken);

        /// <summary>
        /// Increase priority of given entity so that it is loaded before any other entity waiting in the queue.
        /// This won't change any priorities if there is no entity with <paramref name="path"/> in the waiting queue.
        /// </summary>
        /// <param name="path">Path to an entity to prioritize</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        void Prioritize(string path);
    }

    [Export(typeof(IThumbnailLoader))]
    public class ThumbnailLoader : IThumbnailLoader
    {
        private readonly IImageLoader _imageLoader;
        private readonly IThumbnailGenerator _thumbnailGenerator;

        private readonly List<LoadRequest> _requests = new List<LoadRequest>();
        private readonly SemaphoreSlim _requestCount = new SemaphoreSlim(0);

        [ImportingConstructor]
        public ThumbnailLoader(IImageLoader imageLoader, IThumbnailGenerator thumbnailGenerator)
        {
            _imageLoader = imageLoader;
            _thumbnailGenerator = thumbnailGenerator;

            var thread = new Thread(Loader)
            {
                IsBackground = true
            };
            thread.Start();
        }

        public Task<Thumbnail> LoadEmbeddedThumbnailAsync(
            IEntity entity, 
            Size thumbnailAreaSize,
            CancellationToken cancellationToken)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return LoadEmbeddedThumbnailAsyncImpl(entity, thumbnailAreaSize, cancellationToken);
        }

        private async Task<Thumbnail> LoadEmbeddedThumbnailAsyncImpl(IEntity entity, Size thumbnailAreaSize, CancellationToken cancellationToken)
        {
            using (var image = await _imageLoader.LoadThumbnailAsync(entity, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var thumbnail = image != null ? 
                    _thumbnailGenerator.GetThumbnail(image, thumbnailAreaSize) : 
                    null;
                return new Thumbnail(thumbnail, image?.Size ?? Size.Empty);
            }
        }

        public Task<Thumbnail> LoadNativeThumbnailAsync(IEntity entity, Size thumbnailAreaSize, CancellationToken cancellationToken)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var req = new LoadRequest(entity, thumbnailAreaSize, cancellationToken);
            return AddLoadRequest(req);
        }
        
        public void Prioritize(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            lock (_requests)
            {
                // find the request
                var index = _requests.Count - 1;
                for (; index >= 0; --index)
                {
                    if (_requests[index].Entity.Path == path)
                    {
                        break;
                    }
                }
                if (index < 0)
                {
                    return;
                }

                // move it to the start of the collection
                var request = _requests[index];
                _requests.RemoveAt(index);
                _requests.Add(request);
            }
        }

        private class LoadRequest
        {
            public TaskCompletionSource<Thumbnail> Completion { get; } = new TaskCompletionSource<Thumbnail>();

            public CancellationToken Cancellation { get; }
            public IEntity Entity { get; }
            public Size ThumbnailAreaSize { get; }

            public LoadRequest(IEntity entity, Size thumbnailAreaSize, CancellationToken cancellationToken)
            {
                Entity = entity;
                ThumbnailAreaSize = thumbnailAreaSize;
                Cancellation = cancellationToken;
            }
        }

        private void Loader()
        {
            for (;;)
            {
                var req = ConsumeLoadRequest();
                if (req == null)
                {
                    continue;
                }

                try
                {
                    if (req.Cancellation.IsCancellationRequested)
                        continue;

                    using (var image = _imageLoader.LoadImage(req.Entity))
                    {
                        if (req.Cancellation.IsCancellationRequested)
                            continue;
                        var thumbnail = _thumbnailGenerator.GetThumbnail(image, req.ThumbnailAreaSize);
                        req.Completion.SetResult(new Thumbnail(thumbnail, image.Size));
                    }
                }
                catch (Exception e)
                {
                    req.Completion.SetException(e);
                }
            }
        }

        private LoadRequest ConsumeLoadRequest()
        {
            _requestCount.Wait();

            lock (_requests)
            {
                var req = _requests[_requests.Count - 1];
                _requests.RemoveAt(_requests.Count - 1);
                return req;
            }
        }

        private Task<Thumbnail> AddLoadRequest(LoadRequest req)
        {
            lock (_requests)
            {
                _requests.Add(req);
            }
            _requestCount.Release();
            return req.Completion.Task;
        }
    }
}
