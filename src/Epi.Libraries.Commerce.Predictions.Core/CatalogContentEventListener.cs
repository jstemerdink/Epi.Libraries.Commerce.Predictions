// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CatalogContentEventListener.cs" company="Jeroen Stemerdink">
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
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;

    using EPiServer.Core;
    using EPiServer.Events;
    using EPiServer.Events.Clients;
    using EPiServer.Logging;

    using Mediachase.Commerce.Catalog;
    using Mediachase.Commerce.Catalog.Events;

    /// <summary>
    /// Class to listen to CatalogKey events and re-index the content.
    /// </summary>
    public class CatalogContentEventListener
    {
        /// <summary>
        /// The <see cref="ILogger"/> instance
        /// </summary>
        private static readonly ILogger Log = LogManager.GetLogger();

        /// <summary>
        /// The recommendation repository
        /// </summary>
        private readonly IRecommendationRepository recommendationRepository;

        /// <summary>
        /// The reference converter
        /// </summary>
        private readonly ReferenceConverter referenceConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogContentEventListener"/> class.
        /// </summary>
        /// <param name="referenceConverter">The reference converter.</param>
        /// <param name="recommendationRepository">The recommendation repository.</param>
        public CatalogContentEventListener(
            ReferenceConverter referenceConverter,
            IRecommendationRepository recommendationRepository)
        {
            this.referenceConverter = referenceConverter;
            this.recommendationRepository = recommendationRepository;
        }

        /// <summary>
        /// Listens to remote events of CatalogKeyEventBroadcaster.
        /// </summary>
        public void AddEvent()
        {
            Event catalogEventUpdatedEvent = Event.Get(eventId: CatalogEventBroadcaster.CommerceProductUpdated);
            catalogEventUpdatedEvent.Raised += this.CatalogEventUpdated;
        }

        /// <summary>
        /// Removes the event listener from the CatalogKeyEventBroadcaster.
        /// </summary>
        public void RemoveEvent()
        {
            Event catalogEventUpdatedEvent = Event.Get(eventId: CatalogEventBroadcaster.CommerceProductUpdated);
            catalogEventUpdatedEvent.Raised -= this.CatalogEventUpdated;
        }

        /// <summary>
        /// Will try to de-serialize the <see cref="P:EPiServer.Events.EventNotificationEventArgs.Param" /> as the typed event argument.
        /// </summary>
        /// <typeparam name="T">The type to de-serialize</typeparam>
        /// <param name="eventNotificationEventArgs">The <see cref="T:EPiServer.Events.EventNotificationEventArgs" /> instance containing the event data.</param>
        /// <param name="deserializedEventArgs">The De-serialized event argument</param>
        /// <returns><c>True</c> if the parameters was De-serialized to event arguments, and validated successfully. Otherwise <c>false</c>.</returns>
        internal bool TryDeserialize<T>(
            EventNotificationEventArgs eventNotificationEventArgs,
            out T deserializedEventArgs)
            where T : EventArgs
        {
            deserializedEventArgs = default;

            if (eventNotificationEventArgs?.Param == null)
            {
                return false;
            }

            byte[] buffer = eventNotificationEventArgs.Param as byte[];

            if (buffer == null)
            {
                return false;
            }

            EventArgs eventArgs = DeSerialize(buffer: buffer);

            deserializedEventArgs = (T)eventArgs;
            return true;
        }

        /// <summary>De-serialize event arguments.</summary>
        /// <param name="buffer">The buffer</param>
        /// <returns>The de-serialized event arguments.</returns>
        private static EventArgs DeSerialize(byte[] buffer)
        {
            using (MemoryStream memoryStream = new MemoryStream(buffer: buffer))
            {
                return new BinaryFormatter().Deserialize(serializationStream: memoryStream) as EventArgs;
            }
        }

        /// <summary>
        /// The catalog updated event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventNotificationEventArgs"/> instance containing the event data.</param>
        private void CatalogEventUpdated(object sender, EventNotificationEventArgs e)
        {
            CatalogContentUpdateEventArgs deserializedEventArgs;

            if (!this.TryDeserialize(eventNotificationEventArgs: e, deserializedEventArgs: out deserializedEventArgs))
            {
                Log.Debug("[Prediction Engine] Failed to deserialize CatalogEventUpdated event args.");
            }
            else
            {
                string eventType = deserializedEventArgs.EventType;

                if (!(eventType.Equals("CatalogEntryDeleted", comparisonType: StringComparison.OrdinalIgnoreCase)
                      | eventType.Equals("RelationDeleted", comparisonType: StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                IEnumerable<ContentReference> contentLinks = this.GetContentLinks(
                    ids: deserializedEventArgs.CatalogEntryIds,
                    type: CatalogContentType.CatalogEntry);

                foreach (ContentReference contentReference in contentLinks)
                {
                    this.recommendationRepository.Delete(productId: contentReference.ID);
                }
            }
        }

        private IEnumerable<ContentReference> GetContentLinks(IEnumerable<int> ids, CatalogContentType type)
        {
            return ids.Select(x => this.referenceConverter.GetContentLink(objectId: x, contentType: type, 0));
        }
    }
}