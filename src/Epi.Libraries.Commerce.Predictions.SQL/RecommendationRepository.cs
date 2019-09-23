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

namespace Epi.Libraries.Commerce.Predictions.SQL
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.IO;
    using System.Linq;

    using Epi.Libraries.Commerce.Predictions.Core;
    using Epi.Libraries.Commerce.Predictions.Core.Models;
    using Epi.Libraries.Commerce.Predictions.SQL.Models;

    using EPiServer.Data;
    using EPiServer.Framework.Cache;
    using EPiServer.Logging;
    using EPiServer.ServiceLocation;

    using ILogger = EPiServer.Logging.ILogger;

    /// <summary>
    /// Class RecommendationRepository.
    /// Implements the <see cref="IRecommendationRepository" />
    /// </summary>
    /// <seealso cref="IRecommendationRepository" />
    public class RecommendationRepository : IRecommendationRepository
    {
        private const string PredictionsTable = "[dbo].[tblProductCoPurchasePredictions]";

        private const string PredictionsCacheKey = "ProductCoPurchasePredictions";

        private static readonly ILogger Logger = LogManager.GetLogger();

        private readonly ServiceAccessor<IDatabaseExecutor> databaseExecutor;

        private readonly ISynchronizedObjectInstanceCache synchronizedObjectInstanceCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecommendationRepository"/> class.
        /// </summary>
        /// <param name="databaseExecutor">The database executor.</param>
        /// <param name="synchronizedObjectInstanceCache">The synchronized object instance cache.</param>
        public RecommendationRepository(ServiceAccessor<IDatabaseExecutor> databaseExecutor, ISynchronizedObjectInstanceCache synchronizedObjectInstanceCache)
        {
            this.databaseExecutor = databaseExecutor;
            this.synchronizedObjectInstanceCache = synchronizedObjectInstanceCache;
        }

        /// <summary>
        /// Gets the predictions.
        /// </summary>
        /// <value>The predictions.</value>
        public IEnumerable<IProductCoPurchasePrediction> Predictions
        {
            get
            {
                IEnumerable<ProductCoPurchasePrediction> productCoPurchasePredictions =
                    this.synchronizedObjectInstanceCache.Get(PredictionsCacheKey) as IEnumerable<ProductCoPurchasePrediction>;

                if (productCoPurchasePredictions != null)
                {
                    return productCoPurchasePredictions;
                }

                productCoPurchasePredictions = this.GetAll();

                this.synchronizedObjectInstanceCache.Insert(key: PredictionsCacheKey, value: productCoPurchasePredictions, evictionPolicy: CacheEvictionPolicy.Empty);

                return productCoPurchasePredictions;
            }
        }

        /// <summary>
        /// Adds or updates <see cref="IProductCoPurchasePrediction"/> in the repository.
        /// </summary>
        /// <param name="productCoPurchasePredictions">The productCoPurchasePredictions.</param>
        public virtual void AddOrUpdate(IEnumerable<IProductCoPurchasePrediction> productCoPurchasePredictions)
        {
            foreach (IProductCoPurchasePrediction productCoPurchasePrediction in productCoPurchasePredictions
                 .Cast<ProductCoPurchasePrediction>())
            {
                try
                {
                    this.AddOrUpdate(productCoPurchasePrediction);
                }
                catch (Exception e)
                {
                    Logger.Error($"[Prediction Engine] {e.Message} for product with ID '{productCoPurchasePrediction.ProductId}' and co-purchase ID '{productCoPurchasePrediction.CoPurchaseProductId}' ", e);
                }
            }

            this.synchronizedObjectInstanceCache.Remove(key: PredictionsCacheKey);
        }

        /// <summary>
        /// Adds or updates <see cref="IProductCoPurchasePrediction" /> in the repository.
        /// </summary>
        /// <param name="productCoPurchasePrediction">The ProductCoPurchasePrediction.</param>
        public virtual void AddOrUpdate(IProductCoPurchasePrediction productCoPurchasePrediction)
        {
            string sqlCommand =
                $@"UPDATE {PredictionsTable} SET [Score] =  @score WHERE [ProductId] = @productId AND [CoPurchaseProductId] = @coPurchaseProductId
                   IF @@ROWCOUNT = 0
                   INSERT INTO {PredictionsTable} ([ProductId], [CoPurchaseProductId], [Score]) VALUES (@productId, @coPurchaseProductId, @score)";

            this.ExecuteNonQuery(
                () => this.CreateCommand(
                    sqlCommand: sqlCommand,
                    this.CreateIntParameter("productId", value: productCoPurchasePrediction.ProductId),
                    this.CreateIntParameter("coPurchaseProductId", value: productCoPurchasePrediction.CoPurchaseProductId),
                    this.CreateFloatParameter("score", value: productCoPurchasePrediction.Score)),
                "An error occurred while updating or creating a prediction.");
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
                ProductId = productId,
                CoPurchaseProductId = coPurchaseProductId,
                Score = score
            };
        }

        /// <summary>
        /// Deletes the <see cref="ProductCoPurchasePrediction" /> with the specified <param name="productId"></param>.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        public virtual void Delete(int productId)
        {
            string sqlCommand =
                $@"DELETE FROM {PredictionsTable} WHERE [ProductId] = @id OR [CoPurchaseProductId] = @id";

            this.ExecuteNonQuery(
                () => this.CreateCommand(sqlCommand: sqlCommand, this.CreateIntParameter("id", value: productId)),
                "An error occurred while deleting predictions.");

            this.synchronizedObjectInstanceCache.Remove(key: PredictionsCacheKey);
        }

        public virtual void DeleteAll()
        {
            string sqlCommand =
                $@"TRUNCATE TABLE {PredictionsTable}";

            this.ExecuteNonQuery(
                () => this.CreateCommand(sqlCommand: sqlCommand),
                "An error occurred while deleting predictions.");

            this.synchronizedObjectInstanceCache.Remove(key: PredictionsCacheKey);
        }

        public void StoreModel()
        {
        }

        public Stream LoadModel()
        {
            return Stream.Null;
        }

        /// <summary>
        /// Gets all <see cref="ProductCoPurchasePrediction"/> with the specified <param name="productId"></param>.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ProductCoPurchasePrediction"/>.</returns>
        public virtual IEnumerable<IProductCoPurchasePrediction> Get(int productId)
        {
            string sqlCommand =
                $@"SELECT [ProductId], [CoPurchaseProductId], [Score] FROM {PredictionsTable} WHERE ProductId = @productId";

            return this.ExecuteQuery(() => this.CreateCommand(sqlCommand: sqlCommand, this.CreateIntParameter("productId", value: productId)));
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

        public void Initialize()
        {
            string sqlCommand =
                                    $@"IF OBJECT_ID(N'{PredictionsTable}', N'U') IS NULL
                                    BEGIN
                                        CREATE TABLE {PredictionsTable}(
	                                    [CoPurchaseProductId] [int] NOT NULL,
	                                    [ProductId] [int] NOT NULL,
	                                    [Score] [real] NOT NULL,
                                     CONSTRAINT [PK_Prediction] PRIMARY KEY CLUSTERED
                                    ( 
	                                    [ProductId] ASC,
	                                    [CoPurchaseProductId] ASC
                                    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                                    ) ON [PRIMARY]
                                    END";

            this.ExecuteNonQuery(
                () => this.CreateCommand(sqlCommand: sqlCommand),
                "An error occurred while creating predictions table.");
        }

        private static ProductCoPurchasePrediction ToProductCoPurchasePrediction(DataRow x)
        {
            return new ProductCoPurchasePrediction
            {
                ProductId = x.Field<int>("ProductId"),
                CoPurchaseProductId = x.Field<int>("CoPurchaseProductId"),
                Score = x.Field<float>("Score")
            };
        }

        private DbParameter CreateBoolParameter(string name, bool value)
        {
            IDatabaseExecutor db = this.databaseExecutor();

            return db.CreateParameter(
                name: name,
                type: DbType.Boolean,
                direction: ParameterDirection.Input,
                value: value);
        }

        private DbCommand CreateCommand(string sqlCommand, params DbParameter[] parameters)
        {
            IDatabaseExecutor db = this.databaseExecutor();

            DbCommand command = db.CreateCommand();

            foreach (DbParameter parameter in parameters)
            {
                command.Parameters.Add(value: parameter);
            }

            command.CommandText = sqlCommand;
            command.CommandType = CommandType.Text;
            return command;
        }

        private DbParameter CreateFloatParameter(string name, float value)
        {
            IDatabaseExecutor db = this.databaseExecutor();

            return db.CreateParameter(
                name: name,
                type: DbType.Double,
                direction: ParameterDirection.Input,
                value: value);
        }

        private DbParameter CreateIntParameter(string name, int value)
        {
            IDatabaseExecutor db = this.databaseExecutor();

            return db.CreateParameter(
                name: name,
                type: DbType.Int32,
                direction: ParameterDirection.Input,
                value: value);
        }

        private DataTable ExecuteDataTableQuery(DbCommand command)
        {
            IDatabaseExecutor db = this.databaseExecutor();

            DbDataAdapter adapter = db.DbFactory.CreateDataAdapter();

            if (adapter == null)
            {
                throw new Exception("[Prediction Engine] Unable to create DbDataAdapter");
            }

            adapter.SelectCommand = command;
            DataSet ds = new DataSet();
            adapter.Fill(dataSet: ds);
            return ds.Tables[0];
        }

        private IEnumerable<ProductCoPurchasePrediction> ExecuteEnumerableQuery(DbCommand command)
        {
            DataTable table = this.ExecuteDataTableQuery(command: command);

            return table.AsEnumerable().Select(selector: ToProductCoPurchasePrediction);
        }

        private void ExecuteNonQuery(Func<DbCommand> createCommand, string errorMessage)
        {
            IDatabaseExecutor db = this.databaseExecutor();

            db.Execute(
                () =>
                {
                    try
                    {
                        createCommand().ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(message: errorMessage, exception: ex);
                        throw;
                    }
                });
        }

        private IEnumerable<ProductCoPurchasePrediction> ExecuteQuery(Func<DbCommand> createCommand)
        {
            IDatabaseExecutor db = this.databaseExecutor();

            return db.Execute(
                () =>
                {
                    try
                    {
                        return this.ExecuteEnumerableQuery(createCommand());
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Prediction Engine] An error occurred while retrieving predictions.", exception: ex);
                        throw;
                    }
                });
        }

        private IEnumerable<ProductCoPurchasePrediction> GetAll()
        {
            string sqlCommand = $@"SELECT [ProductId], [CoPurchaseProductId], [Score] FROM {PredictionsTable}";

            return this.ExecuteQuery(() => this.CreateCommand(sqlCommand: sqlCommand));
        }
    }
}