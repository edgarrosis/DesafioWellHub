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
    /// Gera uma mensagem humanizada usando IA para diferentes cenários da WellHub
    /// </summary>
    [KernelFunction, Description("Gera mensagens humanizadas usando IA para cenários específicos da WellHub")]
    public async Task<string> GenerateTemplatedResponse(
        [Description("Tipo de cenário: account_summary, checkin_history, failure_analysis, no_checkins_found, partner_info")] string scenarioType,
        [Description("Nome do cliente")] string customerName,
        [Description("Detalhes específicos da situação com dados reais do usuário")] string situationDetails)
    {
        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(situationDetails))
        {
            return "Para gerar uma resposta adequada, preciso do nome do cliente e detalhes da situação.";
        }

        var prompt = $@"
Você é um especialista em atendimento ao cliente da WellHub. Gere uma resposta humanizada e empática baseada nos dados reais do usuário.

CENÁRIO: {scenarioType}
CLIENTE: {customerName}
DADOS REAIS DO SISTEMA:
{situationDetails}

DIRETRIZES ESPECÍFICAS POR CENÁRIO:

Se ACCOUNT_SUMMARY (resumo da conta):
- Cumprimente o cliente pelo nome
- Destaque informações específicas do plano, status e saldo
- Mencione estatísticas de check-ins se houver
- Ofereça dicas personalizadas baseadas no status da conta
- Tom motivacional e positivo

Se CHECKIN_HISTORY (histórico de check-ins):
- Parabenize a dedicação se houver check-ins
- Liste estabelecimentos específicos visitados
- Mencione valores gastos e datas reais
- Se não houver check-ins, seja acolhedor e ofereça sugestões
- Tom encorajador

Se FAILURE_ANALYSIS (análise de falhas):
- Reconheça os problemas de forma empática
- Liste locais e motivos específicos das falhas
- Ofereça soluções práticas baseadas nos erros reais
- Se não houver falhas, parabenize a performance
- Tom solucionador

Se NO_CHECKINS_FOUND (sem check-ins):
- Seja acolhedor para novos usuários
- Personalize baseado no plano do usuário
- Ofereça orientações específicas
- Tom entusiasmado e motivacional

Se PARTNER_INFO (informações do parceiro):
- Forneça detalhes específicos do estabelecimento
- Mencione tipo, localização e status reais
- Oriente sobre como usar o serviço
- Tom informativo e prestativo

FORMATO DE RESPOSTA:
- Use emojis apropriados
- Inclua dados específicos fornecidos
- Mantenha tom profissional mas caloroso
- Termine com assinatura da equipe WellHub
- Máximo 200 palavras

Gere a resposta agora:";

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt);
            return result.ToString().Trim();
        }
        catch (Exception ex)
        {
            // Fallback para uma resposta básica se o LLM falhar
            return $"Olá {customerName}! Recebemos sua consulta sobre {scenarioType} e nossa equipe está analisando os seguintes dados: {situationDetails}. Em breve retornaremos com uma resposta completa. Equipe WellHub 🏃‍♂️";
        }
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