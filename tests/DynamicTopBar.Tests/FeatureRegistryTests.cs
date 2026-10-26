using System;
using System.Linq;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class FeatureRegistryTests
{
    [Fact]
    public void Register_ValidDescriptor_AddsSuccessfully()
    {
        var registry = new FeatureRegistry();
        var descriptor = new FeatureDescriptor
        {
            Id = "test-feature",
            DisplayName = "Test Feature",
            IconKey = "Test",
            DisplayOrder = 1
        };

        registry.Register(descriptor);

        Assert.Equal(1, registry.Count);
        Assert.True(registry.Contains("test-feature"));
        Assert.NotNull(registry.GetDescriptor("test-feature"));
        Assert.Equal("Test Feature", registry.GetDescriptor("test-feature")?.DisplayName);
    }

    [Fact]
    public void Register_DuplicateId_ThrowsArgumentException()
    {
        var registry = new FeatureRegistry();
        var d1 = new FeatureDescriptor { Id = "dup", DisplayName = "First", DisplayOrder = 1 };
        var d2 = new FeatureDescriptor { Id = "dup", DisplayName = "Second", DisplayOrder = 2 };

        registry.Register(d1);

        var ex = Assert.Throws<ArgumentException>(() => registry.Register(d2));
        Assert.Contains("dup", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Register_DuplicateModule_ThrowsArgumentException()
    {
        var registry = new FeatureRegistry();
        var mod1 = new PlaceholderFeatureModule(
            new FeatureDescriptor { Id = "mod-dup", DisplayName = "Mod 1" },
            "Milestone 1", "Scope 1", "None");
        var mod2 = new PlaceholderFeatureModule(
            new FeatureDescriptor { Id = "mod-dup", DisplayName = "Mod 2" },
            "Milestone 2", "Scope 2", "None");

        registry.Register(mod1);

        var ex = Assert.Throws<ArgumentException>(() => registry.Register(mod2));
        Assert.Contains("mod-dup", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unregister_ExistingFeature_RemovesSuccessfully()
    {
        var registry = new FeatureRegistry();
        registry.Register(new FeatureDescriptor { Id = "f1", DisplayName = "F1" });

        Assert.True(registry.Contains("f1"));
        Assert.True(registry.Unregister("f1"));
        Assert.False(registry.Contains("f1"));
        Assert.Null(registry.GetDescriptor("f1"));
        Assert.Equal(0, registry.Count);
    }

    [Fact]
    public void Unregister_NonexistentFeature_ReturnsFalse()
    {
        var registry = new FeatureRegistry();
        Assert.False(registry.Unregister("nonexistent"));
    }

    [Fact]
    public void GetAllDescriptors_OrdersByDisplayOrder()
    {
        var registry = new FeatureRegistry();
        registry.Register(new FeatureDescriptor { Id = "c", DisplayName = "C", DisplayOrder = 30 });
        registry.Register(new FeatureDescriptor { Id = "a", DisplayName = "A", DisplayOrder = 10 });
        registry.Register(new FeatureDescriptor { Id = "b", DisplayName = "B", DisplayOrder = 20 });

        var list = registry.GetAllDescriptors();

        Assert.Equal(3, list.Count);
        Assert.Equal("a", list[0].Id);
        Assert.Equal("b", list[1].Id);
        Assert.Equal("c", list[2].Id);
    }

    [Fact]
    public void CreateDefaultMultifunctionRegistry_RegistersAll11Features()
    {
        var registry = FeatureRegistry.CreateDefaultMultifunctionRegistry();

        Assert.Equal(11, registry.Count);

        string[] expectedIds = new[]
        {
            "media",
            "spotify",
            "clock",
            "weather",
            "todo",
            "focus",
            "quick-launch",
            "system-glance",
            "calendar",
            "notes",
            "clipboard"
        };

        foreach (var id in expectedIds)
        {
            Assert.True(registry.Contains(id), $"Expected registry to contain feature '{id}'.");
            var desc = registry.GetDescriptor(id);
            Assert.NotNull(desc);
            Assert.False(string.IsNullOrWhiteSpace(desc.DisplayName));
        }
    }

    [Fact]
    public void DefaultRegistry_CorrectlyIdentifiesAll11ImplementedModules()
    {
        var registry = FeatureRegistry.CreateDefaultMultifunctionRegistry();

        string[] expectedIds = new[]
        {
            "media",
            "spotify",
            "clock",
            "weather",
            "todo",
            "focus",
            "quick-launch",
            "system-glance",
            "calendar",
            "notes",
            "clipboard"
        };

        foreach (var id in expectedIds)
        {
            var desc = registry.GetDescriptor(id);
            Assert.NotNull(desc);
            Assert.False(desc.IsPlaceholder);
        }
    }
}
