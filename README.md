# 📈 Desafio BT - Monitor de Ações

[![CI/CD Pipeline](https://github.com/GARCl-A/Teste-Inoa/actions/workflows/ci.yml/badge.svg)](https://github.com/GARCl-A/Teste-Inoa/actions/workflows/ci.yml)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=GARCl-A_Desafio-BT&metric=coverage)](https://sonarcloud.io/summary/overall?id=GARCl-A_Desafio-BT)

Sistema de monitoramento de preços de ações com alertas por email.

## 🚀 Funcionalidades

- ✅ Monitoramento em tempo real de preços de ações
- ✅ Alertas automáticos por email
- ✅ Configuração flexível via appsettings/secrets
- ✅ Logs estruturados
- ✅ Tratamento robusto de erros
- ✅ Testes unitários com alta cobertura

## 🛠️ Tecnologias

- **.NET 9.0** - Framework principal
- **xUnit** - Testes unitários
- **Moq** - Mocking para testes
- **MailKit** - Envio de emails
- **Twelve Data API** - Cotações de ações
- **GitHub Actions** - CI/CD
- **SonarQube** - Análise de qualidade

## 📊 Qualidade do Código

- **Cobertura de Testes**: >80%
- **CI/CD**: Build e deploy automatizados
- **Padrões**: Clean Code, SOLID, DRY

## 🏃‍♂️ Como Executar

```bash
# Obter uma chave de api do twelve data
https://twelvedata.com/

# Configurar o SMTP
O SMTP utilizado foi o Mailtrap [https://mailtrap.io/] que fornecia as configurações de e-mail necessárias
OBS: normalmente SenderEmail e Username serão os mesmos. Porém, no mailtrap eram coisas distintas

# Configurar TODOS os secrets necessários
dotnet user-secrets set "DestinationEmail" "seu@email.com"
dotnet user-secrets set "ApiKey" "sua-api-key-twelve-data"
dotnet user-secrets set "EmailSettings:SmtpServer" "smtp.gmail.com"
dotnet user-secrets set "EmailSettings:Port" "587"
dotnet user-secrets set "EmailSettings:SenderEmail" "seu-email@gmail.com"
dotnet user-secrets set "EmailSettings:SmtpUsername" "seu-username-app"
dotnet user-secrets set "EmailSettings:Password" "sua-senha-app"

# Executar
cd .\Desafio-BT\
dotnet run -- PETR4 30.00 25.00

# PETR4: Código da ação a monitorar
# 30.00: Preço limite para VENDA (quando atingir, envia alerta para vender)
# 25.00: Preço limite para COMPRA (quando atingir, envia alerta para comprar)
```

## 🧪 Testes

```bash
# Executar testes
dotnet test

# Cobertura (Windows)
run-coverage.bat

# Cobertura (Linux/Mac)
dotnet test --collect:"XPlat Code Coverage"
```

## 📈 Arquitetura

```
Desafio-BT/                    # Projeto principal
├── Services/                  # Lógica de negócio e integrações
├── Models/                    # Modelos de dados (EmailSettings)
├── Utils/                     # Utilitários (LoggingUtils)
├── AppRunner.cs               # Orquestrador principal
└── Program.cs                 # Configuração e inicialização

Desafio-BT.Tests/              # Projeto de testes
└── Unit/                      # Testes unitários
    └── Services/              # Testes dos serviços
```

## 🎯 Destaques Técnicos

- **Dependency Injection** nativo do .NET
- **Configuração** via Options Pattern
- **Logging** estruturado com ILogger
- **Async/Await** para operações I/O
- **Retry Policy** para chamadas de API
- **Validação** robusta de entrada
- **Testabilidade** com interfaces e mocks
