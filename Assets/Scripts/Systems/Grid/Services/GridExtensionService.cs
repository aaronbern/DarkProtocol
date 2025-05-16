using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Implementation of the grid extension service
    /// </summary>
    public class GridExtensionService : IGridExtensionService
    {
        private readonly IGridService _gridService;
        
        /// <summary>
        /// Dictionary of registered extensions, keyed by extension name
        /// </summary>
        private Dictionary<string, IGridExtension> _extensions = new Dictionary<string, IGridExtension>();
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gridService">The grid service</param>
        public GridExtensionService(IGridService gridService)
        {
            _gridService = gridService;
        }
        
        /// <summary>
        /// Register an extension to the grid system
        /// </summary>
        /// <param name="extension">The extension to register</param>
        public void RegisterExtension(IGridExtension extension)
        {
            if (extension == null)
            {
                Debug.LogWarning("Cannot register null extension");
                return;
            }
            
            // Check if this extension is already registered
            if (_extensions.ContainsKey(extension.Name))
            {
                Debug.LogWarning($"Extension '{extension.Name}' is already registered");
                return;
            }
            
            // Initialize the extension
            extension.Initialize(_gridService);
            
            // Add to the dictionary
            _extensions[extension.Name] = extension;
            
            Debug.Log($"Extension '{extension.Name}' registered");
        }
        
        /// <summary>
        /// Unregister an extension from the grid system
        /// </summary>
        /// <param name="extension">The extension to unregister</param>
        public void UnregisterExtension(IGridExtension extension)
        {
            if (extension == null)
            {
                Debug.LogWarning("Cannot unregister null extension");
                return;
            }
            
            // Check if this extension is registered
            if (!_extensions.ContainsKey(extension.Name))
            {
                Debug.LogWarning($"Extension '{extension.Name}' is not registered");
                return;
            }
            
            // Remove from the dictionary
            _extensions.Remove(extension.Name);
            
            Debug.Log($"Extension '{extension.Name}' unregistered");
        }
        
        /// <summary>
        /// Get an extension of a specific type
        /// </summary>
        /// <typeparam name="T">The type of extension</typeparam>
        /// <returns>The extension, or null if not found</returns>
        public T GetExtension<T>() where T : class, IGridExtension
        {
            foreach (var extension in _extensions.Values)
            {
                if (extension is T typedExtension)
                {
                    return typedExtension;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get an extension by name
        /// </summary>
        /// <param name="name">The name of the extension</param>
        /// <returns>The extension, or null if not found</returns>
        public IGridExtension GetExtension(string name)
        {
            if (_extensions.TryGetValue(name, out var extension))
            {
                return extension;
            }
            
            return null;
        }
        
        /// <summary>
        /// Get all registered extensions
        /// </summary>
        /// <returns>Dictionary of extensions</returns>
        public IReadOnlyDictionary<string, IGridExtension> GetAllExtensions()
        {
            return _extensions;
        }
    }
}