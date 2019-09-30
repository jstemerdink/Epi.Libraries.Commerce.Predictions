// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PredictionEngineInitializationModule.cs" company="Jeroen Stemerdink">
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

    using EPiServer.Framework;
    using EPiServer.Framework.Initialization;
    using EPiServer.ServiceLocation;

    /// <summary>
    /// Class PredictionEngineInitializationModule.
    /// Implements the <see cref="EPiServer.ServiceLocation.IConfigurableModule" />
    /// </summary>
    /// <seealso cref="EPiServer.ServiceLocation.IConfigurableModule" />
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    public class PredictionEngineInitializationModule : IConfigurableModule
    {
        /// <summary>
        /// The prediction engine service
        /// </summary>
        private static IPredictionEngineService predictionEngineService;

        /// <summary>
        /// The catalog content event listener
        /// </summary>
        private static CatalogContentEventListener listener;

        /// <summary>
        /// Indicates whether the module has been initialized
        /// </summary>
        private static bool initialized;

        /// <summary>Configure the IoC container before initialization.</summary>
        /// <param name="context">The context on which the container can be accessed.</param>
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            IServiceConfigurationProvider services = context.Services;

            services.AddSingleton<IPredictionEngineService, PredictionEngineService>();

            services.AddSingleton<CatalogContentEventListener, CatalogContentEventListener>();
        }

        /// <summary>Initializes the <see cref="IPredictionEngineService"/></summary>
        /// <param name="context">The <see cref="InitializationEngine"/></param>
        /// <exception cref="T:EPiServer.ServiceLocation.ActivationException">if there is are errors resolving the service instance.</exception>
        public void Initialize(InitializationEngine context)
        {
            if ((context == null) || (context.HostType != HostType.WebApplication))
            {
                return;
            }

            if (initialized)
            {
                return;
            }

            predictionEngineService = context.Locate.Advanced.GetInstance<IPredictionEngineService>();
            context.InitComplete += InitCompleteHandler;

            listener = context.Locate.Advanced.GetInstance<CatalogContentEventListener>();
            listener.AddEvent();

            initialized = true;
        }

        /// <summary>
        /// Resets the module into an uninitialized state.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <remarks><para>
        /// This method is usually not called when running under a web application since the web app may be shut down very
        /// abruptly, but your module should still implement it properly since it will make integration and unit testing
        /// much simpler.
        /// </para>
        /// <para>
        /// Any work done by <see cref="M:EPiServer.Framework.IInitializableModule.Initialize(EPiServer.Framework.Initialization.InitializationEngine)" /> as well as any code executing on <see cref="E:EPiServer.Framework.Initialization.InitializationEngine.InitComplete" /> should be reversed.
        /// </para></remarks>
        public void Uninitialize(InitializationEngine context)
        {
            if (!initialized)
            {
                return;
            }

            listener.RemoveEvent();
            initialized = false;
        }

        /// <summary>Initializes the complete handler.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private static void InitCompleteHandler(object sender, EventArgs e)
        {
            if (predictionEngineService == null)
            {
                return;
            }

            predictionEngineService.Init();
        }
    }
}