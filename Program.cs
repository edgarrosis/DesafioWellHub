using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SkOfflineCourse.Infra;
using SkOfflineCourse.Plugins;
using System.Text;
using System.Text.Json;

// Configurar codificação para exibir corretamente caracteres especiais
Console.OutputEncoding = Encoding.UTF8;

// Configuração do modelo de linguagem de IA
var kernelBuilder = Kernel.CreateBuilder();

try
{
    // Configuração para o modelo de linguagem local via HTTP
    // IMPORTANTE: Verifique se o modelo llama3.2:3b está disponível em sua instalação Ollama
    // Para verificar: ollama list
    // Para baixar: ollama pull llama3.2:3b
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: "llama3.2:3b",
        apiKey: "apiKey",
        httpClient: new HttpClient { 
            BaseAddress = new Uri("http://localhost:11434/v1/")
        });
        
    Console.WriteLine("✅ Modelo de IA conectado com sucesso!");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Modelo de IA não disponível. Continuando com templates estáticos: {ex.Message}");
}

var kernel = kernelBuilder.Build();

// Cria plugins WellHub
var wellhubTransaction = new WellhubTransactionPlugin();
var wellhubCommunication = new WellhubCommunicationPlugin(kernel);

// Registrando plugins no Kernel
kernel.ImportPluginFromObject(wellhubTransaction, "WellhubTransaction");
kernel.ImportPluginFromObject(wellhubCommunication, "WellhubCommunication");

// Router usando LLM
var router = new AIIntentRouter(kernel);

Console.WriteLine("=== 🏥 Assistente de Diagnóstico WellHub ===");
Console.WriteLine("Sistema especializado em verificação de check-ins e transações");
Console.WriteLine();
Console.WriteLine("🔍 Verificações disponíveis:");
Console.WriteLine();
Console.WriteLine("📊 Verificar Status de Check-in:");
Console.WriteLine("  • \"Verifique o check-in do usuário user123 no parceiro partner456 em 2024-10-02T10:00:00\"");
Console.WriteLine("  • \"Consulte o status da transação do usuário user789 no estabelecimento partner123 às 2024-10-02T09:15:00\"");
Console.WriteLine("  • \"Verificar check-in de user456 em partner789 no horário 2024-10-02T11:30:00\"");
Console.WriteLine();
Console.WriteLine("📋 Registros de Teste:");
Console.WriteLine("  • \"Mostre os registros de teste disponíveis\"");
Console.WriteLine("  • \"Liste os dados simulados\"");
Console.WriteLine();
Console.WriteLine("🎯 Cenários de teste pré-configurados:");
Console.WriteLine("  • user123 + partner456 + 2024-10-02T10:00:00 → SUCESSO");
Console.WriteLine("  • user456 + partner789 + 2024-10-02T11:30:00 → FALHA_TRANSACAO");
Console.WriteLine("  • user789 + partner123 + 2024-10-02T09:15:00 → NAO_LOCALIZADO");
Console.WriteLine();
Console.WriteLine("Digite 'sair' ou 'exit' para encerrar");
Console.WriteLine("----------------------------------------");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("sair", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

    try
    {
        // Analisar o input e gerar resposta integrada
        var response = await ProcessUserRequest(input, wellhubTransaction, wellhubCommunication);
        Console.WriteLine();
        Console.WriteLine(response);
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Erro: {ex.Message}");
    }
}

Console.WriteLine("👋 Obrigado por usar o assistente WellHub!");

