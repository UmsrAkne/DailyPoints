using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
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
    private string inputTasksText = string.Empty;
    private int point = 1000;
    private string inputDeductionTasksText = string.Empty;

    public MainWindowViewModel(PointService pointService)
    {
        this.pointService = pointService;
        SetupDummyData();
    }

    public string Title => appVersionInfo.Title;

    public int Point { get => point; set => SetProperty(ref point, value); }

    public string InputTasksText { get => inputTasksText; set => SetProperty(ref inputTasksText, value); }

    public string InputDeductionTasksText
    {
        get => inputDeductionTasksText;
        set => SetProperty(ref inputDeductionTasksText, value);
    }

    public DelegateCommand CsvToPointCommand => new DelegateCommand(() =>
    {
        if (string.IsNullOrWhiteSpace(InputTasksText))
        {
            return;
        }

        var input = InputTasksText;
        var items = CsvToTaskItems(input);

        var transactions = items.Select(t => pointCalculator.Calculate(t));
        var succeeds = TryAddPointTransactions(transactions);
        Point += succeeds.Sum(t => t.Points);

        InputTasksText = string.Empty;
    });

    public DelegateCommand PointDeductionCommand => new DelegateCommand(() =>
    {
        if (string.IsNullOrWhiteSpace(InputDeductionTasksText))
        {
            return;
        }

        var input = InputDeductionTasksText;
        var items = CsvToTaskItems(input);

        var transactions = items.Select(t => pointCalculator.Deduct(t));
        var succeeds = TryAddPointTransactions(transactions);
        Point += succeeds.Sum(t => t.Points);

        InputDeductionTasksText = string.Empty;
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

    /// <summary>
    /// 未登録のポイント取引データのみを抽出し、データベースに一括で追加します。
    /// </summary>
    /// <param name="transactions">追加を試みるポイント取引データのリスト</param>
    /// <returns>重複がなく、正常に追加されたポイント取引データのリスト</returns>
    private IEnumerable<PointTransaction> TryAddPointTransactions(IEnumerable<PointTransaction> transactions)
    {
        // 1. ループ内での全件全探索を避けるため、既存のIssueIdをHashSetにまとめておく（O(1)で検証可能にする）
        var existingIssueIds = pointService.GetAll()
            .Select(t => t.TaskItem.IssueId)
            .ToHashSet();

        var addedTransactions = new List<PointTransaction>();

        foreach (var transaction in transactions)
        {
            // 2. 既に同じIssueIdが存在する場合はスキップ（重複登録の防止）
            if (existingIssueIds.Contains(transaction.TaskItem.IssueId))
            {
                continue;
            }

            pointService.Add(transaction);
            addedTransactions.Add(transaction);

            // 3. 次のループで同じ引数内の重複にも対応できるよう、HashSetにも追加しておく
            existingIssueIds.Add(transaction.TaskItem.IssueId);
        }

        return addedTransactions;
    }

    [Conditional("DEBUG")]
    private void SetupDummyData()
    {
    }
}