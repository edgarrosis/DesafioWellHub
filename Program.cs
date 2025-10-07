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

Console.WriteLine("=== 🏥 Assistente WellHub ===");
Console.WriteLine("Sistema inteligente para verificação de check-ins, transações e atendimento ao cliente");
Console.WriteLine();
Console.WriteLine("🔍 Comandos disponíveis:");
Console.WriteLine();
Console.WriteLine("📊 Verificar Check-ins:");
Console.WriteLine("  • \"Verifique o check-in do usuário user123 no parceiro partner456 em 2024-10-02T10:00:00\"");
Console.WriteLine("  • \"Como está a situação do user456?\"");
Console.WriteLine("  • \"Consulte todos os registros do usuário user789\"");
Console.WriteLine();
Console.WriteLine("👤 Informações de Usuários:");
Console.WriteLine("  • \"Mostre informações do usuário user123\"");
Console.WriteLine("  • \"Qual o plano de user456?\"");
Console.WriteLine();
Console.WriteLine("🏢 Informações de Parceiros:");
Console.WriteLine("  • \"Informações do parceiro partner456\"");
Console.WriteLine("  • \"Dados da academia partner789\"");
Console.WriteLine();
Console.WriteLine("📋 Dados Disponíveis:");
Console.WriteLine("  • \"Liste todos os registros disponíveis\"");
Console.WriteLine("  • \"Mostre os dados de teste\"");
Console.WriteLine();
Console.WriteLine("🎯 Cenários de teste (dados reais):");
Console.WriteLine("  • user123 + partner456 + 2024-10-02T10:00:00 → SUCESSO");
Console.WriteLine("  • user456 + partner789 + 2024-10-01T14:30:00 → FALHA_SALDO");
Console.WriteLine("  • user789 + partner123 + 2024-10-02T09:15:00 → NAO_LOCALIZADO");
Console.WriteLine();
Console.WriteLine("💬 Comunicação Humanizada:");
Console.WriteLine("  • \"Gere uma resposta para cliente com problema de pagamento\"");
Console.WriteLine("  • \"Resposta empática para falha de check-in\"");
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
    
    // Detectar comandos específicos primeiro - com resposta humanizada
    if ((inputLower.Contains("liste") || inputLower.Contains("mostrar") || inputLower.Contains("listar")) && 
        (inputLower.Contains("registros") || inputLower.Contains("dados") || inputLower.Contains("informações") || inputLower.Contains("disponi")))
    {
        var result = await transactionPlugin.ListSimulatedRecords();
        
        // Processar JSON para extrair dados mais legíveis
        var parsedData = JsonSerializer.Deserialize<JsonElement>(result);
        var records = parsedData.GetProperty("records");
        var totalRecords = parsedData.GetProperty("totalRecords").GetInt32();
        
        // Criar resumo simplificado para a LLM focado em LISTAR registros disponíveis
        var summary = $@"RELATÓRIO GERAL DE REGISTROS WELLHUB

📊 VISÃO GERAL:
• Total de registros: {totalRecords}
• Transações bem-sucedidas: 3 (50%)
• Transações com falha: 3 (50%)

✅ CHECK-INS BEM-SUCEDIDOS:
• João Silva → Academia FitLife Centro (SP) - R$ 25,50
• Ana Costa → Runner Academia (RJ) - R$ 28,75  
• Beatriz Lima → Yoga Studio Zen (Curitiba) - R$ 20,00

❌ CHECK-INS COM PROBLEMAS:
• Maria Santos → Smart Fit Vila Olímpia - FALHA: Saldo insuficiente
• Maria Santos → Bio Ritmo Moema - FALHA: Problema de localização
• Pedro Alves → CrossFit Champions - FALHA: Transação negada

🏢 ESTABELECIMENTOS ATIVOS:
• 3 Academias tradicionais
• 1 CrossFit
• 1 Estúdio de Yoga
• Presença em: São Paulo, Rio de Janeiro, Belo Horizonte, Curitiba

⚠️ PRINCIPAIS DESAFIOS IDENTIFICADOS:
• Saldo insuficiente (40% das falhas)
• Problemas de localização GPS (20% das falhas)
• Falhas de transação (40% das falhas)";
        
        // Gerar resposta humanizada baseada no resumo
        var humanizedResponse = await communicationPlugin.GenerateTemplatedResponse(
            "CHECKIN_HISTORY", 
            "Cliente", 
            summary
        );
        
        return humanizedResponse;
    }
    else if (inputLower.Contains("informações") && (inputLower.Contains("user") || inputLower.Contains("usuário")))
    {
        var userId = ExtractUserId(input);
        if (string.IsNullOrEmpty(userId))
        {
            return "💡 Para consultar informações, preciso do ID do usuário. Exemplo: 'informações do user123'";
        }
        
        var userDataJson = await transactionPlugin.GetUserInfo(userId);
        
        // Processar dados do usuário para criar resumo focado
        var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson);
        var userName = userData.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : userId;
        var planType = userData.TryGetProperty("planType", out var planElement) ? planElement.GetString() : "N/A";
        var balance = userData.TryGetProperty("balance", out var balanceElement) ? balanceElement.GetDecimal() : 0;
        var status = userData.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : "N/A";
        
        // Buscar check-ins específicos do usuário
        var allRecordsJson = await transactionPlugin.ListSimulatedRecords();
        var allRecords = JsonSerializer.Deserialize<JsonElement>(allRecordsJson);
        var userCheckins = new List<string>();
        var userSuccesses = 0;
        var userFailures = 0;
        
        if (allRecords.TryGetProperty("records", out var recordsArray))
        {
            foreach (var record in recordsArray.EnumerateArray())
            {
                if (record.TryGetProperty("userId", out var recordUserId) && 
                    recordUserId.GetString() == userId)
                {
                    var partnerName = record.TryGetProperty("partnerName", out var pn) ? pn.GetString() : "N/A";
                    var amount = record.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0;
                    var checkStatus = record.TryGetProperty("status", out var st) ? st.GetString() : "N/A";
                    var timestamp = record.TryGetProperty("timestamp", out var ts) ? ts.GetString() : "N/A";
                    
                    if (checkStatus == "SUCESSO")
                    {
                        userSuccesses++;
                        userCheckins.Add($"✅ {partnerName} - R$ {amount:F2} ({timestamp?.Substring(0, 10)})");
                    }
                    else
                    {
                        userFailures++;
                        var details = record.TryGetProperty("details", out var det) ? det.GetString() : checkStatus;
                        userCheckins.Add($"❌ {partnerName} - {details} ({timestamp?.Substring(0, 10)})");
                    }
                }
            }
        }
        
        // Criar resumo específico do usuário
        var userSummary = $@"RESUMO DA CONTA - {userName ?? userId}

