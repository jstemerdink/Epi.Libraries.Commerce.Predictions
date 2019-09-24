// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreatePredictionsScheduledJob.cs" company="Jeroen Stemerdink">
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

namespace Epi.Libraries.Commerce.Predictions.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    using Epi.Libraries.Commerce.Predictions.Core.Models;
    using Epi.Libraries.Commerce.Predictions.Engine;

    using EPiServer;
    using EPiServer.Commerce.Catalog.ContentTypes;
    using EPiServer.Commerce.Order;
    using EPiServer.Core;
    using EPiServer.Logging;
    using EPiServer.PlugIn;
    using EPiServer.Scheduler;

    using Mediachase.Commerce.Catalog;

    using Microsoft.ML;
    using Microsoft.ML.Trainers;

    /// <summary>
    /// Class CreatePredictionsScheduledJob.
    /// Implements the <see cref="EPiServer.Scheduler.ScheduledJobBase" />
    /// </summary>
    /// <seealso cref="EPiServer.Scheduler.ScheduledJobBase" />
    [ScheduledPlugIn(
        DisplayName = "Create Predictions",
        Restartable = false,
        GUID = "8b3fd674-40b7-4fa1-953b-c875fe219bc7")]
    public class CreatePredictionsScheduledJob : ScheduledJobBase
    {
        /// <summary>
        /// The content loader
        /// </summary>
        private readonly IContentLoader contentLoader;

        /// <summary>
        /// The <see cref="ILogger"/> instance
        /// </summary>
        private readonly ILogger log = LogManager.GetLogger();

        /// <summary>
        /// The ml context
        /// </summary>
        private readonly MLContext mlContext;

        /// <summary>
        /// The order search service
        /// </summary>
        private readonly IOrderSearchService orderSearchService;

        /// <summary>
        /// The data store provider factory
        /// </summary>
        private readonly IRecommendationRepository recommendationRepository;

        /// <summary>
        /// The reference converter
        /// </summary>
        private readonly ReferenceConverter referenceConverter;

        /// <summary>
        /// The prediction engine
        /// </summary>
        private MlModelEngine<ProductEntry, CoPurchasePrediction> predictionEngine;

        /// <summary><c>true</c> when this scheduled job has been stopped; <c>false</c> when not.</summary>
        private bool stopSignaled;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePredictionsScheduledJob" /> class.
        /// </summary>
        /// <param name="contentLoader">The content loader.</param>
        /// <param name="referenceConverter">The reference converter.</param>
        /// <param name="recommendationRepository">The co purchase prediction repository.</param>
        /// <param name="orderSearchService">The order search service.</param>
        public CreatePredictionsScheduledJob(
            IContentLoader contentLoader,
            ReferenceConverter referenceConverter,
            IRecommendationRepository recommendationRepository,
            IOrderSearchService orderSearchService)
        {
            this.contentLoader = contentLoader;
            this.referenceConverter = referenceConverter;
            this.recommendationRepository = recommendationRepository;
            this.orderSearchService = orderSearchService;

            this.mlContext = new MLContext();

            this.IsStoppable = true;
        }

        /// <summary>
        /// Called when a scheduled job executes
        /// </summary>
        /// <returns>A status message to be stored in the database log and visible from admin mode</returns>
        /// <exception cref="T:System.AggregateException">The exception that contains all the individual exceptions thrown on all threads.</exception>
        public override string Execute()
        {
            this.OnStatusChanged("Starting to update/train the model");

            List<ProductEntry> products = this.GetProductEntries();
            ITransformer transformer = this.LoadDataAndTrain(products: products);

            this.predictionEngine = new MlModelEngine<ProductEntry, CoPurchasePrediction>(transformer: transformer);

            this.OnStatusChanged("Prediction model has been updated and trained");

            ConcurrentBag<IProductCoPurchasePrediction> predictions = new ConcurrentBag<IProductCoPurchasePrediction>();

            List<int> productIds = this.GetAllVariantIds().ToList();

            this.OnStatusChanged("Creating predictions");

            foreach (int productId in productIds)
            {
                if (this.stopSignaled)
                {
                    break;
                }

                List<int> coPurchaseProductIds = productIds.Where(id => id != productId).ToList();

                Parallel.ForEach(
                    source: coPurchaseProductIds,
                    coPurchaseProductId =>
                        {
                            IProductCoPurchasePrediction productCoPurchasePrediction = this.GetPrediction(
                                productId: productId,
                                coPurchaseProductId: coPurchaseProductId);

                            if (productCoPurchasePrediction != null)
                            {
                                predictions.Add(item: productCoPurchasePrediction);
                            }
                        });
            }

            try
            {
                this.recommendationRepository.AddOrUpdate(predictions: predictions);
            }
            catch (Exception exception)
            {
                this.log.Error(
                    $"[Prediction Engine] could not create predictions: {exception.Message}",
                    exception: exception);
                return $"Could not create predictions: {exception.Message}";
            }

            // For long running jobs periodically check if stop is signaled and if so stop execution
            return this.stopSignaled ? "Stop of job was called" : $"Created {predictions.Count} predictions";
        }

        /// <summary>
        /// Called when a user clicks on Stop for a manually started job, or when ASP.NET shuts down.
        /// </summary>
        public override void Stop()
        {
            this.stopSignaled = true;
        }

        /// <summary>
        /// Gets all variant ids.
        /// </summary>
        /// <returns>An IEnumerable of ids.</returns>
        private IEnumerable<int> GetAllVariantIds()
        {
            List<int> variantIdList = new List<int>();

            IEnumerable<CatalogContent> catalogs = this.contentLoader.GetChildren<CatalogContent>(
                this.referenceConverter.GetRootLink(),
                new LoaderOptions { LanguageLoaderOption.MasterLanguage() });

            foreach (CatalogContent catalogContent in catalogs)
            {
                if (this.stopSignaled)
                {
                    break;
                }

                foreach (VariationContent variant in this.GetEntriesRecursive<VariationContent>(
                    parentLink: catalogContent.ContentLink,
                    defaultCulture: catalogContent.MasterLanguage))
                {
                    if (this.stopSignaled)
                    {
                        break;
                    }

                    variantIdList.Add(item: this.referenceConverter.GetObjectId(variant.ContentLink));
                }
            }

            return variantIdList.Distinct();
        }

        /// <summary>
        /// Gets the entries recursive.
        /// </summary>
        /// <typeparam name="T">The entry type.</typeparam>
        /// <param name="parentLink">The parent link.</param>
        /// <param name="defaultCulture">The default culture.</param>
        /// <returns>An IEnumerable of entries.</returns>
        private IEnumerable<T> GetEntriesRecursive<T>(ContentReference parentLink, CultureInfo defaultCulture)
            where T : EntryContentBase
        {
            foreach (NodeContent nodeContent in this.LoadChildrenBatched<NodeContent>(
                parentLink: parentLink,
                defaultCulture: defaultCulture))
            {
                foreach (T entry in this.GetEntriesRecursive<T>(
                    parentLink: nodeContent.ContentLink,
                    defaultCulture: defaultCulture))
                {
                    yield return entry;
                }
            }

            foreach (T entry in this.LoadChildrenBatched<T>(parentLink: parentLink, defaultCulture: defaultCulture))
            {
                yield return entry;
            }
        }

        /// <summary>
        /// Gets the prediction.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="coPurchaseProductId">The co purchase product identifier.</param>
        /// <returns>an instance of <see cref="IProductCoPurchasePrediction"/>.</returns>
        private IProductCoPurchasePrediction GetPrediction(int productId, int coPurchaseProductId)
        {
            if (this.predictionEngine == null)
            {
                return null;
            }

            CoPurchasePrediction prediction = this.predictionEngine.Predict(
                new ProductEntry { ProductId = (uint)productId, CoPurchaseProductId = (uint)coPurchaseProductId });

            return this.recommendationRepository.Create(
                productId: productId,
                coPurchaseProductId: coPurchaseProductId,
                score: prediction.Score);
        }

        /// <summary>
        /// Gets the product entries.
        /// </summary>
        /// <returns>A list of <see cref="ProductEntry"/> from all orders.</returns>
        private List<ProductEntry> GetProductEntries()
        {
            List<ProductEntry> products = new List<ProductEntry>();

            IEnumerable<IPurchaseOrder> orders = this.orderSearchService.FindPurchaseOrders(new OrderSearchFilter())
                .Orders;

            foreach (IPurchaseOrder orderSearchResult in orders)
            {
                IEnumerable<ILineItem> lineItems = orderSearchResult.Forms.FirstOrDefault().GetAllLineItems().ToList();

                if (lineItems.Count() <= 1)
                {
                    continue;
                }

                List<int> productIdList = (from lineItem in lineItems select lineItem.GetEntryContent() into content where content != null select this.referenceConverter.GetObjectId(content.ContentLink)).ToList();

                foreach (int productId in productIdList)
                {
                    List<int> coPurchaseProductIds = productIdList.Where(id => id != productId).ToList();

                    products.AddRange(coPurchaseProductIds.Select(coPurchaseProductId => new ProductEntry { ProductId = (uint)productId, CoPurchaseProductId = (uint)coPurchaseProductId }));
                }
            }

            return products;
        }

        /// <summary>
        /// Loads the children batched.
        /// </summary>
        /// <typeparam name="T">The type to load.</typeparam>
        /// <param name="parentLink">The parent link.</param>
        /// <param name="defaultCulture">The default culture.</param>
        /// <returns>An IEnumerable of items.</returns>
        private IEnumerable<T> LoadChildrenBatched<T>(ContentReference parentLink, CultureInfo defaultCulture)
            where T : IContent
        {
            int start = 0;

            while (true)
            {
                List<T> batch = this.contentLoader.GetChildren<T>(
                    contentLink: parentLink,
                    language: defaultCulture,
                    startIndex: start,
                    50).ToList();

                if (!batch.Any())
                {
                    yield break;
                }

                foreach (T content in batch)
                {
                    if (!parentLink.CompareToIgnoreWorkID(contentReference: content.ParentLink))
                    {
                        continue;
                    }

                    yield return content;
                }

                start += 50;
            }
        }

        /// <summary>
        /// Loads the data and train.
        /// </summary>
        /// <param name="products">The products.</param>
        /// <returns>an instance of <see cref="ITransformer"/>.</returns>
        private ITransformer LoadDataAndTrain(IEnumerable<ProductEntry> products)
        {
            // Read the trained data using TextLoader by defining the schema for reading the product co-purchase data-set
            IDataView traindata = this.mlContext.Data.LoadFromEnumerable(data: products);

            // Your data is already encoded so all you need to do is specify options for MatrixFactorizationTrainer with a few extra hyper parameters
            // LossFunction, Alpha, Lambda and a few others like K and C as shown below and call the trainer. 
            MatrixFactorizationTrainer.Options options = new MatrixFactorizationTrainer.Options
                                                             {
                                                                 MatrixColumnIndexColumnName =
                                                                     nameof(ProductEntry.ProductId),
                                                                 MatrixRowIndexColumnName =
                                                                     nameof(ProductEntry.CoPurchaseProductId),
                                                                 LabelColumnName = nameof(ProductEntry.Label),
                                                                 LossFunction =
                                                                     MatrixFactorizationTrainer.LossFunctionType
                                                                         .SquareLossOneClass,
                                                                 Alpha = 0.01,
                                                                 Lambda = 0.025,
                                                                 ApproximationRank = 100,
                                                                 C = 0.00001
                                                             };

            // Call the MatrixFactorization trainer by passing options.
            MatrixFactorizationTrainer est = this.mlContext.Recommendation().Trainers
                .MatrixFactorization(options: options);

            // Train the model fitting to the DataSet
            ITransformer model = est.Fit(input: traindata);

            return model;
        }
    }
}