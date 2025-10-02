using System.ComponentModel;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace SkOfflineCourse.Plugins;

public class WellhubTransactionPlugin
{
    // Simulação de dados para diferentes cenários de teste
    private readonly Dictionary<string, CheckinResult> _simulatedData;

    public WellhubTransactionPlugin()
    {
        // Inicializa dados simulados para testes
        _simulatedData = new Dictionary<string, CheckinResult>
        {
            // Cenário de sucesso
            { "user123_partner456_2024-10-02T10:00:00", new CheckinResult("SUCESSO", "Check-in realizado com sucesso. Transação processada.") },
            
            // Cenário de falha na transação
            { "user456_partner789_2024-10-02T11:30:00", new CheckinResult("FALHA_TRANSACAO", "Erro no processamento do pagamento - Código: TXN_001") },
            
            // Cenário de registro não localizado
            { "user789_partner123_2024-10-02T09:15:00", new CheckinResult("NAO_LOCALIZADO", "Nenhum registro de check-in encontrado para os parâmetros informados") },
            
            // Dados adicionais para testes variados
            { "user999_partner888_2024-10-02T14:00:00", new CheckinResult("SUCESSO", "Check-in confirmado. Parceiro validado.") },
            { "user111_partner222_2024-10-02T16:30:00", new CheckinResult("FALHA_TRANSACAO", "Transação negada - Saldo insuficiente - Código: TXN_002") }
        };
    }

    /// <summary>
    /// Verifica o status de check-in e transação para um usuário específico
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="partnerId">ID do parceiro/estabelecimento</param>
    /// <param name="timestamp">Data e hora do check-in (formato: yyyy-MM-ddTHH:mm:ss)</param>
    /// <returns>JSON estruturado com status e detalhes</returns>
    [KernelFunction, Description("Verifica o status de check-in e transação de um usuário no sistema WellHub")]
    public async Task<string> VerifyCheckinStatus(
        [Description("ID único do usuário")] string userId,
        [Description("ID do parceiro/estabelecimento")] string partnerId,
        [Description("Data e hora do check-in no formato yyyy-MM-ddTHH:mm:ss")] string timestamp)
    {
        try
        {
            // Simula latência de uma consulta real ao backend
            await Task.Delay(100);

            // Gera chave única para busca nos dados simulados
            var key = $"{userId}_{partnerId}_{timestamp}";

            CheckinResult result;

            // Verifica se existe um registro simulado específico
            if (_simulatedData.ContainsKey(key))
            {
                result = _simulatedData[key];
            }
            else
            {
                // Para dados não encontrados nos cenários simulados, simula comportamento baseado no userId
                result = SimulateRandomResult(userId);
            }

            // Serializa o resultado em JSON estruturado
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            return JsonSerializer.Serialize(result, jsonOptions);
        }
        catch (Exception ex)
        {
            // Em caso de erro, retorna um resultado de falha estruturado
            var errorResult = new CheckinResult("FALHA_TRANSACAO", $"Erro interno do sistema: {ex.Message}");
            return JsonSerializer.Serialize(errorResult, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }
    }

    /// <summary>
    /// Lista todos os registros simulados disponíveis (função auxiliar para debugging)
    /// </summary>
    [KernelFunction, Description("Lista todos os registros de check-in simulados disponíveis para teste")]
    public async Task<string> ListSimulatedRecords()
    {
        await Task.Delay(50); // Simula latência

        var records = _simulatedData.Select(kvp => new
        {
            Key = kvp.Key,
            Status = kvp.Value.Status,
            Details = kvp.Value.Detalhes
        }).ToList();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        return JsonSerializer.Serialize(records, jsonOptions);
    }

    /// <summary>
    /// Simula um resultado baseado no padrão do userId para casos não mapeados
    /// </summary>
    private CheckinResult SimulateRandomResult(string userId)
    {
        // Usa o hash do userId para gerar resultados consistentes mas variados
        var hash = userId.GetHashCode();
        var scenario = Math.Abs(hash) % 3;

        return scenario switch
        {
            0 => new CheckinResult("SUCESSO", "Check-in processado com sucesso."),
            1 => new CheckinResult("FALHA_TRANSACAO", $"Erro de transação - Código: TXN_{Math.Abs(hash) % 100:D3}"),
            _ => new CheckinResult("NAO_LOCALIZADO", "Registro de check-in não encontrado na base de dados.")
        };
    }
}

/// <summary>
/// Modelo de dados para o resultado de verificação de check-in
/// </summary>
public record CheckinResult(
    [property: Description("Status da verificação: SUCESSO, FALHA_TRANSACAO ou NAO_LOCALIZADO")] 
    string Status,
    
    [property: Description("Detalhes adicionais, código de erro ou mensagem para debugging")] 
    string Detalhes
);