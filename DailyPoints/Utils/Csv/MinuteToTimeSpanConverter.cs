using System;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace DailyPoints.Utils.Csv
{
    public class MinuteToTimeSpanConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return TimeSpan.Zero;
            }

            // 「数値 + h」と「数値 + m」をそれぞれオプショナル（あってもなくても良い）でキャプチャ
            // 例: "2h 18m", "2h", "18m", "20" (単位なしは分として扱う)
            var match = Regex.Match(text, @"(?:(?<hours>\d+)\s*h)?\s*(?:(?<minutes>\d+)\s*m?)?");

            if (match.Success)
            {
                double totalMinutes = 0;

                // 時間(h) 部分の抽出と換算
                if (match.Groups["hours"].Success && double.TryParse(match.Groups["hours"].Value, out var hours))
                {
                    totalMinutes += hours * 60;
                }

                // 分(m) 部分の抽出
                if (match.Groups["minutes"].Success && double.TryParse(match.Groups["minutes"].Value, out var minutes))
                {
                    totalMinutes += minutes;
                }

                if (totalMinutes > 0)
                {
                    return TimeSpan.FromMinutes(totalMinutes);
                }
            }

            // 従来のシンプルな数値のみ ("20") だった場合のフォールバック
            if (double.TryParse(text, out var rawMinutes))
            {
                return TimeSpan.FromMinutes(rawMinutes);
            }

            return TimeSpan.Zero;
        }
    }
}