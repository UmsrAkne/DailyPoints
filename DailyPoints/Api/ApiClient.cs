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
            // 1. SSHトンネルの確立を保証
            await EnsureSshTunnelAsync(ct);

            // 2. サーバー側の型（TaskItemCreate）に合わせた匿名オブジェクトを作成
            // TimeSpan型から「総分数（整数）」へ変換します
            var payload = new
            {
                id = taskItem.Id.ToString(),
                issueId = taskItem.IssueId,
                summary = taskItem.Summary,
                estimation = (int)taskItem.Estimation.TotalMinutes, // 総分数に変換
                actualTime = (int)taskItem.ActualTime.TotalMinutes, // 総分数に変換
                rate = taskItem.Rate,
            };

            // 3. JSONにシリアライズ
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            var jsonContent = JsonSerializer.Serialize(payload, jsonOptions);
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // 4. トンネル経由（ポート18000）でサーバーへPOST
            // ※エンドポイントのパス（例: /tasks）は環境に合わせて変更してください
            const string requestUrl = $"{BaseUrl}/api/tasks";

            using var response = await httpClient.PostAsync(requestUrl, content, ct);

            // 5. ステータスコードの検証とレスポンスの返却
            if (!response.IsSuccessStatusCode)
            {
                var errorReason = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"サーバーエラー: {response.StatusCode} - {errorReason}");
            }

            // サーバーから返ってきた文字列（IDやステータスなど）を返す
            return await response.Content.ReadAsStringAsync(ct);
        }

        public async Task<string> PostMoneyExpenseItemAsync(MoneyExpenseItem moneyExpenseItem, CancellationToken ct = default)
        {
            await EnsureSshTunnelAsync(ct);
            return string.Empty;
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

        private async Task PostAsync(string url, HttpContent content, CancellationToken ct)
        {
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    using var response = await httpClient.PostAsync(url, content, ct);

                    // 409 Conflict (IntegrityError) のハンドリングが必要な場合はここで行う
                    response.EnsureSuccessStatusCode();
                    return;
                }
                catch (HttpRequestException) when (i < 2)
                {
                    await Task.Delay(1000, ct);
                    await EnsureSshTunnelAsync(ct);
                }
            }

            throw new Exception("サーバーへのソース追加に失敗しました。");
        }

        private async Task<string> GetAsync(string url, CancellationToken ct)
        {
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    using var response = await httpClient.GetAsync(url, ct);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync(ct);
                }
                catch (HttpRequestException) when (i < 2)
                {
                    // トンネルが開通するのを少し待ってリトライ
                    await Task.Delay(1000, ct);
                    await EnsureSshTunnelAsync(ct);
                }
            }

            throw new Exception("SSHトンネル経由での接続に失敗しました。");
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