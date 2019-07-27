// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPredictionEngineService.cs" company="Jeroen Stemerdink">
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

    using EPiServer.Commerce.Order;
    using EPiServer.Core;

    public interface IPredictionEngineService
    {
        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="contentReference">The content reference.</param>
        /// <returns>A list of <see cref="ContentReference"/> with recommendations for a variation.</returns>
        IEnumerable<ContentReference> GetRecommendations(ContentReference contentReference);

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="contentReference">The content reference.</param>
        /// <param name="amount">The amount of items to return.</param>
        /// <returns>A list of <see cref="ContentReference"/> with recommendations for a variation.</returns>
        IEnumerable<ContentReference> GetRecommendations(ContentReference contentReference, int amount);

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="contentReferences">The content references.</param>
        /// <returns>A list of <see cref="ContentReference"/> with recommendations for list of variations.</returns>
        IEnumerable<ContentReference> GetRecommendations(IEnumerable<ContentReference> contentReferences);

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <returns>A list of <see cref="ContentReference" /> with recommendations for list of variations.</returns>
        IEnumerable<ContentReference> GetRecommendations(ICart cart);

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <param name="amount">The amount of items to return.</param>
        /// <returns>A list of <see cref="ContentReference" /> with recommendations for list of variations.</returns>
        IEnumerable<ContentReference> GetRecommendations(ICart cart, int amount);

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="contentReferences">The content references.</param>
        /// <param name="amount">The amount of items to return.</param>
        /// <returns>A list of <see cref="ContentReference"/> with recommendations for list of variations.</returns>
        IEnumerable<ContentReference> GetRecommendations(IEnumerable<ContentReference> contentReferences, int amount);

        /// <summary>
        /// Gets up sell items.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <returns>A list of <see cref="ContentReference"/> with the best scoring up-sell items.</returns>
        List<ContentReference> GetUpSellItems(ICart cart);

        /// <summary>
        /// Gets up sell items.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <param name="amount">The amount of items to return.</param>
        /// <returns>A list of <see cref="ContentReference"/> with the best scoring up-sell items.</returns>
        List<ContentReference> GetUpSellItems(ICart cart, int amount);

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        void Init();

        /////// <summary>
        /////// Updates the model.
        /////// </summary>
        ////void UpdateModel();
    }
}