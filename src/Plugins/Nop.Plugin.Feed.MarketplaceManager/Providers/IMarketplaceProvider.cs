using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Services.Plugins; // Corrected namespace

namespace Nop.Plugin.Feed.MarketplaceManager.Providers
{
    /// <summary>
    /// Interface for marketplace connectors (Amazon, eBay, Ricardo...)
    /// </summary>
    public interface IMarketplaceProvider : IPlugin
    {
        /// <summary>
        /// Unique system name of the marketplace (e.g. "Feed.Amazon")
        /// </summary>
        string SystemName { get; }

        /// <summary>
        /// Friendly name (e.g. "Amazon")
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// Send a new product to the marketplace
        /// </summary>
        /// <param name="product">Product to list</param>
        /// <returns>Result message or ID</returns>
        Task<string> ListProductAsync(Product product);

        /// <summary>
        /// Update inventory/price for an existing product
        /// </summary>
        /// <param name="product">Product to update</param>
        /// <returns>True if success</returns>
        Task<bool> UpdateProductAsync(Product product);

        /// <summary>
        /// Import orders from the marketplace
        /// </summary>
        /// <returns>Number of orders imported</returns>
        Task<int> ImportOrdersAsync();
    }
}