// Função para processar requisições do usuário e integrar dados com respostas humanizadas
async Task<string> ProcessUserRequest(string input, WellhubTransactionPlugin transactionPlugin, WellhubCommunicationPlugin communicationPlugin)
{
    var inputLower = input.ToLowerInvariant();
    
    // Detectar tipo de consulta baseado no input
    if (inputLower.Contains("situação") || inputLower.Contains("status") || inputLower.Contains("resumo") || inputLower.Contains("conta"))
    {
        return await GenerateUserSituation(input, transactionPlugin, communicationPlugin);
    }
    else if (inputLower.Contains("checkin") || inputLower.Contains("histórico") || inputLower.Contains("atividade"))
    {
        return await GenerateCheckinAnalysis(input, transactionPlugin, communicationPlugin);
    }
    else if (inputLower.Contains("falha") || inputLower.Contains("problema") || inputLower.Contains("erro"))
    {
        return await GenerateFailureAnalysis(input, transactionPlugin, communicationPlugin);
    }
    else if (inputLower.Contains("parceiro") || inputLower.Contains("estabelecimento") || inputLower.Contains("academia"))
    {
        return await GeneratePartnerInfo(input, transactionPlugin, communicationPlugin);
    }
    else
    {
        return await GenerateGeneralResponse(input, transactionPlugin, communicationPlugin);
    }
}

// Gera resposta sobre a situação geral do usuário
async Task<string> GenerateUserSituation(string input, WellhubTransactionPlugin transactionPlugin, WellhubCommunicationPlugin communicationPlugin)
{
    try
    {
        // Extrair ID do usuário do input (simples regex)
        var userId = ExtractUserId(input);
        if (string.IsNullOrEmpty(userId))
        {
            return "💡 Para consultar a situação, preciso do ID do usuário. Exemplo: 'Como está a situação do usuário user123?'";
        }
        
        // Obter dados do usuário
        var userInfoJson = await transactionPlugin.GetUserInfo(userId);
        var userInfo = JsonSerializer.Deserialize<JsonElement>(userInfoJson);
        
        // Obter dados de check-in
        var checkinStatusJson = await transactionPlugin.VerifyCheckinStatus(userId, "partner_not_found", "2025-01-01T00:00:00");
        var checkinStatus = JsonSerializer.Deserialize<JsonElement>(checkinStatusJson);
        
        // Extrair informações
        var nome = userInfo.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : userId;
        var plano = userInfo.TryGetProperty("plan", out var planElement) ? planElement.GetString() : "N/A";
        var status = userInfo.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : "N/A";
        var saldo = userInfo.TryGetProperty("creditBalance", out var balanceElement) ? balanceElement.GetDecimal() : 0;
        
        // Verificar estrutura de resposta do TransactionPlugin para situação
        var checkinRecordsSituation = new List<JsonElement>();
        
        if (checkinStatus.TryGetProperty("Records", out var recordsPropSituation))
        {
            // Múltiplos registros
            foreach (var record in recordsPropSituation.EnumerateArray())
            {
                checkinRecordsSituation.Add(record);
            }
        }
        else if (checkinStatus.TryGetProperty("Status", out var singleStatusPropSituation) && 
                 singleStatusPropSituation.GetString() != "NAO_LOCALIZADO")
        {
            // Registro único
            checkinRecordsSituation.Add(checkinStatus);
        }
        
        var totalCheckins = checkinRecordsSituation.Count;
        
        // Contar sucessos e falhas
        var sucessos = 0;
        var falhas = 0;
        foreach (var record in checkinRecordsSituation)
        {
            var recordStatus = record.TryGetProperty("Status", out var st) ? st.GetString() : "";
            if (recordStatus?.ToUpper() == "SUCESSO" || recordStatus?.ToUpper() == "SUCCESS")
                sucessos++;
            else
                falhas++;
        }
        
        // Preparar contexto para resposta humanizada
        var situacao = $@"Usuário: {nome} (ID: {userId})
Plano: {plano}
Status da conta: {status}
Saldo atual: R$ {saldo:F2}
Total de check-ins: {totalCheckins}
Check-ins bem-sucedidos: {sucessos}
Check-ins com falha: {falhas}
Situação geral: {(status?.ToUpper() == "ACTIVE" ? "Conta ativa" : "Conta com restrições")}";
        
        // Gerar resposta humanizada
        return await communicationPlugin.GenerateTemplatedResponse("account_summary", nome ?? userId, situacao);
    }
    catch (Exception ex)
    {
        return $"❌ Erro ao consultar situação do usuário: {ex.Message}";
    }
}

