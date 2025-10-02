using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace SkOfflineCourse.Plugins;

public class WellhubCommunicationPlugin
{
    private readonly Kernel _kernel;

    public WellhubCommunicationPlugin(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    /// <summary>
    /// Gera uma mensagem de resolução humanizada e empática para comunicação com o cliente
    /// </summary>
    /// <param name="caseContext">Contexto original da reclamação ou situação do cliente</param>
    /// <param name="actionTaken">Ação executada para resolver o problema (ProcessRefund, ReleaseCheckinLock, etc.)</param>
    /// <param name="resultStatus">Resultado técnico da ação (SUCESSO, FALHA_TRANSACAO, NAO_LOCALIZADO)</param>
    /// <returns>Mensagem humanizada pronta para envio ao cliente</returns>
    [KernelFunction, Description("Gera uma resposta humanizada e empática para comunicação com o cliente da WellHub")]
    public async Task<string> GenerateResolutionMessage(
        [Description("Contexto da situação ou reclamação do cliente")] string caseContext,
        [Description("Ação técnica executada para resolver o problema")] string actionTaken,
        [Description("Status do resultado da ação executada")] string resultStatus)
    {
        if (string.IsNullOrWhiteSpace(caseContext))
        {
            return "Para gerar uma resposta adequada, preciso de mais informações sobre a situação do cliente.";
        }

        var systemPrompt = @"
Você é um especialista em atendimento ao cliente da WellHub, uma plataforma líder em bem-estar corporativo.
Sua missão é transformar informações técnicas em comunicação humana, empática e solucionadora.

TOM DE VOZ WELLHUB:
- Profissional mas acolhedor
- Empático e compreensivo
- Direto e objetivo, sem enrolação
- Focado na solução, não no problema
- Assume responsabilidade quando necessário
- Evita jargões técnicos
- Linguagem acessível e clara

DIRETRIZES DE COMUNICAÇÃO:
1. Comece reconhecendo a situação do cliente com empatia
2. Explique de forma clara o que foi feito para resolver
3. Se a resolução foi bem-sucedida, tranquilize e confirme
4. Se houve falha, assuma responsabilidade e ofereça próximos passos
5. Termine sempre com uma nota positiva e disponibilidade para ajudar
6. NUNCA use termos técnicos como 'status', 'processamento', 'transação', 'execução'
7. Responda APENAS com a mensagem final, sem explicações adicionais";

        var userPrompt = $@"
SITUAÇÃO DO CLIENTE:
{caseContext}

AÇÃO REALIZADA:
{actionTaken}

RESULTADO:
{resultStatus}

Transforme essas informações em uma mensagem de suporte empática e humanizada:";

        try
        {
            var result = await _kernel.InvokePromptAsync(userPrompt, new KernelArguments
            {
                ["system"] = systemPrompt
            });

            var response = result.ToString().Trim();
            
            // Remove possíveis marcações ou formatações desnecessárias
            response = response.Replace("```", "").Replace("**", "").Replace("*", "");
            
            return response;
        }
        catch (Exception ex)
        {
            return "Não conseguimos processar sua solicitação no momento. Nossa equipe está trabalhando para resolver esta situação. " +
                   "Por favor, entre em contato conosco pelo suporte@wellhub.com. Pedimos desculpas pelo inconveniente e agradecemos sua paciência.";
        }
    }

    /// <summary>
    /// Gera uma mensagem de suporte usando templates específicos para diferentes cenários
    /// </summary>
    [KernelFunction, Description("Gera mensagens usando templates otimizados para cenários específicos da WellHub")]
    public async Task<string> GenerateTemplatedResponse(
        [Description("Tipo de cenário: REEMBOLSO, CHECKIN_LIBERADO, ERRO_SISTEMA, INVESTIGACAO")] string scenarioType,
        [Description("Nome do cliente")] string customerName,
        [Description("Detalhes específicos da situação")] string situationDetails)
    {
        var templates = new Dictionary<string, string>
        {
            ["REEMBOLSO"] = $@"Olá {customerName},

Entendemos sua preocupação com a cobrança inesperada e queremos resolver isso rapidamente para você.

✅ Boa notícia: Já processamos seu reembolso!
📝 O que aconteceu: {situationDetails}
💰 O valor será devolvido no seu cartão em até 5 dias úteis

Implementamos medidas para evitar que isso aconteça novamente. Se precisar de qualquer esclarecimento, estamos aqui para ajudar.

Com carinho,
Equipe WellHub",

            ["CHECKIN_LIBERADO"] = $@"Oi {customerName}!

Identificamos o que estava impedindo seus check-ins e já corrigimos tudo.

✅ Problema resolvido: {situationDetails}
🏃‍♀️ Seus check-ins já estão liberados no app
⭐ Pode aproveitar todas as atividades disponíveis

Testamos aqui e está funcionando perfeitamente. Bom treino!

Abraços,
Time WellHub",

            ["ERRO_SISTEMA"] = $@"Prezado(a) {customerName},

Pedimos sinceras desculpas pelo transtorno causado.

❌ O que houve: {situationDetails}
🔧 Nossa equipe técnica já solucionou o problema
✅ Tudo voltou ao normal

Tomamos medidas para evitar que isso se repita. Sua experiência é nossa prioridade.

Atenciosamente,
Suporte WellHub",

            ["INVESTIGACAO"] = $@"Olá {customerName},

Recebemos seu contato e estamos tratando sua situação com máxima prioridade.

🔍 Onde estamos: {situationDetails}
⏰ Nossa equipe especializada está investigando
📞 Você será atualizado assim que tivermos novidades

Agradecemos sua paciência enquanto trabalhamos na melhor solução para você.

Com atenção,
Equipe de Relacionamento WellHub"
        };

        if (templates.TryGetValue(scenarioType.ToUpperInvariant(), out var template))
        {
            return template.Trim();
        }

        // Fallback usando IA para cenários personalizados
        return await GenerateResolutionMessage(
            $"Cliente {customerName} com situação: {situationDetails}",
            $"Análise personalizada do cenário {scenarioType}",
            "EM_ANDAMENTO"
        );
    }

    /// <summary>
    /// Ajusta o tom e formalidade da mensagem baseado no contexto e urgência
    /// </summary>
    [KernelFunction, Description("Ajusta o tom da comunicação baseado na urgência e sensibilidade da situação")]
    public async Task<string> AdjustMessageTone(
        [Description("Mensagem original a ser ajustada")] string originalMessage,
        [Description("Nível de urgência: BAIXA, MEDIA, ALTA, CRITICA")] string urgencyLevel,
        [Description("Sensibilidade da situação: BAIXA, MEDIA, ALTA")] string sensitivityLevel)
    {
        if (string.IsNullOrWhiteSpace(originalMessage))
        {
            return "Mensagem original é necessária para ajuste de tom.";
        }

        var tonePrompt = $@"
Você é um especialista em comunicação da WellHub. Ajuste o tom da mensagem considerando:

URGÊNCIA: {urgencyLevel}
SENSIBILIDADE: {sensitivityLevel}

DIRETRIZES DE AJUSTE:
- BAIXA urgência + BAIXA sensibilidade: Tom casual e amigável
- MÉDIA urgência + MÉDIA sensibilidade: Tom profissional e empático
- ALTA urgência + ALTA sensibilidade: Tom mais formal e cuidadoso
- CRÍTICA urgência: Máxima atenção, assumir total responsabilidade

MENSAGEM ORIGINAL:
{originalMessage}

Reescreva com o tom adequado, mantendo todas as informações importantes:";

        try
        {
            var result = await _kernel.InvokePromptAsync(tonePrompt);
            return result.ToString().Trim();
        }
        catch (Exception ex)
        {
            return originalMessage; // Retorna mensagem original se houver falha
        }
    }
}