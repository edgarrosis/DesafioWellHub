using Microsoft.SemanticKernel;
using SkOfflineCourse.MockApi;
using SkOfflineCourse.Plugins;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text.Json;

namespace SkOfflineCourse.TestPrograms;

public class TrainiacSystemTest
{
    private readonly TrainiacMockApi _mockApi;
    private readonly Kernel _kernel;
    private readonly TrainiacDataPlugin _plugin;

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

        // Inicializar Mock API
        _mockApi = new TrainiacMockApi(8081);

        // Configurar Kernel
        var kernelBuilder = Kernel.CreateBuilder();
        _kernel = kernelBuilder.Build();

        // Registrar o plugin
        _plugin = new TrainiacDataPlugin();
        _kernel.Plugins.AddFromObject(_plugin, "TrainiacData");
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
        while (true)
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║           MENU DE TESTES             ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Escolha um teste para executar:");
            Console.WriteLine();
            Console.WriteLine("1️⃣  Listar Usuários Disponíveis");
            Console.WriteLine("2️⃣  Verificar Status de Treinos");
            Console.WriteLine("3️⃣  Listar Exercícios");
            Console.WriteLine("4️⃣  Detalhes de Exercícios");
            Console.WriteLine("5️⃣  Testar Backup de Dados");
            Console.WriteLine("6️⃣  Executar TODOS os Testes");
            Console.WriteLine("0️⃣  Sair");
            Console.WriteLine();
            Console.Write("Digite sua opção: ");

            var option = Console.ReadLine();
            
            try
            {
                switch (option)
                {
                    case "1":
                        await TestListUsers();
                        break;
                    case "2":
                        await TestUserStatuses();
                        break;
                    case "3":
                        await TestListExercises();
                        break;
                    case "4":
                        await TestExerciseDetails();
                        break;
                    case "5":
                        await TestBackupFunctionality();
                        break;
                    case "6":
                        await RunAllTests();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("❌ Opção inválida! Pressione ENTER...");
                        Console.ReadLine();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro no teste: {ex.Message}");
                Console.WriteLine("Pressione ENTER para continuar...");
                Console.ReadLine();
            }
        }
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