// Gera análise de check-ins
async Task<string> GenerateCheckinAnalysis(string input, WellhubTransactionPlugin transactionPlugin, WellhubCommunicationPlugin communicationPlugin)
{
    try
    {
        var userId = ExtractUserId(input);
        if (string.IsNullOrEmpty(userId))
        {
            return "💡 Para analisar check-ins, preciso do ID do usuário. Exemplo: 'Verifique os check-ins do user123'";
        }
        
        var userInfoJson = await transactionPlugin.GetUserInfo(userId);
        var userInfo = JsonSerializer.Deserialize<JsonElement>(userInfoJson);
        var nome = userInfo.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : userId;
        
        var checkinStatusJson = await transactionPlugin.VerifyCheckinStatus(userId, "partner_not_found", "2025-01-01T00:00:00");
        var checkinStatus = JsonSerializer.Deserialize<JsonElement>(checkinStatusJson);
        
        // Verificar estrutura de resposta do TransactionPlugin
        var isNoRecords = false;
        var checkinRecords = new List<JsonElement>();
        
        if (checkinStatus.TryGetProperty("status", out var statusProp) && 
            statusProp.GetString() == "NAO_LOCALIZADO")
        {
            isNoRecords = true;
        }
        else if (checkinStatus.TryGetProperty("records", out var recordsProp))
        {
            // Múltiplos registros
            foreach (var record in recordsProp.EnumerateArray())
            {
                checkinRecords.Add(record);
            }
        }
        else if (checkinStatus.TryGetProperty("status", out var singleStatusProp))
        {
            // Registro único
            checkinRecords.Add(checkinStatus);
        }
        else
        {
            isNoRecords = true;
        }
        
        if (isNoRecords || checkinRecords.Count == 0)
        {
            // Obter dados completos do usuário mesmo sem check-ins
            var userInfoNoCheckinsJson = await transactionPlugin.GetUserInfo(userId);
            var userInfoNoCheckins = JsonSerializer.Deserialize<JsonElement>(userInfoNoCheckinsJson);
            var planoNoCheckins = userInfoNoCheckins.TryGetProperty("plan", out var planNoCheckinsElement) ? planNoCheckinsElement.GetString() : "N/A";
            var statusNoCheckins = userInfoNoCheckins.TryGetProperty("status", out var statusNoCheckinsElement) ? statusNoCheckinsElement.GetString() : "N/A";
            var saldoNoCheckins = userInfoNoCheckins.TryGetProperty("creditBalance", out var balanceNoCheckinsElement) ? balanceNoCheckinsElement.GetDecimal() : 0;
            var emailNoCheckins = userInfoNoCheckins.TryGetProperty("email", out var emailNoCheckinsElement) ? emailNoCheckinsElement.GetString() : "N/A";
            
            var semCheckins = $@"DADOS COMPLETOS DO USUÁRIO:
Nome: {nome}
ID: {userId}
Plano: {planoNoCheckins}
Status da conta: {statusNoCheckins}
Email: {emailNoCheckins}
Saldo atual: R$ {saldoNoCheckins:F2}

SITUAÇÃO: Usuário ainda não realizou nenhum check-in
PERFIL: {(statusNoCheckins?.ToUpper() == "ACTIVE" ? "Conta ativa, pronto para começar" : "Conta com restrições, pode precisar de suporte")}
RECOMENDAÇÃO: {(saldoNoCheckins > 0 ? "Tem saldo disponível para atividades" : "Pode precisar de recarga para usar os serviços")}";
            
            return await communicationPlugin.GenerateTemplatedResponse("no_checkins_found", nome ?? userId, semCheckins);
        }
        
        // Obter dados completos do usuário para contexto
        var userInfoCompleteJson = await transactionPlugin.GetUserInfo(userId);
        var userInfoComplete = JsonSerializer.Deserialize<JsonElement>(userInfoCompleteJson);
        var planoCompleto = userInfoComplete.TryGetProperty("plan", out var planCompleteElement) ? planCompleteElement.GetString() : "N/A";
        var statusCompleto = userInfoComplete.TryGetProperty("status", out var statusCompleteElement) ? statusCompleteElement.GetString() : "N/A";
        var saldoCompleto = userInfoComplete.TryGetProperty("creditBalance", out var balanceCompleteElement) ? balanceCompleteElement.GetDecimal() : 0;
        
        var detalhes = new List<string>();
        var sucessos = 0;
        var totalGasto = 0m;
        var ultimaAtividade = "";
        
        foreach (var record in checkinRecords)
        {
            // Tentar diferentes propriedades dependendo da estrutura (JSON usa camelCase)
            var partner = record.TryGetProperty("partner", out var partnerObj) && partnerObj.TryGetProperty("name", out var partnerName) ? partnerName.GetString() :
                         record.TryGetProperty("PartnerName", out var pn) ? pn.GetString() :
                         "Local não identificado";
            
            var data = record.TryGetProperty("timestamp", out var date) ? date.GetString() : 
                      record.TryGetProperty("Timestamp", out var dateUpper) ? dateUpper.GetString() :
                      "Data não informada";
            var recordStatus = record.TryGetProperty("status", out var st) ? st.GetString() : 
                              record.TryGetProperty("Status", out var stUpper) ? stUpper.GetString() :
                              "";
            var valor = record.TryGetProperty("amount", out var amount) ? amount.GetDecimal() : 
                       record.TryGetProperty("Amount", out var amountUpper) ? amountUpper.GetDecimal() :
                       0m;
            
            if (recordStatus?.ToUpper() == "SUCESSO" || recordStatus?.ToUpper() == "SUCCESS")
            {
                sucessos++;
                totalGasto += valor;
            }
            
            if (string.IsNullOrEmpty(ultimaAtividade) && !string.IsNullOrEmpty(data))
            {
                ultimaAtividade = $"{partner} em {data}";
            }
                
            var detalheCompleto = valor > 0 ? $"• {partner} - {data} ({recordStatus}) - R$ {valor:F2}" 
                                            : $"• {partner} - {data} ({recordStatus})";
            detalhes.Add(detalheCompleto);
        }
        
        var resumoCheckins = $@"DADOS COMPLETOS DO USUÁRIO:
Nome: {nome}
ID: {userId}
Plano: {planoCompleto}
Status da conta: {statusCompleto}
Saldo atual: R$ {saldoCompleto:F2}

ESTATÍSTICAS DE CHECK-INS:
Total de check-ins: {checkinRecords.Count}
Check-ins bem-sucedidos: {sucessos}
Check-ins com problemas: {checkinRecords.Count - sucessos}
Valor total gasto: R$ {totalGasto:F2}
Última atividade: {ultimaAtividade}

HISTÓRICO DETALHADO:
{string.Join("\n", detalhes.Take(5))}
{(detalhes.Count > 5 ? $"\n... e mais {detalhes.Count - 5} atividades registradas" : "")}

PERFIL DE USO: {(sucessos == checkinRecords.Count ? "Usuário consistente, sem problemas" : "Usuário com algumas dificuldades técnicas")}";
        
        return await communicationPlugin.GenerateTemplatedResponse("checkin_history", nome ?? userId, resumoCheckins);
    }
    catch (Exception ex)
    {
        return $"❌ Erro ao analisar check-ins: {ex.Message}";
    }
}

