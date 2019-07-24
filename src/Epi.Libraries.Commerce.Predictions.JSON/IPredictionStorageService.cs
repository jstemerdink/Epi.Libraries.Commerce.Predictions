// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPredictionStorageService.cs" company="Jeroen Stemerdink">
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

    using Epi.Libraries.Commerce.Predictions.Core.Models;
    using Epi.Libraries.Commerce.Predictions.JSON.Models;

    public interface IPredictionStorageService
    {
        /// <summary>
        /// Initializes the prediction storage.
        /// </summary>
        void InitPredictionStorage();

        /// <summary>
        /// Loads the predictions.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IProductCoPurchasePrediction"/>.</returns>
        IEnumerable<ProductCoPurchasePrediction> LoadPredictions();

        /// <summary>
        /// Creates the predictions json.
        /// </summary>
        /// <param name="coPurchasePredictions">The predictions.</param>
        /// <exception cref="T:System.Threading.LockRecursionException">The <see cref="P:System.Threading.ReaderWriterLockSlim.RecursionPolicy" /> property is <see cref="F:System.Threading.LockRecursionPolicy.NoRecursion" /> and the current thread has already entered the lock in any mode. -or-The current thread has entered read mode, so trying to enter the lock in write mode would create the possibility of a deadlock. -or-The recursion number would exceed the capacity of the counter. The limit is so large that applications should never encounter it.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="T:System.Threading.SynchronizationLockException">The current thread has not entered the lock in write mode.</exception>
        void SavePredictions(IEnumerable<IProductCoPurchasePrediction> coPurchasePredictions);
    }
}