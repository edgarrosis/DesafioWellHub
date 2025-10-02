using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Diagnostics;
using System.ComponentModel; // Para o DescriptionAttribute

namespace SkOfflineCourse.Plugins;

public class WellHubCorrectionPlugin
{
    private readonly string _logFile = "correction_logs.json";

    [KernelFunction, Description("Processa um estorno de transação com motivo informado")]
    public async Task<string> ProcessRefund(
        string transactionId,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return "ERRO_OPERACIONAL: transactionId inválido.";

        var log = new
        {
            Action = "ProcessRefund",
            TransactionId = transactionId,
            Reason = reason,
            Status = "SUCESSO_DA_CORRECAO",
            Timestamp = DateTime.UtcNow
        };

        SaveLog(log);

        // Chama o Ollama para gerar resposta humanizada
        var message = await CallOllamaAsync(
            $"Foi solicitado estorno da transação {transactionId} pelo motivo: {reason}. Confirme de forma amigável que a correção foi feita com sucesso."
        );

        return message;
    }

    [KernelFunction, Description("Libera bloqueio de check-in diário do usuário")]
    public async Task<string> ReleaseCheckinLock(
        string userId,
        DateTime date)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return "ERRO_OPERACIONAL: userId inválido.";

        var log = new
        {
            Action = "ReleaseCheckinLock",
            UserId = userId,
            Date = date.ToString("yyyy-MM-dd"),
            Status = "SUCESSO_DA_CORRECAO",
            Timestamp = DateTime.UtcNow
        };

        SaveLog(log);

        // Chama o Ollama para gerar resposta humanizada
        var message = await CallOllamaAsync(
            $"O bloqueio de check-in do usuário {userId} em {date:yyyy-MM-dd} foi liberado. Crie uma resposta amigável de confirmação."
        );

        return message;
    }

    private void SaveLog(object log)
    {
        var logs = new List<object>();

        if (File.Exists(_logFile))
        {
            var existing = File.ReadAllText(_logFile);
            if (!string.IsNullOrWhiteSpace(existing))
                logs = JsonSerializer.Deserialize<List<object>>(existing) ?? new List<object>();
        }

        logs.Add(log);
        File.WriteAllText(_logFile,
            JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<string> CallOllamaAsync(string prompt)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ollama",
            Arguments = $"run llama3 \"{prompt}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return string.IsNullOrWhiteSpace(output) ? "OK" : output.Trim();
    }
}
