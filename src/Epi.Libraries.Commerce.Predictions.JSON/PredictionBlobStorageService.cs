// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PredictionBlobStorageService.cs" company="Jeroen Stemerdink">
//      Copyright © 2019 Jeroen Stemerdink.
//      Permission is hereby granted, free of charge, to any person obtaining a copy
//      of this software and associated documentation files (the "Software"), to deal
//      in the Software without restriction, including without limitation the rights
//      to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//      copies of the Software, and to permit persons to whom the Software is
//      furnished to do so, subject to the following conditions:
// 
//      The above copyright notice and this permission notice shall be included in all
//      copies or substantial portions of the Software.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//      IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//      FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//      AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//      LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//      OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//      SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Epi.Libraries.Commerce.Predictions.JSON
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Web;

    using Epi.Libraries.Commerce.Predictions.Core.Models;
    using Epi.Libraries.Commerce.Predictions.JSON.Models;

    using EPiServer;
    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.Framework.Blobs;
    using EPiServer.Logging;
    using EPiServer.Security;
    using EPiServer.ServiceLocation;

    using Newtonsoft.Json;

    public class PredictionBlobStorageService : IPredictionStorageService, IDisposable
    {
        public const string PredictionsFileName = "all_recommendations";

        public const string PredictionsRootName = "Predictions Root";

        /// <summary>
        /// The BLOB factory
        /// </summary>
        private readonly IBlobFactory blobFactory;

        /// <summary>
        /// The cache lock
        /// </summary>
        private readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The content repository
        /// </summary>
        private readonly IContentRepository contentRepository;

        /// <summary>
        /// The content root service
        /// </summary>
        private readonly ContentRootService contentRootService;

        /// <summary>
        /// The <see cref="ILogger"/> instance
        /// </summary>
        private readonly ILogger log = LogManager.GetLogger();

        /// <summary><c>true</c> when this instance is already disposed off; <c>false</c> to when not.</summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictionBlobStorageService" /> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="blobFactory">The BLOB factory.</param>
        /// <param name="contentRepository">The content repository.</param>
        /// <param name="contentRootService">The content root service.</param>
        public PredictionBlobStorageService(
            ServiceAccessor<HttpContextBase> httpContextAccessor,
            IBlobFactory blobFactory,
            IContentRepository contentRepository,
            ContentRootService contentRootService)
        {
            this.blobFactory = blobFactory;
            this.contentRepository = contentRepository;
            this.contentRootService = contentRootService;
        }

        /// <summary>Gets or sets the predictions root.</summary>
        /// <value>The predictions root.</value>
        public ContentReference PredictionsRoot { get; set; }

        /// <summary>
        /// Gets the predictions root unique identifier.
        /// </summary>
        /// <value>The predictions root unique identifier.</value>
        private Guid PredictionsRootGuid { get; } = new Guid("175889ec-574c-4a39-833f-4d62004ecb20");

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.Dispose(true);
        }

        /// <summary>
        /// Initializes the prediction storage.
        /// </summary>
        public void InitPredictionStorage()
        {
            try
            {
                this.contentRootService.Register<ContentFolder>(
                    rootName: PredictionsRootName,
                    contentRootId: this.PredictionsRootGuid,
                    parent: ContentReference.RootPage);

                this.PredictionsRoot = this.contentRootService.Get(rootName: PredictionsRootName);
            }
            catch (NotSupportedException notSupportedException)
            {
                this.log.Error($"[Predictions] {notSupportedException.Message}", exception: notSupportedException);
                throw;
            }
        }

        /// <summary>
        /// Loads the predictions.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> of <see cref="ProductCoPurchasePrediction"/>.</returns>
        /// <exception cref="T:System.Threading.LockRecursionException">The current thread cannot acquire the write lock when it holds the read lock.-or-The <see cref="P:System.Threading.ReaderWriterLockSlim.RecursionPolicy" /> property is <see cref="F:System.Threading.LockRecursionPolicy.NoRecursion" />, and the current thread has attempted to acquire the read lock when it already holds the read lock. -or-The <see cref="P:System.Threading.ReaderWriterLockSlim.RecursionPolicy" /> property is <see cref="F:System.Threading.LockRecursionPolicy.NoRecursion" />, and the current thread has attempted to acquire the read lock when it already holds the write lock. -or-The recursion number would exceed the capacity of the counter. This limit is so large that applications should never encounter this exception.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.Threading.ReaderWriterLockSlim" /> object has been disposed.</exception>
        /// <exception cref="T:System.Threading.SynchronizationLockException">The current thread has not entered the lock in read mode.</exception>
        public IEnumerable<ProductCoPurchasePrediction> LoadPredictions()
        {
            PredictionsMediaData predictionsMediaData = this.contentRepository
                .GetChildren<PredictionsMediaData>(contentLink: this.PredictionsRoot).FirstOrDefault();

            if (predictionsMediaData == null)
            {
                return new List<ProductCoPurchasePrediction>();
            }

            this.cacheLock.EnterReadLock();

            try
            {
                using (Stream stream = predictionsMediaData.BinaryData.OpenRead())
                {
                    using (StreamReader file = new StreamReader(stream: stream))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        return (List<ProductCoPurchasePrediction>)serializer.Deserialize(
                            reader: file,
                            typeof(List<ProductCoPurchasePrediction>));
                    }
                }
            }
            finally
            {
                this.cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Creates the predictions json.
        /// </summary>
        /// <param name="coPurchasePredictions">The predictions.</param>
        /// <exception cref="T:System.Threading.LockRecursionException">The <see cref="P:System.Threading.ReaderWriterLockSlim.RecursionPolicy" /> property is <see cref="F:System.Threading.LockRecursionPolicy.NoRecursion" /> and the current thread has already entered the lock in any mode. -or-The current thread has entered read mode, so trying to enter the lock in write mode would create the possibility of a deadlock. -or-The recursion number would exceed the capacity of the counter. The limit is so large that applications should never encounter it.</exception>
        /// <exception cref="T:System.Threading.SynchronizationLockException">The current thread has not entered the lock in write mode.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.Threading.ReaderWriterLockSlim" /> object has been disposed.</exception>
        public void SavePredictions(IEnumerable<IProductCoPurchasePrediction> coPurchasePredictions)
        {
            this.cacheLock.EnterWriteLock();

            try
            {
                PredictionsMediaData predictionsMediaData =
                    this.contentRepository.GetChildren<PredictionsMediaData>(contentLink: this.PredictionsRoot)
                        .FirstOrDefault()?.CreateWritableClone() as PredictionsMediaData
                    ?? this.contentRepository.GetDefault<PredictionsMediaData>(parentLink: this.PredictionsRoot);

                Blob blob = this.blobFactory.CreateBlob(id: predictionsMediaData.BinaryDataContainer, ".json");

                using (Stream stream = blob.OpenWrite())
                {
                    using (StreamWriter file = new StreamWriter(stream: stream))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(textWriter: file, value: coPurchasePredictions);
                    }
                }

                predictionsMediaData.BinaryData = blob;
                predictionsMediaData.Name = PredictionsFileName;

                this.contentRepository.Save(
                    content: predictionsMediaData,
                    action: EPiServer.DataAccess.SaveAction.Publish,
                    access: AccessLevel.NoAccess);
            }
            finally
            {
                this.cacheLock.ExitWriteLock();
            }
        }

        /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (this.cacheLock != null)
            {
                this.cacheLock.Dispose();
            }

            this.disposed = true;

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}