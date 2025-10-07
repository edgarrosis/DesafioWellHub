# WellhubCorrectionPlugin - Testes e Documentação

## Cenários de Teste Implementados

O plugin `WellhubCorrectionPlugin` simula diferentes cenários de correção de check-in:

### 1. Cenários Pré-definidos (Dados Simulados)

#### ✅ Sucesso da Correção
- **user123 na data 2024-10-02**
  - Comando: `Corrija o check-in do usuário user123 na data 2024-10-02`
  - [DEBUG] Plugin selecionado: WellHubCorrection, Função: CorrectCheckin
  - Resposta:
    ```json
    {
      "status": "SUCESSO_DA_CORRECAO",
      "detalhes": "Check-in liberado com sucesso."
    }
    ```

- **user456 na data 2024-10-03**
  - Comando: `Corrija o check-in do usuário user456 na data 2024-10-03`
  - [DEBUG] Plugin selecionado: WellHubCorrection, Função: CorrectCheckin
  - Resposta:
    ```json
    {
      "status": "SUCESSO_DA_CORRECAO",
      "detalhes": "Check-in liberado com sucesso."
    }
    ```

#### ❌ Erro Operacional
- **user000 na data 2024-10-02**
  - Comando: `Corrija o check-in do usuário user000 na data 2024-10-02`
  - [DEBUG] Plugin selecionado: WellHubCorrection, Função: CorrectCheckin
  - Resposta:
    ```json
    {
      "status": "ERRO_OPERACIONAL",
      "detalhes": "Falha ao liberar check-in: usuário inválido."
    }
    ```

- **user789 na data 2024-10-04**
  - Comando: `Corrija o check-in do usuário user789 na data 2024-10-04`
  - [DEBUG] Plugin selecionado: WellHubCorrection, Função: CorrectCheckin
  - Resposta:
    ```json
    {
      "status": "ERRO_OPERACIONAL",
      "detalhes": "Falha ao liberar check-in: formato de data inválido."
    }
    ```

### 2. Geração Dinâmica
Para combinações não mapeadas, o sistema retorna erro operacional genérico:
- Status: `ERRO_OPERACIONAL`
- Detalhes: `Falha ao corrigir check-in: registro não encontrado.`

## Exemplos de Uso

### Comando no Chat
```
Corrija o check-in do usuário user123 na data 2024-10-02
Corrija o check-in do usuário user456 na data 2024-10-03
Corrija o check-in do usuário user000 na data 2024-10-02
Corrija o check-in do usuário user789 na data 2024-10-04
```

### Resposta Esperada (JSON)
```json
{
  "status": "SUCESSO_DA_CORRECAO | ERRO_OPERACIONAL",
  "detalhes": "mensagem de sucesso ou erro"
}
```

## Estrutura de Retorno

Todos os retornos seguem o padrão estruturado:

```json
{
  "status": "SUCESSO_DA_CORRECAO | ERRO_OPERACIONAL",
  "detalhes": "mensagem de sucesso ou erro"
}
```

## Funcionalidades Implementadas

- ✅ **CorrectCheckin**: Função principal de correção
- ✅ **Tratamento de erros**: Retorna JSON estruturado mesmo em caso de exceção
- ✅ **Dados simulados**: Resultados consistentes para cenários mapeados
- ✅ **JSON estruturado**: Parsing fácil pelo LLM/SK

## Próximos Passos

Este plugin atende aos requisitos da **Issue #2** e serve como base para:
1. **Issue #3**: WellhubCommunicationPlugin (respostas humanizadas)
2. **Issue #4**: Agente de Resolução usando SK Planner
