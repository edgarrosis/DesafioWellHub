using Microsoft.SemanticKernel;
using System.Text.Json;
using System.ComponentModel;

namespace SkOfflineCourse.Plugins;

/// <summary>
/// Agente Anti-Bug com orquestração usando Semantic Kernel (Issue #12)
/// Responsável por analisar erros e tomar decisões inteligentes sobre qual ação corretiva executar
/// </summary>
public class AntiBugAgent
{
    private readonly Kernel _kernel;
    private readonly TrainiacDataPlugin _dataPlugin;
    private readonly TrainiacCorrectionPlugin _correctionPlugin;

    public AntiBugAgent(Kernel kernel, TrainiacDataPlugin dataPlugin, TrainiacCorrectionPlugin correctionPlugin)
    {
        _kernel = kernel;
        _dataPlugin = dataPlugin;
        _correctionPlugin = correctionPlugin;
    }

    /// <summary>
    /// Função principal de orquestração do Agente Anti-Bug (Issue #12)
    /// Analisa o erro e decide qual ação corretiva executar baseado na criticidade
    /// </summary>
    [KernelFunction("RunAntiBugAgent")]
    [Description("Orquestra a correção de erros no Trainiac baseado no tipo e criticidade do erro detectado")]
    public async Task<string> RunAntiBugAgent(
        [Description("Tipo do erro detectado (SESSAO_PERDIDA, TREINO_NAO_CARREGADO, CONNECTION_ERROR, etc.)")] string errorType,
        [Description("ID do usuário afetado pelo erro")] string userId,
        [Description("Contexto adicional do erro (exercício atual, dados da sessão, etc.)")] string errorContext = "",
        [Description("Dados da sessão atual para backup se necessário")] string sessionData = "")
    {
        Console.WriteLine("🤖 AGENTE ANTI-BUG ATIVADO (Issue #12)");
        Console.WriteLine("=====================================");
        Console.WriteLine($"📋 Analisando erro: {errorType}");
        Console.WriteLine($"👤 Usuário: {userId}");
        Console.WriteLine($"📝 Contexto: {errorContext}");
        Console.WriteLine();

        try
        {
            // Análise inteligente do erro usando LLM
            var errorAnalysis = await AnalyzeErrorWithAI(errorType, errorContext);
            
            // Decisão baseada na criticidade do erro
            var result = await ExecuteCorrectiveAction(errorType, userId, errorContext, sessionData, errorAnalysis);
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro no Agente Anti-Bug: {ex.Message}");
            
            // Fallback de emergência
            var emergencyResult = await ExecuteEmergencyFallback(errorType, userId, errorContext);
            return emergencyResult;
        }
    }

    /// <summary>
    /// Análise inteligente do erro usando IA para determinar criticidade e ação apropriada
    /// </summary>
    private async Task<ErrorAnalysis> AnalyzeErrorWithAI(string errorType, string errorContext)
    {
        Console.WriteLine("🧠 Analisando erro com IA...");
        
        try
        {
            var prompt = $@"Você é um agente especialista em análise de erros de aplicativos de fitness.
            
            Analise o seguinte erro e determine:
            1. Criticidade (CRITICA, ALTA, MEDIA, BAIXA)
            2. Categoria (SESSAO, VISUALIZACAO, CONECTIVIDADE, DISPOSITIVO)
            3. Ação recomendada (BACKUP_SESSAO, FALLBACK_UI, RECUPERAR_DADOS, REINICIAR)
            
            Erro: {errorType}
            Contexto: {errorContext}
            
            Responda APENAS com um JSON no formato:
            {{
                ""criticality"": ""CRITICA|ALTA|MEDIA|BAIXA"",
                ""category"": ""SESSAO|VISUALIZACAO|CONECTIVIDADE|DISPOSITIVO"",
                ""recommendedAction"": ""BACKUP_SESSAO|FALLBACK_UI|RECUPERAR_DADOS|REINICIAR"",
                ""reasoning"": ""breve explicação da análise""
            }}";

            var response = await _kernel.InvokePromptAsync(prompt);
            var analysisJson = response.GetValue<string>();
            
            Console.WriteLine($"🎯 Análise IA: {analysisJson}");
            
            var analysis = JsonSerializer.Deserialize<ErrorAnalysis>(analysisJson);
            return analysis ?? new ErrorAnalysis 
            { 
                Criticality = "MEDIA", 
                Category = "CONECTIVIDADE", 
                RecommendedAction = "FALLBACK_UI",
                Reasoning = "Análise padrão aplicada"
            };
        }
        catch
        {
            Console.WriteLine("⚠️ Usando análise de fallback...");
            return CreateFallbackAnalysis(errorType);
        }
    }

