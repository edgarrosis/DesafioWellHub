using System.ComponentModel;
using Microsoft.SemanticKernel;
using System.Text.Json;
using SkOfflineCourse.Infra;

namespace SkOfflineCourse.Plugins;

public class WellhubTransactionPlugin
{
    private readonly DataManager _dataManager;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public WellhubTransactionPlugin()
    {
        _dataManager = new DataManager();
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
            // Valida se pelo menos userId foi fornecido
            if (string.IsNullOrWhiteSpace(userId))
            {
                var validationError = new CheckinResult("ERRO_VALIDACAO", "ID do usuário é obrigatório");
                return JsonSerializer.Serialize(validationError, JsonOptions);
            }

            // Se não temos todos os parâmetros, faz busca flexível por usuário
            if (string.IsNullOrWhiteSpace(partnerId) || partnerId == "partner_not_found" || 
                string.IsNullOrWhiteSpace(timestamp) || timestamp.StartsWith("2025-"))
            {
                return await SearchUserRecords(userId);
            }

            // Busca registro específico nos dados JSON
            var checkinRecord = await _dataManager.FindCheckinRecordAsync(userId, partnerId, timestamp);
            
            if (checkinRecord != null)
            {
                // Busca informações complementares
                var user = await _dataManager.FindUserAsync(userId);
                var partner = await _dataManager.FindPartnerAsync(partnerId);

                // Retorna dados detalhados do JSON
                var detailedResult = new
                {
                    Status = checkinRecord.Status,
                    Details = checkinRecord.Details,
                    TransactionId = checkinRecord.Id,
                    Amount = checkinRecord.Amount,
                    User = new
                    {
                        Id = checkinRecord.UserId,
                        Name = checkinRecord.UserName,
                        Plan = user?.Plan ?? "UNKNOWN"
                    },
                    Partner = new
                    {
                        Id = checkinRecord.PartnerId,
                        Name = checkinRecord.PartnerName,
                        Type = partner?.Type ?? "UNKNOWN",
                        City = checkinRecord.Location.City,
                        Address = checkinRecord.Location.Address
                    },
                    Timestamp = checkinRecord.Timestamp,
                    ErrorCode = checkinRecord.ErrorCode,
                    ErrorReason = checkinRecord.ErrorReason
                };

                return JsonSerializer.Serialize(detailedResult, JsonOptions);
            }

            // Se não encontrou registro específico, retorna não localizado
            var notFoundResult = new CheckinResult("NAO_LOCALIZADO", $"Nenhum registro encontrado para usuário {userId}, parceiro {partnerId} no horário {timestamp}");
            return JsonSerializer.Serialize(notFoundResult, JsonOptions);
        }
        catch (Exception ex)
        {
            // Em caso de erro, retorna um resultado de falha estruturado
            var errorResult = new CheckinResult("FALHA_TRANSACAO", $"Erro interno do sistema: {ex.Message}");
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    /// <summary>
    /// Lista todos os registros de check-in disponíveis nos dados JSON
    /// </summary>
    [KernelFunction, Description("Lista todos os registros de check-in disponíveis para consulta e teste")]
    public async Task<string> ListSimulatedRecords()
    {
        try
        {
            var checkinRecords = await _dataManager.GetCheckinRecordsAsync();
            var users = await _dataManager.GetUsersAsync();
            var partners = await _dataManager.GetPartnersAsync();

            var summaryRecords = checkinRecords.Select(record => new
            {
                TransactionId = record.Id,
                UserId = record.UserId,
                UserName = record.UserName,
                PartnerId = record.PartnerId,
                PartnerName = record.PartnerName,
                PartnerType = partners.FirstOrDefault(p => p.Id == record.PartnerId)?.Type ?? "UNKNOWN",
                Timestamp = record.Timestamp,
                Status = record.Status,
                Amount = record.Amount,
                City = record.Location.City,
                Details = record.Details,
                ExampleQuery = $"Verifique o check-in do usuário {record.UserId} no parceiro {record.PartnerId} em {record.Timestamp}"
            }).ToList();

            var result = new
            {
                TotalRecords = summaryRecords.Count,
                Records = summaryRecords,
                Instructions = new
                {
                    Usage = "Use os dados acima para testar verificações de check-in",
                    ExampleQueries = summaryRecords.Take(3).Select(r => r.ExampleQuery).ToArray()
                }
            };

            return JsonSerializer.Serialize(result, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new { Error = $"Erro ao listar registros: {ex.Message}" };
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    /// <summary>
    /// Consulta informações detalhadas de um usuário
    /// </summary>
    [KernelFunction, Description("Consulta informações detalhadas de um usuário do sistema WellHub")]
    public async Task<string> GetUserInfo(
        [Description("ID único do usuário")] string userId)
    {
        try
        {
            var user = await _dataManager.FindUserAsync(userId);
            
            if (user == null)
            {
                var notFound = new { Error = $"Usuário {userId} não encontrado" };
                return JsonSerializer.Serialize(notFound, JsonOptions);
            }

            var userInfo = new
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Plan = user.Plan,
                Status = user.Status,
                CreditBalance = user.CreditBalance,
                MonthlyLimit = user.MonthlyLimit,
                Location = user.Location,
                PreferredActivities = user.PreferredActivities,
                RegistrationDate = user.RegistrationDate,
                PaymentIssue = user.PaymentIssue,
                SuspensionReason = user.SuspensionReason
            };

            return JsonSerializer.Serialize(userInfo, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new { Error = $"Erro ao consultar usuário: {ex.Message}" };
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    /// <summary>
    /// Consulta informações detalhadas de um parceiro
    /// </summary>
    [KernelFunction, Description("Consulta informações detalhadas de um parceiro/estabelecimento do sistema WellHub")]
    public async Task<string> GetPartnerInfo(
        [Description("ID único do parceiro/estabelecimento")] string partnerId)
    {
        try
        {
            var partner = await _dataManager.FindPartnerAsync(partnerId);
            
            if (partner == null)
            {
                var notFound = new { Error = $"Parceiro {partnerId} não encontrado" };
                return JsonSerializer.Serialize(notFound, JsonOptions);
            }

            var partnerInfo = new
            {
                Id = partner.Id,
                Name = partner.Name,
                Type = partner.Type,
                City = partner.City,
                Address = partner.Address,
                Phone = partner.Phone,
                Email = partner.Email,
                OperatingHours = partner.OperatingHours,
                Services = partner.Services,
                Active = partner.Active,
                ClosureReason = partner.ClosureReason
            };

            return JsonSerializer.Serialize(partnerInfo, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new { Error = $"Erro ao consultar parceiro: {ex.Message}" };
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    /// <summary>
    /// Busca todos os registros de check-in de um usuário específico
    /// </summary>
    private async Task<string> SearchUserRecords(string userId)
    {
        try
        {
            var allRecords = await _dataManager.GetCheckinRecordsAsync();
            var userRecords = allRecords.Where(r => r.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)).ToArray();

            if (!userRecords.Any())
            {
                var notFound = new CheckinResult("NAO_LOCALIZADO", $"Nenhum registro de check-in encontrado para o usuário {userId}");
                return JsonSerializer.Serialize(notFound, JsonOptions);
            }

            // Se encontrou apenas um registro, retorna detalhado
            if (userRecords.Length == 1)
            {
                var record = userRecords[0];
                var user = await _dataManager.FindUserAsync(userId);
                var partner = await _dataManager.FindPartnerAsync(record.PartnerId);

                var detailedResult = new
                {
                    Status = record.Status,
                    Details = record.Details,
                    TransactionId = record.Id,
                    Amount = record.Amount,
                    User = new
                    {
                        Id = record.UserId,
                        Name = record.UserName,
                        Plan = user?.Plan ?? "UNKNOWN"
                    },
                    Partner = new
                    {
                        Id = record.PartnerId,
                        Name = record.PartnerName,
                        Type = partner?.Type ?? "UNKNOWN",
                        City = record.Location.City,
                        Address = record.Location.Address
                    },
                    Timestamp = record.Timestamp,
                    ErrorCode = record.ErrorCode,
                    ErrorReason = record.ErrorReason
                };

                return JsonSerializer.Serialize(detailedResult, JsonOptions);
            }

            // Se encontrou múltiplos registros, retorna lista resumida
            var summaryResults = userRecords.Select(record => new
            {
                TransactionId = record.Id,
                Status = record.Status,
                PartnerName = record.PartnerName,
                Timestamp = record.Timestamp,
                Amount = record.Amount,
                Details = record.Details
            }).ToArray();

            var multipleResult = new
            {
                Message = $"Encontrados {userRecords.Length} registros para o usuário {userId}",
                UserId = userId,
                UserName = userRecords[0].UserName,
                Records = summaryResults,
                Suggestion = "Use um comando mais específico com parceiro e timestamp para ver detalhes completos"
            };

            return JsonSerializer.Serialize(multipleResult, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new CheckinResult("FALHA_TRANSACAO", $"Erro na busca por registros do usuário: {ex.Message}");
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
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