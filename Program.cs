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

Console.WriteLine("=== 🏥 Assistente de Diagnóstico WellHub com Dados JSON ===");
Console.WriteLine("Sistema completo baseado em dados reais do diretório /data");
Console.WriteLine();
Console.WriteLine("🔍 Verificações de Check-in:");
Console.WriteLine("  • \"Verifique o check-in do usuário user123 no parceiro partner456 em 2024-10-02T10:00:00\"");
Console.WriteLine("  • \"Liste os dados simulados\" - Mostra todos os registros disponíveis");
Console.WriteLine();
Console.WriteLine("� Consultas de Usuários:");
Console.WriteLine("  • \"Consulte informações do usuário user123\"");
Console.WriteLine("  • \"Mostre dados do usuário user456\"");
Console.WriteLine();
Console.WriteLine("🏢 Consultas de Parceiros:");
Console.WriteLine("  • \"Consulte informações do parceiro partner456\"");
Console.WriteLine("  • \"Mostre dados do estabelecimento partner789\"");
Console.WriteLine();
Console.WriteLine("�💬 Comunicação Humanizada:");
Console.WriteLine("  • \"Gere uma resposta para cliente com problema de cobrança indevida\"");
Console.WriteLine("  • \"Use template de reembolso para João com situação: cobrança duplicada\"");
Console.WriteLine();
Console.WriteLine("📊 Dados JSON Disponíveis:");
Console.WriteLine("  • 6 registros de check-in em /data/checkin_records.json");
Console.WriteLine("  • 6 usuários cadastrados em /data/users.json");
Console.WriteLine("  • 6 parceiros ativos em /data/partners.json");
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