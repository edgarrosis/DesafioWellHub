# WellhubTransactionPlugin - Testes e Documentação

## Cenários de Teste Implementados

O plugin `WellhubTransactionPlugin` simula diferentes cenários de verificação de check-in:

### 1. Cenários Pré-definidos (Dados Simulados)

#### ✅ Sucesso
- **user123_partner456_2024-10-02T10:00:00**
  - Status: `SUCESSO`
  - Detalhes: "Check-in realizado com sucesso. Transação processada."

- **user999_partner888_2024-10-02T14:00:00**
  - Status: `SUCESSO`
  - Detalhes: "Check-in confirmado. Parceiro validado."

#### ❌ Falha na Transação
- **user456_partner789_2024-10-02T11:30:00**
  - Status: `FALHA_TRANSACAO`
  - Detalhes: "Erro no processamento do pagamento - Código: TXN_001"

- **user111_partner222_2024-10-02T16:30:00**
  - Status: `FALHA_TRANSACAO`
  - Detalhes: "Transação negada - Saldo insuficiente - Código: TXN_002"

#### 🔍 Não Localizado
- **user789_partner123_2024-10-02T09:15:00**
  - Status: `NAO_LOCALIZADO`
  - Detalhes: "Nenhum registro de check-in encontrado para os parâmetros informados"

### 2. Geração Dinâmica
Para combinações não mapeadas, o sistema gera resultados baseados no hash do `userId`:
- **33% chance**: SUCESSO
- **33% chance**: FALHA_TRANSACAO
- **33% chance**: NAO_LOCALIZADO

## Exemplos de Uso

### Comando no Chat
```
"Verifique o check-in do usuário user123 no parceiro partner456 em 2024-10-02T10:00:00"
```

### Resposta Esperada (JSON)
```json
{
  "status": "SUCESSO",
  "detalhes": "Check-in realizado com sucesso. Transação processada."
}
```

### Listar Registros de Teste
```
"Mostre os registros de teste disponíveis"
```

## Estrutura de Retorno

Todos os retornos seguem o padrão estruturado definido na Issue #1:

```json
{
  "status": "SUCESSO | FALHA_TRANSACAO | NAO_LOCALIZADO",
  "detalhes": "código de erro ou mensagem para debugging"
}
```

## Funcionalidades Implementadas

- ✅ **VerifyCheckinStatus**: Função principal de verificação
- ✅ **ListSimulatedRecords**: Função auxiliar para debugging
- ✅ **Simulação de latência**: Simula chamadas reais ao backend
- ✅ **Tratamento de erros**: Retorna JSON estruturado mesmo em caso de exceção
- ✅ **Geração dinâmica**: Cria resultados consistentes para dados não mapeados
- ✅ **JSON estruturado**: Parsing fácil pelo LLM/SK

## Próximos Passos

Este plugin atende aos requisitos da **Issue #1** e serve como base para:
1. **Issue #2**: WellhubCorrectionPlugin (ações corretivas)
2. **Issue #3**: WellhubCommunicationPlugin (respostas humanizadas)  
3. **Issue #4**: Agente de Resolução usando SK Planner