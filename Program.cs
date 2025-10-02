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
        modelId: "llama3.1:8b",
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

// Cria plugin WellHub Transaction
var wellhubTransaction = new WellhubTransactionPlugin();

// Registrando plugin no Kernel
kernel.ImportPluginFromObject(wellhubTransaction, "WellhubTransaction");

// Router usando LLM
var router = new AIIntentRouter(kernel);

Console.WriteLine("=== 🏥 Assistente de Diagnóstico WellHub ===");
Console.WriteLine("Sistema especializado em verificação de check-ins e transações");
Console.WriteLine();
Console.WriteLine("� Verificações disponíveis:");
Console.WriteLine();
Console.WriteLine("📊 Verificar Status de Check-in:");
Console.WriteLine("  • \"Verifique o check-in do usuário user123 no parceiro partner456 em 2024-10-02T10:00:00\"");
Console.WriteLine("  • \"Consulte o status da transação do usuário user789 no estabelecimento partner123 às 2024-10-02T09:15:00\"");
Console.WriteLine("  • \"Verificar check-in de user456 em partner789 no horário 2024-10-02T11:30:00\"");
Console.WriteLine();
Console.WriteLine("� Registros de Teste:");
Console.WriteLine("  • \"Mostre os registros de teste disponíveis\"");
Console.WriteLine("  • \"Liste os dados simulados\"");
Console.WriteLine();
Console.WriteLine("� Cenários de teste pré-configurados:");
Console.WriteLine("  • user123 + partner456 + 2024-10-02T10:00:00 → SUCESSO");
Console.WriteLine("  • user456 + partner789 + 2024-10-02T11:30:00 → FALHA_TRANSACAO");
Console.WriteLine("  • user789 + partner123 + 2024-10-02T09:15:00 → NAO_LOCALIZADO");
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
            Console.WriteLine("❓ Desculpe, não entendi o que você precisa. Tente dizer de outra forma ou consulte as sugestões acima.");
            Console.WriteLine("   Por exemplo: \"Preciso comprar café\" ou \"Mostre minhas tarefas\".");
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