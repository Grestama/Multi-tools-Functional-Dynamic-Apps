using System;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Base class providing default lifecycle and activation state management for feature modules.
/// </summary>
public abstract class FeatureModuleBase : IFeatureModule
{
    private bool _isActive;
    private bool _isDisposed;

    public FeatureDescriptor Descriptor { get; }

    public bool IsActive => _isActive;

    protected FeatureModuleBase(FeatureDescriptor descriptor)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    public virtual void Activate()
    {
        if (_isDisposed) return;
        _isActive = true;
        OnActivated();
    }

    public virtual void Deactivate()
    {
        if (_isDisposed) return;
        _isActive = false;
        OnDeactivated();
    }

    protected virtual void OnActivated() { }

    protected virtual void OnDeactivated() { }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing)
        {
            _isActive = false;
        }
        _isDisposed = true;
    }
}
