// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RecommendationRepository.cs" company="Jeroen Stemerdink">
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

namespace Epi.Libraries.Commerce.Predictions.DDS
{
    using System.Collections.Generic;
    using System.Linq;

    using Epi.Libraries.Commerce.Predictions.Core;
    using Epi.Libraries.Commerce.Predictions.Core.Models;
    using Epi.Libraries.Commerce.Predictions.DDS.Models;

    using EPiServer.Data.Dynamic;
    using EPiServer.Data.Dynamic.Providers;

    /// <summary>
    /// Class RecommendationRepository.
    /// Implements the <see cref="IRecommendationRepository" />
    /// </summary>
    /// <seealso cref="IRecommendationRepository" />
    public class RecommendationRepository : IRecommendationRepository
    {
        /// <summary>
        /// The data store provider factory
        /// </summary>
        private readonly IDataStoreProviderFactory dataStoreProviderFactory;

        /// <summary>
        /// The dynamic data store factory
        /// </summary>
        private readonly DynamicDataStoreFactory dynamicDataStoreFactory;

        /// <summary>
        /// The productCoPurchasePredictions
        /// </summary>
        private IEnumerable<ProductCoPurchasePrediction> predictions;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecommendationRepository"/> class.
        /// </summary>
        /// <param name="dynamicDataStoreFactory">The dynamic data store factory.</param>
        /// <param name="dataStoreProviderFactory">The data store provider factory.</param>
        public RecommendationRepository(
            DynamicDataStoreFactory dynamicDataStoreFactory,
            IDataStoreProviderFactory dataStoreProviderFactory)
        {
            this.dynamicDataStoreFactory = dynamicDataStoreFactory;
            this.dataStoreProviderFactory = dataStoreProviderFactory;
        }

        /// <summary>
        /// Gets the predictions.
        /// </summary>
        /// <value>The predictions.</value>
        public IEnumerable<IProductCoPurchasePrediction> Predictions
        {
            get
            {
                return this.predictions ?? (this.predictions = this.Store.LoadAll<ProductCoPurchasePrediction>());
            }
        }

        /// <summary>Gets the backing DDS store.</summary>
        private DynamicDataStore Store
        {
            get
            {
                return this.dynamicDataStoreFactory.GetStore(typeof(ProductCoPurchasePrediction))
                       ?? this.dynamicDataStoreFactory.CreateStore(typeof(ProductCoPurchasePrediction));
            }
        }

        /// <summary>
        /// Adds or updates <see cref="IProductCoPurchasePrediction"/> in the repository.
        /// </summary>
        /// <param name="productCoPurchasePredictions">The productCoPurchasePredictions.</param>
        public virtual void AddOrUpdate(IEnumerable<IProductCoPurchasePrediction> productCoPurchasePredictions)
        {
            DynamicDataStore currentStore = this.Store;

            currentStore.DeleteAll();

            DataStoreProvider provider = this.dataStoreProviderFactory.Create();

            provider.ExecuteTransaction(
                () =>
                    {
                        currentStore.DataStoreProvider = provider;

                        foreach (ProductCoPurchasePrediction productCoPurchasePrediction in productCoPurchasePredictions
                            .Cast<ProductCoPurchasePrediction>())
                        {
                            this.Store.Save(value: productCoPurchasePrediction);
                        }
                    });

            this.predictions = null;
        }

        /// <summary>
        /// Creates a new prediction.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="coPurchaseProductId">The co purchase product identifier.</param>
        /// <param name="score">The score.</param>
        /// <returns>An instance of <see cref="T:Epi.Libraries.Commerce.Predictions.Models.IProductCoPurchasePrediction" />.</returns>
        public IProductCoPurchasePrediction Create(int productId, int coPurchaseProductId, float score)
        {
            return new ProductCoPurchasePrediction
                       {
                           ProductId = productId, CoPurchaseProductId = coPurchaseProductId, Score = score
                       };
        }

        /// <summary>
        /// Deletes the <see cref="ProductCoPurchasePrediction" /> with the specified <param name="productId"></param>.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        public virtual void Delete(int productId)
        {
            List<ProductCoPurchasePrediction> existingCoPurchasePredictions = new List<ProductCoPurchasePrediction>();

            DynamicDataStore currentStore = this.Store;

            existingCoPurchasePredictions.AddRange(
                this.Store.Find<ProductCoPurchasePrediction>("ProductId", value: productId));
            existingCoPurchasePredictions.AddRange(
                this.Store.Find<ProductCoPurchasePrediction>("CoPurchaseProductId", value: productId));

            DataStoreProvider provider = this.dataStoreProviderFactory.Create();

            provider.ExecuteTransaction(
                () =>
                    {
                        currentStore.DataStoreProvider = provider;

                        foreach (ProductCoPurchasePrediction productCoPurchasePrediction in
                            existingCoPurchasePredictions)
                        {
                            this.Store.Delete(id: productCoPurchasePrediction.Id);
                        }
                    });

            this.predictions = null;
        }

        /// <summary>
        /// Gets all <see cref="ProductCoPurchasePrediction"/> with the specified <param name="productId"></param>.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ProductCoPurchasePrediction"/>.</returns>
        public virtual IEnumerable<IProductCoPurchasePrediction> Get(int productId)
        {
            return this.Store.Find<ProductCoPurchasePrediction>("ProductId", value: productId);
        }

        /// <summary>
        /// Gets the combined <see cref="ProductCoPurchasePrediction" /> for the specified <param name="productIds"></param>.
        /// </summary>
        /// <param name="productIds">The product ids.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="ProductCoPurchasePrediction" />.</returns>
        public IEnumerable<IProductCoPurchasePrediction> Get(IEnumerable<int> productIds)
        {
            List<IProductCoPurchasePrediction> combinedRecommendations = new List<IProductCoPurchasePrediction>();

            foreach (int id in productIds)
            {
                combinedRecommendations.AddRange(this.Get(productId: id));
            }

            return combinedRecommendations;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
        }
    }
}