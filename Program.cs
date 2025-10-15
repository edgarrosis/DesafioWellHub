using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using SkOfflineCourse.Infra;
using SkOfflineCourse.TestPrograms;
using System.Text;
using DotNetEnv;

// Configurar codificacao para exibir corretamente caracteres especiais
try
{
    Console.OutputEncoding = Encoding.UTF8;
    Console.InputEncoding = Encoding.UTF8;
}
catch
{
    // Se nao conseguir configurar UTF-8, continua com encoding padrao
    // para evitar erros em alguns ambientes
}

// Carregar variaveis de ambiente do arquivo .env
Env.Load();

// Menu principal da aplicação
Console.Clear();
Console.WriteLine("WELLHUB SYSTEM - SELECAO DE MODULO");
Console.WriteLine("==================================");
Console.WriteLine();
Console.WriteLine("Escolha qual sistema deseja executar:");
Console.WriteLine();
Console.WriteLine("1. WellHub Check-in");
Console.WriteLine("2. Trainiac System");
Console.WriteLine("0. Sair");
Console.WriteLine();
Console.Write("Digite sua opcao: ");

var choice = Console.ReadLine()?.Trim();

switch (choice)
{
    case "1":
        await RunWellHubSystem();
        break;
    
    case "2":
        await RunTrainiacSystem();
        break;
    
    case "0":
        Console.WriteLine("Ate logo!");
        return;
    
    default:
        Console.WriteLine("Opcao invalida!");
        return;
}

async Task RunWellHubSystem()
{
    Console.Clear();
    Console.WriteLine("INICIANDO SISTEMA WELLHUB CHECK-IN");
    Console.WriteLine("==================================");
    Console.WriteLine();

    // Configuração do modelo de linguagem de IA
    var kernelBuilder = Kernel.CreateBuilder();

    try
    {
        // Configuração para Google Gemini
        var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        var geminiModel = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-2.0-flash-exp";
        
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            throw new InvalidOperationException("GEMINI_API_KEY não encontrada no arquivo .env");
        }
        
        kernelBuilder.AddGoogleAIGeminiChatCompletion(
            modelId: geminiModel,
            apiKey: geminiApiKey);
            
        Console.WriteLine($"Google Gemini ({geminiModel}) conectado com sucesso!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"AVISO: Modelo de IA nao disponivel. Continuando com templates estaticos: {ex.Message}");
    }

    var kernel = kernelBuilder.Build();

    // Inicializa o sistema de login
    var loginManager = new UserLoginManager(kernel);

    // Exibe a tela de login (ponto final da aplicação)
    await loginManager.ShowLoginScreen();
}

async Task RunTrainiacSystem()
{
    Console.Clear();
    Console.WriteLine("INICIANDO SISTEMA TRAINIAC");
    Console.WriteLine("==========================");
    Console.WriteLine();

    var trainiacTest = new TrainiacSystemTest();
    await trainiacTest.RunTestsAsync();
}