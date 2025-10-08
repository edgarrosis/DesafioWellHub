using System.Text.Json;
using Microsoft.SemanticKernel;
using SkOfflineCourse.Plugins;

namespace SkOfflineCourse.Infra;

public class UserLoginManager
{
    private readonly string _dataPath;
    private readonly Kernel _kernel;

    public UserLoginManager(Kernel kernel)
    {
        _dataPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        _kernel = kernel;
    }

    private List<JsonElement> GetUsers()
    {
        try
        {
            var filePath = Path.Combine(_dataPath, "users.json");
            if (!File.Exists(filePath)) return new List<JsonElement>();
            
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            
            if (data.TryGetProperty("users", out var users))
            {
                return users.EnumerateArray().ToList();
            }
            
            return new List<JsonElement>();
        }
        catch
        {
            return new List<JsonElement>();
        }
    }

    private List<JsonElement> GetCheckinRecords()
    {
        try
        {
            var filePath = Path.Combine(_dataPath, "checkin_records.json");
            if (!File.Exists(filePath)) return new List<JsonElement>();
            
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            
            if (data.TryGetProperty("checkin_records", out var records))
            {
                return records.EnumerateArray().ToList();
            }
            
            return new List<JsonElement>();
        }
        catch
        {
            return new List<JsonElement>();
        }
    }

    private List<JsonElement> GetPartners()
    {
        try
        {
            var filePath = Path.Combine(_dataPath, "partners.json");
            if (!File.Exists(filePath)) return new List<JsonElement>();
            
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            
            if (data.TryGetProperty("partners", out var partners))
            {
                return partners.EnumerateArray().ToList();
            }
            
            return new List<JsonElement>();
        }
        catch
        {
            return new List<JsonElement>();
        }
    }

    public async Task ShowLoginScreen()
    {
        Console.Clear();
        Console.WriteLine("=== WELLHUB - SISTEMA DE CHECK-IN ===");
        Console.WriteLine();
        Console.WriteLine("Bem-vindo(a)! Para começar, selecione seu usuário:");
        Console.WriteLine();

        var users = GetUsers();
        if (!users.Any())
        {
            Console.WriteLine("❌ Nenhum usuário encontrado no sistema.");
            return;
        }

        // Mostrar lista de usuários
        for (int i = 0; i < users.Count; i++)
        {
            var user = users[i];
            var statusIcon = GetUserStatusIcon(user);
            var planBadge = GetPlanBadge(user.GetProperty("plan").GetString() ?? "");
            
            Console.WriteLine($"{i + 1}. {statusIcon} {user.GetProperty("name").GetString()} {planBadge}");
            Console.WriteLine($"   📧 {user.GetProperty("email").GetString()}");
            Console.WriteLine($"   💰 Saldo: R$ {user.GetProperty("credit_balance").GetDouble():F2}");
            Console.WriteLine();
        }

        Console.WriteLine("0. Sair");
        Console.WriteLine();
        Console.Write("Digite o número do usuário (ou digite parte do nome para buscar): ");

        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            ShowLoginScreen();
            return;
        }

