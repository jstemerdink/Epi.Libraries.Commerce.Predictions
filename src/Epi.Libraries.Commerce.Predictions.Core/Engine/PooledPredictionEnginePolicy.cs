// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PooledPredictionEnginePolicy.cs" company="Valtech">
//     Copyright © 2019 Valtech.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Epi.Libraries.Commerce.Predictions.Engine
{
    using System.Diagnostics;

    using Microsoft.Extensions.ObjectPool;
    using Microsoft.ML;

    public class PooledPredictionEnginePolicy<TData, TPrediction> : IPooledObjectPolicy<PredictionEngine<TData, TPrediction>>
        where TData : class where TPrediction : class, new()
    {
        private readonly MLContext mlContext;

        private readonly ITransformer model;

        public PooledPredictionEnginePolicy(MLContext mlContext, ITransformer model)
        {
            this.mlContext = mlContext;
            this.model = model;
        }

        public PredictionEngine<TData, TPrediction> Create()
        {
            PredictionEngine<TData, TPrediction> predictionEngine =
                this.mlContext.Model.CreatePredictionEngine<TData, TPrediction>(transformer: this.model);

            return predictionEngine;
        }

        public bool Return(PredictionEngine<TData, TPrediction> obj)
        {
            return obj != null;
        }
    }
}