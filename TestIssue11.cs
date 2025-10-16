using Microsoft.SemanticKernel;
using SkOfflineCourse.Plugins;

namespace SkOfflineCourse;

/// <summary>
/// Demonstração das funcionalidades implementadas para a Issue #11
/// </summary>
public class TestIssue11
{
    public static async Task RunDemonstration()
    {
        Console.WriteLine("=== DEMONSTRAÇÃO ISSUE #11 - PLUGIN DE CORREÇÃO ===");
        Console.WriteLine();

        // Criar kernel e plugin
        var kernel = Kernel.CreateBuilder().Build();
        var correctionPlugin = new TrainiacCorrectionPlugin(kernel);
        kernel.ImportPluginFromObject(correctionPlugin, "TrainiacCorrection");

        Console.WriteLine("✅ TrainiacCorrectionPlugin registrado no Kernel");
        Console.WriteLine();

        // Teste 1: SaveSessionBackup
        Console.WriteLine("🔹 TESTE 1: SaveSessionBackup");
        Console.WriteLine("===============================");
        
        var backupResult = await correctionPlugin.SaveSessionBackup(
            userId: "user123", 
            currentStep: "Exercício 2/5 - Flexão de Braço"
        );
        
        Console.WriteLine("📋 Resultado do Backup:");
        Console.WriteLine(backupResult);
        Console.WriteLine();

        // Aguardar para melhor visualização
        await Task.Delay(2000);

        // Teste 2: GenerateFallbackUI - Connection Error
        Console.WriteLine("🔹 TESTE 2: GenerateFallbackUI - Connection Error");
        Console.WriteLine("================================================");
        
        var fallbackResult1 = await correctionPlugin.GenerateFallbackUI(
            exerciseDetails: "Agachamento - 3 séries de 15 repetições",
            errorType: "CONNECTION_ERROR"
        );
        
        Console.WriteLine("🎯 Fallback UI Gerado:");
        var fallbackData1 = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(fallbackResult1);
        if (fallbackData1.TryGetProperty("fallbackUI", out var ui1))
        {
            Console.WriteLine(ui1.GetString());
        }
        Console.WriteLine();

        // Aguardar para melhor visualização
        await Task.Delay(3000);

        // Teste 3: GenerateFallbackUI - Device Error
        Console.WriteLine("🔹 TESTE 3: GenerateFallbackUI - Device Error");
        Console.WriteLine("==============================================");
        
        var fallbackResult2 = await correctionPlugin.GenerateFallbackUI(
            exerciseDetails: "Prancha - Manter por 60 segundos",
            errorType: "DEVICE_ERROR"
        );
        
        Console.WriteLine("🎯 Fallback UI Gerado:");
        var fallbackData2 = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(fallbackResult2);
        if (fallbackData2.TryGetProperty("fallbackUI", out var ui2))
        {
            Console.WriteLine(ui2.GetString());
        }
        Console.WriteLine();

        // Aguardar para melhor visualização
        await Task.Delay(3000);

        // Teste 4: GenerateFallbackUI - User Not Found
        Console.WriteLine("🔹 TESTE 4: GenerateFallbackUI - User Not Found");
        Console.WriteLine("===============================================");
        
        var fallbackResult3 = await correctionPlugin.GenerateFallbackUI(
            exerciseDetails: "Corrida na esteira - 20 minutos",
            errorType: "USER_NOT_FOUND"
        );
        
        Console.WriteLine("🎯 Fallback UI Gerado:");
        var fallbackData3 = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(fallbackResult3);
        if (fallbackData3.TryGetProperty("fallbackUI", out var ui3))
        {
            Console.WriteLine(ui3.GetString());
        }
        Console.WriteLine();

        Console.WriteLine("=== DEMONSTRAÇÃO CONCLUÍDA ===");
        Console.WriteLine();
        Console.WriteLine("✅ Todas as funcionalidades da Issue #11 implementadas:");
        Console.WriteLine("   1. SaveSessionBackup - Salva progresso no Mock Backend");
        Console.WriteLine("   2. GenerateFallbackUI - Gera conteúdo motivacional de fallback");
        Console.WriteLine("   3. TrainiacCorrectionPlugin registrado no Kernel");
        Console.WriteLine();
    }
}