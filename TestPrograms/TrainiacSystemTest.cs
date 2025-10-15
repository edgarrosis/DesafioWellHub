using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using SkOfflineCourse.MockApi;
using SkOfflineCourse.Plugins;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text.Json;
using DotNetEnv;

namespace SkOfflineCourse.TestPrograms;

public class TrainiacSystemTest
{
    private readonly TrainiacMockApi _mockApi;
    private readonly Kernel _kernel;
    private readonly TrainiacDataPlugin _plugin;
    private readonly TrainiacCorrectionPlugin _correctionPlugin;

    public TrainiacSystemTest()
    {
        // Configurar codificacao para caracteres especiais
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
        catch
        {
            // Se nao conseguir configurar UTF-8, continua com encoding padrao
        }

        // Carregar variaveis de ambiente
        Env.Load();

        // Inicializar Mock API
        _mockApi = new TrainiacMockApi(8081);

        // Configurar Kernel com LLM
        var kernelBuilder = Kernel.CreateBuilder();
        
        try
        {
            // Configuração para Google Gemini
            var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            var geminiModel = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-2.0-flash-exp";
            
            if (!string.IsNullOrEmpty(geminiApiKey))
            {
                kernelBuilder.AddGoogleAIGeminiChatCompletion(
                    modelId: geminiModel,
                    apiKey: geminiApiKey);
                Console.WriteLine($"🤖 LLM Personal Trainer ativado ({geminiModel})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ LLM não disponível, usando modo simulado: {ex.Message}");
        }

        _kernel = kernelBuilder.Build();

        // Registrar plugins
        _plugin = new TrainiacDataPlugin();
        _correctionPlugin = new TrainiacCorrectionPlugin(_kernel); // Passa kernel para usar LLM
        _kernel.Plugins.AddFromObject(_plugin, "TrainiacData");
        _kernel.Plugins.AddFromObject(_correctionPlugin, "TrainiacCorrection");
    }

    public async Task RunTestsAsync()
    {
        ShowHeader();

        try
        {
            // Inicializar sistema
            await InitializeSystem();
            
            // Menu de testes
            await ShowTestMenu();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERRO: {ex.Message}");
        }
        finally
        {
            _mockApi.Stop();
            Console.WriteLine();
            Console.WriteLine("Pressione qualquer tecla para sair...");
            await WaitForUserInput();
        }
    }

    private async Task TestAutomaticErrorCorrection()
    {
        Console.Clear();
        Console.WriteLine("🔧 TESTE: Correção Automática de Erros");
        Console.WriteLine("=====================================");
        Console.WriteLine();
        
        var testUsers = new[] 
        { 
            ("user456", "Maria Santos - CONEXAO_INSTAVEL"), 
            ("user789", "Carlos Oliveira - TREINO_NAO_CARREGADO"), 
            ("user111", "Pedro Alves - DADOS_CORROMPIDOS"),
            ("user_test_timeout", "Usuário Timeout - TIMEOUT_SERVIDOR")
        };
        
        Console.WriteLine("🔍 Testando correção automática para diferentes tipos de erro...");
        Console.WriteLine();
        
        foreach (var (userId, description) in testUsers)
        {
            try
            {
                Console.WriteLine($"  🛠️ {description}...");
                
                var result = await _kernel.InvokeAsync("TrainiacCorrection", "GetTrainingStatusWithAutoCorrection", 
                    new KernelArguments { ["userId"] = userId });
                
                var resultString = result.GetValue<string>();
                ShowCorrectionResult(resultString, userId);
                
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro para {userId}: {ex.Message}");
                Console.WriteLine();
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para voltar ao menu...");
        await WaitForUserInput();
    }

    private async Task TestForceCompleteCorrection()
    {
        Console.Clear();
        Console.WriteLine("🛠️ TESTE: Correção Completa Forçada");
        Console.WriteLine("===================================");
        Console.WriteLine();
        
        Console.Write("Digite o ID do usuário para correção completa: ");
        var userId = Console.ReadLine()?.Trim();
        
        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("❌ ID do usuário é obrigatório!");
            Console.WriteLine("Testando com usuário padrão: user456");
            userId = "user456";
        }
        
        try
        {
            Console.WriteLine($"🔄 Aplicando correção completa para {userId}...");
            Console.WriteLine();
            
            var result = await _kernel.InvokeAsync("TrainiacCorrection", "ForceCorrectAllTrainingIssues", 
                new KernelArguments { ["userId"] = userId });
            
            var resultString = result.GetValue<string>();
            ShowDetailedCorrectionResult(resultString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro: {ex.Message}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para voltar ao menu...");
        await WaitForUserInput();
    }

    private async Task TestRecoverLostData()
    {
        Console.Clear();
        Console.WriteLine("💾 TESTE: Recuperação de Dados Perdidos");
        Console.WriteLine("=======================================");
        Console.WriteLine();
        
        var testUsers = new[] { "user123", "user456", "user_inexistente" };
        
        Console.WriteLine("🔍 Testando recuperação de dados para diferentes usuários...");
        Console.WriteLine();
        
        foreach (var userId in testUsers)
        {
            try
            {
                Console.WriteLine($"  💾 Recuperando dados para {userId}...");
                
                var result = await _kernel.InvokeAsync("TrainiacCorrection", "RecoverLostTrainingData", 
                    new KernelArguments { ["userId"] = userId });
                
                var resultString = result.GetValue<string>();
                ShowRecoveryResult(resultString, userId);
                
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro para {userId}: {ex.Message}");
                Console.WriteLine();
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para voltar ao menu...");
        await WaitForUserInput();
    }

    private void ShowCorrectionResult(string? jsonResult, string userId)
    {
        if (string.IsNullOrEmpty(jsonResult))
        {
            Console.WriteLine("❌ Sem resposta");
            return;
        }
        
        try
        {
            using var doc = JsonDocument.Parse(jsonResult);
            var root = doc.RootElement;
            
            var status = root.GetProperty("status").GetString();
            var message = root.GetProperty("message").GetString();
            var autoCorrected = root.TryGetProperty("autoCorrected", out var correctedProp) && correctedProp.GetBoolean();
            
            if (autoCorrected)
            {
                var originalError = root.TryGetProperty("originalError", out var errorProp) ? errorProp.GetString() : "Desconhecido";
                Console.WriteLine($"     ✅ CORRIGIDO: {originalError} → {status}");
                Console.WriteLine($"     📝 {message}");
                
                if (root.TryGetProperty("correctionsApplied", out var corrections))
                {
                    Console.WriteLine("     🔧 Correções aplicadas:");
                    foreach (var correction in corrections.EnumerateArray())
                    {
                        Console.WriteLine($"        • {correction.GetString()}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"     ✅ OK: {status} - {message}");
            }
        }
        catch
        {
            Console.WriteLine("     📄 Resposta processada com sucesso");
        }
    }

    private void ShowDetailedCorrectionResult(string? jsonResult)
    {
        if (string.IsNullOrEmpty(jsonResult))
        {
            Console.WriteLine("❌ Sem resposta");
            return;
        }
        
        try
        {
            using var doc = JsonDocument.Parse(jsonResult);
            var root = doc.RootElement;
            
            var status = root.GetProperty("status").GetString();
            var message = root.GetProperty("message").GetString();
            
            Console.WriteLine($"📋 RESULTADO DA CORREÇÃO COMPLETA:");
            Console.WriteLine("==================================");
            Console.WriteLine($"   Status: {(status == "CORRECTION_SUCCESS" ? "✅" : "❌")} {status}");
            Console.WriteLine($"   Mensagem: {message}");
            
            if (root.TryGetProperty("correctionsApplied", out var corrections))
            {
                Console.WriteLine();
                Console.WriteLine("🔧 CORREÇÕES APLICADAS:");
                foreach (var correction in corrections.EnumerateArray())
                {
                    Console.WriteLine($"   ✓ {correction.GetString()}");
                }
            }
            
            if (root.TryGetProperty("sessionRestored", out var restored) && restored.GetBoolean())
            {
                Console.WriteLine();
                Console.WriteLine("🔄 SESSÃO RESTAURADA COM SUCESSO");
            }
            
            if (root.TryGetProperty("dataPreserved", out var preserved) && preserved.GetBoolean())
            {
                Console.WriteLine("💾 DADOS DE TREINO PRESERVADOS");
            }
        }
        catch
        {
            Console.WriteLine("📄 Resposta detalhada:");
            Console.WriteLine(jsonResult);
        }
    }

    private void ShowRecoveryResult(string? jsonResult, string userId)
    {
        if (string.IsNullOrEmpty(jsonResult))
        {
            Console.WriteLine("❌ Sem resposta");
            return;
        }
        
        try
        {
            using var doc = JsonDocument.Parse(jsonResult);
            var root = doc.RootElement;
            
            var status = root.GetProperty("status").GetString();
            var message = root.GetProperty("message").GetString();
            
            var statusIcon = status switch
            {
                "DATA_RECOVERED" => "✅",
                "FALLBACK_DATA_GENERATED" => "⚡",
                _ => "❓"
            };
            
            Console.WriteLine($"     {statusIcon} {status}");
            Console.WriteLine($"     📝 {message}");
            
            if (root.TryGetProperty("isFallback", out var isFallback) && isFallback.GetBoolean())
            {
                Console.WriteLine($"     ⚡ Dados padrão gerados para continuidade");
            }
            
            if (root.TryGetProperty("cacheTimestamp", out var cacheTime))
            {
                Console.WriteLine($"     🕐 Cache: {cacheTime}");
            }
        }
        catch
        {
            Console.WriteLine("     📄 Dados recuperados com sucesso");
        }
    }

    private async Task WaitForUserInput()
    {
        try 
        {
            Console.ReadKey();
        }
        catch 
        {
            // Se console input foi redirecionado, apenas aguarda um pouco
            await Task.Delay(2000);
        }
        Console.WriteLine();
    }

    private void ShowHeader()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║        SISTEMA TRAINIAC - DEMO      ║");
        Console.WriteLine("║     Backend Simulando Erros         ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.WriteLine();
    }

    private async Task InitializeSystem()
    {
        Console.WriteLine("🚀 Inicializando sistema...");
        await _mockApi.StartAsync();
        await Task.Delay(1000);
        
        Console.WriteLine("✅ API Mock iniciada com sucesso!");
        Console.WriteLine($"📍 Endereço: http://localhost:8081/");
        Console.WriteLine();
        
        Console.WriteLine("Pressione ENTER para continuar...");
        await WaitForUserInput();
    }

    private async Task ShowTestMenu()
    {
        await StartUserExperience();
    }

    private async Task StartUserExperience()
    {
        Console.Clear();
        Console.WriteLine("💪 BEM-VINDO AO TRAINIAC!");
        Console.WriteLine("==========================");
        Console.WriteLine();
        Console.WriteLine("Olá! Que bom te ver aqui! 😊");
        Console.WriteLine("Estou aqui para te ajudar com seu treino personalizado.");
        Console.WriteLine();
        Console.WriteLine("Vamos começar?");
        Console.WriteLine();
        Console.WriteLine("1️⃣  🏃‍♂️ Iniciar Treino Cardiovascular");
        Console.WriteLine("2️⃣  � Iniciar Treino de Força");  
        Console.WriteLine("3️⃣  🧘‍♀️ Iniciar Treino Funcional");
        Console.WriteLine("4️⃣  🎯 Treino Personalizado (IA escolhe)");
        Console.WriteLine("0️⃣  Sair do app");
        Console.WriteLine();
        Console.Write("Escolha seu treino: ");

        var option = Console.ReadLine();
        
        switch (option)
        {
            case "1":
                await StartWorkoutExperience("cardiovascular", "Treino Cardiovascular");
                break;
            case "2":
                await StartWorkoutExperience("forca", "Treino de Força");
                break;
            case "3":
                await StartWorkoutExperience("funcional", "Treino Funcional");
                break;
            case "4":
                await StartAIPersonalizedWorkout();
                break;
            case "0":
                return;
            default:
                Console.WriteLine("❌ Opção inválida! Pressione ENTER para tentar novamente...");
                Console.ReadLine();
                await StartUserExperience(); // Volta ao menu
                break;
        }
    }

    private async Task StartWorkoutExperience(string workoutType, string workoutName)
    {
        Console.Clear();
        Console.WriteLine($"🏋️‍♂️ {workoutName.ToUpper()}");
        Console.WriteLine("================================");
        Console.WriteLine();

        // Simular carregamento
        Console.WriteLine("📱 Carregando seu treino personalizado...");
        await Task.Delay(1500);

        // Aqui é onde vamos simular o usuário real
        await SimulatePersonalTrainerExperience(workoutType, workoutName);
    }

    private async Task StartAIPersonalizedWorkout()
    {
        Console.Clear();
        Console.WriteLine("🤖 TREINO PERSONALIZADO COM IA");
        Console.WriteLine("===============================");
        Console.WriteLine();

        if (_kernel != null)
        {
            try
            {
                Console.WriteLine("🧠 Analisando seu perfil e preferências...");
                await Task.Delay(2000);

                var prompt = @"Você é um personal trainer experiente. Crie uma resposta curta e motivadora 
                escolhendo um tipo de treino personalizado para o usuário. Seja caloroso e motivacional.
                Termine sugerindo que vamos começar com alguns exercícios específicos.";

                var response = await _kernel.InvokePromptAsync(prompt);
                Console.WriteLine($"🎯 {response.GetValue<string>()}");
                Console.WriteLine();

                await Task.Delay(1000);
                await SimulatePersonalTrainerExperience("personalizado", "Treino Personalizado IA");
            }
            catch
            {
                Console.WriteLine("🎯 Treino personalizado selecionado: Treino Funcional");
                Console.WriteLine("Vamos focar em movimentos que trabalham múltiplos grupos musculares!");
                Console.WriteLine();
                await Task.Delay(1000);
                await SimulatePersonalTrainerExperience("funcional", "Treino Funcional");
            }
        }
        else
        {
            Console.WriteLine("🎯 Treino personalizado selecionado: Treino Funcional");
            Console.WriteLine("Vamos focar em movimentos que trabalham múltiplos grupos musculares!");
            Console.WriteLine();
            await Task.Delay(1000);
            await SimulatePersonalTrainerExperience("funcional", "Treino Funcional");
        }
    }

    private async Task SimulatePersonalTrainerExperience(string workoutType, string workoutName)
    {
        // Esta será nossa simulação principal onde os erros vão aparecer
        Console.WriteLine("👨‍🏫 PERSONAL TRAINER: Oi! Sou seu personal trainer virtual!");
        Console.WriteLine();

        // Simular alguns exercícios onde podem ocorrer erros
        var exercises = GetExercisesForWorkout(workoutType);
        
        for (int i = 0; i < exercises.Length; i++)
        {
            var exercise = exercises[i];
            await SimulateExercise(exercise, i + 1, exercises.Length);
        }

        Console.WriteLine();
        Console.WriteLine("🎉 Parabéns! Treino concluído com sucesso!");
        Console.WriteLine("💪 Você se superou hoje!");
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para voltar ao menu...");
        Console.ReadLine();
        await StartUserExperience();
    }

    private async Task SimulateExercise(string exercise, int currentExercise, int totalExercises)
    {
        Console.WriteLine($"📋 Exercício {currentExercise}/{totalExercises}: {exercise}");
        Console.WriteLine();

        if (_kernel != null)
        {
            try
            {
                var prompt = $@"Você é um personal trainer entusiasmado orientando o exercício: {exercise}.
                Dê instruções curtas e motivacionais (máximo 2 frases). 
                Seja encorajador e específico sobre a técnica.";

                var instruction = await _kernel.InvokePromptAsync(prompt);
                Console.WriteLine($"💬 PERSONAL: {instruction.GetValue<string>()}");
            }
            catch
            {
                Console.WriteLine($"💬 PERSONAL: Vamos fazer {exercise}! Mantenha a postura e respire corretamente!");
            }
        }
        else
        {
            Console.WriteLine($"💬 PERSONAL: Vamos fazer {exercise}! Mantenha a postura e respire corretamente!");
        }

        Console.WriteLine();
        Console.WriteLine("⏱️  Iniciando em 3... 2... 1... VAI!");
        
        // Aqui é onde vamos injetar erros propositalmente
        await SimulateExerciseWithPotentialErrors(exercise, currentExercise);
    }

    private async Task SimulateExerciseWithPotentialErrors(string exercise, int exerciseNumber)
    {
        // Simular progresso do exercício
        for (int rep = 1; rep <= 5; rep++)
        {
            Console.Write($"Rep {rep}/5... ");
            await Task.Delay(800);

            // Injetar erro em momentos específicos
            if (exerciseNumber == 2 && rep == 3)
            {
                Console.WriteLine();
                Console.WriteLine("❌ Ops! Problema de conexão...");
                await HandleErrorWithCorrection("CONEXAO_INSTAVEL", exercise, rep);
                return;
            }
            else if (exerciseNumber == 3 && rep == 2)
            {
                Console.WriteLine();
                Console.WriteLine("❌ Dados do treino não carregaram...");
                await HandleErrorWithCorrection("TREINO_NAO_CARREGADO", exercise, rep);
                return;
            }

            Console.WriteLine("✅");
        }
        
        Console.WriteLine("🎯 Exercício completado!");
        Console.WriteLine();
        await Task.Delay(1000);
    }

    private async Task HandleErrorWithCorrection(string errorType, string exercise, int currentRep)
    {
        Console.WriteLine();
        Console.WriteLine("🔧 Sistema de correção ativado...");
        
        try
        {
            // Usar o plugin de correção para mascarar o erro
            var result = await _kernel.InvokeAsync("TrainiacCorrection", "GetTrainingStatusWithAutoCorrection",
                new KernelArguments { ["userId"] = "user_simulation" });

            // O plugin vai mascarar o erro e retornar uma resposta natural
            Console.WriteLine("✅ Problema resolvido automaticamente!");
            
            if (_kernel != null)
            {
                try
                {
                    var prompt = $@"Você é um personal trainer e houve uma pequena pausa técnica durante o exercício {exercise}.
                    Crie uma frase motivacional natural para continuar o treino, como se fosse apenas uma pausa normal.
                    Seja positivo e encoraje a continuar de onde parou (rep {currentRep}).";

                    var recovery = await _kernel.InvokePromptAsync(prompt);
                    Console.WriteLine($"💬 PERSONAL: {recovery.GetValue<string>()}");
                }
                catch
                {
                    Console.WriteLine($"💬 PERSONAL: Perfeito! Vamos continuar de onde paramos. Você está indo muito bem!");
                }
            }
            else
            {
                Console.WriteLine($"💬 PERSONAL: Perfeito! Vamos continuar de onde paramos. Você está indo muito bem!");
            }

            Console.WriteLine();
            
            // Continuar exercício após a correção
            for (int rep = currentRep; rep <= 5; rep++)
            {
                Console.Write($"Rep {rep}/5... ");
                await Task.Delay(800);
                Console.WriteLine("✅");
            }
            
            Console.WriteLine("🎯 Exercício completado!");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Sistema de backup ativado: {ex.Message}");
            Console.WriteLine("💬 PERSONAL: Que tal fazer uma pequena pausa? Beba água e já voltamos!");
        }
    }

    private string[] GetExercisesForWorkout(string workoutType)
    {
        return workoutType.ToLower() switch
        {
            "cardiovascular" => new[] { "Polichinelo", "Corrida no lugar", "Mountain Climbers", "Burpees" },
            "forca" => new[] { "Flexão de braço", "Agachamento", "Prancha", "Levantamento de peso" },
            "funcional" => new[] { "Agachamento funcional", "Prancha lateral", "Afundo", "Bear crawl" },
            _ => new[] { "Alongamento dinâmico", "Movimentos compostos", "Core training", "Mobilidade" }
        };
    }

    private async Task RunAllTests()
    {
        Console.Clear();
        Console.WriteLine("🔄 Executando TODOS os testes...");
        Console.WriteLine("================================");
        Console.WriteLine();

        await TestListUsers();
        await TestUserStatuses();
        await TestListExercises();
        await TestExerciseDetails();
        await TestBackupFunctionality();

        Console.WriteLine();
        Console.WriteLine("✅ TODOS OS TESTES CONCLUÍDOS!");
        Console.WriteLine("==============================");
        Console.WriteLine("Pressione ENTER para voltar ao menu...");
        await WaitForUserInput();
    }

    private async Task TestListUsers()
    {
        Console.Clear();
        Console.WriteLine("👥 TESTE: Listar Usuários Disponíveis");
        Console.WriteLine("====================================");
        Console.WriteLine();
        
        try
        {
            Console.WriteLine("📋 Buscando usuários do WellHub...");
            var result = await _kernel.InvokeAsync("TrainiacData", "GetAvailableTestUsers");
            var resultString = result.GetValue<string>();
            
            Console.WriteLine("✅ Usuários encontrados!");
            Console.WriteLine();
            
            // Mostrar apenas resumo dos usuários, não o JSON completo
            if (!string.IsNullOrEmpty(resultString))
            {
                ShowUsersSummary(resultString);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro: {ex.Message}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para voltar ao menu...");
        await WaitForUserInput();
    }

    private void ShowUsersSummary(string jsonResult)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResult);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("users", out var users))
            {
                Console.WriteLine($"📊 Total de usuários: {users.GetArrayLength()}");
                Console.WriteLine();
                
                foreach (var user in users.EnumerateArray())
                {
                    var name = user.GetProperty("name").GetString();
                    var plan = user.GetProperty("plan").GetString();
                    var status = user.GetProperty("trainingStatus").GetString();
                    var balance = user.GetProperty("creditBalance").GetDouble();
                    
                    var statusIcon = status switch
                    {
                        "SUCCESS" => "✅",
                        "CONEXAO_INSTAVEL" => "⚠️",
                        "TREINO_NAO_CARREGADO" => "❌",
                        "DADOS_CORROMPIDOS" => "🔧",
                        _ => "❓"
                    };
                    
                    Console.WriteLine($"  {statusIcon} {name} | {plan} | R$ {balance:F2}");
                }
            }
        }
        catch
        {
            Console.WriteLine("Raw JSON:");
            Console.WriteLine(jsonResult);
        }
    }

    private async Task TestUserStatuses()
    {
        Console.Clear();
        Console.WriteLine("📊 TESTE: Status de Treinos");
        Console.WriteLine("==========================");
        Console.WriteLine();
        
        var testUsers = new[] 
        { 
            ("user123", "João Silva"), 
            ("user456", "Maria Santos"), 
            ("user789", "Carlos Oliveira"), 
            ("user999", "Ana Costa"), 
            ("user_inexistente", "Usuário Inexistente") 
        };
        
        Console.WriteLine("🔍 Verificando status de diferentes usuários...");
        Console.WriteLine();
        
        foreach (var (userId, userName) in testUsers)
        {
            try
            {
                Console.Write($"  👤 {userName} ({userId})... ");
                
                var result = await _kernel.InvokeAsync("TrainiacData", "GetActiveSessionStatus", 
                    new KernelArguments { ["userId"] = userId });
                
                var resultString = result.GetValue<string>();
                ShowStatusSummary(resultString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERRO: {ex.Message}");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para voltar ao menu...");
        await WaitForUserInput();
    }

    private void ShowStatusSummary(string? jsonResult)
    {
        if (string.IsNullOrEmpty(jsonResult)) 
        {
            Console.WriteLine("❌ Sem resposta");
            return;
        }
        
        try
        {
            using var doc = JsonDocument.Parse(jsonResult);
            var root = doc.RootElement;
            
            var status = root.GetProperty("status").GetString();
            var message = root.GetProperty("message").GetString();
            
            var statusIcon = status switch
            {
                "SUCCESS" => "✅ SUCESSO",
                "CONEXAO_INSTAVEL" => "⚠️ CONEXÃO INSTÁVEL", 
                "TREINO_NAO_CARREGADO" => "❌ FALHA CARREGAMENTO",
                "DADOS_CORROMPIDOS" => "🔧 DADOS CORROMPIDOS",
                "USER_NOT_FOUND" => "❓ USUÁRIO NÃO ENCONTRADO",
                _ => $"❓ {status}"
            };
            
            Console.WriteLine($"{statusIcon}");
        }
        catch
        {
            Console.WriteLine("❌ Erro ao processar resposta");
        }
    }

    private async Task TestListExercises()
    {
        Console.Clear();
        Console.WriteLine("🏋️ TESTE: Exercícios Disponíveis");
        Console.WriteLine("=================================");
        Console.WriteLine();
        
        try
        {
            Console.WriteLine("📋 Carregando exercícios...");
            var result = await _kernel.InvokeAsync("TrainiacData", "GetAvailableExercises");
            var resultString = result.GetValue<string>();
            
            Console.WriteLine("✅ Exercícios encontrados!");
            Console.WriteLine();
            
            if (!string.IsNullOrEmpty(resultString))
            {
                ShowExercisesSummary(resultString);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro: {ex.Message}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para voltar ao menu...");
        await WaitForUserInput();
    }

    private void ShowExercisesSummary(string jsonResult)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResult);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("availableExercises", out var exercises))
            {
                Console.WriteLine($"📊 Total de exercícios: {exercises.GetArrayLength()}");
                Console.WriteLine();
                
                foreach (var exercise in exercises.EnumerateArray())
                {
                    var id = exercise.GetProperty("exerciseId").GetString();
                    var name = exercise.GetProperty("name").GetString();
                    var difficulty = exercise.GetProperty("difficulty").GetString();
                    
                    var difficultyIcon = difficulty switch
                    {
                        "Basico" => "🟢",
                        "Intermediario" => "🟡", 
                        "Avancado" => "🔴",
                        _ => "⚪"
                    };
                    
                    Console.WriteLine($"  {difficultyIcon} {name} ({id}) - {difficulty}");
                }
            }
        }
        catch
        {
            Console.WriteLine("Raw JSON:");
            Console.WriteLine(jsonResult);
        }
    }

    private async Task TestExerciseDetails()
    {
        Console.Clear();
        Console.WriteLine("🔍 TESTE: Detalhes de Exercícios");
        Console.WriteLine("================================");
        Console.WriteLine();
        
        var testExercises = new[] 
        { 
            ("musc_001", "Musculação Completa"),
            ("cardio_001", "Treino Cardiovascular"), 
            ("func_001", "Treino Funcional"),
            ("ex_inexistente", "Exercício Inexistente")
        };
        
        Console.WriteLine("🔍 Buscando detalhes dos exercícios...");
        Console.WriteLine();
        
        foreach (var (exerciseId, exerciseName) in testExercises)
        {
            try
            {
                Console.Write($"  🏋️ {exerciseName} ({exerciseId})... ");
                
                var result = await _kernel.InvokeAsync("TrainiacData", "GetExerciseDetails", 
                    new KernelArguments { ["exerciseId"] = exerciseId });
                
                var resultString = result.GetValue<string>();
                ShowExerciseDetailSummary(resultString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERRO: {ex.Message}");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para voltar ao menu...");
        await WaitForUserInput();
    }

    private void ShowExerciseDetailSummary(string? jsonResult)
    {
        if (string.IsNullOrEmpty(jsonResult))
        {
            Console.WriteLine("❌ Sem resposta");
            return;
        }
        
        try
        {
            using var doc = JsonDocument.Parse(jsonResult);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("error", out _))
            {
                Console.WriteLine("❌ NÃO ENCONTRADO");
            }
            else if (root.TryGetProperty("name", out var nameElement))
            {
                var duration = root.GetProperty("duration").GetString();
                var difficulty = root.GetProperty("difficulty").GetString();
                
                var difficultyIcon = difficulty switch
                {
                    "Basico" => "🟢",
                    "Intermediario" => "🟡",
                    "Avancado" => "🔴", 
                    _ => "⚪"
                };
                
                Console.WriteLine($"✅ {difficultyIcon} {duration}");
            }
        }
        catch
        {
            Console.WriteLine("❌ Erro ao processar");
        }
    }

    private async Task TestBackupFunctionality()
    {
        Console.Clear();
        Console.WriteLine("💾 TESTE: Backup de Dados");
        Console.WriteLine("=========================");
        Console.WriteLine();
        
        var sampleData = new
        {
            userId = "user_test",
            sessionId = "sess_12345", 
            exercises = new[]
            {
                new { exerciseId = "musc_001", duration = 120, reps = 15 },
                new { exerciseId = "cardio_001", duration = 90, reps = 20 }
            },
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        
        try
        {
            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };
            
            var jsonData = JsonSerializer.Serialize(sampleData, jsonOptions);
            
            Console.WriteLine("📦 Preparando dados para backup...");
            Console.WriteLine($"   📄 Usuário: {sampleData.userId}");
            Console.WriteLine($"   🔗 Sessão: {sampleData.sessionId}");
            Console.WriteLine($"   🏋️ Exercícios: {sampleData.exercises.Length}");
            Console.WriteLine();
            
            Console.WriteLine("🚀 Enviando para backup...");
            var result = await _kernel.InvokeAsync("TrainiacData", "BackupTrainingData", 
                new KernelArguments { ["trainingData"] = jsonData });
            
            var resultString = result.GetValue<string>();
            ShowBackupSummary(resultString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro no backup: {ex.Message}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para voltar ao menu...");
        await WaitForUserInput();
    }

    private void ShowBackupSummary(string? jsonResult)
    {
        if (string.IsNullOrEmpty(jsonResult))
        {
            Console.WriteLine("❌ Sem resposta do backup");
            return;
        }
        
        try
        {
            using var doc = JsonDocument.Parse(jsonResult);
            var root = doc.RootElement;
            
            var success = root.GetProperty("success").GetBoolean();
            var backupId = root.GetProperty("backupId").GetString();
            var dataSize = root.GetProperty("dataSize").GetInt32();
            
            if (success)
            {
                Console.WriteLine("✅ BACKUP REALIZADO COM SUCESSO!");
                Console.WriteLine($"   🆔 ID: {backupId}");
                Console.WriteLine($"   📊 Tamanho: {dataSize} bytes");
                Console.WriteLine($"   🕐 Processado em: {DateTime.Now:HH:mm:ss}");
            }
            else
            {
                Console.WriteLine("❌ FALHA NO BACKUP");
            }
        }
        catch
        {
            Console.WriteLine("❌ Erro ao processar resposta do backup");
        }
    }
}