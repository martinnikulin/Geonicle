using System.Windows;
using Geonicle.Wpf.Menus;
using Geonicle.Wpf.Services;
using Geonicle.Wpf.ViewModels;

namespace Geonicle.Wpf.Tests;

/// <summary>Заглушка диалогов: запоминает показанные сообщения и отвечает заданными ответами.</summary>
public sealed class FakeDialogs : IUserDialogs
{
    private readonly Queue<MessageBoxResult> answers = new();

    public List<ShownMessage> Shown { get; } = new();

    /// <summary>Ответ, когда очередь ответов пуста.</summary>
    public MessageBoxResult DefaultAnswer { get; set; } = MessageBoxResult.Yes;

    public void Answer(params MessageBoxResult[] results)
    {
        foreach (var result in results)
            answers.Enqueue(result);
    }

    public MessageBoxResult Show(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon)
    {
        Shown.Add(new ShownMessage(text, caption, buttons, icon));
        return answers.Count > 0 ? answers.Dequeue() : DefaultAnswer;
    }

    public sealed record ShownMessage(string Text, string Caption, MessageBoxButton Buttons, MessageBoxImage Icon);
}

/// <summary>Экран-заглушка: управляемые загрузка, сохранение и признак несохранённых изменений.</summary>
public class TestScreen : DataViewModelBase
{
    public TestScreen(IUserDialogs dialogs) : base(dialogs)
    {
    }

    public override string Title => "Тестовый экран";

    public bool Pending { get; set; }

    public int Loads { get; private set; }

    public int Saves { get; private set; }

    public bool Disposed { get; private set; }

    public Exception LoadError { get; set; }

    public Exception SaveError { get; set; }

    public GridMenu Menu => CreateGridMenu();

    protected override Task LoadCoreAsync()
    {
        Loads++;
        if (LoadError != null)
            throw LoadError;
        return Task.CompletedTask;
    }

    protected override Task SaveCoreAsync()
    {
        if (SaveError != null)
            throw SaveError;
        Saves++;
        Pending = false;
        return Task.CompletedTask;
    }

    protected override bool HasPendingChanges() => Pending;

    public override void Dispose() => Disposed = true;
}

/// <summary>Второй экран, чтобы было куда переходить.</summary>
public class OtherScreen : TestScreen
{
    public OtherScreen(IUserDialogs dialogs) : base(dialogs)
    {
    }
}
