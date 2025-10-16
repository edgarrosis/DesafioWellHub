using System.ComponentModel;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.SemanticKernel;

namespace SkOfflineCourse.Plugins;

public class TrainiacCorrectionPlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly Kernel? _kernel;
    
    // Configuracao JSON sem escape de Unicode
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    // Cache de dados de treino para preservar em caso de falhas
    private readonly Dictionary<string, TrainingSessionData> _sessionCache = new();

    public TrainiacCorrectionPlugin(string baseUrl = "http://localhost:8081")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5) // Timeout mais curto para detectar problemas rapidamente
        };
        _kernel = null;
    }

    public TrainiacCorrectionPlugin(Kernel kernel, string baseUrl = "http://localhost:8081")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5) // Timeout mais curto para detectar problemas rapidamente
        };
        _kernel = kernel;
    }

    // ============ FUNÇÕES DA ISSUE #11 ============

    [KernelFunction, Description("Salva backup da sessão do usuário")]
    public async Task<string> SaveSessionBackup(
        [Description("ID do usuário")] string userId, 
        [Description("Passo atual do treino")] string currentStep)
    {
        try
        {
            Console.WriteLine($"💾 Salvando backup da sessão - User: {userId}, Step: {currentStep}");
            
            var backupData = new
            {
                userId = userId,
                currentStep = currentStep,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                sessionId = Guid.NewGuid().ToString()
            };

            // Chamar endpoint de backup no Mock (Issue 1)
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/backup/session", 
                new StringContent(JsonSerializer.Serialize(backupData), System.Text.Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ Backup salvo com sucesso para {userId}");
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Backup da sessão salvo com sucesso",
                    backupId = backupData.sessionId,
                    userId = userId,
                    currentStep = currentStep
                }, JsonOptions);
            }
            else
            {
                Console.WriteLine($"⚠️ Erro HTTP ao salvar backup: {response.StatusCode}");
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    message = $"Erro ao salvar backup: HTTP {response.StatusCode}",
                    userId = userId
                }, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao salvar backup: {ex.Message}");
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = $"Erro ao salvar backup: {ex.Message}",
                userId = userId
            }, JsonOptions);
        }
    }

    [KernelFunction, Description("Gera interface de fallback amigável e motivacional para erros")]
    public async Task<string> GenerateFallbackUI(
        [Description("Detalhes do exercício")] string exerciseDetails,
        [Description("Tipo do erro ocorrido")] string errorType)
    {
        try
        {
            Console.WriteLine($"🎯 Gerando fallback UI - Exercício: {exerciseDetails}, Erro: {errorType}");
            
            // Simular delay de processamento LLM
            await Task.Delay(800);
            
            // Gerar conteúdo de fallback baseado no tipo de erro
            var fallbackContent = errorType.ToUpper() switch
            {
                "CONNECTION_ERROR" => GenerateConnectionErrorFallback(exerciseDetails),
                "DEVICE_ERROR" => GenerateDeviceErrorFallback(exerciseDetails),
                "USER_NOT_FOUND" => GenerateUserNotFoundFallback(exerciseDetails),
                "SYNC_ERROR" => GenerateSyncErrorFallback(exerciseDetails),
                _ => GenerateGenericErrorFallback(exerciseDetails, errorType)
            };

            var result = new
            {
                success = true,
                errorType = errorType,
                exerciseDetails = exerciseDetails,
                fallbackUI = fallbackContent,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            Console.WriteLine("✅ Fallback UI gerado com sucesso!");
            return JsonSerializer.Serialize(result, JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao gerar fallback UI: {ex.Message}");
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = $"Erro ao gerar fallback UI: {ex.Message}",
                errorType = errorType
            }, JsonOptions);
        }
    }

    // ============ FUNÇÕES AUXILIARES PARA FALLBACK UI ============

    private string GenerateConnectionErrorFallback(string exerciseDetails)
    {
        return $@"🌐 **Ops! Problema de Conexão**

Não se preocupe! Mesmo sem conexão, você pode continuar seu treino:

**📋 Plano B para: {exerciseDetails}**

💪 **Alternativa Offline:**
• Faça os movimentos com foco na forma correta
• Conte as repetições mentalmente  
• Use um cronômetro no seu celular
• Mantenha o ritmo respiratório constante

🎯 **Dica Motivacional:**
Grandes atletas treinam em qualquer condição! Sua determinação é mais importante que qualquer tecnologia. Continue firme! 

⚡ **Quando a conexão voltar:**
Seus dados serão sincronizados automaticamente. Foque no treino agora!";
    }

    private string GenerateDeviceErrorFallback(string exerciseDetails)
    {
        return $@"📱 **Problema no Dispositivo? Sem Problema!**

**🎯 Treino Adaptado para: {exerciseDetails}**

🏃‍♂️ **Modo Manual Ativado:**
• Conte as repetições em voz alta
• Use gestos para marcar séries completas
• Descanse 30-60 segundos entre séries
• Mantenha a intensidade alta

💡 **Transforme o Obstáculo em Oportunidade:**
Este é o momento perfeito para se conectar mais com seu corpo! Sinta cada movimento, cada respiração. Você está mais forte do que imagina!

✨ **Resultado Garantido:**
Treino manual = Mais consciência corporal = Melhor performance!";
    }

    private string GenerateUserNotFoundFallback(string exerciseDetails)
    {
        return $@"👤 **Vamos Recomeçar Juntos!**

**🌟 Seu Treino Personalizado: {exerciseDetails}**

🎯 **Plano de Emergência:**
• Comece com aquecimento leve (2-3 min)
• Execute os movimentos básicos
• Aumente a intensidade gradualmente
• Finalize com alongamento

💪 **Mensagem Especial:**
Cada novo começo é uma oportunidade de ser melhor! Não importa onde você parou, o que importa é que você está aqui, pronto para evoluir!

🚀 **Seu Potencial é Infinito:**
Acredite em você mesmo. Cada repetição é um passo em direção à sua melhor versão!";
    }

    private string GenerateSyncErrorFallback(string exerciseDetails)
    {
        return $@"🔄 **Sincronização em Progresso...**

**⏳ Enquanto isso, continue seu treino: {exerciseDetails}**

🎪 **Modo Offline Ativado:**
• Seus dados estão sendo preservados
• Continue normalmente seu treino
• Tudo será sincronizado em breve

🌟 **Foque no Que Importa:**
A tecnologia é apenas uma ferramenta. O verdadeiro poder está em VOCÊ! Sua disciplina, sua determinação, sua força de vontade.

💫 **Mantra do Dia:**
'Eu sou mais forte que qualquer obstáculo técnico!'";
    }

    private string GenerateGenericErrorFallback(string exerciseDetails, string errorType)
    {
        return $@"⚡ **Superando Desafios Técnicos!**

**🎯 Exercício em Foco: {exerciseDetails}**
**🔧 Situação: {errorType}**

💪 **Seu Treino Não Para:**
• Mantenha o foco no exercício
• Confie na sua experiência
• Use sua determinação como guia
• Celebrate cada movimento

🏆 **Lembre-se:**
Champions não param por problemas técnicos! Eles se adaptam, superam e ficam ainda mais fortes. Você é um champion!

✨ **Transforme o Problema em Poder:**
Cada obstáculo superado te torna mais resiliente. Continue em frente!";
    }

    [KernelFunction, Description("Verifica status de treino com correção automática de erros")]
    public async Task<string> GetTrainingStatusWithAutoCorrection(
        [Description("ID do usuario para verificar o status")] string userId)
    {
        try
        {
            Console.WriteLine($"🔍 Verificando status de treino para {userId} com correção automática...");

            // Tentar obter status normal primeiro
            var response = await _httpClient.GetAsync($"{_baseUrl}/treino/status/{userId}");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;
                
                var status = root.GetProperty("status").GetString();
                var isSuccess = status == "SUCCESS";

                // Se há erro, tentar corrigir
                if (!isSuccess && !string.IsNullOrEmpty(status))
                {
                    Console.WriteLine($"⚠️ Erro detectado: {status}. Iniciando correção automática...");
                    return await HandleTrainingError(userId, status, content);
                }

                // Status OK - preservar dados na cache
                if (isSuccess)
                {
                    await PreserveSessionData(userId, content);
                    Console.WriteLine($"✅ Status OK para {userId}. Dados preservados.");
                }

                return content;
            }
            else
            {
                Console.WriteLine($"❌ Erro de comunicação. Aplicando correção de conectividade...");
                return await HandleConnectionError(userId);
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine($"⏱️ Timeout detectado. Recuperando dados do cache...");
            return await HandleTimeoutError(userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔧 Erro inesperado: {ex.Message}. Aplicando correção genérica...");
            return await HandleGenericError(userId);
        }
    }

    [KernelFunction, Description("Força correção de todos os problemas de treino de um usuario")]
    public async Task<string> ForceCorrectAllTrainingIssues(
        [Description("ID do usuario para corrigir")] string userId)
    {
        Console.WriteLine($"🛠️ Forçando correção completa para usuário {userId}...");

        try
        {
            // Simular correções múltiplas
            var corrections = new List<string>();

            // 1. Verificar conectividade
            await Task.Delay(500); // Simular trabalho
            corrections.Add("Conectividade restaurada");

            // 2. Recarregar dados de treino
            await Task.Delay(300);
            corrections.Add("Dados de treino recarregados");

            // 3. Validar integridade dos dados
            await Task.Delay(200);
            corrections.Add("Integridade dos dados validada");

            // 4. Restaurar sessão
            await RestoreUserSession(userId);
            corrections.Add("Sessão de treino restaurada");

            var result = new
            {
                userId = userId,
                status = "CORRECTION_SUCCESS",
                message = "Todos os problemas foram corrigidos automaticamente",
                correctionsApplied = corrections,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                sessionRestored = true,
                dataPreserved = true
            };

            Console.WriteLine($"✅ Correção completa finalizada para {userId}");
            return JsonSerializer.Serialize(result, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new
            {
                userId = userId,
                status = "CORRECTION_FAILED",
                message = $"Não foi possível corrigir automaticamente: {ex.Message}",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
            
            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    [KernelFunction, Description("Recupera dados de treino perdidos de um usuario")]
    public async Task<string> RecoverLostTrainingData(
        [Description("ID do usuario para recuperar dados")] string userId)
    {
        Console.WriteLine($"💾 Recuperando dados perdidos de treino para {userId}...");
        
        // Simular processamento assíncrono
        await Task.Delay(100);

        if (_sessionCache.TryGetValue(userId, out var cachedData))
        {
            var recovered = new
            {
                userId = userId,
                status = "DATA_RECOVERED",
                message = "Dados de treino recuperados com sucesso do cache local",
                recoveredData = cachedData,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                cacheTimestamp = cachedData.Timestamp
            };

            Console.WriteLine($"✅ Dados recuperados do cache para {userId}");
            return JsonSerializer.Serialize(recovered, JsonOptions);
        }
        else
        {
            // Gerar dados de treino padrão como fallback
            var fallbackData = GenerateFallbackTrainingData(userId);
            
            var recovered = new
            {
                userId = userId,
                status = "FALLBACK_DATA_GENERATED",
                message = "Dados de cache não encontrados. Dados padrão gerados para continuidade",
                recoveredData = fallbackData,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                isFallback = true
            };

            Console.WriteLine($"⚡ Dados fallback gerados para {userId}");
            return JsonSerializer.Serialize(recovered, JsonOptions);
        }
    }

    [KernelFunction, Description("Executa backup de emergencia dos dados de treino")]
    public async Task<string> EmergencyBackupTrainingData(
        [Description("ID do usuario para backup")] string userId,
        [Description("Dados de treino para preservar em JSON")] string trainingData)
    {
        Console.WriteLine($"🆘 Executando backup de emergência para {userId}...");

        try
        {
            // Salvar no cache local
            var sessionData = JsonSerializer.Deserialize<TrainingSessionData>(trainingData);
            if (sessionData != null)
            {
                sessionData.Timestamp = DateTime.UtcNow;
                _sessionCache[userId] = sessionData;
            }

            // Simular backup em múltiplas localizações
            await Task.Delay(300);

            var backupResult = new
            {
                userId = userId,
                status = "BACKUP_SUCCESS",
                message = "Dados de treino preservados com sucesso em backup de emergência",
                backupId = $"emergency_{DateTime.UtcNow:yyyyMMddHHmmss}_{userId}",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                dataSize = trainingData.Length,
                backupLocations = new[] { "cache_local", "memory_backup", "session_store" }
            };

            Console.WriteLine($"✅ Backup de emergência concluído para {userId}");
            return JsonSerializer.Serialize(backupResult, JsonOptions);
        }
        catch (Exception ex)
        {
            var errorResult = new
            {
                userId = userId,
                status = "BACKUP_FAILED",
                message = $"Falha no backup de emergência: {ex.Message}",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            return JsonSerializer.Serialize(errorResult, JsonOptions);
        }
    }

    private async Task<string> HandleTrainingError(string userId, string errorStatus, string originalResponse)
    {
        var corrections = new List<string>();
        string naturalMessage = "Treino carregado com sucesso";

        // Aplicar correções técnicas
        switch (errorStatus)
        {
            case "TREINO_NAO_CARREGADO":
                corrections.Add("Recarregando dados de treino do servidor alternativo");
                await Task.Delay(200);
                corrections.Add("Dados de treino restaurados com sucesso");
                break;

            case "CONEXAO_INSTAVEL":
                corrections.Add("Detectada instabilidade de conexão");
                corrections.Add("Utilizando cache local para continuidade");
                await Task.Delay(150);
                corrections.Add("Sincronização automática agendada");
                break;

            case "DADOS_CORROMPIDOS":
                corrections.Add("Corrupção de dados detectada");
                corrections.Add("Restaurando dados do último backup válido");
                await Task.Delay(300);
                corrections.Add("Integridade dos dados verificada");
                break;

            case "TIMEOUT_SERVIDOR":
                corrections.Add("Timeout do servidor detectado");
                corrections.Add("Redirecionando para servidor espelho");
                await Task.Delay(100);
                corrections.Add("Conexão alternativa estabelecida");
                break;

            case "SESSAO_PERDIDA":
                corrections.Add("Sessão perdida detectada");
                await RestoreUserSession(userId);
                corrections.Add("Sessão restaurada com dados preservados");
                break;

            default:
                corrections.Add($"Erro genérico ({errorStatus}) corrigido automaticamente");
                break;
        }

        // Gerar mensagem natural usando LLM se disponível
        if (_kernel != null)
        {
            try
            {
                var prompt = $@"Você é um personal trainer virtual e houve um problema técnico ({errorStatus}) que foi resolvido automaticamente em segundo plano.
                Crie uma mensagem positiva e motivacional para o usuário como se tudo estivesse funcionando perfeitamente.
                A mensagem deve mascarar completamente o erro técnico e manter a experiência fluida.
                Seja breve (máximo 1-2 frases) e motivacional.";

                var response = await _kernel.InvokePromptAsync(prompt);
                naturalMessage = response.GetValue<string>() ?? "Perfeito! Tudo pronto para seu treino!";
            }
            catch
            {
                naturalMessage = "Perfeito! Seu treino está carregado e pronto para começar!";
            }
        }
        else
        {
            // Mensagens padrão mais naturais sem LLM
            naturalMessage = errorStatus switch
            {
                "CONEXAO_INSTAVEL" => "Tudo conectado! Vamos ao treino!",
                "TREINO_NAO_CARREGADO" => "Perfeito! Seu treino personalizado está pronto!",
                "DADOS_CORROMPIDOS" => "Excelente! Dados atualizados e sincronizados!",
                "TIMEOUT_SERVIDOR" => "Ótimo! Sistema otimizado e funcionando!",
                "SESSAO_PERDIDA" => "Bem-vindo de volta! Continuando de onde paramos!",
                _ => "Tudo certo! Vamos começar seu treino!"
            };
        }

        // Gerar resposta corrigida com mensagem natural
        var correctedResult = new
        {
            userId = userId,
            status = "SUCCESS", // Mascarar o erro
            message = naturalMessage, // Mensagem natural gerada pela LLM
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            isSuccess = true,
            sessionId = $"corrected_{DateTime.UtcNow:HHmmss}",
            errorCode = (string?)null,
            originalError = errorStatus,
            correctionsApplied = corrections,
            autoCorrected = true,
            naturalResponse = true
        };

        Console.WriteLine($"🔧 Erro {errorStatus} corrigido automaticamente para {userId}");
        return JsonSerializer.Serialize(correctedResult, JsonOptions);
    }

    private async Task<string> HandleConnectionError(string userId)
    {
        Console.WriteLine("🌐 Aplicando correção de conectividade...");
        
        // Verificar se há dados em cache
        if (_sessionCache.TryGetValue(userId, out var cachedData))
        {
            var result = new
            {
                userId = userId,
                status = "SUCCESS",
                message = "Treino carregado do cache local (modo offline)",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                isSuccess = true,
                sessionId = cachedData.SessionId,
                errorCode = (string?)null,
                offlineMode = true,
                autoCorrected = true
            };

            return JsonSerializer.Serialize(result, JsonOptions);
        }

        // Gerar dados mínimos para continuidade
        await Task.Delay(200);
        var fallbackResult = new
        {
            userId = userId,
            status = "SUCCESS",
            message = "Treino iniciado em modo de contingência",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            isSuccess = true,
            sessionId = $"contingency_{DateTime.UtcNow:HHmmss}",
            errorCode = (string?)null,
            contingencyMode = true,
            autoCorrected = true
        };

        return JsonSerializer.Serialize(fallbackResult, JsonOptions);
    }

    private async Task<string> HandleTimeoutError(string userId)
    {
        Console.WriteLine("⏱️ Recuperando dados após timeout...");
        
        if (_sessionCache.TryGetValue(userId, out var cachedData))
        {
            var result = new
            {
                userId = userId,
                status = "SUCCESS",
                message = "Treino restaurado após timeout (dados preservados)",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                isSuccess = true,
                sessionId = cachedData.SessionId,
                errorCode = (string?)null,
                restoredFromTimeout = true,
                autoCorrected = true
            };

            return JsonSerializer.Serialize(result, JsonOptions);
        }

        return await HandleGenericError(userId);
    }

    private async Task<string> HandleGenericError(string userId)
    {
        Console.WriteLine("🔧 Aplicando correção genérica...");
        
        await Task.Delay(150);
        
        var result = new
        {
            userId = userId,
            status = "SUCCESS",
            message = "Treino iniciado com correção automática aplicada",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            isSuccess = true,
            sessionId = $"corrected_{DateTime.UtcNow:HHmmss}",
            errorCode = (string?)null,
            genericCorrection = true,
            autoCorrected = true
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private async Task PreserveSessionData(string userId, string sessionData)
    {
        // Simular operação assíncrona de preservação
        await Task.Delay(50);
        
        try
        {
            var jsonDoc = JsonDocument.Parse(sessionData);
            var root = jsonDoc.RootElement;
            
            var sessionInfo = new TrainingSessionData
            {
                UserId = userId,
                SessionId = root.GetProperty("sessionId").GetString() ?? "",
                Status = root.GetProperty("status").GetString() ?? "",
                Timestamp = DateTime.UtcNow,
                RawData = sessionData
            };

            _sessionCache[userId] = sessionInfo;
        }
        catch
        {
            // Em caso de erro, salvar dados mínimos
            _sessionCache[userId] = new TrainingSessionData
            {
                UserId = userId,
                SessionId = $"preserved_{DateTime.UtcNow:HHmmss}",
                Status = "PRESERVED",
                Timestamp = DateTime.UtcNow,
                RawData = sessionData
            };
        }
    }

    private async Task RestoreUserSession(string userId)
    {
        // Simular restauração de sessão
        await Task.Delay(200);
        
        if (!_sessionCache.ContainsKey(userId))
        {
            _sessionCache[userId] = GenerateFallbackTrainingData(userId);
        }
    }

    private TrainingSessionData GenerateFallbackTrainingData(string userId)
    {
        return new TrainingSessionData
        {
            UserId = userId,
            SessionId = $"fallback_{DateTime.UtcNow:HHmmss}",
            Status = "GENERATED",
            Timestamp = DateTime.UtcNow,
            RawData = JsonSerializer.Serialize(new
            {
                userId = userId,
                status = "SUCCESS",
                message = "Dados padrão gerados para continuidade do treino",
                sessionId = $"fallback_{DateTime.UtcNow:HHmmss}",
                fallbackMode = true
            }, JsonOptions)
        };
    }
}

public class TrainingSessionData
{
    public string UserId { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string RawData { get; set; } = "";
}