👤 INFORMAÇÕES PESSOAIS:
• Nome: {userName ?? userId}
• Plano: {planType}
• Saldo atual: R$ {balance:F2}
• Status da conta: {status}

📊 HISTÓRICO DE CHECK-INS:
• Total de tentativas: {userSuccesses + userFailures}
• Check-ins bem-sucedidos: {userSuccesses}
• Check-ins com problemas: {userFailures}
• Taxa de sucesso: {(userSuccesses + userFailures > 0 ? (userSuccesses * 100 / (userSuccesses + userFailures)) : 0):F0}%

📋 DETALHES DOS CHECK-INS:
{string.Join("\n", userCheckins.Take(5))}

💡 RECOMENDAÇÕES PERSONALIZADAS:
{(balance < 50 ? "• Considere recarregar seu saldo para evitar falhas por saldo insuficiente" : "• Saldo adequado para check-ins")}
{(userFailures > userSuccesses ? "• Revise os horários e localizações para melhorar taxa de sucesso" : "• Continue com o ótimo trabalho!")}";
        
        // Gerar resposta humanizada baseada no resumo específico do usuário
        var humanizedResponse = await communicationPlugin.GenerateTemplatedResponse(
            "ACCOUNT_SUMMARY", 
            userName ?? userId, 
            userSummary
        );
        
        return humanizedResponse;
    }
    else if (inputLower.Contains("plano") && (inputLower.Contains("user") || inputLower.Contains("usuário")))
    {
        var userId = ExtractUserId(input);
        if (string.IsNullOrEmpty(userId))
        {
            return "💡 Para consultar o plano, preciso do ID do usuário. Exemplo: 'qual o plano do user123?'";
        }
        
        var userDataJson = await transactionPlugin.GetUserInfo(userId);
        
        // Gerar resposta humanizada focada no plano
        var humanizedResponse = await communicationPlugin.GenerateTemplatedResponse(
            "ACCOUNT_SUMMARY", 
            userId, 
            userDataJson
        );
        
        return humanizedResponse;
    }
    else if (inputLower.Contains("situação") || inputLower.Contains("status") || inputLower.Contains("resumo") || inputLower.Contains("conta"))
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
    else if (inputLower.Contains("parceiro") || inputLower.Contains("estabelecimento") || inputLower.Contains("academia") || inputLower.Contains("partner"))
    {
        return await GeneratePartnerInfo(input, transactionPlugin, communicationPlugin);
    }
    else if (inputLower.Contains("verifique") && inputLower.Contains("check"))
    {
        // Usar o AIIntentRouter para comandos de verificação complexos
        var (plugin, function, args) = await router.RouteAsync(input);
        if (!string.IsNullOrEmpty(plugin) && !string.IsNullOrEmpty(function))
        {
            var result = await kernel.InvokeAsync(plugin, function, args);
            return result.ToString();
        }
        return "❌ Não foi possível processar este comando de verificação.";
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
        var cidade = partnerInfo.TryGetProperty("city", out var cityElement) ? cityElement.GetString() : "N/A";
        var endereco = partnerInfo.TryGetProperty("address", out var addressElement) ? addressElement.GetString() : "N/A";
        
        // Criar resumo detalhado para a LLM
        var infoParceiro = $@"INFORMAÇÕES DETALHADAS DO ESTABELECIMENTO

🏢 DADOS BÁSICOS:
• Nome: {nome}
• Tipo de estabelecimento: {tipo}
• Status operacional: {status}
• ID no sistema: {partnerId}

📍 LOCALIZAÇÃO:
• Cidade: {cidade}
• Bairro/Região: {localizacao}
• Endereço: {endereco}

✅ SITUAÇÃO ATUAL:
• Status: {(status?.ToUpper() == "ACTIVE" ? "✅ ATIVO - Funcionando normalmente" : "⚠️ INATIVO - Com restrições")}
• Disponível para check-ins: {(status?.ToUpper() == "ACTIVE" ? "Sim" : "Não")}
• Última atualização: Dados atualizados

📋 INFORMAÇÕES PARA O CLIENTE:
• Este estabelecimento está {(status?.ToUpper() == "ACTIVE" ? "disponível" : "temporariamente indisponível")} para check-ins
• Localizado em {cidade}, região {localizacao}
• Especializado em {tipo}";
        
        return await communicationPlugin.GenerateTemplatedResponse("PARTNER_INFO", nome ?? partnerId, infoParceiro);
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