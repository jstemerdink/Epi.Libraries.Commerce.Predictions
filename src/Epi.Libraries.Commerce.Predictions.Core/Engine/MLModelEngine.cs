// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MLModelEngine.cs" company="Valtech">
//     Copyright © 2019 Valtech.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Epi.Libraries.Commerce.Predictions.Engine
{
    using System.IO;

    using Microsoft.Extensions.ObjectPool;
    using Microsoft.ML;

    public class MlModelEngine<TData, TPrediction>
        where TData : class where TPrediction : class, new()
    {
        private readonly int maxObjectsRetained;

        private readonly MLContext mlContext;

        private readonly ITransformer mlModel;

        private readonly ObjectPool<PredictionEngine<TData, TPrediction>> predictionEnginePool;

        /// <summary>
        /// Initializes a new instance of the <see cref="MlModelEngine{TData, TPrediction}"/> class.
        /// </summary>
        /// <param name="modelFilePathName">Name of the model file path.</param>
        /// <param name="maxObjectsRetained">The maximum objects retained.</param>
        /// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="T:System.UnauthorizedAccessException"><paramref name="path" /> specified a directory.-or- The caller does not have the required permission.</exception>
        /// <exception cref="T:System.IO.FileNotFoundException">The file specified in <paramref name="path" /> was not found.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred while opening the file.</exception>
        public MlModelEngine(string modelFilePathName, int maxObjectsRetained = -1)
        {
            // Create the MLContext object to use under the scope of this class 
            this.mlContext = new MLContext();

            using (FileStream fileStream = File.OpenRead(path: modelFilePathName))
            {
                this.mlModel = this.mlContext.Model.Load(stream: fileStream, out DataViewSchema _);
            }

            this.maxObjectsRetained = maxObjectsRetained;

            // Create PredictionEngine Object Pool
            this.predictionEnginePool = this.CreatePredictionEngineObjectPool();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MlModelEngine{TData, TPrediction}"/> class.
        /// </summary>
        /// <param name="transformer">The transformer.</param>
        /// <param name="maxObjectsRetained">The maximum objects retained.</param>
        public MlModelEngine(ITransformer transformer, int maxObjectsRetained = -1)
        {
            // Create the MLContext object to use under the scope of this class 
            this.mlContext = new MLContext();

            this.mlModel = transformer;

            this.maxObjectsRetained = maxObjectsRetained;

            // Create PredictionEngine Object Pool
            this.predictionEnginePool = this.CreatePredictionEngineObjectPool();
        }

        /// <summary>
        /// Gets the ML model allowing additional ITransformer operations such as Bulk predictions', etc.
        /// </summary>
        public ITransformer MlModel
        {
            get
            {
                return this.mlModel;
            }
        }

        /// <summary>
        /// The Predict() method performs a single prediction based on sample data provided (dataSample) and returning the Prediction.
        /// This implementation uses an object pool internally so it is optimized for scalable and multi-threaded apps.
        /// </summary>
        /// <param name="dataSample">The data sample</param>
        /// <returns>The prediction</returns>
        public TPrediction Predict(TData dataSample)
        {
            // Get PredictionEngine object from the Object Pool
            PredictionEngine<TData, TPrediction> predictionEngine = this.predictionEnginePool.Get();

            try
            {
                // Predict
                TPrediction prediction = predictionEngine.Predict(example: dataSample);
                return prediction;
            }
            finally
            {
                // Release used PredictionEngine object into the Object Pool
                this.predictionEnginePool.Return(obj: predictionEngine);
            }
        }

        // Create the Object Pool based on the PooledPredictionEnginePolicy.
        // This method is only used once, from the constructor.
        private ObjectPool<PredictionEngine<TData, TPrediction>> CreatePredictionEngineObjectPool()
        {
            PooledPredictionEnginePolicy<TData, TPrediction> predEnginePolicy = new PooledPredictionEnginePolicy<TData, TPrediction>(
                mlContext: this.mlContext,
                model: this.mlModel);

            DefaultObjectPool<PredictionEngine<TData, TPrediction>> pool;

            if (this.maxObjectsRetained != -1)
            {
                pool = new DefaultObjectPool<PredictionEngine<TData, TPrediction>>(
                    policy: predEnginePolicy,
                    maximumRetained: this.maxObjectsRetained);
            }
            else
            {
                // default maximumRetained is Environment.ProcessorCount * 2, if not explicitly provided
                pool = new DefaultObjectPool<PredictionEngine<TData, TPrediction>>(policy: predEnginePolicy);
            }

            return pool;
        }
    }
}