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
    { "WellhubTransaction", new List<string> { "VerifyCheckinStatus", "ListSimulatedRecords" } },
    { "WellhubCorrection", new List<string> { "CorrectCheckin" } } 
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

                            // Tratamento específico para WellhubCorrection
                            if (routeInfo.Function.Equals("ReleaseCheckinLock", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!args.ContainsKey("userId"))
                                args["userId"] = ExtractUserId(input) ?? "user_not_found";
                                if (!args.ContainsKey("partnerId"))
                                args["partnerId"] = ExtractPartnerId(input) ?? "partner_not_found";
                                if (!args.ContainsKey("timestamp"))
                                args["timestamp"] = ExtractTimestamp(input) ?? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                            }

                            
                            return (routeInfo.Plugin, routeInfo.Function, args);
                        }
                    }
                }
                catch (JsonException jex)
                {
                    Console.WriteLine($"Erro ao analisar JSON da resposta do modelo: {jex.Message}");
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
        
        // Remove quebras de linha e espaços extras
        json = System.Text.RegularExpressions.Regex.Replace(json, @"\r\n|\r|\n", " ");
        json = System.Text.RegularExpressions.Regex.Replace(json, @"\s+", " ");
        
        // Corrige problemas comuns de JSON
        json = json.Replace("\"null\"", "null");
        json = json.Replace("\"\"", "null");
        json = json.Replace(": null", ": null"); // Garante espaçamento correto
        
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

        // Palavras-chave para verificar check-in
        var verifyKeywords = new[] { "verificar", "verifique", "check-in", "checkin", "status", "transação", "transacao" };
        if (verifyKeywords.Any(keyword => inputLower.Contains(keyword)))
        {
            // Extrair parâmetros manualmente
            var userId = ExtractUserId(input);
            var partnerId = ExtractPartnerId(input);
            var timestamp = ExtractTimestamp(input);

            args["userId"] = string.IsNullOrEmpty(userId) ? "user_default" : userId;
            args["partnerId"] = string.IsNullOrEmpty(partnerId) ? "partner_default" : partnerId;
            args["timestamp"] = string.IsNullOrEmpty(timestamp) ? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") : timestamp;

            return ("WellhubTransaction", "VerifyCheckinStatus", args);
        }

        // Fallback para WellhubCorrection
        if (inputLower.StartsWith("wellhubcorrection.correctcheckin"))
        {
            // Extrai userId e data
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var userId = parts.Length > 1 ? parts[1] : "user_not_found";
            var date = parts.Length > 2 ? parts[2] : DateTime.Now.ToString("yyyy-MM-dd");

            args["userId"] = userId;
            args["date"] = date;

            return ("WellhubCorrection", "CorrectCheckin", args);
        }

        // Nenhuma correspondência encontrada
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

    private class RouteInfo
    {
        public string? Plugin { get; set; }
        public string? Function { get; set; }
        public Dictionary<string, object?>? Parameters { get; set; }
    }
}