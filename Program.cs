using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using SkOfflineCourse.Infra;
using System.Text;
using DotNetEnv;

// Configurar codificação para exibir corretamente caracteres especiais
Console.OutputEncoding = Encoding.UTF8;

// Carregar variáveis de ambiente do arquivo .env
Env.Load();

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
        
    Console.WriteLine($"✅ Google Gemini ({geminiModel}) conectado com sucesso!");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Modelo de IA não disponível. Continuando com templates estáticos: {ex.Message}");
}

var kernel = kernelBuilder.Build();

// Inicializa o sistema de login
var loginManager = new UserLoginManager(kernel);

// Exibe a tela de login (ponto final da aplicação)
await loginManager.ShowLoginScreen();