using System.ComponentModel;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.SemanticKernel;

namespace SkOfflineCourse.Plugins;

public class TrainiacDataPlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    
    // Configuracao JSON sem escape de Unicode
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public TrainiacDataPlugin(string baseUrl = "http://localhost:8081")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    [KernelFunction, Description("Obtem o status da sessao ativa de treino para um usuario")]
    public async Task<string> GetActiveSessionStatus(
        [Description("ID do usuario para verificar o status da sessao")] string userId)
    {
        try
        {
            Console.WriteLine($"Verificando status da sessao para usuario: {userId}");

            var response = await _httpClient.GetAsync($"{_baseUrl}/treino/status/{userId}");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;
                
                var status = root.GetProperty("status").GetString();
                var message = root.GetProperty("message").GetString();
                var timestamp = root.GetProperty("timestamp").GetString();

                var result = new
                {
                    userId = userId,
                    status = status,
                    message = message,
                    timestamp = timestamp,
                    isSuccess = status == "SUCCESS",
                    sessionId = status == "SUCCESS" && root.TryGetProperty("sessionId", out var sessId) 
                        ? sessId.GetString() 
                        : null,
                    errorCode = status != "SUCCESS" && root.TryGetProperty("errorCode", out var errCode) 
                        ? errCode.GetString() 
                        : null
                };

                var formattedResult = JsonSerializer.Serialize(result, JsonOptions);

                Console.WriteLine($"Status obtido: {status}");
                return formattedResult;
            }
            else
            {
                var errorResult = new
                {
                    userId = userId,
                    error = "Falha na comunicacao com o servidor",
                    statusCode = (int)response.StatusCode,
                    details = content,
                    isSuccess = false
                };

                Console.WriteLine($"Erro HTTP {response.StatusCode}");
                return JsonSerializer.Serialize(errorResult, JsonOptions);
            }
        }
        catch (TaskCanceledException)
        {
            var timeoutResult = new
            {
                userId = userId,
                error = "Timeout na comunicacao com o servidor",
                status = "TIMEOUT_SERVIDOR",
                isSuccess = false,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            Console.WriteLine($"Timeout ao verificar status para {userId}");
            return JsonSerializer.Serialize(timeoutResult, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new
            {
                userId = userId,
                error = "Erro interno ao verificar status da sessao",
                details = ex.Message,
                isSuccess = false,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            Console.WriteLine($"Erro ao verificar status: {ex.Message}");
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    [KernelFunction, Description("Obtem os detalhes completos de um exercicio especifico")]
    public async Task<string> GetExerciseDetails(
        [Description("ID do exercicio para obter os detalhes")] string exerciseId)
    {
        try
        {
            Console.WriteLine($"Buscando detalhes do exercicio: {exerciseId}");

            var response = await _httpClient.GetAsync($"{_baseUrl}/exercicio/{exerciseId}");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Tentar fazer parse para validar o JSON e formata-lo
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Verificar se e uma resposta de erro
                if (root.TryGetProperty("error", out var errorProp))
                {
                    var errorResult = new
                    {
                        exerciseId = exerciseId,
                        error = errorProp.GetString(),
                        availableExercises = root.TryGetProperty("availableExercises", out var available)
                            ? available.EnumerateArray().Select(x => x.GetString()).ToArray()
                            : Array.Empty<string>(),
                        isSuccess = false
                    };

                    Console.WriteLine($"Exercicio nao encontrado: {exerciseId}");
                    return JsonSerializer.Serialize(errorResult, JsonOptions);
                }

                // Se nao ha erro, e um exercicio valido - retornar os dados formatados
                var exerciseData = JsonSerializer.Deserialize<object>(content);
                Console.WriteLine($"Detalhes do exercicio obtidos: {exerciseId}");
                
                return JsonSerializer.Serialize(exerciseData, JsonOptions);
            }
            else
            {
                var errorResult = new
                {
                    exerciseId = exerciseId,
                    error = "Falha na comunicacao com o servidor de exercicios",
                    statusCode = (int)response.StatusCode,
                    details = content,
                    isSuccess = false
                };

                Console.WriteLine($"Erro HTTP {response.StatusCode} ao buscar exercicio");
                return JsonSerializer.Serialize(errorResult, JsonOptions);
            }
        }
        catch (TaskCanceledException)
        {
            var timeoutResult = new
            {
                exerciseId = exerciseId,
                error = "Timeout ao buscar detalhes do exercicio",
                isSuccess = false,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            Console.WriteLine($"Timeout ao buscar exercicio {exerciseId}");
            return JsonSerializer.Serialize(timeoutResult, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new
            {
                exerciseId = exerciseId,
                error = "Erro interno ao buscar detalhes do exercicio",
                details = ex.Message,
                isSuccess = false,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            Console.WriteLine($"Erro ao buscar exercicio: {ex.Message}");
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    [KernelFunction, Description("Envia dados para backup no servidor Trainiac")]
    public async Task<string> BackupTrainingData(
        [Description("Dados do treino para fazer backup (formato JSON)")] string trainingData)
    {
        try
        {
            Console.WriteLine($"Enviando dados para backup...");

            var content = new StringContent(trainingData, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/treino/backup", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                var result = new
                {
                    success = root.GetProperty("success").GetBoolean(),
                    backupId = root.GetProperty("backupId").GetString(),
                    timestamp = root.GetProperty("timestamp").GetString(),
                    message = root.GetProperty("message").GetString(),
                    dataSize = root.GetProperty("dataSize").GetInt32(),
                    originalData = trainingData
                };

                Console.WriteLine($"Backup realizado com sucesso - ID: {result.backupId}");
                return JsonSerializer.Serialize(result, JsonOptions);
            }
            else
            {
                var errorResult = new
                {
                    success = false,
                    error = "Falha ao realizar backup",
                    statusCode = (int)response.StatusCode,
                    details = responseContent,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                Console.WriteLine($"Erro no backup HTTP {response.StatusCode}");
                return JsonSerializer.Serialize(errorResult, JsonOptions);
            }
        }
        catch (TaskCanceledException)
        {
            var timeoutResult = new
            {
                success = false,
                error = "Timeout ao realizar backup",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            Console.WriteLine($"Timeout durante backup");
            return JsonSerializer.Serialize(timeoutResult, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new
            {
                success = false,
                error = "Erro interno ao realizar backup",
                details = ex.Message,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            Console.WriteLine($"Erro durante backup: {ex.Message}");
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    [KernelFunction, Description("Obtem uma lista de todos os usuarios reais do WellHub disponiveis para teste")]
    public async Task<string> GetAvailableTestUsers()
    {
        try
        {
            Console.WriteLine($"Buscando usuarios reais do WellHub...");

            var response = await _httpClient.GetAsync($"{_baseUrl}/usuarios");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Lista de usuarios obtida com sucesso");
                return content;
            }
            else
            {
                var errorResult = new
                {
                    error = "Falha ao obter lista de usuarios",
                    statusCode = (int)response.StatusCode,
                    details = content,
                    message = "Erro na comunicacao com o sistema WellHub"
                };

                Console.WriteLine($"Erro ao obter usuarios: HTTP {response.StatusCode}");
                return JsonSerializer.Serialize(errorResult, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            var errorResult = new
            {
                error = "Erro interno ao buscar usuarios",
                details = ex.Message,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                message = "Falha na conexao com o sistema de dados do WellHub"
            };

            Console.WriteLine($"Erro ao buscar usuarios: {ex.Message}");
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    [KernelFunction, Description("Obtem uma lista de todos os exercicios disponiveis")]
    public async Task<string> GetAvailableExercises()
    {
        var exercises = new[]
        {
            new { exerciseId = "musc_001", name = "Musculacao Completa", difficulty = "Intermediario" },
            new { exerciseId = "cardio_001", name = "Treino Cardiovascular", difficulty = "Basico" },
            new { exerciseId = "func_001", name = "Treino Funcional com Personal", difficulty = "Avancado" }
        };

        var result = new
        {
            availableExercises = exercises,
            totalExercises = exercises.Length,
            message = "Lista de exercicios disponiveis no sistema Trainiac"
        };

        Console.WriteLine($"Listando {exercises.Length} exercicios disponiveis");
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}