using Microsoft.SemanticKernel;
using SkOfflineCourse.MockApi;
using SkOfflineCourse.Plugins;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;

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
        Console.WriteLine("INICIANDO TESTES DO SISTEMA TRAINIAC");
        Console.WriteLine("====================================");
        Console.WriteLine();

        try
        {
            // Iniciar a API Mock
            await _mockApi.StartAsync();
            await Task.Delay(1000); // Aguardar inicializacao

            Console.WriteLine("EXECUTANDO BATERIA DE TESTES");
            Console.WriteLine("----------------------------");
            Console.WriteLine();

            // Teste 1: Listar usuarios disponiveis
            await TestListUsers();
            
            // Teste 2: Testar diferentes status de usuarios
            await TestUserStatuses();
            
            // Teste 3: Listar exercicios disponiveis  
            await TestListExercises();
            
            // Teste 4: Testar detalhes de exercicios
            await TestExerciseDetails();
            
            // Teste 5: Testar backup de dados
            await TestBackupFunctionality();

            Console.WriteLine();
            Console.WriteLine("TODOS OS TESTES CONCLUIDOS!");
            Console.WriteLine("===========================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRO DURANTE OS TESTES: {ex.Message}");
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

    private async Task TestListUsers()
    {
        Console.WriteLine("Teste 1: Listando usuarios disponiveis");
        Console.WriteLine("---------------------------------------");
        
        try
        {
            var result = await _kernel.InvokeAsync("TrainiacData", "GetAvailableTestUsers");
            Console.WriteLine("Resultado:");
            Console.WriteLine(result.GetValue<string>());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Pressione qualquer tecla para continuar...");
        await WaitForUserInput();
    }

    private async Task TestUserStatuses()
    {
        Console.WriteLine("Teste 2: Verificando status de diferentes usuarios");
        Console.WriteLine("--------------------------------------------------");
        
        var testUsers = new[] { "user123", "user456", "user789", "user999", "user_inexistente" };
        
        foreach (var userId in testUsers)
        {
            try
            {
                Console.WriteLine($"Verificando usuario: {userId}");
                
                var result = await _kernel.InvokeAsync("TrainiacData", "GetActiveSessionStatus", 
                    new KernelArguments { ["userId"] = userId });
                
                Console.WriteLine("Resultado:");
                Console.WriteLine(result.GetValue<string>());
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar {userId}: {ex.Message}");
                Console.WriteLine();
            }
        }
        
        Console.WriteLine("Pressione qualquer tecla para continuar...");
        await WaitForUserInput();
    }

    private async Task TestListExercises()
    {
        Console.WriteLine("Teste 3: Listando exercicios disponiveis");
        Console.WriteLine("----------------------------------------");
        
        try
        {
            var result = await _kernel.InvokeAsync("TrainiacData", "GetAvailableExercises");
            Console.WriteLine("Resultado:");
            Console.WriteLine(result.GetValue<string>());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Pressione qualquer tecla para continuar...");
        await WaitForUserInput();
    }

    private async Task TestExerciseDetails()
    {
        Console.WriteLine("Teste 4: Obtendo detalhes de exercicios");
        Console.WriteLine("---------------------------------------");
        
        var testExercises = new[] { "musc_001", "cardio_001", "func_001", "ex_inexistente" };
        
        foreach (var exerciseId in testExercises)
        {
            try
            {
                Console.WriteLine($"Buscando exercicio: {exerciseId}");
                
                var result = await _kernel.InvokeAsync("TrainiacData", "GetExerciseDetails", 
                    new KernelArguments { ["exerciseId"] = exerciseId });
                
                Console.WriteLine("Resultado:");
                Console.WriteLine(result.GetValue<string>());
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar {exerciseId}: {ex.Message}");
                Console.WriteLine();
            }
        }
        
        Console.WriteLine("Pressione qualquer tecla para continuar...");
        await WaitForUserInput();
    }

    private async Task TestBackupFunctionality()
    {
        Console.WriteLine("Teste 5: Testando funcionalidade de backup");
        Console.WriteLine("------------------------------------------");
        
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
            var jsonOptions = new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };
            
            var jsonData = System.Text.Json.JsonSerializer.Serialize(sampleData, jsonOptions);
            
            Console.WriteLine("Dados para backup:");
            Console.WriteLine(jsonData);
            Console.WriteLine();
            
            var result = await _kernel.InvokeAsync("TrainiacData", "BackupTrainingData", 
                new KernelArguments { ["trainingData"] = jsonData });
            
            Console.WriteLine("Resultado do backup:");
            Console.WriteLine(result.GetValue<string>());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no backup: {ex.Message}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Pressione qualquer tecla para continuar...");
        await WaitForUserInput();
    }
}