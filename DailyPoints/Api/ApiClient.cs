using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DailyPoints.Databases;
using DailyPoints.Models;
using DailyPoints.Utils;

namespace DailyPoints.Api
{
    public sealed class ApiClient : IDisposable
    {
        private const string BaseUrl = "http://127.0.0.1:18000";

        private readonly HttpClient httpClient;
        private readonly AppSettings appSettings;
        private readonly SemaphoreSlim sshLock = new SemaphoreSlim(1, 1); // 複数スレッドでの同時起動を防ぐ
        private Process sshProcess;

        public ApiClient(AppSettings appSettings)
        {
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10),
            };

            this.appSettings = appSettings;
        }

        public async Task<List<PointTransaction>> GetPointTransactionsAsync(CancellationToken ct = default)
        {
            // 1. SSHトンネルの確立を保証
            await EnsureSshTunnelAsync(ct);

            // 2. エンドポイントのURLを構築
            const string requestUrl = $"{BaseUrl}/api/transactions";

            // 3. トンネル経由でサーバーからGET
            using var response = await httpClient.GetAsync(requestUrl, ct);

            // 4. ステータスコードの検証
            if (!response.IsSuccessStatusCode)
            {
                var errorReason = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"サーバーエラー: {response.StatusCode} - {errorReason}");
            }

            // 5. JSONのデシリアライズ設定
            // JSONのキーが "sequence_number" や "task_item" のようになっているため、
            // SnakeCaseLower を指定するとC#のパスカルケースプロパティに自動マッピングされます。
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            // レスポンスのJSONストリームから直接デシリアライズしてメモリ効率を最適化
            await using var responseStream = await response.Content.ReadAsStreamAsync(ct);

            // ルートの { "transactions": [...] } を受けるためのラッパー型をその場で定義してデシリアライズ
            var result = await JsonSerializer.DeserializeAsync<PointTransactionResponseWrapper>(responseStream, jsonOptions, ct);

            // ヌルチェックをしてリストを返却
            return result?.Transactions ?? new List<PointTransaction>();
        }

        public async Task<string> PostTaskItemAsync(TaskItem taskItem, CancellationToken ct = default)
        {
            var payload = new
            {
                id = taskItem.Id.ToString(),
                issueId = taskItem.IssueId,
                summary = taskItem.Summary,
                estimation = (int)taskItem.Estimation.TotalMinutes,
                actualTime = (int)taskItem.ActualTime.TotalMinutes,
                rate = taskItem.Rate,
            };

            return await PostJsonAsync("/api/tasks", payload, ct);
        }

        public async Task<string> PostMoneyExpenseAsync(MoneyExpenseItem moneyExpenseItem, CancellationToken ct = default)
        {
            var payload = new
            {
                id = moneyExpenseItem.Id.ToString(),
                description = moneyExpenseItem.Description,
                amount = moneyExpenseItem.Amount,
            };

            return await PostJsonAsync("/api/expenses", payload, ct);
        }

        public void Dispose()
        {
            try
            {
                if (sshProcess is { HasExited: false, })
                {
                    sshProcess.Kill(true);
                    sshProcess.Dispose();
                }
            }
            catch
            {
                /* 無視 */
            }

            sshLock.Dispose();
            httpClient.Dispose();
        }

        private async Task<string> PostJsonAsync<T>(string relativeUrl, T payload, CancellationToken ct)
        {
            // サーバーに Json を投げるという共通処理を抽出したメソッド。
            // SSHトンネルの確立を保証
            await EnsureSshTunnelAsync(ct);

            // JSONにシリアライズ
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var jsonContent = JsonSerializer.Serialize(payload, jsonOptions);
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // リクエストURLの組み立てと送信
            var requestUrl = $"{BaseUrl}{relativeUrl}";
            using var response = await httpClient.PostAsync(requestUrl, content, ct);

            // ステータスコードの検証
            if (!response.IsSuccessStatusCode)
            {
                var errorReason = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"サーバーエラー: {response.StatusCode} - {errorReason}");
            }

            // レスポンスを返す
            return await response.Content.ReadAsStringAsync(ct);
        }

        private async Task EnsureSshTunnelAsync(CancellationToken ct)
        {
            // すでにプロセスが動いていれば何もしない
            if (sshProcess is { HasExited: false, })
            {
                return;
            }

            await sshLock.WaitAsync(ct);
            try
            {
                // ロックを待っている間に別のスレッドが起動完了している可能性をチェック
                if (sshProcess is { HasExited: false, })
                {
                    return;
                }

                var options = new[]
                {
                    "-N",
                    "-L 18000:127.0.0.1:18000",
                    "-o ConnectTimeout=5",
                    "-o ExitOnForwardFailure=yes",
                    "-o ServerAliveInterval=15",
                    "-o StrictHostKeyChecking=no",
                };

                var startInfo = new ProcessStartInfo
                {
                    FileName = "ssh",
                    Arguments = $"{string.Join(" ", options)} {appSettings.SshUserName}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                };

                sshProcess = Process.Start(startInfo)
                             ?? throw new InvalidOperationException("Failed to start ssh process");

                // SSHプロセスが起動してから、ポート転送が利用可能になるまで少し待つ
                // 本来はポートのリスニング状態をチェックするのが確実ですが、簡易的には1〜2秒待機します
                await Task.Delay(1500, ct);
            }
            finally
            {
                sshLock.Release();
            }
        }

        private class PointTransactionResponseWrapper
        {
            [JsonPropertyName("transactions")]
            public List<PointTransaction> Transactions { get; set; } = new();
        }
    }
}