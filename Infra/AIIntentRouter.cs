using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SkOfflineCourse.Infra;

public class AIIntentRouter
{
    private readonly Kernel _kernel;
    private readonly Dictionary<string, List<string>> _pluginFunctions;

    // Regex patterns compilados para melhor performance
    private static readonly Regex[] UserIdPatterns = new[]
    {
        new Regex(@"user\s*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"usuário\s*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"usuario\s*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"id\s*do\s*usuário\s*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"user\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"\busr\w*\s*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)
    };

    private static readonly Regex[] PartnerIdPatterns = new[]
    {
        new Regex(@"partner\s*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"parceiro\s*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"estabelecimento\s*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"local\s*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"id\s*do\s*parceiro\s*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"partner\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase)
    };

    private static readonly Regex[] DateTimePatterns = new[]
    {
        new Regex(@"\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}:\d{2}", RegexOptions.Compiled),
        new Regex(@"\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}", RegexOptions.Compiled),
        new Regex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}", RegexOptions.Compiled),
        new Regex(@"\d{2}/\d{2}/\d{4}\s+às\s+\d{2}:\d{2}:\d{2}", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"às\s+\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"horário\s+\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}", RegexOptions.Compiled | RegexOptions.IgnoreCase)
    };

    public AIIntentRouter(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _pluginFunctions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "WellhubTransaction", new List<string> { "VerifyCheckinStatus", "ListSimulatedRecords", "GetUserInfo", "GetPartnerInfo" } },
            { "WellhubCommunication", new List<string> { "GenerateResolutionMessage", "GenerateTemplatedResponse", "AdjustMessageTone" } }
        };
    }

    public async Task<(string? plugin, string? function, KernelArguments args)> RouteAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (null, null, new KernelArguments());

        var args = new KernelArguments();

        try
        {
            var prompt = GetWellhubIntentPromptTemplate(input);

            var result = await _kernel.InvokePromptAsync(prompt);
            var response = result.ToString().Trim();

            // Extrair apenas o primeiro objeto JSON válido da resposta
            var firstJsonObject = ExtractFirstJsonObject(response);
            
            if (!string.IsNullOrEmpty(firstJsonObject))
            {
                
                // Limpar JSON problemático antes do parsing
                var jsonRaw = CleanJsonString(firstJsonObject);
                
                // Tenta fazer o parsing do JSON
                var options = new JsonSerializerOptions { 
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                
                try 
                {
                    var routeInfo = JsonSerializer.Deserialize<RouteInfo>(jsonRaw, options);
                
                    if (routeInfo != null && !string.IsNullOrEmpty(routeInfo.Plugin) && !string.IsNullOrEmpty(routeInfo.Function))
                    {
                        // Verificar se o plugin e função existem
                        if (_pluginFunctions.TryGetValue(routeInfo.Plugin, out var functions) && 
                            functions.Contains(routeInfo.Function, StringComparer.OrdinalIgnoreCase))
                        {
                            // Adicionar parâmetros ao KernelArguments
                            if (routeInfo.Parameters != null)
                            {
                                foreach (var param in routeInfo.Parameters)
                                {
                                    args[param.Key] = param.Value?.ToString();
                                }
                            }
                            
                            // Tratamentos específicos para parâmetros do WellhubTransaction
                            if (routeInfo.Function.Equals("VerifyCheckinStatus", StringComparison.OrdinalIgnoreCase))
                            {
                                // Verificar e corrigir userId
                                if (!args.ContainsKey("userId") || 
                                    string.IsNullOrWhiteSpace(args["userId"]?.ToString()) ||
                                    args["userId"]?.ToString()?.Contains("[ID do usuário extraído]") == true)
                                {
                                    var userId = ExtractUserId(input);
                                    args["userId"] = string.IsNullOrWhiteSpace(userId) ? "user_not_found" : userId;
                                }
                                
                                // Verificar e corrigir partnerId
                                if (!args.ContainsKey("partnerId") || 
                                    string.IsNullOrWhiteSpace(args["partnerId"]?.ToString()) ||
                                    args["partnerId"]?.ToString()?.Contains("[ID do parceiro extraído]") == true)
                                {
                                    var partnerId = ExtractPartnerId(input);
                                    args["partnerId"] = string.IsNullOrWhiteSpace(partnerId) ? "partner_not_found" : partnerId;
                                }
                                
                                // Verificar e corrigir timestamp
                                if (!args.ContainsKey("timestamp") || 
                                    string.IsNullOrWhiteSpace(args["timestamp"]?.ToString()) ||
                                    args["timestamp"]?.ToString()?.Contains("[timestamp no formato") == true)
                                {
                                    var timestamp = ExtractTimestamp(input);
                                    args["timestamp"] = string.IsNullOrWhiteSpace(timestamp) ? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") : timestamp;
                                }
                            }
                            
                            // Tratamento específico para GetUserInfo
                            else if (routeInfo.Function.Equals("GetUserInfo", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!args.ContainsKey("userId") || 
                                    string.IsNullOrWhiteSpace(args["userId"]?.ToString()) ||
                                    args["userId"]?.ToString()?.Contains("[ID do usuário extraído]") == true)
                                {
                                    var userId = ExtractUserId(input);
                                    args["userId"] = string.IsNullOrWhiteSpace(userId) ? "user_not_found" : userId;
                                }
                            }
                            
                            // Tratamento específico para GetPartnerInfo
                            else if (routeInfo.Function.Equals("GetPartnerInfo", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!args.ContainsKey("partnerId") || 
                                    string.IsNullOrWhiteSpace(args["partnerId"]?.ToString()) ||
                                    args["partnerId"]?.ToString()?.Contains("[ID do parceiro extraído]") == true)
                                {
                                    var partnerId = ExtractPartnerId(input);
                                    args["partnerId"] = string.IsNullOrWhiteSpace(partnerId) ? "partner_not_found" : partnerId;
                                }
                            }
                            
                            return (routeInfo.Plugin, routeInfo.Function, args);
                        }
                    }
                }
                catch (JsonException jex)
                {
                    Console.WriteLine($"Erro ao analisar JSON da resposta do modelo: {jex.Message}");
                    Console.WriteLine($"JSON que causou o erro: {jsonRaw}");
                    // Tenta limpar o JSON de caracteres problemáticos e tentar novamente
                    try
                    {
                        string cleanedJson = jsonRaw
                            .Replace("\\", "\\\\")  // Escape backslashes
                            .Replace("\r", "")      // Remove carriage returns
                            .Replace("\n", " ")     // Replace newlines with spaces
                            .Replace("/", "\\/");   // Escape forward slashes
                            
                        var routeInfo = JsonSerializer.Deserialize<RouteInfo>(cleanedJson, options);
                        
                        if (routeInfo != null && !string.IsNullOrEmpty(routeInfo.Plugin) && !string.IsNullOrEmpty(routeInfo.Function))
                        {
                            if (_pluginFunctions.TryGetValue(routeInfo.Plugin, out var functions) && 
                                functions.Contains(routeInfo.Function, StringComparer.OrdinalIgnoreCase))
                            {
                                // Processar parâmetros
                                if (routeInfo.Parameters != null)
                                {
                                    foreach (var param in routeInfo.Parameters)
                                    {
                                        args[param.Key] = param.Value?.ToString();
                                    }
                                }
                                
                                return (routeInfo.Plugin, routeInfo.Function, args);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Falha na segunda tentativa de parsing do JSON: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao rotear com modelo de IA: {ex.Message}");
        }

        // Fallback manual baseado em palavras-chave
        return TryManualRoute(input, args);
    }

    private string ExtractContentAfterKeyword(string input, string keyword)
    {
        var keywordIndex = input.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (keywordIndex < 0) return string.Empty;
        
        return input[(keywordIndex + keyword.Length)..].Trim();
    }

    private string ExtractUserId(string input)
    {
        // Usa padrões regex compilados para melhor performance
        foreach (var regex in UserIdPatterns)
        {
            var match = regex.Match(input);
            if (match.Success)
            {
                var result = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                return result;
            }
        }

        return string.Empty;
    }

    private string ExtractPartnerId(string input)
    {
        // Usa padrões regex compilados para melhor performance
        foreach (var regex in PartnerIdPatterns)
        {
            var match = regex.Match(input);
            if (match.Success)
            {
                var result = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                return result;
            }
        }

        return string.Empty;
    }

    private string ExtractTimestamp(string input)
    {
        // Usa padrões regex compilados para melhor performance
        foreach (var regex in DateTimePatterns)
        {
            var match = regex.Match(input);
            if (match.Success)
            {
                // Tenta converter para o formato ISO se necessário
                if (match.Value.Contains('T'))
                {
                    return match.Value; // Já está no formato ISO
                }
                
                if (DateTime.TryParse(match.Value, out var date))
                {
                    return date.ToString("yyyy-MM-ddTHH:mm:ss");
                }
            }
        }

        return string.Empty;
    }

    private string ExtractFirstJsonObject(string response)
    {
        var jsonStart = response.IndexOf('{');
        if (jsonStart < 0) return string.Empty;
        
        int braceCount = 0;
        int endIndex = jsonStart;
        
        for (int i = jsonStart; i < response.Length; i++)
        {
            if (response[i] == '{')
                braceCount++;
            else if (response[i] == '}')
            {
                braceCount--;
                if (braceCount == 0)
                {
                    endIndex = i;
                    break;
                }
            }
        }
        
        if (endIndex > jsonStart)
        {
            return response.Substring(jsonStart, endIndex - jsonStart + 1);
        }
        
        return string.Empty;
    }

    private string CleanJsonString(string json)
    {
        // Remove comentários de linha única (// ...)
        json = System.Text.RegularExpressions.Regex.Replace(json, @"//.*?(?=\r|\n|$)", "");
        
        // Remove comentários de bloco (/* ... */)
        json = System.Text.RegularExpressions.Regex.Replace(json, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        
        // Preservar quebras de linha dentro de strings, mas remover quebras desnecessárias
        // Primeiro, proteger strings JSON válidas
        var stringMatches = System.Text.RegularExpressions.Regex.Matches(json, @"""[^""\\]*(?:\\.[^""\\]*)*""");
        var protectedStrings = new Dictionary<string, string>();
        int counter = 0;
        
        foreach (System.Text.RegularExpressions.Match match in stringMatches)
        {
            var placeholder = $"__STRING_PLACEHOLDER_{counter}__";
            protectedStrings[placeholder] = match.Value;
            json = json.Replace(match.Value, placeholder);
            counter++;
        }
        
        // Agora limpar quebras de linha fora das strings
        json = System.Text.RegularExpressions.Regex.Replace(json, @"\r\n|\r|\n", " ");
        json = System.Text.RegularExpressions.Regex.Replace(json, @"\s+", " ");
        
        // Restaurar strings protegidas
        foreach (var kvp in protectedStrings)
        {
            json = json.Replace(kvp.Key, kvp.Value);
        }
        
        // Corrige problemas comuns de JSON
        json = json.Replace("\"null\"", "null");
        json = json.Replace("\"\"", "null");
        json = json.Replace(": null", ": null"); // Garante espaçamento correto
        
        // Fix caracteres especiais que podem quebrar JSON
        json = System.Text.RegularExpressions.Regex.Replace(json, @"(?<!\\)\\(?![""\\\/bfnrt])", @"\\");
        
        // Remove propriedades com valores null (opcional - pode ser mantido se necessário)
        // json = System.Text.RegularExpressions.Regex.Replace(json, @",\s*""[^""]*""\s*:\s*null", "");
        // json = System.Text.RegularExpressions.Regex.Replace(json, @"""[^""]*""\s*:\s*null,", "");
        
        // Remove vírgulas duplas
        json = System.Text.RegularExpressions.Regex.Replace(json, @",\s*,", ",");
        
        // Remove vírgula antes de }
        json = System.Text.RegularExpressions.Regex.Replace(json, @",\s*}", "}");
        
        // Remove vírgula após {
        json = System.Text.RegularExpressions.Regex.Replace(json, @"{\s*,", "{");
        
        return json.Trim();
    }

    private (string? plugin, string? function, KernelArguments args) TryManualRoute(string input, KernelArguments args)
    {
        var inputLower = input.ToLowerInvariant();

        // Palavras-chave para listar registros
        var listKeywords = new[] { "listar", "mostrar", "lista", "dados", "registros", "simulados", "disponíveis" };
        if (listKeywords.Any(keyword => inputLower.Contains(keyword)))
        {
            return ("WellhubTransaction", "ListSimulatedRecords", args);
        }

        // Palavras-chave para comunicação humanizada (prioridade alta)
        var communicationKeywords = new[] { "gere", "gerar", "resposta", "cliente", "template", "reembolso", "ajuste", "ajustar", "tom", "mensagem", "humanizada", "empática", "problema", "cobrança" };
        if (communicationKeywords.Any(keyword => inputLower.Contains(keyword)) && !inputLower.Contains("verificar") && !inputLower.Contains("verifique"))
        {
            // Verificar se é template específico
            if (inputLower.Contains("template") || inputLower.Contains("reembolso") || inputLower.Contains("checkin") || inputLower.Contains("erro"))
            {
                // Extrair nome do cliente
                var customerName = ExtractCustomerName(input);
                if (string.IsNullOrEmpty(customerName)) customerName = "Cliente";
                
                // Determinar tipo de scenario
                var scenarioType = "REEMBOLSO"; // Padrão
                if (inputLower.Contains("checkin")) scenarioType = "CHECKIN_LIBERADO";
                else if (inputLower.Contains("erro")) scenarioType = "ERRO_SISTEMA";
                else if (inputLower.Contains("investigação") || inputLower.Contains("investigacao")) scenarioType = "INVESTIGACAO";
                
                args["scenarioType"] = scenarioType;
                args["customerName"] = customerName;
                args["situationDetails"] = ExtractSituationFromInput(input);
                return ("WellhubCommunication", "GenerateTemplatedResponse", args);
            }
            
            // Verificar se é ajuste de tom
            if (inputLower.Contains("ajuste") || inputLower.Contains("ajustar") || inputLower.Contains("tom"))
            {
                args["originalMessage"] = "Mensagem a ser ajustada";
                args["urgencyLevel"] = "MEDIA";
                args["sensitivityLevel"] = "MEDIA";
                return ("WellhubCommunication", "AdjustMessageTone", args);
            }
            
            // Geração de resposta padrão
            args["caseContext"] = ExtractCaseContextFromInput(input);
            args["actionTaken"] = "Resolução de problema";
            args["resultStatus"] = "EM_ANDAMENTO";
            return ("WellhubCommunication", "GenerateResolutionMessage", args);
        }

        // Palavras-chave para consultar usuário
        var userKeywords = new[] { "usuário", "usuario", "user", "informações", "informacoes", "dados", "consulte", "mostre" };
        if (userKeywords.Any(keyword => inputLower.Contains(keyword)) && (inputLower.Contains("user") || inputLower.Contains("usuário") || inputLower.Contains("usuario")))
        {
            var userId = ExtractUserId(input);
            if (!string.IsNullOrEmpty(userId))
            {
                args["userId"] = userId;
                return ("WellhubTransaction", "GetUserInfo", args);
            }
        }

        // Palavras-chave para consultar parceiro
        var partnerKeywords = new[] { "parceiro", "partner", "estabelecimento", "informações", "informacoes", "dados", "consulte", "mostre" };
        if (partnerKeywords.Any(keyword => inputLower.Contains(keyword)) && (inputLower.Contains("partner") || inputLower.Contains("parceiro") || inputLower.Contains("estabelecimento")))
        {
            var partnerId = ExtractPartnerId(input);
            if (!string.IsNullOrEmpty(partnerId))
            {
                args["partnerId"] = partnerId;
                return ("WellhubTransaction", "GetPartnerInfo", args);
            }
        }

        // Palavras-chave para verificar check-in
        var verifyKeywords = new[] { "verificar", "verifique", "check-in", "checkin", "status", "transação", "transacao" };
        if (verifyKeywords.Any(keyword => inputLower.Contains(keyword)))
        {
            // Extrair parâmetros manualmente
            var userId = ExtractUserId(input);
            var partnerId = ExtractPartnerId(input);
            var timestamp = ExtractTimestamp(input);

            if (!string.IsNullOrEmpty(userId))
            {
                args["userId"] = userId;
                args["partnerId"] = string.IsNullOrEmpty(partnerId) ? "partner_default" : partnerId;
                args["timestamp"] = string.IsNullOrEmpty(timestamp) ? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") : timestamp;
                
                return ("WellhubTransaction", "VerifyCheckinStatus", args);
            }
        }

        return (null, null, args);
    }

    /// <summary>
    /// Retorna o template de prompt otimizado para roteamento de intenções da WellHub
    /// </summary>
    private string GetWellhubIntentPromptTemplate(string input)
    {
        return @$"
Você é um assistente especializado em diagnóstico de check-ins da WellHub. Sua função é identificar intenções do usuário relacionadas à verificação de transações e status de check-in.

Analise a entrada do usuário e determine qual função deve ser chamada de acordo com as seguintes opções disponíveis:

Plugin WellhubTransaction:
- VerifyCheckinStatus: Verifica o status de check-in e transação de um usuário
  Parâmetros obrigatórios:
  • userId: ID único do usuário (string)
  • partnerId: ID do parceiro/estabelecimento (string) 
  • timestamp: Data e hora do check-in no formato yyyy-MM-ddTHH:mm:ss (string)

- ListSimulatedRecords: Lista todos os registros simulados disponíveis para teste
  Parâmetros: nenhum

EXEMPLOS DE ENTRADA VÁLIDAS:
- ""Verifique o check-in do usuário user123 no parceiro partner456 em 2024-10-02T10:00:00""
- ""Consulte o status da transação do usuário user789 no estabelecimento partner123 às 2024-10-02T09:15:00""
- ""Verificar check-in de user456 em partner789 no horário 2024-10-02T11:30:00""
- ""Mostre os registros de teste disponíveis""
- ""Liste os dados simulados""

PADRÕES DE EXTRAÇÃO:
- Procure por IDs de usuário (user + números, ou apenas números)
- Procure por IDs de parceiro (partner + números, estabelecimento, local)  
- Procure por timestamps no formato ISO ou data/hora mencionados
- Palavras-chave: check-in, transação, status, verificar, consultar, parceiro, usuário

Entrada do usuário: {input}

Responda APENAS em formato JSON válido:

Para verificar check-in:
{{
  ""plugin"": ""WellhubTransaction"",
  ""function"": ""VerifyCheckinStatus"",
  ""parameters"": {{
    ""userId"": ""[ID do usuário extraído]"",
    ""partnerId"": ""[ID do parceiro extraído]"",
    ""timestamp"": ""[timestamp no formato yyyy-MM-ddTHH:mm:ss]""
  }}
}}

Para listar registros:
{{
  ""plugin"": ""WellhubTransaction"",
  ""function"": ""ListSimulatedRecords""
}}

Se não corresponder a nenhuma função:
{{
  ""plugin"": null,
  ""function"": null
}}
";
    }

    private static string ExtractCustomerName(string input)
    {
        // Implementação simples para extrair nome do cliente
        var patterns = new[] { @"cliente\s+(\w+)", @"usuário\s+(\w+)", @"user\s+(\w+)" };
        
        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }
        
        return "Cliente";
    }

    private static string ExtractSituationFromInput(string input)
    {
        var inputLower = input.ToLower();
        
        if (inputLower.Contains("erro") || inputLower.Contains("falha"))
            return "ERRO_TRANSACAO";
        if (inputLower.Contains("reembolso") || inputLower.Contains("estorno"))
            return "SOLICITACAO_REEMBOLSO";
        if (inputLower.Contains("bloqueio") || inputLower.Contains("suspensão"))
            return "CONTA_BLOQUEADA";
        if (inputLower.Contains("cartão") || inputLower.Contains("pagamento"))
            return "PROBLEMA_PAGAMENTO";
            
        return "CONSULTA_GERAL";
    }

    private static string ExtractCaseContextFromInput(string input)
    {
        var inputLower = input.ToLower();
        
        if (inputLower.Contains("urgente") || inputLower.Contains("crítico"))
            return "ALTA_PRIORIDADE";
        if (inputLower.Contains("reclamação") || inputLower.Contains("insatisfação"))
            return "RECLAMACAO";
        if (inputLower.Contains("dúvida") || inputLower.Contains("consulta"))
            return "DUVIDA_SIMPLES";
            
        return "SITUACAO_PADRAO";
    }

    private class RouteInfo
    {
        public string? Plugin { get; set; }
        public string? Function { get; set; }
        public Dictionary<string, object?>? Parameters { get; set; }
    }
}