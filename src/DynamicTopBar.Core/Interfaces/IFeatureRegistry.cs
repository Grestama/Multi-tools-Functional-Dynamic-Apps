using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Registry managing registration, unique identification, and retrieval of feature modules.
/// </summary>
public interface IFeatureRegistry
{
    /// <summary>
    /// Event fired when a new feature module is registered.
    /// </summary>
    event EventHandler<FeatureDescriptor>? FeatureRegistered;

    /// <summary>
    /// Event fired when a feature module is unregistered.
    /// </summary>
    event EventHandler<string>? FeatureUnregistered;

    /// <summary>
    /// Total number of registered features.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Registers a feature module with a unique identifier.
    /// Throws ArgumentException if a module with the same ID already exists.
    /// </summary>
    void Register(IFeatureModule module);

    /// <summary>
    /// Registers a feature descriptor directly.
    /// Throws ArgumentException if a module with the same ID already exists.
    /// </summary>
    void Register(FeatureDescriptor descriptor);

    /// <summary>
    /// Unregisters a feature module by its identifier.
    /// </summary>
    bool Unregister(string featureId);

    /// <summary>
    /// Checks whether a feature module with the specified ID is registered.
    /// </summary>
    bool Contains(string featureId);

    /// <summary>
    /// Retrieves a feature descriptor by its identifier, or null if not registered.
    /// </summary>
    FeatureDescriptor? GetDescriptor(string featureId);

    /// <summary>
    /// Retrieves a feature module instance by its identifier, or null if not registered or registered via descriptor only.
    /// </summary>
    IFeatureModule? GetModule(string featureId);

    /// <summary>
    /// Returns all registered feature descriptors ordered by DisplayOrder.
    /// </summary>
    IReadOnlyList<FeatureDescriptor> GetAllDescriptors();

    /// <summary>
    /// Returns all registered feature modules ordered by DisplayOrder.
    /// </summary>
    IReadOnlyList<IFeatureModule> GetAllModules();
}
