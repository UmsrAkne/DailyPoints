using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CsvHelper;
using CsvHelper.Configuration;
using DailyPoints.Api;
using DailyPoints.Core;
using DailyPoints.Databases;
using DailyPoints.Models;
using DailyPoints.Utils;
using DailyPoints.Utils.Csv;
using Prism.Mvvm;

namespace DailyPoints.ViewModels;

public sealed class MainWindowViewModel : BindableBase, IDisposable
{
    #if DEBUG
    // ReSharper disable once UnusedMember.Local
    private readonly string testDirectoryPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            @"myFiles\tests\RiderProjects\DailyPoints");
    #endif

    private readonly AppVersionInfo appVersionInfo = new();
    private readonly PointCalculator pointCalculator = new();
    private readonly PointService pointService;
    private readonly ApiClient apiClient;
    private readonly CancellationTokenSource cts = new();
    private string inputTasksText = string.Empty;
    private int point;
    private string inputDeductionTasksText = string.Empty;
    private int expensePrice;
    private string expenseDetailText = string.Empty;
    private AsyncRelayCommand csvToPointCommand;
    private AsyncRelayCommand fetchPointTransactionsAsyncCommand;
    private AsyncRelayCommand pointDeductionCommand;
    private AsyncRelayCommand pointDeductionFromExpenseCommand;
    private bool disposed;

    public MainWindowViewModel(PointService pointService)
    {
        apiClient = new ApiClient(AppSettings.Load());
        this.pointService = pointService;
        SetupDummyData();
    }

    public string Title => appVersionInfo.Title;

    public int Point { get => point; set => SetProperty(ref point, value); }

    public ObservableCollection<PointTransaction> PointTransactions { get; set; } = new ();

    public string InputTasksText { get => inputTasksText; set => SetProperty(ref inputTasksText, value); }

    public string InputDeductionTasksText
    {
        get => inputDeductionTasksText;
        set => SetProperty(ref inputDeductionTasksText, value);
    }

    public int ExpensePrice { get => expensePrice; set => SetProperty(ref expensePrice, value); }

    public string ExpenseDetailText
    {
        get => expenseDetailText;
        set => SetProperty(ref expenseDetailText, value);
    }

    public AsyncRelayCommand FetchPointTransactionsAsyncCommand =>
        fetchPointTransactionsAsyncCommand ??= new AsyncRelayCommand(async () =>
        {
            await UpdatePointTransactions();
        });

    public AsyncRelayCommand CsvToPointAsyncCommand =>
        csvToPointCommand ??= new AsyncRelayCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(InputTasksText))
            {
                return;
            }

            var input = InputTasksText;

            var items = CsvToTaskItems(input);
            foreach (var taskItem in items)
            {
                try
                {
                    var response = await apiClient.PostTaskItemAsync(taskItem);
                    await Task.Delay(40);
                    Console.WriteLine(response);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            await UpdatePointTransactions();
        });

    public AsyncRelayCommand PointDeductionAsyncCommand =>
        pointDeductionCommand ??= new AsyncRelayCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(InputDeductionTasksText))
            {
                return;
            }

            var input = InputDeductionTasksText;

            // var items = CsvToTaskItems(input);
            // PointTransaction はサーバー側で作成して追加する仕様に変更。
            // items の状態でサーバーに送る。
            InputDeductionTasksText = string.Empty;
            await UpdatePointTransactions();
        });

    public AsyncRelayCommand PointDeductionFromExpenseAsyncCommand =>
        pointDeductionFromExpenseCommand ??= new AsyncRelayCommand(async () =>
        {
            if (ExpensePrice <= 0)
            {
                return;
            }

            var input = ExpensePrice;
            var item = new MoneyExpenseItem
            {
                Description = ExpenseDetailText,
                Amount = input,
            };

            var isSuccess = true;

            try
            {
                var message = await apiClient.PostMoneyExpenseAsync(item);
                Console.WriteLine(message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                isSuccess = false;
            }

            if (isSuccess)
            {
                ExpensePrice = 0;
                ExpenseDetailText = string.Empty;
            }

            await UpdatePointTransactions();
        });

    public void Dispose()
    {
        Dispose(true);
    }

    private List<TaskItem> CsvToTaskItems(string csvContent)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
        };

        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap<TaskItemMap>();

        var records = csv.GetRecords<TaskItem>().ToList();
        return records;
    }

    private async Task UpdatePointTransactions(CancellationToken ct = default)
    {
        var list = await apiClient.GetPointTransactionsAsync(ct);
        PointTransactions.Clear();
        PointTransactions.AddRange(list.OrderByDescending(t => t.SequenceNumber));

        Point = PointTransactions.FirstOrDefault()?.Balance ?? 0;
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            cts.Cancel();
            cts.Dispose();
            apiClient.Dispose();
        }

        disposed = true;
    }

    [Conditional("DEBUG")]
    private void SetupDummyData()
    {
    }
}