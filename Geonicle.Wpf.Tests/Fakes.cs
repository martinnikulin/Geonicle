using System.Windows;
using Geonicle.Infrastructure;
using Geonicle.Wpf.Menus;
using Geonicle.Wpf.Ribbon;
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

/// <summary>Экран с группой «Прослой».</summary>
public class InterbedScreen : TestScreen
{
    public InterbedScreen(IUserDialogs dialogs) : base(dialogs)
    {
    }

    public override RibbonGroups VisibleRibbonGroups => RibbonGroups.Data | RibbonGroups.Interbed;
}

/// <summary>Экран со всеми группами (как «Данные по скважинам»).</summary>
public class AllGroupsScreen : TestScreen
{
    public AllGroupsScreen(IUserDialogs dialogs) : base(dialogs)
    {
    }

    public override RibbonGroups VisibleRibbonGroups =>
        RibbonGroups.Data | RibbonGroups.Interbed | RibbonGroups.LineBorehole;
}

/// <summary>Заглушка списков для ленты: данные задаются в тесте, вызовы запоминаются.</summary>
public sealed class FakeLookups : IFilterLookupService
{
    public List<InterbedItem> Interbeds { get; } = new();

    public List<LineItem> Lines { get; } = new();

    /// <summary>Скважины по линиям: ключ — LineId.</summary>
    public Dictionary<int, List<LineBoreholeItem>> BoreholesByLine { get; } = new();

    /// <summary>Скважины при «Все скважины».</summary>
    public List<LineBoreholeItem> AllBoreholes { get; } = new();

    public Exception Error { get; set; }

    public int InterbedCalls { get; private set; }

    public int LineCalls { get; private set; }

    public List<(int? LineId, bool All)> BoreholeCalls { get; } = new();

    /// <summary>Если задан, перехватывает загрузку скважин (для проверки гонок).</summary>
    public Func<int?, bool, Task<IReadOnlyList<LineBoreholeItem>>> BoreholesHandler { get; set; }

    public Task<IReadOnlyList<InterbedItem>> GetInterbedsAsync()
    {
        InterbedCalls++;
        if (Error != null) throw Error;
        return Task.FromResult<IReadOnlyList<InterbedItem>>(Interbeds.ToList());
    }

    public Task<IReadOnlyList<LineItem>> GetLinesAsync()
    {
        LineCalls++;
        if (Error != null) throw Error;
        return Task.FromResult<IReadOnlyList<LineItem>>(Lines.ToList());
    }

    public Task<IReadOnlyList<LineBoreholeItem>> GetLineBoreholesAsync(int? lineId, bool allBoreholes)
    {
        BoreholeCalls.Add((lineId, allBoreholes));
        if (Error != null) throw Error;
        if (BoreholesHandler != null) return BoreholesHandler(lineId, allBoreholes);

        IReadOnlyList<LineBoreholeItem> result = allBoreholes
            ? AllBoreholes.ToList()
            : lineId != null && BoreholesByLine.TryGetValue(lineId.Value, out var list) ? list.ToList() : new List<LineBoreholeItem>();
        return Task.FromResult(result);
    }

    /// <summary>Две линии и скважины в них: A и B в первой, C во второй; все: A, B, C.</summary>
    public static FakeLookups WithSampleData()
    {
        var fake = new FakeLookups();
        fake.Interbeds.AddRange(new[] { new InterbedItem(100, "Прослой 0,3 м"), new InterbedItem(200, "Прослой 0,5 м") });
        fake.Lines.AddRange(new[] { new LineItem(10, 1, "Л1"), new LineItem(20, 2, "Л2") });
        fake.BoreholesByLine[10] = new List<LineBoreholeItem> { new(1, 1, "A"), new(2, 2, "B") };
        fake.BoreholesByLine[20] = new List<LineBoreholeItem> { new(3, 1, "C") };
        fake.AllBoreholes.AddRange(new[] { new LineBoreholeItem(1, null, "A"), new LineBoreholeItem(2, null, "B"), new LineBoreholeItem(3, null, "C") });
        return fake;
    }
}
