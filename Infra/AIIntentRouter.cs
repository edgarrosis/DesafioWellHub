using Microsoft.SemanticKernel;
using System.Text.Json;

namespace SkOfflineCourse.Infra;

public class AIIntentRouter
{
    private readonly Kernel _kernel;
    private readonly Dictionary<string, List<string>> _pluginFunctions;

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
            var prompt = @$"
Você é um assistente especializado da WellHub para diagnóstico de check-ins e comunicação humanizada.

Analise a entrada do usuário e determine qual função chamar:

Plugin WellhubTransaction:
- VerifyCheckinStatus: Verifica status de check-in (parâmetros: userId, partnerId, timestamp)
- ListSimulatedRecords: Lista registros de teste (sem parâmetros)
- GetUserInfo: Consulta informações de usuário (parâmetros: userId)
- GetPartnerInfo: Consulta informações de parceiro (parâmetros: partnerId)

Plugin WellhubCommunication:
- GenerateResolutionMessage: Gera resposta humanizada (parâmetros: caseContext, actionTaken, resultStatus)
- GenerateTemplatedResponse: Usa templates específicos (parâmetros: scenarioType, customerName, situationDetails)
- AdjustMessageTone: Ajusta tom da mensagem (parâmetros: originalMessage, urgencyLevel, sensitivityLevel)

EXEMPLOS:
- ""Verifique user123 partner456"" → WellhubTransaction.VerifyCheckinStatus
- ""Liste os dados"" ou ""Liste users"" → WellhubTransaction.ListSimulatedRecords
- ""Consulte usuário user123"" → WellhubTransaction.GetUserInfo
- ""Informações do parceiro partner456"" → WellhubTransaction.GetPartnerInfo
- ""Gere resposta para cliente com cobrança"" → WellhubCommunication.GenerateResolutionMessage

Entrada: {input}

Responda APENAS JSON:

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

Para consultar usuário:
{{
  ""plugin"": ""WellhubTransaction"",
  ""function"": ""GetUserInfo"",
  ""parameters"": {{
    ""userId"": ""[ID do usuário extraído]""
  }}
}}

Para consultar parceiro:
{{
  ""plugin"": ""WellhubTransaction"",
  ""function"": ""GetPartnerInfo"",
  ""parameters"": {{
    ""partnerId"": ""[ID do parceiro extraído]""
  }}
}}

Se não corresponder a nenhuma função:
{{
  ""plugin"": null,
  ""function"": null
}}
";

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
                                // Garantir que todos os parâmetros obrigatórios estejam presentes
                                if (!args.ContainsKey("userId"))
                                {
                                    var userId = ExtractUserId(input);
                                    args["userId"] = string.IsNullOrWhiteSpace(userId) ? "user_not_found" : userId;
                                }
                                
                                if (!args.ContainsKey("partnerId"))
                                {
                                    var partnerId = ExtractPartnerId(input);
                                    args["partnerId"] = string.IsNullOrWhiteSpace(partnerId) ? "partner_not_found" : partnerId;
                                }
                                
                                if (!args.ContainsKey("timestamp"))
                                {
                                    var timestamp = ExtractTimestamp(input);
                                    args["timestamp"] = string.IsNullOrWhiteSpace(timestamp) ? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") : timestamp;
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
        // Procura por padrões como "user123", "usuário 123", "user_123", etc.
        var patterns = new[]
        {
            @"user\s*(\w+)",
            @"usuário\s*(\w+)",
            @"usuario\s*(\w+)",
            @"id\s*do\s*usuário\s*(\w+)",
            @"user\d+",
            @"\busr\w*\s*(\w+)"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
            }
        }

        return string.Empty;
    }

    private string ExtractPartnerId(string input)
    {
        // Procura por padrões como "partner456", "parceiro 456", "estabelecimento 456", etc.
        var patterns = new[]
        {
            @"partner\s*(\w+)",
            @"parceiro\s*(\w+)",
            @"estabelecimento\s*(\w+)",
            @"local\s*(\w+)",
            @"id\s*do\s*parceiro\s*(\w+)",
            @"partner\d+",
            @"\bptr\w*\s*(\w+)"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
            }
        }

        return string.Empty;
    }

    private string ExtractTimestamp(string input)
    {
        // Procura por timestamps no formato ISO (yyyy-MM-ddTHH:mm:ss)
        var isoPattern = @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}";
        var match = System.Text.RegularExpressions.Regex.Match(input, isoPattern);
        
        if (match.Success)
        {
            return match.Value;
        }

        // Procura por outros formatos de data/hora e tenta converter
        var datePatterns = new[]
        {
            @"\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}:\d{2}",
            @"\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}",
            @"\d{2}-\d{2}-\d{4}\s+\d{2}:\d{2}"
        };

        foreach (var pattern in datePatterns)
        {
            match = System.Text.RegularExpressions.Regex.Match(input, pattern);
            if (match.Success)
            {
                // Tenta converter para o formato ISO
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

    private string ExtractCaseContextFromInput(string input)
    {
        // Extrai contexto da situação do cliente
        var keywords = new[] { "problema", "cobrança", "check-in", "acesso", "erro", "falha", "cliente" };
        var foundKeywords = keywords.Where(k => input.ToLowerInvariant().Contains(k)).ToList();
        
        if (foundKeywords.Any())
        {
            return $"Cliente relatou: {string.Join(", ", foundKeywords)}. Situação: {input}";
        }
        
        return $"Situação reportada pelo cliente: {input}";
    }

    private string ExtractCustomerName(string input)
    {
        // Buscar padrões para nome de usuário/cliente
        var patterns = new[]
        {
            @"para\s+([A-Za-z0-9_-]+)", // "para user123"
            @"cliente\s+([A-Za-z0-9_-]+)", // "cliente joão"
            @"usuário\s+([A-Za-z0-9_-]+)", // "usuário maria"
            @"usuario\s+([A-Za-z0-9_-]+)", // "usuario carlos"
            @"user\s+([A-Za-z0-9_-]+)", // "user teste"
        };
        
        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }
        
        return string.Empty;
    }

    private string ExtractSituationFromInput(string input)
    {
        // Extrai detalhes específicos da situação
        if (input.ToLowerInvariant().Contains("cobrança"))
            return "Cobrança indevida identificada na conta do cliente";
        if (input.ToLowerInvariant().Contains("check-in"))
            return "Dificuldades para realizar check-in no aplicativo";
        if (input.ToLowerInvariant().Contains("acesso"))
            return "Problemas de acesso à plataforma";
        
        return "Situação específica do cliente necessita atenção";
    }

    private class RouteInfo
    {
        public string? Plugin { get; set; }
        public string? Function { get; set; }
        public Dictionary<string, object?>? Parameters { get; set; }
    }
}