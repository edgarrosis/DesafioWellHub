# Sistema de Dados JSON - WellHub

## Visão Geral

O sistema agora utiliza dados estruturados em arquivos JSON no diretório `/data`, tornando as consultas mais realistas e permitindo testes com cenários variados.

## Estrutura de Dados

### 📁 `/data/checkin_records.json`
Contém 6 registros de transações de check-in com diferentes status:

- **SUCESSO**: Transações processadas com sucesso
- **FALHA_TRANSACAO**: Erros de pagamento, cartão expirado, saldo insuficiente
- **NAO_LOCALIZADO**: Registros não encontrados

**Campos disponíveis:**
- `id`: ID único da transação
- `userId`: ID do usuário
- `partnerId`: ID do parceiro/estabelecimento
- `timestamp`: Data/hora do check-in
- `status`: Status da transação
- `amount`: Valor da transação
- `partner_name`: Nome do estabelecimento
- `user_name`: Nome do usuário
- `location`: Cidade e endereço
- `error_code`: Código de erro (quando aplicável)
- `error_reason`: Motivo do erro (quando aplicável)

### 📁 `/data/users.json`
Contém 6 usuários com diferentes perfis e status:

**Perfis inclusos:**
- Usuários ativos com diferentes planos (BASIC, STANDARD, PREMIUM)
- Usuário com cartão de crédito expirado
- Usuário suspenso por pagamento em atraso
- Diferentes saldos e limites mensais

**Campos disponíveis:**
- `id`: ID único do usuário
- `name`: Nome completo
- `email`: E-mail de contato
- `phone`: Telefone
- `plan`: Plano contratado
- `status`: Status da conta (ACTIVE, SUSPENDED)
- `credit_balance`: Saldo disponível
- `monthly_limit`: Limite mensal
- `preferred_activities`: Atividades preferidas
- `location`: Localização (cidade/região)
- `payment_issue`: Problemas de pagamento (quando aplicável)

### 📁 `/data/partners.json`
Contém 6 parceiros de diferentes tipos:

**Tipos de estabelecimentos:**
- ACADEMIA: Academias tradicionais
- CROSSFIT: Boxes de CrossFit
- YOGA: Estúdios de yoga

**Campos disponíveis:**
- `id`: ID único do parceiro
- `name`: Nome do estabelecimento
- `type`: Tipo de estabelecimento
- `city`: Cidade
- `address`: Endereço completo
- `phone`: Telefone de contato
- `email`: E-mail de contato
- `operating_hours`: Horários de funcionamento
- `services`: Serviços oferecidos
- `active`: Status (ativo/inativo)
- `closure_reason`: Motivo de fechamento (quando aplicável)

## Funcionalidades Disponíveis

### 🔍 Verificação de Check-in
```
"Verifique o check-in do usuário user123 no parceiro partner456 em 2024-10-02T10:00:00"
```

### 📋 Listar Todos os Registros
```
"Liste os dados simulados"
```

### 👤 Consultar Usuário
```
"Consulte informações do usuário user123"
"Mostre dados do usuário user456"
```

### 🏢 Consultar Parceiro
```
"Consulte informações do parceiro partner456"
"Mostre dados do estabelecimento partner789"
```

## Cenários de Teste Pré-configurados

### ✅ Sucesso
- `user123` + `partner456` + `2024-10-02T10:00:00` → Academia FitLife Centro, São Paulo
- `user999` + `partner888` + `2024-10-02T14:00:00` → Runner Academia, Rio de Janeiro
- `user555` + `partner333` + `2024-10-03T08:45:00` → Yoga Studio Zen, Curitiba (com desconto)

### ❌ Falha na Transação
- `user456` + `partner789` + `2024-10-02T11:30:00` → Smart Fit Vila Olímpia (cartão expirado)
- `user111` + `partner222` + `2024-10-02T16:30:00` → CrossFit Champions (saldo insuficiente)

### 🔍 Não Localizado
- `user789` + `partner123` + `2024-10-02T09:15:00` → Bio Ritmo Moema (usuário suspenso)

## Vantagens do Sistema

1. **Dados Realistas**: Valores monetários, endereços, telefones reais
2. **Cenários Variados**: Diferentes tipos de erro e situações
3. **Fácil Manutenção**: Editar JSONs para adicionar novos cenários
4. **Consultas Detalhadas**: Informações completas de usuários e parceiros
5. **Integração com AI**: Responses mais contextualizada e humanizada

## Como Adicionar Novos Dados

1. Edite os arquivos JSON no diretório `/data`
2. Mantenha a estrutura existente
3. O sistema carregará automaticamente os novos dados
4. Não é necessário recompilar o código

## Exemplo de Resposta Detalhada

```json
{
  "status": "SUCESSO",
  "details": "Check-in realizado com sucesso. Transação processada.",
  "transactionId": "txn_001",
  "amount": 25.50,
  "user": {
    "id": "user123",
    "name": "João Silva",
    "plan": "PREMIUM"
  },
  "partner": {
    "id": "partner456",
    "name": "Academia FitLife Centro",
    "type": "ACADEMIA",
    "city": "São Paulo",
    "address": "Rua Augusta, 123"
  },
  "timestamp": "2024-10-02T10:00:00"
}
```