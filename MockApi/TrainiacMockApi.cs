using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace SkOfflineCourse.MockApi;

public class TrainiacMockApi
{
    private readonly HttpListener _listener;
    private readonly string _baseUrl;
    private readonly string _dataPath;
    private bool _isRunning;
    private CancellationTokenSource _cancellationTokenSource;
    
    // Cache para dados carregados
    private List<JsonElement> _users = new();
    private List<JsonElement> _checkinRecords = new();
    private List<JsonElement> _partners = new();

    // Mapeamento de usuários para status de treino baseado em dados reais
    private Dictionary<string, string> _sessionStatuses = new();
    
    // Configuracao JSON sem escape de Unicode
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public TrainiacMockApi(int port = 8081)
    {
        _baseUrl = $"http://localhost:{port}/";
        _dataPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        _listener = new HttpListener();
        _listener.Prefixes.Add(_baseUrl);
        _cancellationTokenSource = new CancellationTokenSource();
        
        LoadDataFromFiles();
        GenerateSessionStatuses();
    }

    private void LoadDataFromFiles()
    {
        try
        {
            // Carregar usuarios
            var usersFile = Path.Combine(_dataPath, "users.json");
            if (File.Exists(usersFile))
            {
                var usersJson = File.ReadAllText(usersFile);
                var usersData = JsonSerializer.Deserialize<JsonElement>(usersJson);
                if (usersData.TryGetProperty("users", out var users))
                {
                    _users = users.EnumerateArray().ToList();
                }
            }

            // Carregar registros de check-in
            var checkinFile = Path.Combine(_dataPath, "checkin_records.json");
            if (File.Exists(checkinFile))
            {
                var checkinJson = File.ReadAllText(checkinFile);
                var checkinData = JsonSerializer.Deserialize<JsonElement>(checkinJson);
                if (checkinData.TryGetProperty("checkin_records", out var records))
                {
                    _checkinRecords = records.EnumerateArray().ToList();
                }
            }

            // Carregar parceiros
            var partnersFile = Path.Combine(_dataPath, "partners.json");
            if (File.Exists(partnersFile))
            {
                var partnersJson = File.ReadAllText(partnersFile);
                var partnersData = JsonSerializer.Deserialize<JsonElement>(partnersJson);
                if (partnersData.TryGetProperty("partners", out var partners))
                {
                    _partners = partners.EnumerateArray().ToList();
                }
            }

            Console.WriteLine($"Dados carregados: {_users.Count} usuarios, {_checkinRecords.Count} check-ins, {_partners.Count} parceiros");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar dados: {ex.Message}");
        }
    }

    private void GenerateSessionStatuses()
    {
        // Gerar status baseado nos dados reais dos usuarios e seus historicos
        foreach (var user in _users)
        {
            var userId = user.GetProperty("id").GetString() ?? "";
            var userStatus = user.GetProperty("status").GetString() ?? "";
            var creditBalance = user.GetProperty("credit_balance").GetDouble();

            // Logica para determinar status do treino baseado em dados reais
            var status = DetermineTrainingStatus(userId, userStatus, creditBalance);
            _sessionStatuses[userId] = status;
        }

        // Adicionar alguns usuarios especificos para testes se nao existirem
        EnsureTestUsers();
    }

    private string DetermineTrainingStatus(string userId, string userStatus, double creditBalance)
    {
        // Verificar historico de check-ins do usuario
        var userCheckins = _checkinRecords.Where(r => 
            r.GetProperty("userId").GetString() == userId).ToList();

        var recentFailures = userCheckins
            .Where(r => r.GetProperty("status").GetString()?.Contains("FALHA") == true)
            .Count();

        // Logica baseada nos dados reais
        if (userStatus != "ACTIVE") return "TREINO_NAO_CARREGADO";
        if (creditBalance < 20) return "DADOS_CORROMPIDOS"; // Saldo muito baixo pode indicar problema
        if (recentFailures > 2) return "SESSAO_PERDIDA"; // Muitas falhas recentes
        if (creditBalance < 50) return "CONEXAO_INSTAVEL"; // Saldo baixo = instabilidade
        
        return "SUCCESS";
    }

