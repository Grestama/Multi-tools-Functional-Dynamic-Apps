using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class MemoryTodoRepository : ITodoRepository
{
    private List<TodoItem> _items = new();

    public Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TodoItem>>(_items.ToList());
    }

    public Task SaveAllAsync(IEnumerable<TodoItem> items, CancellationToken cancellationToken = default)
    {
        _items = items.ToList();
        return Task.CompletedTask;
    }
}

public class TodoServiceTests
{
    [Fact]
    public async Task AddItem_CreatesItemWithExpectedPriorityAndCounts()
    {
        var repo = new MemoryTodoRepository();
        var service = new TodoService(repo);

        bool eventFired = false;
        service.ItemsChanged += (s, e) => eventFired = true;

        var item = await service.AddItemAsync("Prepare sprint demo", TodoPriority.High);

        Assert.True(eventFired);
        Assert.Equal("Prepare sprint demo", item.Title);
        Assert.Equal(TodoPriority.High, item.Priority);
        Assert.False(item.IsCompleted);
        Assert.Equal(1, service.ActiveCount);
        Assert.Equal(0, service.CompletedCount);
        Assert.Single(service.Items);
    }

    [Fact]
    public async Task ToggleComplete_FlipsCompletionStatusAndUpdatesCounts()
    {
        var repo = new MemoryTodoRepository();
        var service = new TodoService(repo);

        var item = await service.AddItemAsync("Buy groceries", TodoPriority.Normal);
        Assert.False(item.IsCompleted);
        Assert.Equal(1, service.ActiveCount);
        Assert.Equal(0, service.CompletedCount);

        // Toggle to completed
        bool ok = await service.ToggleCompleteAsync(item.Id);
        Assert.True(ok);
        Assert.Equal(0, service.ActiveCount);
        Assert.Equal(1, service.CompletedCount);
        Assert.True(service.Items.First(i => i.Id == item.Id).IsCompleted);

        // Toggle back to active
        await service.ToggleCompleteAsync(item.Id);
        Assert.Equal(1, service.ActiveCount);
        Assert.Equal(0, service.CompletedCount);
        Assert.False(service.Items.First(i => i.Id == item.Id).IsCompleted);
    }

    [Fact]
    public async Task Items_SortedWithHighPriorityFirst()
    {
        var repo = new MemoryTodoRepository();
        var service = new TodoService(repo);

        await service.AddItemAsync("Task Low", TodoPriority.Low);
        await service.AddItemAsync("Task High", TodoPriority.High);
        await service.AddItemAsync("Task Normal", TodoPriority.Normal);

        var items = service.Items;

        Assert.Equal("Task High", items[0].Title);
        Assert.Equal("Task Normal", items[1].Title);
        Assert.Equal("Task Low", items[2].Title);
    }

    [Fact]
    public async Task DeleteItem_RemovesItemFromServiceAndRepository()
    {
        var repo = new MemoryTodoRepository();
        var service = new TodoService(repo);

        var item1 = await service.AddItemAsync("Task 1");
        var item2 = await service.AddItemAsync("Task 2");
        Assert.Equal(2, service.Items.Count);

        bool removed = await service.DeleteItemAsync(item1.Id);

        Assert.True(removed);
        Assert.Single(service.Items);
        Assert.Equal(item2.Id, service.Items[0].Id);
    }

    [Fact]
    public async Task ClearCompleted_RemovesOnlyCompletedTasks()
    {
        var repo = new MemoryTodoRepository();
        var service = new TodoService(repo);

        var task1 = await service.AddItemAsync("Active Task 1");
        var task2 = await service.AddItemAsync("Done Task 2");
        var task3 = await service.AddItemAsync("Done Task 3");

        await service.ToggleCompleteAsync(task2.Id);
        await service.ToggleCompleteAsync(task3.Id);

        Assert.Equal(1, service.ActiveCount);
        Assert.Equal(2, service.CompletedCount);

        await service.ClearCompletedAsync();

        Assert.Single(service.Items);
        Assert.Equal(task1.Id, service.Items[0].Id);
        Assert.Equal(1, service.ActiveCount);
        Assert.Equal(0, service.CompletedCount);
    }

    [Fact]
    public async Task FileTodoRepository_SavesAndLoadsJsonFileAtomically()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"todo_test_{Guid.NewGuid():N}.json");
        try
        {
            var repo = new FileTodoRepository(tempFile);

            var initialList = await repo.GetAllAsync();
            Assert.Empty(initialList);

            var itemsToSave = new List<TodoItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Persistent task",
                    Priority = TodoPriority.High,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            };

            await repo.SaveAllAsync(itemsToSave);

            var reloadedList = await repo.GetAllAsync();
            Assert.Single(reloadedList);
            Assert.Equal("Persistent task", reloadedList[0].Title);
            Assert.Equal(TodoPriority.High, reloadedList[0].Priority);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
    }
}
