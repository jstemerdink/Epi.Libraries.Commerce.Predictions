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

namespace Epi.Libraries.Commerce.Predictions.EF
{
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;

    using Epi.Libraries.Commerce.Predictions.Core;
    using Epi.Libraries.Commerce.Predictions.Core.Models;
    using Epi.Libraries.Commerce.Predictions.EF.Models;

    /// <summary>
    /// Class RecommendationRepository.
    /// Implements the <see cref="IRecommendationRepository" />
    /// </summary>
    /// <seealso cref="IRecommendationRepository" />
    public class RecommendationRepository : IRecommendationRepository
    {
        /// <summary>
        /// The productCoPurchasePredictions
        /// </summary>
        private IEnumerable<ProductCoPurchasePrediction> predictions;

        /// <summary>
        /// Gets the predictions.
        /// </summary>
        /// <value>The predictions.</value>
        public IEnumerable<IProductCoPurchasePrediction> Predictions
        {
            get
            {
                return this.predictions ?? (this.predictions = this.GetAllPredictions());
            }
        }

        /// <summary>
        /// Adds or updates <see cref="IProductCoPurchasePrediction"/> in the repository.
        /// </summary>
        /// <param name="productCoPurchasePredictions">The productCoPurchasePredictions.</param>
        /// <exception cref="T:System.Data.Entity.Infrastructure.DbUpdateException">An error occurred sending updates to the database.</exception>
        /// <exception cref="T:System.Data.Entity.Infrastructure.DbUpdateConcurrencyException">A database command did not affect the expected number of rows. This usually indicates an optimistic 
        ///             concurrency violation; that is, a row has been changed in the database since it was queried.</exception>
        /// <exception cref="T:System.Data.Entity.Validation.DbEntityValidationException">The save was aborted because validation of entity property values failed.</exception>
        public virtual void AddOrUpdate(IEnumerable<IProductCoPurchasePrediction> productCoPurchasePredictions)
        {
            this.RemoveAllFromContext();

            PredictionDataContext context = null;

            try
            {
                context = new PredictionDataContext();
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;

                int count = 0;
                foreach (ProductCoPurchasePrediction entityToInsert in productCoPurchasePredictions
                    .Cast<ProductCoPurchasePrediction>())
                {
                    if (entityToInsert == null)
                    {
                        continue;
                    }

                    ++count;

                    context.Set<ProductCoPurchasePrediction>().Add(entity: entityToInsert);

                    if (count % 100 == 0)
                    {
                        context.SaveChanges();
                    }
                }

                context.SaveChanges();
            }
            finally
            {
                if (context != null)
                {
                    context.Dispose();
                }
            }

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
        /// <exception cref="T:System.Data.Entity.Infrastructure.DbUpdateException">An error occurred sending updates to the database.</exception>
        /// <exception cref="T:System.Data.Entity.Infrastructure.DbUpdateConcurrencyException">A database command did not affect the expected number of rows. This usually indicates an optimistic 
        ///             concurrency violation; that is, a row has been changed in the database since it was queried.</exception>
        /// <exception cref="T:System.Data.Entity.Validation.DbEntityValidationException">The save was aborted because validation of entity property values failed.</exception>
        public virtual void Delete(int productId)
        {
            List<ProductCoPurchasePrediction> existingCoPurchasePredictions = new List<ProductCoPurchasePrediction>();

            PredictionDataContext context = null;

            try
            {
                context = new PredictionDataContext();
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;

                existingCoPurchasePredictions.AddRange(
                    context.ProductCoPurchasePredictions.Where(p => p.ProductId.Equals(productId)));

                existingCoPurchasePredictions.AddRange(
                    context.ProductCoPurchasePredictions.Where(p => p.CoPurchaseProductId.Equals(productId)));

                context.ProductCoPurchasePredictions.RemoveRange(entities: existingCoPurchasePredictions);

                context.SaveChanges();
            }
            finally
            {
                if (context != null)
                {
                    context.Dispose();
                }
            }

            this.predictions = null;
        }

        /// <summary>
        /// Gets all <see cref="ProductCoPurchasePrediction"/> with the specified <param name="productId"></param>.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ProductCoPurchasePrediction"/>.</returns>
        public virtual IEnumerable<IProductCoPurchasePrediction> Get(int productId)
        {
            return this.predictions.Where(p => p.ID.Equals(obj: productId));
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

        /// <summary>
        /// Gets all predictions form the DB.
        /// </summary>
        /// <returns>A <see cref="DbSet{TEntity}"/> of <see cref="ProductCoPurchasePrediction"/>.</returns>
        private DbSet<ProductCoPurchasePrediction> GetAllPredictions()
        {
            PredictionDataContext context = null;

            try
            {
                context = new PredictionDataContext();
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;

                return context.ProductCoPurchasePredictions;
            }
            finally
            {
                if (context != null)
                {
                    context.Dispose();
                }
            }
        }

        /// <summary>
        /// Removes all entities from the DB.
        /// </summary>
        private void RemoveAllFromContext()
        {
            PredictionDataContext context = null;

            try
            {
                context = new PredictionDataContext();
                context.Database.ExecuteSqlCommand($"TRUNCATE TABLE [{PredictionDataContext.ProductCoPurchasePredictionsDatabaseTableName}]");
                context.Dispose();
            }
            finally
            {
                if (context != null)
                {
                    context.Dispose();
                }
            }
        }
    }
}