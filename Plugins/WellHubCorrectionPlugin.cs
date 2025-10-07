using System.ComponentModel;
using Microsoft.SemanticKernel;
using System.Text.Json;
using SkOfflineCourse.Infra;

namespace SkOfflineCourse.Plugins;

public class WellhubCorrectionPlugin
{
    private readonly DataManager _dataManager;
    private readonly Kernel? _kernel;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public WellhubCorrectionPlugin(Kernel? kernel = null)
    {
        _kernel = kernel;
        _dataManager = new DataManager();
    }

    /// <summary>
    /// Libera o bloqueio de check-in diário do usuário modificando os dados reais
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

        try
        {
            // Buscar registros de check-in
            var checkinRecords = await _dataManager.GetCheckinRecordsAsync();
            
            bool recordFound = false;
            bool correctionMade = false;

            // Procurar registro com falha para o usuário na data especificada
            foreach (dynamic record in checkinRecords)
            {
                try
                {
                    string recordUserId = record.userId?.ToString() ?? "";
                    string timestamp = record.timestamp?.ToString() ?? "";
                    
                    if (recordUserId == userId && timestamp.StartsWith(date))
                    {
                        recordFound = true;
                        string currentStatus = record.status?.ToString() ?? "";
                        
                        // Se tem falha, corrigir para sucesso
                        if (currentStatus != "SUCESSO" && currentStatus != "SUCCESS")
                        {
                            // **NOVA FUNCIONALIDADE: Corrigir o registro no JSON real**
                            var parsedDate = DateTime.Parse(timestamp);
                            var updateSuccess = await _dataManager.UpdateCheckinRecordStatusAsync(
                                userId, 
                                record.partnerId?.ToString() ?? "", 
                                parsedDate,
                                "SUCESSO",
                                "Check-in liberado pelo sistema de correção. Transação processada com sucesso."
                            );

                            if (updateSuccess)
                            {
                                correctionMade = true;
                                Console.WriteLine($"✅ Registro atualizado no JSON: {userId} - {date}");
                                break;
                            }
                            else
                            {
                                Console.WriteLine($"❌ Falha ao atualizar registro no JSON: {userId} - {date}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erro ao processar registro: {ex.Message}");
                    continue;
                }
            }

            CorrectionResult result;
            if (!recordFound)
            {
                result = new CorrectionResult("USUARIO_NAO_ENCONTRADO", $"Nenhum registro encontrado para {userId} na data {date}");
            }
            else if (correctionMade)
            {
                result = new CorrectionResult("SUCESSO_DA_CORRECAO", $"Check-in de {userId} liberado e salvo no arquivo JSON para {date}");
            }
            else
            {
                result = new CorrectionResult("JA_LIBERADO", $"Check-in de {userId} já estava liberado para {date}");
            }

            // Gerar resposta humanizada se kernel disponível
            if (_kernel != null && result.Status == "SUCESSO_DA_CORRECAO")
            {
                try
                {
                    var humanizedResponse = await GenerateHumanizedCorrectionResponse(result, userId, date);
                    return humanizedResponse;
                }
                catch
                {
                    // Fallback para resposta JSON se LLM falhar
                    return JsonSerializer.Serialize(result, JsonOptions);
                }
            }

            return JsonSerializer.Serialize(result, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new CorrectionResult("ERRO_OPERACIONAL", $"Erro ao processar liberação: {ex.Message}");
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    /// <summary>
    /// Processa estorno de transação simulando API de pagamento
    /// </summary>
    [KernelFunction, Description("Processa estorno de uma transação no sistema de pagamento")]
    public async Task<string> ProcessRefund(
        [Description("ID da transação a ser estornada")] string transactionId,
        [Description("Motivo do estorno para auditoria")] string reason)
    {
        // Validação básica
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return JsonSerializer.Serialize(new CorrectionResult("ERRO_OPERACIONAL", "transactionId obrigatório"));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return JsonSerializer.Serialize(new CorrectionResult("ERRO_OPERACIONAL", "reason obrigatório para auditoria"));
        }

        try
        {
            // Buscar a transação nos registros
            var checkinRecords = await _dataManager.GetCheckinRecordsAsync();
            dynamic? foundTransaction = null;

            foreach (dynamic record in checkinRecords)
            {
                try
                {
                    var recordDict = record as Dictionary<string, object>;
                    if (recordDict == null) continue;

                    string recordId = recordDict.TryGetValue("id", out var idObj) ? idObj?.ToString() ?? "" : "";
                    
                    if (recordId == transactionId)
                    {
                        foundTransaction = record;
                        break;
                    }
                }
                catch
                {
                    continue;
                }
            }

            if (foundTransaction == null)
            {
                var errorResult = new CorrectionResult("TRANSACAO_NAO_ENCONTRADA", $"Transação {transactionId} não encontrada no sistema");
                return JsonSerializer.Serialize(errorResult, JsonOptions);
            }

            // Simular chamada à API de estorno
            await SimulateRefundAPI(transactionId, reason, foundTransaction);

            // Log de auditoria detalhado
            var auditLog = new
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                action = "PROCESS_REFUND",
                transactionId = transactionId,
                reason = reason,
                userId = ((Dictionary<string, object>)foundTransaction).TryGetValue("userId", out var userIdObj) ? userIdObj?.ToString() : "N/A",
                partnerId = ((Dictionary<string, object>)foundTransaction).TryGetValue("partnerId", out var partnerIdObj) ? partnerIdObj?.ToString() : "N/A",
                amount = ((Dictionary<string, object>)foundTransaction).TryGetValue("amount", out var amountObj) ? amountObj?.ToString() : "0",
                status = "SUCESSO_DA_CORRECAO"
            };

            Console.WriteLine($"📋 AUDITORIA - ESTORNO PROCESSADO:");
            Console.WriteLine($"   Transação: {transactionId}");
            Console.WriteLine($"   Motivo: {reason}");
            Console.WriteLine($"   Usuário: {auditLog.userId}");
            Console.WriteLine($"   Valor: R$ {auditLog.amount}");
            Console.WriteLine($"   Timestamp: {auditLog.timestamp}");

            var result = new CorrectionResult("SUCESSO_DA_CORRECAO", $"Estorno processado com sucesso para transação {transactionId}. Motivo: {reason}");

            // Gerar resposta humanizada se kernel disponível
            if (_kernel != null)
            {
                try
                {
                    var humanizedResponse = await GenerateHumanizedRefundResponse(result, transactionId, reason);
                    return humanizedResponse;
                }
                catch
                {
                    // Fallback para resposta JSON se LLM falhar
                    return JsonSerializer.Serialize(result, JsonOptions);
                }
            }

            return JsonSerializer.Serialize(result, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new CorrectionResult("ERRO_OPERACIONAL", $"Erro ao processar estorno: {ex.Message}");
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    /// <summary>
    /// Simula chamada à API de estorno do sistema de pagamento
    /// </summary>
    private async Task SimulateRefundAPI(string transactionId, string reason, dynamic transaction)
    {
        Console.WriteLine($"🔄 Simulando API de Estorno...");
        Console.WriteLine($"   Endpoint: POST /api/payments/refund");
        Console.WriteLine($"   TransactionId: {transactionId}");
        Console.WriteLine($"   Reason: {reason}");
        
        // Simular latência da API
        await Task.Delay(1500);
        
        Console.WriteLine($"✅ API de Estorno respondeu: SUCCESS");
        Console.WriteLine($"   RefundId: REF_{transactionId}_{DateTime.Now:yyyyMMddHHmmss}");
    }

    /// <summary>
    /// Gera resposta humanizada para estornos usando o WellhubCommunicationPlugin
    /// </summary>
    private async Task<string> GenerateHumanizedRefundResponse(CorrectionResult result, string transactionId, string reason)
    {
        if (_kernel == null) return JsonSerializer.Serialize(result, JsonOptions);

        try
        {
            var context = result.Status == "SUCESSO_DA_CORRECAO" 
                ? $"O estorno da transação {transactionId} foi processado com sucesso. Motivo: {reason}. {result.Detalhes}"
                : $"Falha ao processar o estorno da transação {transactionId}. Motivo: {reason}. {result.Detalhes}";

            var tone = result.Status == "SUCESSO_DA_CORRECAO" ? "profissional e tranquilizador" : "empático e solucionador";

            var kernelArgs = new KernelArguments
            {
                ["situacao"] = context,
                ["tom"] = tone,
                ["incluir_emojis"] = "true"
            };

            var response = await _kernel.InvokeAsync("WellhubCommunication", "GenerateTemplatedResponse", kernelArgs);
            return response.GetValue<string>() ?? JsonSerializer.Serialize(result, JsonOptions);
        }
        catch
        {
            return JsonSerializer.Serialize(result, JsonOptions);
        }
    }

    /// <summary>
    /// Lista todos os registros de check-in com falhas que podem ser corrigidos
    /// </summary>
    [KernelFunction, Description("Lista todos os registros de check-in com falhas disponíveis para correção")]
    public async Task<string> ListFailedRecords()
    {
        try
        {
            var checkinRecords = await _dataManager.GetCheckinRecordsAsync();
            var failedRecords = new List<object>();

            foreach (dynamic record in checkinRecords)
            {
                try
                {
                    string status = record.status?.ToString() ?? "";
                    if (status != "SUCESSO" && status != "SUCCESS" && !string.IsNullOrEmpty(status))
                    {
                        failedRecords.Add(new
                        {
                            userId = record.userId?.ToString() ?? "",
                            partnerId = record.partnerId?.ToString() ?? "",
                            timestamp = record.timestamp?.ToString() ?? "",
                            status = status,
                            details = record.details?.ToString() ?? "",
                            partner_name = record.partner_name?.ToString() ?? "",
                            user_name = record.user_name?.ToString() ?? ""
                        });
                    }
                }
                catch
                {
                    continue;
                }
            }

            return JsonSerializer.Serialize(new { 
                failedRecords, 
                total = failedRecords.Count,
                message = $"Encontrados {failedRecords.Count} registros com problemas que podem ser corrigidos"
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Erro ao listar registros: {ex.Message}" }, JsonOptions);
        }
    }

    [KernelFunction, Description("Corrige o check-in de um usuário em uma data específica")]
    public async Task<string> CorrectCheckin(
        [Description("ID único do usuário")] string userId,
        [Description("Data do check-in a ser corrigida (formato: yyyy-MM-dd)")] string date)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return JsonSerializer.Serialize(new CorrectionResult("ERRO_OPERACIONAL", "userId obrigatório"));
        if (string.IsNullOrWhiteSpace(date))
            return JsonSerializer.Serialize(new CorrectionResult("ERRO_OPERACIONAL", "date obrigatório"));

        try
        {
            // Buscar registros de check-in
            var checkinRecords = await _dataManager.GetCheckinRecordsAsync();
            
            bool recordFound = false;
            bool correctionMade = false;
            string foundPartnerName = "";

            // Procurar registro com falha para o usuário na data especificada
            foreach (dynamic record in checkinRecords)
            {
                try
                {
                    string recordUserId = record.userId?.ToString() ?? "";
                    string timestamp = record.timestamp?.ToString() ?? "";
                    
                    if (recordUserId == userId && timestamp.StartsWith(date))
                    {
                        recordFound = true;
                        foundPartnerName = record.partner_name?.ToString() ?? "Local não identificado";
                        string currentStatus = record.status?.ToString() ?? "";
                        
                        // Se tem falha, corrigir para sucesso
                        if (currentStatus != "SUCESSO" && currentStatus != "SUCCESS")
                        {
                            // **NOVA FUNCIONALIDADE: Corrigir o registro no JSON real**
                            var parsedDate = DateTime.Parse(timestamp);
                            var updateSuccess = await _dataManager.UpdateCheckinRecordStatusAsync(
                                userId, 
                                record.partnerId?.ToString() ?? "", 
                                parsedDate,
                                "SUCESSO",
                                "Check-in corrigido pelo sistema de correção. Problema resolvido e transação processada com sucesso."
                            );

                            if (updateSuccess)
                            {
                                correctionMade = true;
                                Console.WriteLine($"✅ Check-in corrigido e salvo: {userId} em {foundPartnerName} - {date}");
                                break;
                            }
                            else
                            {
                                Console.WriteLine($"❌ Falha ao salvar correção: {userId} - {date}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erro ao processar registro: {ex.Message}");
                    continue;
                }
            }

            CorrectionResult result;
            if (!recordFound)
            {
                result = new CorrectionResult("USUARIO_NAO_ENCONTRADO", $"Nenhum registro encontrado para usuário {userId} na data {date}");
            }
            else if (correctionMade)
            {
                result = new CorrectionResult("SUCESSO_DA_CORRECAO", $"Check-in de {userId} em {foundPartnerName} corrigido e salvo permanentemente para {date}");
            }
            else
            {
                result = new CorrectionResult("JA_CORRIGIDO", $"Check-in de {userId} em {foundPartnerName} já estava correto para {date}");
            }

            // Gerar resposta humanizada se kernel disponível
            if (_kernel != null && result.Status == "SUCESSO_DA_CORRECAO")
            {
                try
                {
                    var humanizedResponse = await GenerateHumanizedCorrectionResponse(result, userId, date);
                    return humanizedResponse;
                }
                catch
                {
                    // Fallback para resposta JSON se LLM falhar
                    return JsonSerializer.Serialize(result, JsonOptions);
                }
            }

            return JsonSerializer.Serialize(result, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new CorrectionResult("ERRO_OPERACIONAL", $"Erro ao processar correção: {ex.Message}");
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    /// <summary>
    /// Corrige TODOS os check-ins com falha de um usuário específico
    /// </summary>
    [KernelFunction, Description("Corrige todos os check-ins com problemas de um usuário")]
    public async Task<string> CorrectAllUserFailures(
        [Description("ID único do usuário")] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return JsonSerializer.Serialize(new CorrectionResult("ERRO_OPERACIONAL", "userId obrigatório"));

        try
        {
            var checkinRecords = await _dataManager.GetCheckinRecordsAsync();
            int correctionsMade = 0;
            var correctedRecords = new List<string>();

            foreach (dynamic record in checkinRecords)
            {
                try
                {
                    // Cast to Dictionary for proper access
                    var recordDict = record as Dictionary<string, object>;
                    if (recordDict == null) continue;

                    string recordUserId = recordDict.TryGetValue("userId", out var userIdObj) ? userIdObj?.ToString() ?? "" : "";
                    
                    if (recordUserId == userId)
                    {
                        string currentStatus = recordDict.TryGetValue("status", out var statusObj) ? statusObj?.ToString() ?? "" : "";
                        string timestamp = recordDict.TryGetValue("timestamp", out var timestampObj) ? timestampObj?.ToString() ?? "" : "";
                        string partnerName = recordDict.TryGetValue("partner_name", out var partnerObj) ? partnerObj?.ToString() ?? "Local não identificado" : "Local não identificado";
                        
                        // Se tem falha, corrigir para sucesso
                        if (currentStatus != "SUCESSO" && currentStatus != "SUCCESS")
                        {
                            var parsedDate = DateTime.Parse(timestamp);
                            string partnerId = recordDict.TryGetValue("partnerId", out var partnerIdObj) ? partnerIdObj?.ToString() ?? "" : "";
                            var updateSuccess = await _dataManager.UpdateCheckinRecordStatusAsync(
                                userId, 
                                partnerId, 
                                parsedDate,
                                "SUCESSO",
                                "Check-in corrigido automaticamente pelo sistema. Todos os problemas foram resolvidos."
                            );

                            if (updateSuccess)
                            {
                                correctionsMade++;
                                correctedRecords.Add($"{partnerName} em {parsedDate:dd/MM/yyyy}");
                                Console.WriteLine($"✅ Corrigido: {partnerName} - {parsedDate:dd/MM/yyyy}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erro ao processar registro: {ex.Message}");
                    continue;
                }
            }

            CorrectionResult result;
            if (correctionsMade == 0)
            {
                result = new CorrectionResult("NENHUMA_CORRECAO", $"Usuário {userId} não possui check-ins com problemas para corrigir");
            }
            else
            {
                var detalhes = $"Corrigidos {correctionsMade} check-ins: " + string.Join(", ", correctedRecords);
                result = new CorrectionResult("SUCESSO_DA_CORRECAO", detalhes);
            }

            return JsonSerializer.Serialize(result, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new CorrectionResult("ERRO_OPERACIONAL", $"Erro ao processar correções em lote: {ex.Message}");
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    /// <summary>
    /// Gera resposta humanizada para correções usando o WellhubCommunicationPlugin
    /// </summary>
    private async Task<string> GenerateHumanizedCorrectionResponse(CorrectionResult result, string userId, string date)
    {
        if (_kernel == null) return JsonSerializer.Serialize(result, JsonOptions);

        try
        {
            var context = result.Status == "SUCESSO_DA_CORRECAO" 
                ? $"O usuário {userId} teve seu check-in da data {date} corrigido com sucesso e salvo permanentemente no sistema. {result.Detalhes}"
                : $"Falha ao corrigir o check-in do usuário {userId} para a data {date}. {result.Detalhes}";

            var tone = result.Status == "SUCESSO_DA_CORRECAO" ? "positivo e comemorativo" : "empático e solucionador";

            var kernelArgs = new KernelArguments
            {
                ["situacao"] = context,
                ["tom"] = tone,
                ["incluir_emojis"] = "true"
            };

            var response = await _kernel.InvokeAsync("WellhubCommunication", "GenerateTemplatedResponse", kernelArgs);
            return response.GetValue<string>() ?? JsonSerializer.Serialize(result, JsonOptions);
        }
        catch
        {
            return JsonSerializer.Serialize(result, JsonOptions);
        }
    }
}

/// <summary>
/// Modelo de dados para o resultado de liberação de check-in
/// </summary>
public record CorrectionResult(
    [property: Description("Status da correção: SUCESSO_DA_CORRECAO, ERRO_OPERACIONAL, JA_CORRIGIDO, etc.")]
    string Status,
    
    [property: Description("Detalhes adicionais ou mensagem de erro")]
    string Detalhes
);