        // Tentar converter para número
        if (int.TryParse(input, out int choice))
        {
            if (choice == 0)
            {
                Console.WriteLine("👋 Até logo!");
                Environment.Exit(0);
            }

            if (choice > 0 && choice <= users.Count)
            {
                var selectedUser = users[choice - 1];
                await LoginAsUser(selectedUser);
            }
            else
            {
                Console.WriteLine("❌ Opção inválida. Pressione qualquer tecla para tentar novamente...");
                Console.ReadKey();
                ShowLoginScreen();
            }
        }
        else
        {
            // Buscar por nome
            var filteredUsers = SearchUsersByName(users, input);
            if (filteredUsers.Any())
            {
                await ShowSearchResults(filteredUsers);
            }
            else
            {
                Console.WriteLine($"❌ Nenhum usuário encontrado com o termo '{input}'. Pressione qualquer tecla para tentar novamente...");
                Console.ReadKey();
                await ShowLoginScreen();
            }
        }
    }

    private List<JsonElement> SearchUsersByName(List<JsonElement> users, string searchTerm)
    {
        return users.Where(user => 
        {
            var name = user.GetProperty("name").GetString() ?? "";
            var email = user.GetProperty("email").GetString() ?? "";
            return name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                   email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
        }).ToList();
    }

    private async Task ShowSearchResults(List<JsonElement> filteredUsers)
    {
        Console.Clear();
        Console.WriteLine("=== RESULTADOS DA BUSCA ===");
        Console.WriteLine();

        for (int i = 0; i < filteredUsers.Count; i++)
        {
            var user = filteredUsers[i];
            var statusIcon = GetUserStatusIcon(user);
            var planBadge = GetPlanBadge(user.GetProperty("plan").GetString() ?? "");
            
            Console.WriteLine($"{i + 1}. {statusIcon} {user.GetProperty("name").GetString()} {planBadge}");
            Console.WriteLine($"   📧 {user.GetProperty("email").GetString()}");
            Console.WriteLine($"   💰 Saldo: R$ {user.GetProperty("credit_balance").GetDouble():F2}");
            Console.WriteLine();
        }

        Console.WriteLine("0. Voltar à lista completa");
        Console.WriteLine();
        Console.Write("Selecione o usuário: ");

        var input = Console.ReadLine()?.Trim();
        if (int.TryParse(input, out int choice))
        {
            if (choice == 0)
            {
                ShowLoginScreen();
                return;
            }

            if (choice > 0 && choice <= filteredUsers.Count)
            {
                var selectedUser = filteredUsers[choice - 1];
                await LoginAsUser(selectedUser);
            }
            else
            {
                Console.WriteLine("❌ Opção inválida. Pressione qualquer tecla para tentar novamente...");
                Console.ReadKey();
                await ShowSearchResults(filteredUsers);
            }
        }
        else
        {
            await ShowSearchResults(filteredUsers);
        }
    }

    private async Task LoginAsUser(JsonElement user)
    {
        var userId = user.GetProperty("id").GetString();
        var userName = user.GetProperty("name").GetString();
        var userPlan = user.GetProperty("plan").GetString();
        var userBalance = user.GetProperty("credit_balance").GetDouble();

        Console.Clear();
        Console.WriteLine("✅ Login realizado com sucesso!");
        Console.WriteLine();
        Console.WriteLine($"👋 Olá, {userName}!");
        Console.WriteLine($"📋 Plano: {GetPlanBadge(userPlan ?? "")}");
        Console.WriteLine($"💰 Saldo disponível: R$ {userBalance:F2}");
        Console.WriteLine();

        // Carregar histórico de check-ins do usuário
        var userCheckins = GetCheckinRecords()
            .Where(record => record.GetProperty("userId").GetString() == userId)
            .ToList();

        // Mensagem humanizada de boas-vindas
        await GenerateWelcomeMessage(user, userCheckins);

        ShowUserContextMenu(user, userCheckins);
    }

    private void ShowUserContextMenu(JsonElement user, List<JsonElement> userCheckins)
    {
        var userName = user.GetProperty("name").GetString();
        var userId = user.GetProperty("id").GetString();
        var userBalance = user.GetProperty("credit_balance").GetDouble();

        Console.WriteLine("=== OPÇÕES PERSONALIZADAS ===");
        Console.WriteLine();

        // Análise do histórico do usuário
        var recentSuccessful = userCheckins
            .Where(c => c.GetProperty("status").GetString() == "SUCESSO")
            .OrderByDescending(c => c.GetProperty("timestamp").GetString())
            .Take(3)
            .ToList();

        var failedCheckins = userCheckins
            .Where(c => c.GetProperty("status").GetString() != "SUCESSO")
            .Count();

        // Opções contextualizadas baseadas no histórico
        if (recentSuccessful.Any())
        {
            Console.WriteLine("🎯 BASEADO NO SEU HISTÓRICO:");
            Console.WriteLine("1. Ver últimos check-ins • 'historico' ou 'ultimos'");
            Console.WriteLine("2. Locais favoritos • 'favoritos' ou 'repetir'");
        }

        if (failedCheckins > 0)
        {
            Console.WriteLine("⚠️  ATENÇÃO:");
            Console.WriteLine($"3. Resolver problemas • 'problemas' ou 'corrigir'");
        }

        if (userBalance < 50)
        {
            Console.WriteLine("💳 SALDO:");
            Console.WriteLine("4. Opções de recarga • 'recarga' ou 'saldo'");
        }

        Console.WriteLine();
        Console.WriteLine("📋 OPÇÕES GERAIS:");
        Console.WriteLine("5. Novo check-in • 'checkin' ou 'novo'");
        Console.WriteLine("6. Histórico completo • 'transacoes' ou 'completo'");
        Console.WriteLine("7. Parceiros disponíveis • 'parceiros' ou 'academias'");
        Console.WriteLine("8. Trocar usuário • 'trocar' ou 'logout'");
        Console.WriteLine("0. Sair • 'sair' ou 'exit'");
        Console.WriteLine();

        // Mensagem personalizada baseada no contexto
        ShowPersonalizedMessage(user, userCheckins);
        
        Console.Write("Digite o número ou comando desejado: ");
        
        var input = Console.ReadLine()?.Trim();
        var interpretedChoice = InterpretUserCommand(input, user, userCheckins);
        HandleUserChoice(interpretedChoice, user, userCheckins);
    }

    private void ShowPersonalizedMessage(JsonElement user, List<JsonElement> userCheckins)
    {
        var userName = user.GetProperty("name").GetString();
        var userPlan = user.GetProperty("plan").GetString();
        var userBalance = user.GetProperty("credit_balance").GetDouble();

        Console.WriteLine("💬 DICA PERSONALIZADA:");
        
        if (userCheckins.Any())
        {
            var lastCheckin = userCheckins
                .OrderByDescending(c => c.GetProperty("timestamp").GetString())
                .First();
            
            var lastStatus = lastCheckin.GetProperty("status").GetString();
            var lastPartner = lastCheckin.GetProperty("partner_name").GetString();
            
            if (lastStatus == "SUCESSO")
            {
                Console.WriteLine($"✨ Que bom ver você de novo, {userName}! Seu último check-in em {lastPartner} foi um sucesso! 🎉");
            }
            else
            {
                Console.WriteLine($"🤝 Oi {userName}! Notei que houve um problema no seu último check-in em {lastPartner}. Que tal resolvermos isso juntos?");
            }
        }
        else
        {
            Console.WriteLine($"🎉 Seja bem-vindo(a) ao WellHub, {userName}! Esta parece ser sua primeira vez aqui. Vamos começar sua jornada fitness!");
        }

        if (userPlan == "PREMIUM" && userBalance > 100)
        {
            Console.WriteLine("👑 Como usuário Premium com bom saldo, você tem acesso total a todos os nossos parceiros!");
        }
        else if (userBalance < 25)
        {
            Console.WriteLine("⚡ Seu saldo está baixinho. Que tal fazer uma recarga para aproveitar mais atividades?");
        }

        Console.WriteLine();
    }

    private string InterpretUserCommand(string? input, JsonElement user, List<JsonElement> userCheckins)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        var userName = user.GetProperty("name").GetString();
        
        // Primeiro verificar se é um número direto
        if (int.TryParse(input, out int number) && number >= 0 && number <= 8)
        {
            return input;
        }

        // Usar IA para interpretar comando natural
        try
        {
            var prompt = $@"
Você é um assistente do WellHub que interpreta comandos de usuários.

Contexto do usuário:
- Nome: {userName}
- Tem {userCheckins.Count} registros de check-in
- Último status: {(userCheckins.Any() ? userCheckins.OrderByDescending(c => c.GetProperty("timestamp").GetString()).First().GetProperty("status").GetString() : "Nenhum")}

Comando do usuário: '{input}'

Opções disponíveis:
1 = Ver últimos check-ins bem-sucedidos
2 = Locais favoritos 
3 = Resolver problemas
4 = Opções de recarga
5 = Novo check-in
6 = Histórico completo
7 = Consultar parceiros
8 = Trocar usuário
0 = Sair

Responda APENAS com o número (1-8 ou 0) que melhor corresponde ao comando do usuário.
Se não conseguir identificar claramente, responda 'unclear'.

Exemplos:
- 'novo checkin' -> 5
- 'parceiros' -> 7
- 'historico' -> 6
- 'sair' -> 0";

            var result = Task.Run(async () =>
            {
                var response = await _kernel.InvokePromptAsync(prompt);
                return response.GetValue<string>()?.Trim() ?? "unclear";
            }).Result;

            // Validar resposta da IA
            if (int.TryParse(result, out int aiChoice) && aiChoice >= 0 && aiChoice <= 8)
            {
                Console.WriteLine($"🤖 Interpretei '{input}' como opção {aiChoice}");
                return result;
            }

            Console.WriteLine($"🤖 Processando '{input}'...");
            return "unclear";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ IA não disponível, usando interpretação básica: {ex.Message}");
            
            // Fallback para interpretação básica
            var lowerInput = input.ToLower();
            if (lowerInput.Contains("checkin") || lowerInput.Contains("novo"))
                return "5";
            if (lowerInput.Contains("parceiro") || lowerInput.Contains("academia"))
                return "7";
            if (lowerInput.Contains("historico") || lowerInput.Contains("ultimo"))
                return "1";
            if (lowerInput.Contains("favorito") || lowerInput.Contains("repetir"))
                return "2";
            if (lowerInput.Contains("problema") || lowerInput.Contains("corrigir"))
                return "3";
            if (lowerInput.Contains("recarga") || lowerInput.Contains("saldo"))
                return "4";
            if (lowerInput.Contains("completo") || lowerInput.Contains("transac"))
                return "6";
            if (lowerInput.Contains("trocar") || lowerInput.Contains("logout"))
                return "8";
            if (lowerInput.Contains("sair") || lowerInput.Contains("exit"))
                return "0";
                
            return "unclear";
        }
    }

    private void HandleUserChoice(string? choice, JsonElement user, List<JsonElement> userCheckins)
    {
        if (string.IsNullOrEmpty(choice))
        {
            ShowUserContextMenu(user, userCheckins);
            return;
        }

        switch (choice)
        {
            case "1":
                ShowRecentSuccessfulCheckins(user, userCheckins);
                break;
            case "2":
                ShowFavoriteLocations(user, userCheckins);
                break;
            case "3":
                Task.Run(async () => await ShowFailedCheckins(user, userCheckins)).Wait();
                break;
            case "4":
                ShowRechargeOptions(user);
                break;
            case "5":
                StartNewCheckin(user);
                break;
            case "6":
                ShowFullHistory(user, userCheckins);
                break;
            case "7":
                ShowAvailablePartners(user);
                break;
            case "8":
                ShowLoginScreen();
                break;
            case "0":
                Console.WriteLine("👋 Até logo!");
                Environment.Exit(0);
                break;
            case "unclear":
                Console.WriteLine("🤔 Não consegui entender seu comando. Pode tentar novamente?");
                Console.WriteLine("💡 Exemplo: 'novo checkin', 'ver parceiros', 'meu historico', 'quero sair'");
                Console.WriteLine();
                Console.WriteLine("Ou use os números das opções (1-8):");
                Console.WriteLine("Pressione qualquer tecla para ver o menu novamente...");
                Console.ReadKey();
                ShowUserContextMenu(user, userCheckins);
                break;
            default:
                Console.WriteLine($"❌ Opção '{choice}' não reconhecida.");
                Console.WriteLine("💡 Tente usar comandos mais naturais como 'novo checkin' ou números (1-8)");
                Console.WriteLine("Pressione qualquer tecla para tentar novamente...");
                Console.ReadKey();
                ShowUserContextMenu(user, userCheckins);
                break;
        }
    }

    private void ShowRecentSuccessfulCheckins(JsonElement user, List<JsonElement> userCheckins)
    {
        Console.Clear();
        Console.WriteLine("✅ SEUS ÚLTIMOS CHECK-INS BEM-SUCEDIDOS");
        Console.WriteLine();

        var successful = userCheckins
            .Where(c => c.GetProperty("status").GetString() == "SUCESSO")
            .OrderByDescending(c => c.GetProperty("timestamp").GetString())
            .Take(5)
            .ToList();

        if (!successful.Any())
        {
            Console.WriteLine("📭 Você ainda não tem check-ins bem-sucedidos registrados.");
        }
        else
        {
            foreach (var checkin in successful)
            {
                var timestamp = DateTime.Parse(checkin.GetProperty("timestamp").GetString() ?? "");
                var partnerName = checkin.GetProperty("partner_name").GetString();
                var amount = checkin.GetProperty("amount").GetDouble();
                
                Console.WriteLine($"🎯 {partnerName}");
                Console.WriteLine($"   📅 {timestamp:dd/MM/yyyy HH:mm}");
                Console.WriteLine($"   💰 R$ {amount:F2}");
                Console.WriteLine();
            }
        }

        Console.WriteLine("Pressione qualquer tecla para voltar...");
        Console.ReadKey();
        ShowUserContextMenu(user, userCheckins);
    }

    private void ShowFavoriteLocations(JsonElement user, List<JsonElement> userCheckins)
    {
        Console.Clear();
        Console.WriteLine("❤️  SEUS LOCAIS FAVORITOS");
        Console.WriteLine();

        var favoritePartners = userCheckins
            .Where(c => c.GetProperty("status").GetString() == "SUCESSO")
            .GroupBy(c => c.GetProperty("partner_name").GetString())
            .OrderByDescending(g => g.Count())
            .Take(5)
            .ToList();

        if (!favoritePartners.Any())
        {
            Console.WriteLine("📭 Você ainda não tem locais favoritos baseados no histórico.");
        }
        else
        {
            Console.WriteLine("Baseado nos seus check-ins bem-sucedidos:");
            Console.WriteLine();
            
            foreach (var partner in favoritePartners)
            {
                var count = partner.Count();
                var lastVisit = partner.OrderByDescending(c => c.GetProperty("timestamp").GetString()).First();
                var lastAmount = lastVisit.GetProperty("amount").GetDouble();
                
                Console.WriteLine($"🏆 {partner.Key}");
                Console.WriteLine($"   ✅ {count} check-ins realizados");
                Console.WriteLine($"   💰 Último valor: R$ {lastAmount:F2}");
                Console.WriteLine();
            }
        }

        Console.WriteLine("Pressione qualquer tecla para voltar...");
        Console.ReadKey();
        ShowUserContextMenu(user, userCheckins);
    }

    private async Task ShowFailedCheckins(JsonElement user, List<JsonElement> userCheckins)
    {
        Console.Clear();
        Console.WriteLine("⚠️  CHECK-INS COM PROBLEMAS");
        Console.WriteLine();

        var failed = userCheckins
            .Where(c => c.GetProperty("status").GetString() != "SUCESSO")
            .OrderByDescending(c => c.GetProperty("timestamp").GetString())
            .ToList();

        if (!failed.Any())
        {
            Console.WriteLine("✅ Parabéns! Você não tem check-ins com problemas.");
        }
        else
        {
            // Mensagem motivacional sobre resolução de problemas
            await GenerateCheckInProblemMessage();
            
            Console.WriteLine($"Encontrei {failed.Count} transação(ões) que precisam de atenção:");
            Console.WriteLine();
            
            foreach (var checkin in failed)
            {
                var timestamp = DateTime.Parse(checkin.GetProperty("timestamp").GetString() ?? "");
                var partnerName = checkin.GetProperty("partner_name").GetString();
                var status = checkin.GetProperty("status").GetString();
                var details = checkin.GetProperty("details").GetString();
                
                Console.WriteLine($"❌ {partnerName}");
                Console.WriteLine($"   📅 {timestamp:dd/MM/yyyy HH:mm}");
                Console.WriteLine($"   🚨 Status: {status}");
                Console.WriteLine($"   💬 {details}");
                Console.WriteLine();
            }
            
            Console.WriteLine();
            Console.WriteLine("� QUER CORRIGIR AGORA?");
            Console.WriteLine("1. Sim, corrigir todos automaticamente");
            Console.WriteLine("2. Não, apenas visualizar");
            Console.WriteLine();
            Console.Write("Escolha (1 ou 2): ");
            
            var choice = Console.ReadLine()?.Trim();
            if (choice == "1")
            {
                await CorrectAllUserProblems(user.GetProperty("id").GetString() ?? "", user.GetProperty("name").GetString() ?? "");
            }
        }

        Console.WriteLine("Pressione qualquer tecla para voltar...");
        Console.ReadKey();
        
        var userId = user.GetProperty("id").GetString();
        var refreshedCheckins = GetCheckinRecords()
            .Where(r => r.GetProperty("userId").GetString() == userId)
            .ToList();
        ShowUserContextMenu(user, refreshedCheckins);
    }

    private async Task CorrectAllUserProblems(string userId, string userName)
    {
        Console.Clear();
        Console.WriteLine("🔧 CORRIGINDO TODOS OS PROBLEMAS");
        Console.WriteLine();
        Console.WriteLine($"Processando correções para {userName}...");
        Console.WriteLine();

        try
        {
            // Usar o WellhubCorrectionPlugin para corrigir
            var correctionPlugin = new WellhubCorrectionPlugin(_kernel);
            var result = await correctionPlugin.CorrectAllUserFailures(userId);
            
            Console.WriteLine("📋 RESULTADO DA CORREÇÃO:");
            Console.WriteLine(result);
            Console.WriteLine();
            Console.WriteLine("✅ Processo concluído! Seus check-ins foram atualizados no sistema.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao processar correções: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Pressione qualquer tecla para continuar...");
        Console.ReadKey();
    }

    private void ShowRechargeOptions(JsonElement user)
    {
        var userName = user.GetProperty("name").GetString();
        var currentBalance = user.GetProperty("credit_balance").GetDouble();
        
        Console.Clear();
        Console.WriteLine("💳 OPÇÕES DE RECARGA");
        Console.WriteLine();
        Console.WriteLine($"💰 Saldo atual: R$ {currentBalance:F2}");
        Console.WriteLine();
        Console.WriteLine($"Oi {userName}! Para garantir que você não perca nenhuma atividade,");
        Console.WriteLine("aqui estão algumas sugestões de recarga:");
        Console.WriteLine();
        Console.WriteLine("💎 R$ 50,00 - Pacote Básico (2-3 atividades)");
        Console.WriteLine("⭐ R$ 100,00 - Pacote Recomendado (4-5 atividades)"); 
        Console.WriteLine("🚀 R$ 200,00 - Pacote Premium (8-10 atividades)");
        Console.WriteLine();
        Console.WriteLine("🎁 Dica: Recargas acima de R$ 100 ganham 10% de bônus!");
        Console.WriteLine();
        Console.WriteLine("Para fazer a recarga, entre em contato com o suporte ou");
        Console.WriteLine("acesse sua conta no aplicativo WellHub.");
        
        Console.WriteLine();
        Console.WriteLine("Pressione qualquer tecla para voltar...");
        Console.ReadKey();
        
        var userId = user.GetProperty("id").GetString();
        var refreshedCheckins = GetCheckinRecords()
            .Where(r => r.GetProperty("userId").GetString() == userId)
            .ToList();
        ShowUserContextMenu(user, refreshedCheckins);
    }

    private void StartNewCheckin(JsonElement user)
    {
        var userName = user.GetProperty("name").GetString();
        var userBalance = user.GetProperty("credit_balance").GetDouble();
        
        Console.Clear();
        Console.WriteLine("🎯 NOVO CHECK-IN");
        Console.WriteLine();
        Console.WriteLine($"Olá {userName}! Vamos fazer um novo check-in.");
        Console.WriteLine($"💰 Seu saldo atual: R$ {userBalance:F2}");
        Console.WriteLine();
        
        Console.WriteLine("Para continuar com o check-in, você pode:");
        Console.WriteLine("• Dizer o nome da academia/parceiro");
        Console.WriteLine("• Usar comandos como 'fazer checkin na academia fitlife'");
        Console.WriteLine("• Digitar 'voltar' para retornar ao menu");
        Console.WriteLine();
        
        Console.Write("Como posso ajudá-lo com o check-in? ");
        var input = Console.ReadLine()?.Trim().ToLower();
        
        if (string.IsNullOrEmpty(input))
        {
            StartNewCheckin(user);
            return;
        }
        
        if (input.Contains("voltar") || input == "0")
        {
            var userId = user.GetProperty("id").GetString();
            var refreshedCheckins = GetCheckinRecords()
                .Where(r => r.GetProperty("userId").GetString() == userId)
                .ToList();
            ShowUserContextMenu(user, refreshedCheckins);
            return;
        }
        
        // Simular processamento do comando
        Console.WriteLine();
        Console.WriteLine("🤖 Processando sua solicitação...");
        Console.WriteLine($"Comando recebido: '{input}'");
        Console.WriteLine();
        Console.WriteLine("💡 Esta funcionalidade se integrará com o sistema principal de IA");
        Console.WriteLine("   para processar comandos naturais de check-in.");
        Console.WriteLine();
        
        Console.WriteLine("Pressione qualquer tecla para voltar ao menu...");
        Console.ReadKey();
        
        var userId2 = user.GetProperty("id").GetString();
        var refreshedCheckins2 = GetCheckinRecords()
            .Where(r => r.GetProperty("userId").GetString() == userId2)
            .ToList();
        ShowUserContextMenu(user, refreshedCheckins2);
    }

    private void ShowFullHistory(JsonElement user, List<JsonElement> userCheckins)
    {
        Console.Clear();
        Console.WriteLine("📜 HISTÓRICO COMPLETO DE TRANSAÇÕES");
        Console.WriteLine();

        if (!userCheckins.Any())
        {
            Console.WriteLine("📭 Você ainda não tem transações registradas.");
        }
        else
        {
            var sortedCheckins = userCheckins
                .OrderByDescending(c => c.GetProperty("timestamp").GetString())
                .ToList();
                
            foreach (var checkin in sortedCheckins)
            {
                var timestamp = DateTime.Parse(checkin.GetProperty("timestamp").GetString() ?? "");
                var partnerName = checkin.GetProperty("partner_name").GetString();
                var status = checkin.GetProperty("status").GetString();
                var amount = checkin.GetProperty("amount").GetDouble();
                var statusIcon = status == "SUCESSO" ? "✅" : "❌";
                
                Console.WriteLine($"{statusIcon} {partnerName}");
                Console.WriteLine($"   📅 {timestamp:dd/MM/yyyy HH:mm}");
                Console.WriteLine($"   💰 R$ {amount:F2} - {status}");
                Console.WriteLine();
            }
        }

        Console.WriteLine("Pressione qualquer tecla para voltar...");
        Console.ReadKey();
        ShowUserContextMenu(user, userCheckins);
    }

    private void ShowAvailablePartners(JsonElement user)
    {
        Console.Clear();
        Console.WriteLine("🏢 PARCEIROS DISPONÍVEIS");
        Console.WriteLine();

        var partners = GetPartners().Take(5).ToList();
        
        foreach (var partner in partners)
        {
            var name = partner.GetProperty("name").GetString();
            var type = partner.GetProperty("type").GetString();
            var active = partner.GetProperty("active").GetBoolean();
            var statusIcon = active ? "✅" : "❌";
            
            Console.WriteLine($"{statusIcon} {name}");
            Console.WriteLine($"   🏷️  Tipo: {type}");
            Console.WriteLine($"   📍 Status: {(active ? "Ativo" : "Inativo")}");
            Console.WriteLine();
        }

        Console.WriteLine("Pressione qualquer tecla para voltar...");
        Console.ReadKey();
        
        var userId = user.GetProperty("id").GetString();
        var refreshedCheckins = GetCheckinRecords()
            .Where(r => r.GetProperty("userId").GetString() == userId)
            .ToList();
        ShowUserContextMenu(user, refreshedCheckins);
    }

    private string GetUserStatusIcon(JsonElement user)
    {
        var balance = user.GetProperty("credit_balance").GetDouble();
        return balance > 0 ? "✅" : "⚠️";
    }

    private string GetPlanBadge(string plan)
    {
        return plan switch
        {
            "PREMIUM" => "👑 PREMIUM",
            "STANDARD" => "⭐ STANDARD", 
            "BASIC" => "📋 BASIC",
            _ => "❓ " + plan
        };
    }

    private async Task GenerateWelcomeMessage(JsonElement user, List<JsonElement> userCheckins)
    {
        try
        {
            var userName = user.GetProperty("name").GetString();
            var userPlan = user.GetProperty("plan").GetString();
            var userBalance = user.GetProperty("credit_balance").GetDouble();
            
            // Análise detalhada dos problemas na conta
            var problemsCount = userCheckins.Count(c => c.GetProperty("status").GetString() != "SUCESSO");
            var problemTypes = userCheckins
                .Where(c => c.GetProperty("status").GetString() != "SUCESSO")
                .Select(c => c.GetProperty("status").GetString())
                .Distinct()
                .ToList();
            
            var lastActivity = userCheckins.Any() 
                ? userCheckins.OrderByDescending(c => c.GetProperty("timestamp").GetString()).First()
                : (JsonElement?)null;
            
            var lastStatus = lastActivity?.GetProperty("status").GetString();
            var lastPartner = lastActivity?.GetProperty("partner_name").GetString();
            
            // Detectar situações específicas da conta
            var accountIssues = new List<string>();
            if (userBalance < 25) accountIssues.Add("saldo baixo");
            if (problemTypes.Contains("FALHA_SALDO")) accountIssues.Add("falhas por saldo insuficiente");
            if (problemTypes.Contains("FALHA_LOCALIZACAO")) accountIssues.Add("problemas de localização");
            if (problemTypes.Contains("FALHA_PARTNER")) accountIssues.Add("problemas com parceiros");

            var prompt = $@"
Você é um assistente virtual especializado do WellHub. Crie uma mensagem personalizada e inteligente para {userName}.

PERFIL COMPLETO:
- Nome: {userName}
- Plano: {userPlan}
- Saldo: R$ {userBalance:F2}
- Total de check-ins: {userCheckins.Count}
- Check-ins com problemas: {problemsCount}
- Última atividade: {(lastActivity != null ? $"{lastPartner} ({lastStatus})" : "Nenhuma")}

PROBLEMAS DETECTADOS:
{(accountIssues.Any() ? string.Join(", ", accountIssues) : "Nenhum problema detectado")}

TIPOS DE ERRO: {(problemTypes.Any() ? string.Join(", ", problemTypes) : "Nenhum")}

INSTRUÇÕES:
- Seja caloroso, mas focado nos problemas reais da conta
- Máximo 2 frases
- Se há problemas específicos, mencione como resolver
- Se saldo baixo, sugira recarga
- Se última atividade foi erro, ofereça ajuda específica
- Use emojis relevantes
- Seja proativo e útil

Responda apenas com a mensagem personalizada:";

            var response = await _kernel.InvokePromptAsync(prompt);
            var message = response.GetValue<string>()?.Trim();

            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine($"🤖 {message}");
                Console.WriteLine();
                Console.WriteLine("Pressione qualquer tecla para continuar...");
                Console.ReadKey();
                Console.Clear();
            }
        }
        catch
        {
            // Fallback mais informativo se Gemini não estiver disponível
            var userName = user.GetProperty("name").GetString();
            var problemsCount = userCheckins.Count(c => c.GetProperty("status").GetString() != "SUCESSO");
            var userBalance = user.GetProperty("credit_balance").GetDouble();
            
            Console.WriteLine($"🤖 Olá, {userName}! 👋");
            if (problemsCount > 0)
            {
                Console.WriteLine($"Detectei {problemsCount} check-in(s) com problemas - vamos resolver juntos! 🔧");
            }
            else if (userBalance < 25)
            {
                Console.WriteLine("Seu saldo está baixo, que tal fazer uma recarga? 💰");
            }
            else
            {
                Console.WriteLine("Tudo certo com sua conta! Pronto para mais atividades? 💪");
            }
            Console.WriteLine();
            Console.WriteLine("Pressione qualquer tecla para continuar...");
            Console.ReadKey();
            Console.Clear();
        }
    }

    private async Task GenerateCheckInProblemMessage()
    {
        try
        {
            var prompt = @"
Você é um assistente do WellHub. Crie uma mensagem curta e motivacional sobre resolver problemas de check-in.

INSTRUÇÕES:
- Seja encorajador e positivo
- Máximo 1-2 frases
- Mencione que problemas podem ser facilmente resolvidos
- Use emoji apropriado

Responda apenas com a mensagem:";

            var response = await _kernel.InvokePromptAsync(prompt);
            var message = response.GetValue<string>()?.Trim();

            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine($"💡 {message}");
                Console.WriteLine();
                Console.WriteLine("Pressione qualquer tecla para continuar...");
                Console.ReadKey();
            }
        }
        catch
        {
            Console.WriteLine("💡 Não se preocupe! Vamos resolver esses problemas de check-in rapidamente. 😊");
            Console.WriteLine();
            Console.WriteLine("Pressione qualquer tecla para continuar...");
            Console.ReadKey();
        }
    }
}