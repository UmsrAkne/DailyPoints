using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
using Prism.Commands;
using Prism.Mvvm;

namespace DailyPoints.ViewModels;

public class MainWindowViewModel : BindableBase
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
    private string inputTasksText = string.Empty;
    private int point;
    private string inputDeductionTasksText = string.Empty;
    private int expensePrice;
    private string expenseDetailText = string.Empty;
    private AsyncRelayCommand csvToPointCommand;
    private AsyncRelayCommand fetchPointTransactionsAsyncCommand;

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
            var list = await apiClient.GetPointTransactionsAsync();
            PointTransactions.Clear();
            PointTransactions.AddRange(list);

            // 現在の保有ポイントを表示する
            // Point = PointTransactions.FirstOrDefault()?.Points ?? 0;
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

            UpdatePointTransactions();
        });

    public DelegateCommand PointDeductionCommand => new DelegateCommand(() =>
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
        UpdatePointTransactions();
    });

    public DelegateCommand PointDeductionFromExpenseCommand => new DelegateCommand(() =>
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

        var transaction = pointCalculator.Deduct(item);
        pointService.Add(transaction);
        Point += transaction.Points;

        ExpensePrice = 0;
        ExpenseDetailText = string.Empty;
        UpdatePointTransactions();
    });

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

    private void UpdatePointTransactions()
    {
        PointTransactions.Clear();
        PointTransactions.AddRange(pointService.GetAll().OrderBy(t => t.Date));
    }

    [Conditional("DEBUG")]
    private void SetupDummyData()
    {
    }
}