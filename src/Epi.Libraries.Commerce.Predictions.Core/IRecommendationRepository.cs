// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IRecommendationRepository.cs" company="Jeroen Stemerdink">
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
    using System.Collections.Generic;

    using Epi.Libraries.Commerce.Predictions.Core.Models;

    public interface IRecommendationRepository
    {
        /// <summary>
        /// Adds or updates <see cref="IProductCoPurchasePrediction"/> in the repository.
        /// </summary>
        /// <param name="predictions">The productCoPurchasePredictions.</param>
        void AddOrUpdate(IEnumerable<IProductCoPurchasePrediction> predictions);

        /// <summary>
        /// Creates a new prediction.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="coPurchaseProductId">The co purchase product identifier.</param>
        /// <param name="score">The score.</param>
        /// <returns>An instance of <see cref="IProductCoPurchasePrediction"/>.</returns>
        IProductCoPurchasePrediction Create(int productId, int coPurchaseProductId, float score);

        /// <summary>
        /// Deletes the <see cref="IProductCoPurchasePrediction" /> with the specified <param name="productId"></param>.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        void Delete(int productId);

        /// <summary>
        /// Gets all <see cref="IProductCoPurchasePrediction"/> with the specified <param name="productId"></param>.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IProductCoPurchasePrediction"/>.</returns>
        IEnumerable<IProductCoPurchasePrediction> Get(int productId);

        /// <summary>
        /// Gets the combined <see cref="IProductCoPurchasePrediction"/> for the specified <param name="productIds"></param>.
        /// </summary>
        /// <param name="productIds">The product ids.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IProductCoPurchasePrediction"/>.</returns>
        IEnumerable<IProductCoPurchasePrediction> Get(IEnumerable<int> productIds);
    }
}