using System.ComponentModel;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace SkOfflineCourse.Plugins;

public class WellhubCorrectionPlugin
{
    // Simulação de dados para diferentes cenários de liberação de check-in
    private readonly Dictionary<string, CorrectionResult> _simulatedData;

    public WellhubCorrectionPlugin()
    {
        // Inicializa dados simulados para testes
        _simulatedData = new Dictionary<string, CorrectionResult>
        {
            // Cenários de sucesso
            { "user123_2024-10-02", new CorrectionResult("SUCESSO_DA_CORRECAO", "Check-in liberado com sucesso.") },
            { "user456_2024-10-03", new CorrectionResult("SUCESSO_DA_CORRECAO", "Check-in liberado com sucesso.") },

            // Cenários de erro operacional
            { "user000_2024-10-02", new CorrectionResult("ERRO_OPERACIONAL", "Falha ao liberar check-in: usuário inválido.") },
            { "user789_2024-10-04", new CorrectionResult("ERRO_OPERACIONAL", "Falha ao liberar check-in: formato de data inválido.") }
        };
    }

    /// <summary>
    /// Libera o bloqueio de check-in diário do usuário
    /// </summary>
    [KernelFunction, Description("Libera o bloqueio de check-in diário de um usuário")]
    public async Task<string> ReleaseCheckinLock(
        [Description("ID do usuário")] string userId,
        [Description("Data do check-in no formato yyyy-MM-dd")] string date)
    {
        // Validação básica
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(date))
        {
            return JsonSerializer.Serialize(new CorrectionResult("ERRO_OPERACIONAL", "Parâmetros obrigatórios ausentes."));
        }

        // Simula latência
        await Task.Delay(50);

        // Gera chave única
        var key = $"{userId}_{date}";

        CorrectionResult result;
        if (_simulatedData.ContainsKey(key))
        {
            result = _simulatedData[key];
        }
        else
        {
            // Se não houver dado simulado, simula erro operacional genérico
            result = new CorrectionResult("ERRO_OPERACIONAL", "Falha ao liberar check-in: registro não encontrado.");
        }

        // Serializa em JSON estruturado
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default
        };

        return JsonSerializer.Serialize(result, jsonOptions);
    }

    /// <summary>
    /// Lista todos os registros simulados disponíveis (função auxiliar para debug)
    /// </summary>
    [KernelFunction, Description("Lista todos os registros de liberação de check-in simulados disponíveis para teste")]
    public async Task<string> ListSimulatedRecords()
    {
        await Task.Delay(50);

        var records = _simulatedData.Select(kvp => new
        {
            Key = kvp.Key,
            Status = kvp.Value.Status,
            Details = kvp.Value.Detalhes
        }).ToList();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default
        };

        return JsonSerializer.Serialize(records, jsonOptions);
    }

    [KernelFunction, Description("Corrige o check-in de um usuário em uma data específica")]
    public async Task<string> CorrectCheckin(
        [Description("ID único do usuário")] string userId,
        [Description("Data do check-in a ser corrigida (formato: yyyy-MM-dd)")] string date)
    {
        await Task.Delay(100); // Simula latência

        if (string.IsNullOrWhiteSpace(userId))
            return JsonSerializer.Serialize(new CorrectionResult("ERRO_OPERACIONAL", "userId obrigatório"));
        if (string.IsNullOrWhiteSpace(date))
            return JsonSerializer.Serialize(new CorrectionResult("ERRO_OPERACIONAL", "date obrigatório"));

        // Gera chave única
        var key = $"{userId}_{date}";

        CorrectionResult result;
        if (_simulatedData.ContainsKey(key))
        {
            result = _simulatedData[key];
        }
        else
        {
            // Se não houver dado simulado, simula erro operacional genérico
            result = new CorrectionResult("ERRO_OPERACIONAL", "Falha ao corrigir check-in: registro não encontrado.");
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default
        };

        return JsonSerializer.Serialize(result, jsonOptions);
    }
}

/// <summary>
/// Modelo de dados para o resultado de liberação de check-in
/// </summary>
public record CorrectionResult(
    [property: Description("Status da correção: SUCESSO_DA_CORRECAO ou ERRO_OPERACIONAL")]
    string Status,
    
    [property: Description("Detalhes adicionais ou mensagem de erro")]
    string Detalhes
);