    /// <summary>
    /// Executa a ação corretiva baseada na análise do erro
    /// </summary>
    private async Task<string> ExecuteCorrectiveAction(string errorType, string userId, string errorContext, string sessionData, ErrorAnalysis analysis)
    {
        Console.WriteLine($"🔧 Executando ação corretiva: {analysis.RecommendedAction}");
        Console.WriteLine($"🎯 Criticidade: {analysis.Criticality}");
        Console.WriteLine($"📂 Categoria: {analysis.Category}");
        Console.WriteLine();

        var result = new
        {
            agentAction = analysis.RecommendedAction,
            errorType = errorType,
            criticality = analysis.Criticality,
            category = analysis.Category,
            reasoning = analysis.Reasoning,
            timestamp = DateTime.UtcNow,
            actionResults = new List<string>()
        };

        // Lógica de decisão conforme especificação da Issue #12
        switch (errorType.ToUpper())
        {
            case "SESSAO_PERDIDA":
                // SE o erro for crítico (SESSAO_PERDIDA), ENTÃO SaveSessionBackup
                Console.WriteLine("💾 AÇÃO: Executando backup de sessão crítica...");
                var backupResult = await ExecuteSessionBackup(userId, sessionData, errorContext);
                result.actionResults.Add($"Backup executado: {backupResult}");
                break;
                
            case "TREINO_NAO_CARREGADO":
                // SE o erro for de visualização (TREINO_NAO_CARREGADO), ENTÃO GetExerciseDetails → GenerateFallbackUI
                Console.WriteLine("🛡️ AÇÃO: Recuperando exercícios e gerando UI de fallback...");
                var exerciseResult = await RecoverExerciseAndGenerateFallback(userId, errorContext);
                result.actionResults.Add($"Fallback UI gerado: {exerciseResult}");
                break;
                
            case "CONNECTION_ERROR":
            case "DEVICE_ERROR":
            case "SYNC_ERROR":
                // Ações baseadas na análise IA
                if (analysis.Criticality == "CRITICA" && analysis.RecommendedAction == "BACKUP_SESSAO")
                {
                    Console.WriteLine("💾 AÇÃO: Backup de emergência devido à criticidade...");
                    var emergencyBackup = await ExecuteSessionBackup(userId, sessionData, errorContext);
                    result.actionResults.Add($"Backup de emergência: {emergencyBackup}");
                }
                else
                {
                    Console.WriteLine("🛡️ AÇÃO: Gerando interface de fallback...");
                    var fallbackResult = await GenerateFallbackUI(errorType, userId, errorContext);
                    result.actionResults.Add($"Fallback UI: {fallbackResult}");
                }
                break;
                
            default:
                Console.WriteLine("🔄 AÇÃO: Aplicando correção automática padrão...");
                var autoCorrection = await ApplyAutoCorrection(userId, errorType);
                result.actionResults.Add($"Correção automática: {autoCorrection}");
                break;
        }

        Console.WriteLine("✅ Ação corretiva executada com sucesso!");
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Executa backup de sessão usando o plugin de correção
    /// </summary>
    private async Task<string> ExecuteSessionBackup(string userId, string sessionData, string errorContext)
    {
        try
        {
            var backupData = string.IsNullOrEmpty(sessionData) 
                ? GenerateEmergencySessionData(userId, errorContext)
                : sessionData;

            var result = await _kernel.InvokeAsync("TrainiacCorrection", "SaveSessionBackup", 
                new KernelArguments 
                { 
                    ["sessionData"] = backupData,
                    ["userId"] = userId,
                    ["currentStep"] = "anti_bug_agent_backup"
                });

            return result.GetValue<string>() ?? "Backup executado";
        }
        catch (Exception ex)
        {
            return $"Erro no backup: {ex.Message}";
        }
    }

    /// <summary>
    /// Recupera detalhes dos exercícios e gera UI de fallback
    /// </summary>
    private async Task<string> RecoverExerciseAndGenerateFallback(string userId, string errorContext)
    {
        try
        {
            // 1. Tentar recuperar detalhes dos exercícios
            Console.WriteLine("📋 Recuperando detalhes dos exercícios...");
            var exerciseResult = await _kernel.InvokeAsync("TrainiacData", "GetAvailableExercises");
            
            var exerciseDetails = "Exercícios básicos disponíveis";
            if (exerciseResult != null && !string.IsNullOrEmpty(exerciseResult.GetValue<string>()))
            {
                exerciseDetails = exerciseResult.GetValue<string>()!;
            }

            // 2. Gerar UI de fallback com os exercícios recuperados
            Console.WriteLine("🛡️ Gerando UI de fallback com exercícios recuperados...");
            var fallbackResult = await _kernel.InvokeAsync("TrainiacCorrection", "GenerateFallbackUI",
                new KernelArguments 
                { 
                    ["errorType"] = "TREINO_NAO_CARREGADO",
                    ["userContext"] = $"Usuário: {userId}, Contexto: {errorContext}",
                    ["exerciseDetails"] = exerciseDetails
                });

            return fallbackResult.GetValue<string>() ?? "UI de fallback gerada com exercícios recuperados";
        }
        catch (Exception ex)
        {
            return $"Erro na recuperação: {ex.Message}";
        }
    }

    /// <summary>
    /// Gera UI de fallback para erros específicos
    /// </summary>
    private async Task<string> GenerateFallbackUI(string errorType, string userId, string errorContext)
    {
        try
        {
            var result = await _kernel.InvokeAsync("TrainiacCorrection", "GenerateFallbackUI",
                new KernelArguments 
                { 
                    ["errorType"] = errorType,
                    ["userContext"] = $"Usuário: {userId}, Contexto: {errorContext}",
                    ["exerciseDetails"] = "Exercícios alternativos disponíveis"
                });

            return result.GetValue<string>() ?? "UI de fallback gerada";
        }
        catch (Exception ex)
        {
            return $"Erro na UI de fallback: {ex.Message}";
        }
    }

    /// <summary>
    /// Aplica correção automática usando o plugin de correção
    /// </summary>
    private async Task<string> ApplyAutoCorrection(string userId, string errorType)
    {
        try
        {
            var result = await _kernel.InvokeAsync("TrainiacCorrection", "GetTrainingStatusWithAutoCorrection",
                new KernelArguments { ["userId"] = userId });

            return result.GetValue<string>() ?? "Correção automática aplicada";
        }
        catch (Exception ex)
        {
            return $"Erro na correção automática: {ex.Message}";
        }
    }

    /// <summary>
    /// Cria análise de fallback quando a IA não está disponível
    /// </summary>
    private ErrorAnalysis CreateFallbackAnalysis(string errorType)
    {
        return errorType.ToUpper() switch
        {
            "SESSAO_PERDIDA" => new ErrorAnalysis { Criticality = "CRITICA", Category = "SESSAO", RecommendedAction = "BACKUP_SESSAO", Reasoning = "Sessão crítica perdida" },
            "TREINO_NAO_CARREGADO" => new ErrorAnalysis { Criticality = "ALTA", Category = "VISUALIZACAO", RecommendedAction = "FALLBACK_UI", Reasoning = "Interface de treino inacessível" },
            "CONNECTION_ERROR" => new ErrorAnalysis { Criticality = "MEDIA", Category = "CONECTIVIDADE", RecommendedAction = "FALLBACK_UI", Reasoning = "Problema de conectividade temporário" },
            "DEVICE_ERROR" => new ErrorAnalysis { Criticality = "MEDIA", Category = "DISPOSITIVO", RecommendedAction = "FALLBACK_UI", Reasoning = "Falha no dispositivo detectada" },
            _ => new ErrorAnalysis { Criticality = "BAIXA", Category = "CONECTIVIDADE", RecommendedAction = "FALLBACK_UI", Reasoning = "Erro genérico" }
        };
    }

    /// <summary>
    /// Gera dados de sessão de emergência
    /// </summary>
    private string GenerateEmergencySessionData(string userId, string errorContext)
    {
        var emergencyData = new
        {
            userId = userId,
            sessionId = $"emergency_{DateTime.Now:yyyyMMdd_HHmmss}",
            errorContext = errorContext,
            timestamp = DateTime.UtcNow,
            type = "emergency_backup",
            status = "error_recovery"
        };

        return JsonSerializer.Serialize(emergencyData);
    }

    /// <summary>
    /// Executa fallback de emergência quando tudo mais falha
    /// </summary>
    private async Task<string> ExecuteEmergencyFallback(string errorType, string userId, string errorContext)
    {
        Console.WriteLine("🚨 EXECUTANDO FALLBACK DE EMERGÊNCIA...");
        
        var emergencyResult = new
        {
            emergencyAction = "fallback_total",
            errorType = errorType,
            userId = userId,
            errorContext = errorContext,
            timestamp = DateTime.UtcNow,
            message = "Sistema de emergência ativado - funcionalidade básica mantida"
        };

        return JsonSerializer.Serialize(emergencyResult, new JsonSerializerOptions { WriteIndented = true });
    }
}

/// <summary>
/// Modelo para análise de erros
/// </summary>
public class ErrorAnalysis
{
    public string Criticality { get; set; } = "";
    public string Category { get; set; } = "";
    public string RecommendedAction { get; set; } = "";
    public string Reasoning { get; set; } = "";
}