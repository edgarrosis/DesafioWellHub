using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SkOfflineCourse.Infra;
using SkOfflineCourse.Plugins;
using System.Text;

// Configurar codificação para exibir corretamente caracteres especiais
Console.OutputEncoding = Encoding.UTF8;

// Configuração do modelo de linguagem de IA
var kernelBuilder = Kernel.CreateBuilder();

try
{
    // Configuração para o modelo de linguagem local via HTTP
    // Ajuste o endpoint e as configurações para corresponder à sua instalação local
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: "llama3.2:3b",
        apiKey: "apiKey",
        httpClient: new HttpClient { 
            BaseAddress = new Uri("http://localhost:11434/v1/") // Ajuste conforme sua configuração local
        });
        
    Console.WriteLine("✅ Modelo de IA conectado com sucesso!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erro ao conectar ao modelo de IA: {ex.Message}");
    Console.WriteLine("O aplicativo precisa de um modelo de IA para funcionar.");
    Environment.Exit(1); // Sair do programa se não conseguir conectar
}

var kernel = kernelBuilder.Build();

// Cria plugins WellHub
var wellhubTransaction = new WellhubTransactionPlugin();
var wellhubCommunication = new WellhubCommunicationPlugin(kernel);

// Registrando plugins no Kernel
kernel.ImportPluginFromObject(wellhubTransaction, "WellhubTransaction");
kernel.ImportPluginFromObject(wellhubCommunication, "WellhubCommunication");

// Router usando LLM
var router = new AIIntentRouter(kernel);

Console.WriteLine("=== 🏥 Assistente de Diagnóstico e Comunicação WellHub ===");
Console.WriteLine("Sistema completo para verificação de check-ins e comunicação humanizada");
Console.WriteLine();
Console.WriteLine("🔍 Verificações Disponíveis:");
Console.WriteLine("  • \"Verifique o check-in do usuário user123 no parceiro partner456\"");
Console.WriteLine("  • \"Liste os dados simulados\"");
Console.WriteLine();
Console.WriteLine("💬 Comunicação Humanizada:");
Console.WriteLine("  • \"Gere uma resposta para cliente com problema de cobrança indevida\"");
Console.WriteLine("  • \"Use template de reembolso para João com situação: cobrança duplicada\"");
Console.WriteLine("  • \"Ajuste o tom desta mensagem para urgência alta\"");
Console.WriteLine();
Console.WriteLine("🎯 Cenários Pré-configurados:");
Console.WriteLine("  • user123 + partner456 → SUCESSO");
Console.WriteLine("  • user456 + partner789 → FALHA_TRANSACAO");
Console.WriteLine("  • user789 + partner123 → NAO_LOCALIZADO");
Console.WriteLine();
Console.WriteLine("Digite 'sair' ou 'exit' para encerrar");
Console.WriteLine("----------------------------------------");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

    try
    {
        // Usando o router assíncrono baseado em LLM
        var routeResult = await router.RouteAsync(input);
        var plugin = routeResult.plugin;
        var functionName = routeResult.function;
        var skArgs = routeResult.args;

        if (plugin is null || functionName is null)
        {
            Console.WriteLine("❓ Não foi possível identificar a operação solicitada. Tente reformular sua solicitação.");
            Console.WriteLine("   💡 Exemplos válidos:");
            Console.WriteLine("   • \"Verifique o check-in do usuário user123 no parceiro partner456 em 2024-10-02T10:00:00\"");
            Console.WriteLine("   • \"Mostre os registros de teste disponíveis\"");
            Console.WriteLine("   • \"Liste os dados simulados\"");
            continue;
        }

        var result = await kernel.InvokeAsync(plugin, functionName, skArgs);
        Console.WriteLine(result?.ToString());
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Erro: {ex.Message}");
    }
}