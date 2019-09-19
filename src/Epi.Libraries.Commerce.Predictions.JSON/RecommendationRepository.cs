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

namespace Epi.Libraries.Commerce.Predictions.JSON
{
    using System.Collections.Generic;
    using System.Linq;

    using Epi.Libraries.Commerce.Predictions.Core;
    using Epi.Libraries.Commerce.Predictions.Core.Models;
    using Epi.Libraries.Commerce.Predictions.JSON.Models;

    /// <summary>
    /// Class RecommendationRepository.
    /// Implements the <see cref="IRecommendationRepository" />
    /// </summary>
    /// <seealso cref="IRecommendationRepository" />
    public class RecommendationRepository : IRecommendationRepository
    {
        /// <summary>
        /// The prediction storage service
        /// </summary>
        private readonly IPredictionStorageService predictionStorageService;

        /// <summary>
        /// The predictions
        /// </summary>
        private IEnumerable<IProductCoPurchasePrediction> predictions;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecommendationRepository"/> class.
        /// </summary>
        /// <param name="predictionStorageService">The prediction storage service.</param>
        public RecommendationRepository(IPredictionStorageService predictionStorageService)
        {
            this.predictionStorageService = predictionStorageService;
        }

        /// <summary>
        /// Gets the predictions.
        /// </summary>
        /// <value>The predictions.</value>
        public IEnumerable<IProductCoPurchasePrediction> Predictions
        {
            get
            {
                return this.predictions ?? (this.predictions = this.predictionStorageService.LoadPredictions());
            }
        }

        /// <summary>
        /// Adds or updates <see cref="IProductCoPurchasePrediction"/> in the repository.
        /// </summary>
        /// <param name="productCoPurchasePredictions">The product co purchase predictions.</param>
        /// <exception cref="T:System.Threading.LockRecursionException">The <see cref="P:System.Threading.ReaderWriterLockSlim.RecursionPolicy" /> property is <see cref="F:System.Threading.LockRecursionPolicy.NoRecursion" /> and the current thread has already entered the lock in any mode. -or-The current thread has entered read mode, so trying to enter the lock in write mode would create the possibility of a deadlock. -or-The recursion number would exceed the capacity of the counter. The limit is so large that applications should never encounter it.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="T:System.Threading.SynchronizationLockException">The current thread has not entered the lock in write mode.</exception>
        public void AddOrUpdate(IEnumerable<IProductCoPurchasePrediction> productCoPurchasePredictions)
        {
            List<IProductCoPurchasePrediction> recommendations = productCoPurchasePredictions.ToList();

            this.predictionStorageService.SavePredictions(coPurchasePredictions: recommendations);
            this.predictions = recommendations;
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
        /// Deletes the <see cref="T:Epi.Libraries.Commerce.Predictions.Models.IProductCoPurchasePrediction" /> with the specified <param name="productId"></param>.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        public void Delete(int productId)
        {
            List<IProductCoPurchasePrediction> currentCoPurchasePredictions = this.Predictions.ToList();
            List<IProductCoPurchasePrediction> existingCoPurchasePredictions = new List<IProductCoPurchasePrediction>();

            existingCoPurchasePredictions.AddRange(this.Predictions.Where(p => p.ProductId == productId));
            existingCoPurchasePredictions.AddRange(this.Predictions.Where(p => p.CoPurchaseProductId == productId));

            foreach (IProductCoPurchasePrediction existingCoPurchasePrediction in existingCoPurchasePredictions)
            {
                currentCoPurchasePredictions.Remove(item: existingCoPurchasePrediction);
            }

            this.predictions = currentCoPurchasePredictions;
            this.predictionStorageService.SavePredictions(coPurchasePredictions: currentCoPurchasePredictions);
        }

        /// <summary>
        /// Gets all <see cref="T:Epi.Libraries.Commerce.Predictions.Models.IProductCoPurchasePrediction" /> with the specified <param name="productId"></param>.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Epi.Libraries.Commerce.Predictions.Models.IProductCoPurchasePrediction" />.</returns>
        public IEnumerable<IProductCoPurchasePrediction> Get(int productId)
        {
            return this.Predictions.Where(cp => cp.ProductId == productId);
        }

        /// <summary>
        /// Gets the combined <see cref="T:Epi.Libraries.Commerce.Predictions.Models.IProductCoPurchasePrediction" /> for the specified <param name="productIds"></param>.
        /// </summary>
        /// <param name="productIds">The product ids.</param>
        /// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Epi.Libraries.Commerce.Predictions.Models.IProductCoPurchasePrediction" />.</returns>
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