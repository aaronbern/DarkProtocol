using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Service locator for grid system services.
    /// Provides centralized access to all services and handles dependencies between them.
    /// </summary>
    public class GridServiceLocator
    {
        #region Singleton
        private static GridServiceLocator _instance;
        public static GridServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GridServiceLocator();
                }
                return _instance;
            }
        }
        #endregion

        #region Services
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        /// <summary>
        /// Register a service with the service locator
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        /// <param name="service">The service implementation</param>
        public void RegisterService<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"Service of type {type.Name} already registered. Overwriting.");
            }
            
            _services[type] = service;
            Debug.Log($"Registered service of type {type.Name}");
        }
        
        /// <summary>
        /// Get a service from the service locator
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        /// <returns>The service implementation, or null if not found</returns>
        public T GetService<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            
            Debug.LogWarning($"Service of type {type.Name} not registered!");
            return null;
        }
        
        /// <summary>
        /// Check if a service is registered
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        /// <returns>True if the service is registered</returns>
        public bool HasService<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Unregister a service from the service locator
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        public void UnregisterService<T>() where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
                Debug.Log($"Unregistered service of type {type.Name}");
            }
        }
        
        /// <summary>
        /// Clear all registered services
        /// </summary>
        public void ClearServices()
        {
            _services.Clear();
            Debug.Log("Cleared all registered services");
        }
        #endregion
    }
    
    /// <summary>
    /// Simplified static access to grid services
    /// Use this class for convenience instead of accessing the service locator directly
    /// </summary>
    public static class GridServices
    {
        public static IGridService Grid => GridServiceLocator.Instance.GetService<IGridService>();
        public static IPathfindingService Pathfinding => GridServiceLocator.Instance.GetService<IPathfindingService>();
        public static IUnitGridService Units => GridServiceLocator.Instance.GetService<IUnitGridService>();
        public static IGridVisualizationService Visualization => GridServiceLocator.Instance.GetService<IGridVisualizationService>();
        public static IGridInputService Input => GridServiceLocator.Instance.GetService<IGridInputService>();
        public static IGridSerializationService Serialization => GridServiceLocator.Instance.GetService<IGridSerializationService>();
        public static IGridExtensionService Extensions => GridServiceLocator.Instance.GetService<IGridExtensionService>();
    }
}