// Gera análise de falhas
async Task<string> GenerateFailureAnalysis(string input, WellhubTransactionPlugin transactionPlugin, WellhubCommunicationPlugin communicationPlugin)
{
    try
    {
        var userId = ExtractUserId(input);
        if (string.IsNullOrEmpty(userId))
        {
            return "💡 Para analisar falhas, preciso do ID do usuário. Exemplo: 'Analise as falhas do user456'";
        }
        
        var userInfoJson = await transactionPlugin.GetUserInfo(userId);
        var userInfo = JsonSerializer.Deserialize<JsonElement>(userInfoJson);
        var nome = userInfo.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : userId;
        
        var checkinStatusJson = await transactionPlugin.VerifyCheckinStatus(userId, "partner_not_found", "2025-01-01T00:00:00");
        var checkinStatus = JsonSerializer.Deserialize<JsonElement>(checkinStatusJson);
        
        // Verificar estrutura de resposta do TransactionPlugin para falhas
        var isNoRecordsFailure = false;
        var checkinRecordsFailure = new List<JsonElement>();
        
        if (checkinStatus.TryGetProperty("Status", out var statusPropFailure) && 
            statusPropFailure.GetString() == "NAO_LOCALIZADO")
        {
            isNoRecordsFailure = true;
        }
        else if (checkinStatus.TryGetProperty("Records", out var recordsPropFailure))
        {
            // Múltiplos registros
            foreach (var record in recordsPropFailure.EnumerateArray())
            {
                checkinRecordsFailure.Add(record);
            }
        }
        else if (checkinStatus.TryGetProperty("Status", out var singleStatusPropFailure))
        {
            // Registro único
            checkinRecordsFailure.Add(checkinStatus);
        }
        else
        {
            isNoRecordsFailure = true;
        }
        
        if (isNoRecordsFailure || checkinRecordsFailure.Count == 0)
        {
            var semDados = $"Usuário {nome} não possui histórico de check-ins para análise de falhas.";
            return await communicationPlugin.GenerateTemplatedResponse("no_failures_found", nome ?? userId, semDados);
        }
        
        var falhas = new List<string>();
        var sucessos = 0;
        foreach (var record in checkinRecordsFailure)
        {
            var recordStatus = record.TryGetProperty("Status", out var st) ? st.GetString() : "";
            if (recordStatus?.ToUpper() == "SUCESSO" || recordStatus?.ToUpper() == "SUCCESS")
            {
                sucessos++;
            }
            else
            {
                var partner = record.TryGetProperty("PartnerName", out var pn) ? pn.GetString() :
                             record.TryGetProperty("Partner", out var partnerObj) && partnerObj.TryGetProperty("Name", out var partnerName) ? partnerName.GetString() :
                             "Local não identificado";
                var motivo = record.TryGetProperty("ErrorReason", out var er) ? er.GetString() : 
                            record.TryGetProperty("Details", out var details) ? details.GetString() :
                            "Motivo não especificado";
                falhas.Add($"• {partner}: {motivo}");
            }
        }
        
        if (falhas.Count == 0)
        {
            var perfeito = $"Usuário {nome} tem performance perfeita! {sucessos} check-ins, todos bem-sucedidos.";
            return await communicationPlugin.GenerateTemplatedResponse("no_failures_found", nome ?? userId, perfeito);
        }
        
        // Obter dados completos do usuário para contexto
        var userInfoCompleto = await transactionPlugin.GetUserInfo(userId);
        var userInfoData = JsonSerializer.Deserialize<JsonElement>(userInfoCompleto);
        var plano = userInfoData.TryGetProperty("plan", out var planElement) ? planElement.GetString() : "N/A";
        var statusConta = userInfoData.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : "N/A";
        var saldo = userInfoData.TryGetProperty("creditBalance", out var balanceElement) ? balanceElement.GetDecimal() : 0;
        
        var analiseFalhas = $@"DADOS COMPLETOS DO USUÁRIO:
Nome: {nome}
ID: {userId}
Plano: {plano}
Status da conta: {statusConta}
Saldo atual: R$ {saldo:F2}

ANÁLISE DE PERFORMANCE:
Total de check-ins: {checkinRecordsFailure.Count}
Check-ins bem-sucedidos: {sucessos}
Check-ins com falha: {falhas.Count}
Taxa de sucesso: {(sucessos * 100.0 / checkinRecordsFailure.Count):F1}%

FALHAS DETALHADAS:
{string.Join("\n", falhas)}

POSSÍVEIS CAUSAS:
{(saldo < 20 ? "• Saldo baixo pode estar causando falhas\n" : "")}
{(statusConta?.ToUpper() != "ACTIVE" ? "• Status da conta não está ativo\n" : "")}
• Problemas técnicos nos estabelecimentos
• Tentativas fora do horário de funcionamento

RECOMENDAÇÕES PERSONALIZADAS:
{(saldo < 20 ? "• Considere recarregar seu saldo\n" : "")}
{(statusConta?.ToUpper() != "ACTIVE" ? "• Entre em contato para reativar sua conta\n" : "")}
• Verifique horários antes de ir aos estabelecimentos";
        
        return await communicationPlugin.GenerateTemplatedResponse("failure_analysis", nome ?? userId, analiseFalhas);
    }
    catch (Exception ex)
    {
        return $"❌ Erro ao analisar falhas: {ex.Message}";
    }
}

