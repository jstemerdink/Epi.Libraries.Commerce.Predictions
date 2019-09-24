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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Epi.Libraries.Commerce.Predictions.Core.Models;

    using EPiServer;
    using EPiServer.Commerce.Catalog.ContentTypes;
    using EPiServer.Commerce.Catalog.Linking;
    using EPiServer.Commerce.Order;
    using EPiServer.Core;
    using EPiServer.Filters;
    using EPiServer.Globalization;
    using EPiServer.Security;

    using Mediachase.Commerce;
    using Mediachase.Commerce.Catalog;
    using Mediachase.Commerce.Security;

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
        /// The content loader
        /// </summary>
        private readonly IContentLoader contentLoader;

        /// <summary>
        /// The current market
        /// </summary>
        private readonly ICurrentMarket currentMarket;

        /// <summary>
        /// The published filter
        /// </summary>
        private readonly FilterPublished filterPublished;

        /// <summary>
        /// The language resolver
        /// </summary>
        private readonly LanguageResolver languageResolver;

        /// <summary>
        /// The order repository
        /// </summary>
        private readonly IOrderRepository orderRepository;

        /// <summary>
        /// The co-purchase prediction repository
        /// </summary>
        private readonly IRecommendationRepository recommendationRepository;

        /// <summary>
        /// The reference converter
        /// </summary>
        private readonly ReferenceConverter referenceConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictionEngineService" /> class.
        /// </summary>
        /// <param name="associationRepository">The association repository.</param>
        /// <param name="recommendationRepository">The recommendation repository.</param>
        /// <param name="referenceConverter">The reference converter.</param>
        /// <param name="orderRepository">The order repository.</param>
        /// <param name="currentMarket">The current market.</param>
        /// <param name="filterPublished">The filter published.</param>
        /// <param name="contentLoader">The content loader.</param>
        /// <param name="languageResolver">The language resolver.</param>
        public PredictionEngineService(
            IAssociationRepository associationRepository,
            IRecommendationRepository recommendationRepository,
            ReferenceConverter referenceConverter,
            IOrderRepository orderRepository,
            ICurrentMarket currentMarket,
            FilterPublished filterPublished,
            IContentLoader contentLoader,
            LanguageResolver languageResolver)
        {
            this.associationRepository = associationRepository;
            this.recommendationRepository = recommendationRepository;
            this.referenceConverter = referenceConverter;
            this.orderRepository = orderRepository;
            this.currentMarket = currentMarket;
            this.filterPublished = filterPublished;
            this.contentLoader = contentLoader;
            this.languageResolver = languageResolver;
        }

        /// <summary>
        /// Gets the personalized recommendations.
        /// </summary>
        /// <param name="contentReference">The content reference.</param>
        /// <returns>A list of <see cref="ContentReference" /> with 3 personalized recommendations for a variation.</returns>
        public virtual IEnumerable<ContentReference> GetPersonalizedRecommendations(ContentReference contentReference)
        {
            return this.GetPersonalizedRecommendations(contentReference: contentReference, 3);
        }

        /// <summary>
        /// Gets the personalized recommendations.
        /// </summary>
        /// <param name="contentReference">The content reference.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>A list of <see cref="ContentReference" /> with personalized recommendations for a variation.</returns>
        public virtual IEnumerable<ContentReference> GetPersonalizedRecommendations(
            ContentReference contentReference,
            int amount)
        {
            int currentProductId = this.referenceConverter.GetObjectId(contentLink: contentReference);

            // Initialize with the current product
            List<int> productIdList = new List<int> { currentProductId };

            productIdList.AddRange(this.GetPersonalProductIds());

            IEnumerable<IProductCoPurchasePrediction> filteredRecommendations = this.recommendationRepository
                .Get(productIds: productIdList).Where(
                    productCoPurchasePrediction =>
                        !productIdList.Contains(item: productCoPurchasePrediction.CoPurchaseProductId));

            return filteredRecommendations.OrderByDescending(p => p.Score).Take(count: amount).Select(
                p => this.referenceConverter.GetEntryContentLink(objectId: p.CoPurchaseProductId));
        }

        /// <summary>
        /// Gets the personalized recommendations.
        /// </summary>
        /// <typeparam name="T">The type of variation to get.</typeparam>
        /// <param name="contentReference">The content reference.</param>
        /// <returns>A list of <see cref="VariationContent" /> with personalized recommendations for a variation.</returns>
        public virtual IEnumerable<T> GetPersonalizedRecommendations<T>(ContentReference contentReference)
            where T : VariationContent
        {
            return this.GetPersonalizedRecommendations<T>(contentReference: contentReference, 3);
        }

        /// <summary>
        /// Gets the personalized recommendations.
        /// </summary>
        /// <typeparam name="T">The type of variation to get.</typeparam>
        /// <param name="contentReference">The content reference.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>A list of <see cref="VariationContent" /> with personalized recommendations for a variation.</returns>
        public virtual IEnumerable<T> GetPersonalizedRecommendations<T>(ContentReference contentReference, int amount)
            where T : VariationContent
        {
            IEnumerable<ContentReference> contentLinks = this.GetPersonalizedRecommendations(
                contentReference: contentReference,
                amount: int.MaxValue);

            return this.GetContents<T>(contentLinks: contentLinks, amount: amount);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="contentReference">The content reference.</param>
        /// <returns>A list of <see cref="ContentReference"/> with recommendations for a variation.</returns>
        public virtual IEnumerable<ContentReference> GetRecommendations(ContentReference contentReference)
        {
            return this.GetRecommendations(new List<ContentReference> { contentReference }, 3);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="contentReference">The content reference.</param>
        /// <param name="amount">The amount of items to return.</param>
        /// <returns>A list of <see cref="ContentReference"/> with recommendations for a variation.</returns>
        public virtual IEnumerable<ContentReference> GetRecommendations(ContentReference contentReference, int amount)
        {
            return this.GetRecommendations(new List<ContentReference> { contentReference }, amount: amount);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <typeparam name="T">The type of variation to get.</typeparam>
        /// <param name="contentReference">The content reference.</param>
        /// <returns>A list of <see cref="VariationContent" /> with recommendations for a variation.</returns>
        public virtual IEnumerable<T> GetRecommendations<T>(ContentReference contentReference)
            where T : VariationContent
        {
            return this.GetRecommendations<T>(contentReference: contentReference, 3);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <typeparam name="T">The type of variation to get.</typeparam>
        /// <param name="contentReference">The content reference.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>A list of <see cref="VariationContent" /> with recommendations for a variation.</returns>
        public virtual IEnumerable<T> GetRecommendations<T>(ContentReference contentReference, int amount)
            where T : VariationContent
        {
            IEnumerable<ContentReference> contentLinks = this.GetRecommendations(
                contentReference: contentReference,
                amount: int.MaxValue);

            return this.GetContents<T>(contentLinks: contentLinks, amount: amount);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <returns>A list of <see cref="ContentReference" /> with recommendations for list of variations.</returns>
        public virtual IEnumerable<ContentReference> GetRecommendations(ICart cart)
        {
            IEnumerable<ContentReference> contentReferences = GetContentReferences(cart: cart);
            return this.GetRecommendations(contentReferences: contentReferences, 3);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <param name="amount">The amount of items to return.</param>
        /// <returns>A list of <see cref="ContentReference" /> with recommendations for list of variations.</returns>
        public virtual IEnumerable<ContentReference> GetRecommendations(ICart cart, int amount)
        {
            IEnumerable<ContentReference> contentReferences = GetContentReferences(cart: cart);
            return this.GetRecommendations(contentReferences: contentReferences, amount: amount);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <typeparam name="T">The type of variation to get.</typeparam>
        /// <param name="cart">The cart.</param>
        /// <returns>A list of <see cref="VariationContent" /> with recommendations for a variation.</returns>
        public virtual IEnumerable<T> GetRecommendations<T>(ICart cart)
            where T : VariationContent
        {
            return this.GetRecommendations<T>(cart: cart, 3);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <typeparam name="T">The type of variation to get.</typeparam>
        /// <param name="cart">The cart.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>A list of <see cref="VariationContent" /> with recommendations for a variation.</returns>
        public virtual IEnumerable<T> GetRecommendations<T>(ICart cart, int amount)
            where T : VariationContent
        {
            IEnumerable<ContentReference> contentLinks = this.GetRecommendations(cart: cart, amount: int.MaxValue);

            return this.GetContents<T>(contentLinks: contentLinks, amount: amount);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="contentReferences">The content references.</param>
        /// <returns>A list of <see cref="ContentReference"/> with recommendations for list of variations.</returns>
        public virtual IEnumerable<ContentReference> GetRecommendations(IEnumerable<ContentReference> contentReferences)
        {
            return this.GetRecommendations(contentReferences: contentReferences, 3);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <param name="contentReferences">The content references.</param>
        /// <param name="amount">The amount of items to return.</param>
        /// <returns>A list of <see cref="ContentReference"/> with recommendations for list of variations.</returns>
        public virtual IEnumerable<ContentReference> GetRecommendations(
            IEnumerable<ContentReference> contentReferences,
            int amount)
        {
            List<IProductCoPurchasePrediction> combinedRecommendations = this.recommendationRepository
                .Get(contentReferences.Select(r => this.referenceConverter.GetObjectId(contentLink: r))).ToList();

            return combinedRecommendations.OrderByDescending(p => p.Score).Take(count: amount).Select(
                p => this.referenceConverter.GetEntryContentLink(objectId: p.CoPurchaseProductId));
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <typeparam name="T">The type of variation to get.</typeparam>
        /// <param name="contentReferences">The content references.</param>
        /// <returns>A list of <see cref="VariationContent" /> with recommendations for a variation.</returns>
        public virtual IEnumerable<T> GetRecommendations<T>(IEnumerable<ContentReference> contentReferences)
            where T : VariationContent
        {
            return this.GetRecommendations<T>(contentReferences: contentReferences, 3);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <typeparam name="T">The type of variation to get.</typeparam>
        /// <param name="contentReferences">The content references.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>A list of <see cref="VariationContent" /> with recommendations for a variation.</returns>
        public virtual IEnumerable<T> GetRecommendations<T>(IEnumerable<ContentReference> contentReferences, int amount)
            where T : VariationContent
        {
            IEnumerable<ContentReference> contentLinks = this.GetRecommendations(
                contentReferences: contentReferences,
                amount: int.MaxValue);

            return this.GetContents<T>(contentLinks: contentLinks, amount: amount);
        }

        /// <summary>
        /// Gets up sell items.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <returns>A list of <see cref="ContentReference"/> with the best scoring up-sell items.</returns>
        public virtual IEnumerable<ContentReference> GetUpSellItems(ICart cart)
        {
            return this.GetUpSellItems(cart: cart, 3);
        }

        /// <summary>
        /// Gets up sell items.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <param name="amount">The amount of items to return.</param>
        /// <returns>A list of <see cref="ContentReference"/> with the best scoring up-sell items.</returns>
        public virtual IEnumerable<ContentReference> GetUpSellItems(ICart cart, int amount)
        {
            List<ContentReference> contentReferences = GetContentReferences(cart: cart).ToList();

            List<ContentReference> upsellItems = new List<ContentReference>();

            foreach (IEnumerable<Association> associations in contentReferences.Select(selector: this.ListAssociations))
            {
                upsellItems.AddRange(associations.Select(association => association.Source));
            }

            return this.GetRecommendations(!upsellItems.Any() ? contentReferences : upsellItems, amount: amount);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <typeparam name="T">The type of variation to get.</typeparam>
        /// <param name="cart">The cart.</param>
        /// <returns>A list of <see cref="VariationContent" /> with recommendations for a variation.</returns>
        public virtual IEnumerable<T> GetUpSellItems<T>(ICart cart)
            where T : VariationContent
        {
            return this.GetUpSellItems<T>(cart: cart, 3);
        }

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <typeparam name="T">The type of variation to get.</typeparam>
        /// <param name="cart">The cart.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>A list of <see cref="VariationContent" /> with recommendations for a variation.</returns>
        public virtual IEnumerable<T> GetUpSellItems<T>(ICart cart, int amount)
            where T : VariationContent
        {
            IEnumerable<ContentReference> contentLinks = this.GetUpSellItems(cart: cart, amount: int.MaxValue);

            return this.GetContents<T>(contentLinks: contentLinks, amount: amount);
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public virtual void Init()
        {
        }

        /// <summary>
        /// Gets the content references to items in the cart.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ContentReference"/>.</returns>
        private static IEnumerable<ContentReference> GetContentReferences(ICart cart)
        {
            IEnumerable<ILineItem> lineItems = cart.GetAllLineItems();

            return lineItems.Select(lineItem => lineItem.GetEntryContent()).Select(entry => entry.ContentLink).ToList();
        }

        /// <summary>
        /// Gets the contents.
        /// </summary>
        /// <typeparam name="T">The type of content.</typeparam>
        /// <param name="contentLinks">The content links.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="VariationContent"/>.</returns>
        private IEnumerable<T> GetContents<T>(IEnumerable<ContentReference> contentLinks, int amount)
            where T : VariationContent
        {
            return this.contentLoader.GetItems(contentLinks: contentLinks, this.languageResolver.GetPreferredCulture())
                .OfType<T>()
                .Where(
                    v => v.IsAvailableInCurrentMarket(currentMarketService: this.currentMarket)
                         && !this.filterPublished.ShouldFilter(content: v)).Take(count: amount);
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

        private IEnumerable<int> GetPersonalProductIds()
        {
            List<int> productIdList = new List<int>();

            Guid contactId = PrincipalInfo.CurrentPrincipal.GetContactId();

            List<IPurchaseOrder> purchaseOrders =
                this.orderRepository.Load<IPurchaseOrder>(customerId: contactId).ToList();

            productIdList.AddRange(this.GetLineItemIds(orderGroups: purchaseOrders));

            if (productIdList.Any())
            {
                return productIdList.Distinct();
            }

            // If there are no orders yet, look in cart and wish-lists
            List<ICart> carts = this.orderRepository.Load<ICart>(customerId: contactId).ToList();

            productIdList.AddRange(this.GetLineItemIds(orderGroups: carts));

            return productIdList.Distinct();
        }

        private IEnumerable<int> GetLineItemIds(IEnumerable<IOrderGroup> orderGroups)
        {
            List<int> idList = new List<int>();

            foreach (List<ILineItem> lineItems in orderGroups
                .Select(orderGroup => orderGroup.GetAllLineItems().ToList())
                .Where(lineItems => lineItems.Count > 1))
            {
                idList.AddRange(
                    from lineItem in lineItems
                    select lineItem.GetEntryContent()
                    into content
                    where content != null
                    select this.referenceConverter.GetObjectId(contentLink: content.ContentLink));
            }

            return idList;
        }
    }
}