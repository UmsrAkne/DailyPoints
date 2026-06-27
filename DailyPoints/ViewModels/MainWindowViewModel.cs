using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using DailyPoints.Models;
using DailyPoints.Utils;
using DailyPoints.Utils.Csv;
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

    public MainWindowViewModel()
    {
        SetupDummyData();
    }

    public string Title => appVersionInfo.Title;

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

    [Conditional("DEBUG")]
    private void SetupDummyData()
    {
    }
}