    private void EnsureTestUsers()
    {
        // Garantir que temos alguns casos especificos para demonstracao
        var testCases = new Dictionary<string, string>
        {
            { "user_test_timeout", "TIMEOUT_SERVIDOR" },
            { "user_test_corrupted", "DADOS_CORROMPIDOS" },
            { "user_test_lost", "SESSAO_PERDIDA" }
        };

        foreach (var testCase in testCases)
        {
            if (!_sessionStatuses.ContainsKey(testCase.Key))
            {
                _sessionStatuses[testCase.Key] = testCase.Value;
            }
        }
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        // Aguardar inicialização completa
        await Task.Delay(10);

        try
        {
            _listener.Start();
            _isRunning = true;
            
            Console.WriteLine($"Trainiac Mock API iniciada em: {_baseUrl}");
            Console.WriteLine("Endpoints disponiveis:");
            Console.WriteLine($"   GET {_baseUrl}treino/status/{{userId}}");
            Console.WriteLine($"   POST {_baseUrl}treino/backup");
            Console.WriteLine($"   GET {_baseUrl}exercicio/{{exerciseId}}");
            Console.WriteLine($"   GET {_baseUrl}usuarios");
            Console.WriteLine();
            Console.WriteLine($"Dados carregados: {_users.Count} usuarios reais do WellHub");

            // Processar requisicoes em background
            _ = Task.Run(async () => await ProcessRequestsAsync(_cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao iniciar Mock API: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessRequestsAsync(CancellationToken cancellationToken)
    {
        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar requisicao: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            Console.WriteLine($"-> {request.HttpMethod} {request.Url?.AbsolutePath}");

            // CORS headers
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            var path = request.Url?.AbsolutePath ?? "";
            var responseContent = "";

            switch (request.HttpMethod)
            {
                case "GET" when path.StartsWith("/treino/status/"):
                    responseContent = HandleGetTrainingStatus(path);
                    break;
                
                case "GET" when path.StartsWith("/exercicio/"):
                    responseContent = HandleGetExerciseDetails(path);
                    break;
                
                case "GET" when path == "/usuarios":
                    responseContent = HandleGetUsers();
                    break;
                
                case "POST" when path == "/treino/backup":
                    responseContent = await HandlePostTrainingBackup(request);
                    break;
                
                default:
                    response.StatusCode = 404;
                    responseContent = JsonSerializer.Serialize(new { error = "Endpoint nao encontrado" });
                    break;
            }

            var buffer = Encoding.UTF8.GetBytes(responseContent);
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar requisicao: {ex.Message}");
            try
            {
                response.StatusCode = 500;
                var errorResponse = JsonSerializer.Serialize(new { error = "Erro interno do servidor" });
                var errorBuffer = Encoding.UTF8.GetBytes(errorResponse);
                await response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                response.Close();
            }
            catch { }
        }
    }

    private string HandleGetTrainingStatus(string path)
    {
        var userId = path.Split('/').LastOrDefault();
        
        if (string.IsNullOrEmpty(userId))
        {
            return JsonSerializer.Serialize(new { error = "UserId nao fornecido" });
        }

        // Buscar usuario real nos dados
        var user = _users.FirstOrDefault(u => u.GetProperty("id").GetString() == userId);
        
        if (user.ValueKind == JsonValueKind.Undefined)
        {
            // Verificar se e um usuario de teste
            if (!_sessionStatuses.ContainsKey(userId))
            {
                return JsonSerializer.Serialize(new { 
                    userId = userId,
                    status = "USER_NOT_FOUND",
                    message = "Usuario nao encontrado no sistema",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    errorCode = "TR006"
                });
            }
        }

        var status = _sessionStatuses.GetValueOrDefault(userId, "USER_NOT_FOUND");
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // Se temos dados do usuario real, adicionar informacoes extras
        if (user.ValueKind != JsonValueKind.Undefined)
        {
            var enhancedResponse = new
            {
                userId = userId,
                userName = user.GetProperty("name").GetString(),
                userEmail = user.GetProperty("email").GetString(),
                plan = user.GetProperty("plan").GetString(),
                creditBalance = user.GetProperty("credit_balance").GetDouble(),
                status = status,
                timestamp = timestamp,
                message = GetStatusMessage(status),
                sessionId = status == "SUCCESS" ? $"sess_{Random.Shared.Next(1000, 9999)}" : null,
                errorCode = status != "SUCCESS" ? GetErrorCode(status) : null,
                recentCheckins = GetUserRecentCheckins(userId)
            };

            Console.WriteLine($"<- Status do treino para {user.GetProperty("name").GetString()} ({userId}): {status}");
            return JsonSerializer.Serialize(enhancedResponse, JsonOptions);
        }

        // Resposta basica para usuarios de teste
        var basicResponse = new
        {
            userId = userId,
            status = status,
            timestamp = timestamp,
            message = GetStatusMessage(status),
            sessionId = status == "SUCCESS" ? $"sess_{Random.Shared.Next(1000, 9999)}" : null,
            errorCode = status != "SUCCESS" ? GetErrorCode(status) : null
        };

        Console.WriteLine($"<- Status do treino para {userId}: {status}");
        return JsonSerializer.Serialize(basicResponse, JsonOptions);
    }

    private object[] GetUserRecentCheckins(string userId)
    {
        return _checkinRecords
            .Where(r => r.GetProperty("userId").GetString() == userId)
            .Take(3)
            .Select(r => new
            {
                timestamp = r.GetProperty("timestamp").GetString(),
                status = r.GetProperty("status").GetString(),
                partner = r.GetProperty("partner_name").GetString(),
                amount = r.GetProperty("amount").GetDouble()
            })
            .ToArray();
    }

    private string HandleGetUsers()
    {
        var usersWithStatus = _users.Select(user => new
        {
            id = user.GetProperty("id").GetString(),
            name = user.GetProperty("name").GetString(),
            email = user.GetProperty("email").GetString(),
            plan = user.GetProperty("plan").GetString(),
            status = user.GetProperty("status").GetString(),
            creditBalance = user.GetProperty("credit_balance").GetDouble(),
            trainingStatus = _sessionStatuses.GetValueOrDefault(user.GetProperty("id").GetString() ?? "", "UNKNOWN"),
            recentActivity = GetUserRecentCheckins(user.GetProperty("id").GetString() ?? "").Length
        }).ToArray();

        var response = new
        {
            users = usersWithStatus,
            totalUsers = usersWithStatus.Length,
            activeUsers = usersWithStatus.Count(u => u.status == "ACTIVE"),
            usersWithTrainingIssues = usersWithStatus.Count(u => u.trainingStatus != "SUCCESS"),
            message = "Lista de usuarios reais do WellHub com status de treino simulado"
        };

        Console.WriteLine($"Listando {usersWithStatus.Length} usuarios do WellHub");
        return JsonSerializer.Serialize(response, JsonOptions);
    }

    private string HandleGetExerciseDetails(string path)
    {
        var exerciseId = path.Split('/').LastOrDefault();
        
        if (string.IsNullOrEmpty(exerciseId))
        {
            return JsonSerializer.Serialize(new { error = "ExerciseId nao fornecido" });
        }

        // Gerar exercicios baseados nos servicos dos parceiros
        var exercises = GenerateExercisesFromPartners();
        
        if (exercises.TryGetValue(exerciseId, out var exercise))
        {
            Console.WriteLine($"Detalhes do exercicio: {exerciseId}");
            return JsonSerializer.Serialize(exercise, JsonOptions);
        }
        else
        {
            var errorResponse = new
            {
                error = "Exercicio nao encontrado",
                exerciseId = exerciseId,
                availableExercises = exercises.Keys.ToArray(),
                partnersWithService = _partners
                    .Where(p => p.GetProperty("services").EnumerateArray()
                        .Any(s => s.GetString()?.Contains("Musculacao") == true || 
                                 s.GetString()?.Contains("Cardio") == true))
                    .Select(p => new { 
                        name = p.GetProperty("name").GetString(),
                        services = p.GetProperty("services").EnumerateArray()
                            .Select(s => s.GetString()).ToArray()
                    })
                    .ToArray()
            };
            
            Console.WriteLine($"Exercicio nao encontrado: {exerciseId}");
            return JsonSerializer.Serialize(errorResponse, JsonOptions);
        }
    }

    private Dictionary<string, object> GenerateExercisesFromPartners()
    {
        var exercises = new Dictionary<string, object>();

        // Exercicios baseados nos servicos dos parceiros
        var muscularPartners = _partners.Where(p => 
            p.GetProperty("services").EnumerateArray()
                .Any(s => s.GetString()?.Contains("Musculacao") == true)).ToList();

        var cardioPartners = _partners.Where(p => 
            p.GetProperty("services").EnumerateArray()
                .Any(s => s.GetString()?.Contains("Cardio") == true)).ToList();

        if (muscularPartners.Any())
        {
            exercises["musc_001"] = new
            {
                id = "musc_001",
                name = "Musculacao Completa",
                description = "Treino de musculacao disponivel nos parceiros WellHub",
                type = "Musculacao",
                difficulty = "Intermediario",
                duration = "60 minutos",
                availableAt = muscularPartners.Take(3).Select(p => new
                {
                    partnerId = p.GetProperty("id").GetString(),
                    name = p.GetProperty("name").GetString(),
                    address = p.GetProperty("address").GetString()
                }).ToArray()
            };
        }

        if (cardioPartners.Any())
        {
            exercises["cardio_001"] = new
            {
                id = "cardio_001", 
                name = "Treino Cardiovascular",
                description = "Exercicios cardiovasculares nos parceiros WellHub",
                type = "Cardio",
                difficulty = "Basico",
                duration = "45 minutos",
                availableAt = cardioPartners.Take(3).Select(p => new
                {
                    partnerId = p.GetProperty("id").GetString(),
                    name = p.GetProperty("name").GetString(),
                    address = p.GetProperty("address").GetString()
                }).ToArray()
            };
        }

        // Adicionar exercicio funcional se ha parceiros com Personal Trainer
        var personalTrainerPartners = _partners.Where(p => 
            p.GetProperty("services").EnumerateArray()
                .Any(s => s.GetString()?.Contains("Personal") == true)).ToList();

        if (personalTrainerPartners.Any())
        {
            exercises["func_001"] = new
            {
                id = "func_001",
                name = "Treino Funcional com Personal",
                description = "Treino funcional personalizado com acompanhamento",
                type = "Funcional",
                difficulty = "Avancado",
                duration = "50 minutos",
                availableAt = personalTrainerPartners.Take(2).Select(p => new
                {
                    partnerId = p.GetProperty("id").GetString(),
                    name = p.GetProperty("name").GetString(),
                    address = p.GetProperty("address").GetString()
                }).ToArray()
            };
        }

        return exercises;
    }

    private async Task<string> HandlePostTrainingBackup(HttpListenerRequest request)
    {
        var requestBody = "";
        
        if (request.HasEntityBody)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            requestBody = await reader.ReadToEndAsync();
        }

        var backupId = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Random.Shared.Next(1000, 9999)}";
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        Console.WriteLine($"Backup de dados de treino registrado - ID: {backupId}");
        Console.WriteLine($"Dados recebidos: {(string.IsNullOrEmpty(requestBody) ? "Nenhum" : $"{requestBody.Length} caracteres")}");

        // Parse do JSON para evitar escape duplo
        object? parsedData = null;
        try
        {
            if (!string.IsNullOrEmpty(requestBody))
            {
                parsedData = JsonSerializer.Deserialize<object>(requestBody, JsonOptions);
            }
        }
        catch
        {
            parsedData = requestBody; // Se não conseguir parse, mantém como string
        }

        // Simular integracao with sistema de backup real
        var response = new
        {
            success = true,
            backupId = backupId,
            timestamp = timestamp,
            message = "Backup de dados de treino registrado com sucesso no sistema WellHub",
            dataSize = requestBody.Length,
            originalData = parsedData,
            integrationInfo = new
            {
                wellhubSystem = "CONNECTED",
                totalUsers = _users.Count,
                totalPartners = _partners.Count,
                recentCheckins = _checkinRecords.Count
            }
        };

        return JsonSerializer.Serialize(response, JsonOptions);
    }

    private static string GetStatusMessage(string status) => status switch
    {
        "SUCCESS" => "Treino carregado com sucesso",
        "TREINO_NAO_CARREGADO" => "Falha ao carregar dados do treino",
        "SESSAO_PERDIDA" => "Sessao de treino foi perdida",
        "CONEXAO_INSTAVEL" => "Conexao com o servidor instavel",
        "DADOS_CORROMPIDOS" => "Dados do treino corrompidos",
        "TIMEOUT_SERVIDOR" => "Timeout na comunicacao com o servidor",
        "USER_NOT_FOUND" => "Usuario nao encontrado no sistema",
        _ => "Status desconhecido"
    };

    private static string GetErrorCode(string status) => status switch
    {
        "TREINO_NAO_CARREGADO" => "TR001",
        "SESSAO_PERDIDA" => "TR002", 
        "CONEXAO_INSTAVEL" => "TR003",
        "DADOS_CORROMPIDOS" => "TR004",
        "TIMEOUT_SERVIDOR" => "TR005",
        "USER_NOT_FOUND" => "TR006",
        _ => "TR999"
    };

    public void Stop()
    {
        if (!_isRunning) return;

        try
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            _listener.Close();
            
            Console.WriteLine("Trainiac Mock API parada");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao parar Mock API: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource?.Dispose();
        _listener?.Close();
    }
}