// Gera informações sobre parceiros
async Task<string> GeneratePartnerInfo(string input, WellhubTransactionPlugin transactionPlugin, WellhubCommunicationPlugin communicationPlugin)
{
    try
    {
        var partnerId = ExtractPartnerId(input);
        if (string.IsNullOrEmpty(partnerId))
        {
            return "💡 Para consultar parceiro, preciso do ID. Exemplo: 'Status do parceiro partner456'";
        }
        
        var partnerInfoJson = await transactionPlugin.GetPartnerInfo(partnerId);
        var partnerInfo = JsonSerializer.Deserialize<JsonElement>(partnerInfoJson);
        
        var nome = partnerInfo.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : partnerId;
        var tipo = partnerInfo.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : "N/A";
        var status = partnerInfo.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : "N/A";
        var localizacao = partnerInfo.TryGetProperty("location", out var locationElement) ? locationElement.GetString() : "N/A";
        
        var infoParceiro = $@"Estabelecimento: {nome}
Tipo: {tipo}
Status: {status}
Localização: {localizacao}
Situação: {(status?.ToUpper() == "ACTIVE" ? "Funcionando normalmente" : "Com restrições")}";
        
        return await communicationPlugin.GenerateTemplatedResponse("partner_info", nome ?? partnerId, infoParceiro);
    }
    catch (Exception ex)
    {
        return $"❌ Erro ao consultar parceiro: {ex.Message}";
    }
}

// Resposta geral para consultas não específicas
async Task<string> GenerateGeneralResponse(string input, WellhubTransactionPlugin transactionPlugin, WellhubCommunicationPlugin communicationPlugin)
{
    var contexto = $"Consulta geral do usuário: {input}";
    return await communicationPlugin.GenerateResolutionMessage(contexto, "Consulta de informações", "EM_ANDAMENTO");
}

// Função auxiliar para extrair ID do usuário
string ExtractUserId(string input)
{
    var patterns = new[] { @"user\d+", @"usuário\s+(\w+)", @"user\s+(\w+)" };
    foreach (var pattern in patterns)
    {
        var match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
        }
    }
    return "";
}

// Função auxiliar para extrair ID do parceiro
string ExtractPartnerId(string input)
{
    var patterns = new[] { @"partner\d+", @"parceiro\s+(\w+)", @"partner\s+(\w+)" };
    foreach (var pattern in patterns)
    {
        var match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
        }
    }
    return "";
}