// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PredictionEngineService.cs" company="Jeroen Stemerdink">
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
    using System.Linq;

    using Epi.Libraries.Commerce.Predictions.Core.Models;

    using EPiServer.Commerce.Catalog.Linking;
    using EPiServer.Commerce.Order;
    using EPiServer.Core;
    using EPiServer.Logging;

    /// <summary>
    /// Class PredictionEngineService.
    /// </summary>
    public class PredictionEngineService : IPredictionEngineService
    {
        /// <summary>
        /// The association repository
        /// </summary>
        private readonly IAssociationRepository associationRepository;

        /// <summary>
        /// The co purchase prediction repository
        /// </summary>
        private readonly IRecommendationRepository recommendationRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictionEngineService" /> class.
        /// </summary>
        /// <param name="associationRepository">The association repository.</param>
        /// <param name="recommendationRepository">The recommendation repository.</param>
        public PredictionEngineService(
            IAssociationRepository associationRepository,
            IRecommendationRepository recommendationRepository)
        {
            this.associationRepository = associationRepository;
            this.recommendationRepository = recommendationRepository;
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="contentReference">The content reference.</param>
        /// <returns>A list of <see cref="ContentReference"/> with recommendations for a variation.</returns>
        public IEnumerable<ContentReference> GetRecommendations(ContentReference contentReference)
        {
            return this.GetRecommendations(contentReference: contentReference, 3);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="contentReference">The content reference.</param>
        /// <param name="amount">The amount of items to return.</param>
        /// <returns>A list of <see cref="ContentReference"/> with recommendations for a variation.</returns>
        public IEnumerable<ContentReference> GetRecommendations(ContentReference contentReference, int amount)
        {
            return this.recommendationRepository.GetFromCache(productId: contentReference.ID).OrderByDescending(p => p.Score)
                .Take(count: amount).Select(p => new ContentReference(contentID: p.CoPurchaseProductId));
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="contentReferences">The content references.</param>
        /// <returns>A list of <see cref="ContentReference"/> with recommendations for list of variations.</returns>
        public IEnumerable<ContentReference> GetRecommendations(IEnumerable<ContentReference> contentReferences)
        {
            return this.GetRecommendations(contentReferences: contentReferences, 3);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <param name="amount">The amount of items to return.</param>
        /// <returns>A list of <see cref="ContentReference" /> with recommendations for list of variations.</returns>
        public IEnumerable<ContentReference> GetRecommendations(ICart cart, int amount)
        {
            IEnumerable<ContentReference> contentReferences = GetContentReferences(cart: cart);
            return this.GetRecommendations(contentReferences: contentReferences, amount: amount);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <returns>A list of <see cref="ContentReference" /> with recommendations for list of variations.</returns>
        public IEnumerable<ContentReference> GetRecommendations(ICart cart)
        {
            IEnumerable<ContentReference> contentReferences = GetContentReferences(cart: cart);
            return this.GetRecommendations(contentReferences: contentReferences, 3);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="contentReferences">The content references.</param>
        /// <param name="amount">The amount of items to return.</param>
        /// <returns>A list of <see cref="ContentReference"/> with recommendations for list of variations.</returns>
        public IEnumerable<ContentReference> GetRecommendations(
            IEnumerable<ContentReference> contentReferences,
            int amount)
        {
            List<IProductCoPurchasePrediction> combinedRecommendations =
                this.recommendationRepository.GetFromCache(contentReferences.Select(r => r.ID)).ToList();

            return combinedRecommendations.OrderByDescending(p => p.Score).Take(count: amount)
                .Select(p => new ContentReference(contentID: p.CoPurchaseProductId));
        }

        /// <summary>
        /// Gets up sell items.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <returns>A list of <see cref="ContentReference"/> with the best scoring up-sell items.</returns>
        public List<ContentReference> GetUpSellItems(ICart cart)
        {
            return this.GetUpSellItems(cart: cart, 3);
        }

        /// <summary>
        /// Gets up sell items.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <param name="amount">The amount of items to return.</param>
        /// <returns>A list of <see cref="ContentReference"/> with the best scoring up-sell items.</returns>
        public List<ContentReference> GetUpSellItems(ICart cart, int amount)
        {
            IEnumerable<ContentReference> contentReferences = GetContentReferences(cart: cart);

            List<ContentReference> upsellItems = new List<ContentReference>();

            foreach (ContentReference contentReference in contentReferences)
            {
                IEnumerable<Association> associations = this.ListAssociations(referenceToEntry: contentReference);

                upsellItems.AddRange(associations.Select(association => association.Source));
            }

            List<IProductCoPurchasePrediction> combinedRecommendations =
                this.recommendationRepository.GetFromCache(upsellItems.Select(r => r.ID)).ToList();

            return combinedRecommendations.OrderByDescending(i => i.Score).Take(count: amount)
                .Select(p => new ContentReference(contentID: p.CoPurchaseProductId)).ToList();
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Init()
        {
        }

        /// <summary>
        /// Gets the content references to items in the cart.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ContentReference"/>.</returns>
        private static IEnumerable<ContentReference> GetContentReferences(ICart cart)
        {
            IEnumerable<ILineItem> lineItems = cart.GetFirstForm().GetAllLineItems();

            return lineItems.Select(lineItem => lineItem.GetEntryContent()).Select(entry => entry.ContentLink).ToList();
        }

        /// <summary>
        /// Lists the associations.
        /// </summary>
        /// <param name="referenceToEntry">The reference to entry.</param>
        /// <returns>A list of <see cref="Association"/> for the entry.</returns>
        private IEnumerable<Association> ListAssociations(ContentReference referenceToEntry)
        {
            IEnumerable<Association> associations =
                this.associationRepository.GetAssociations(contentLink: referenceToEntry);
            return associations;
        }
    }
}