# 📈 Desafio BT - Monitor de Ações

[![CI/CD Pipeline](https://github.com/GARCl-A/Teste-Inoa/actions/workflows/ci.yml/badge.svg)](https://github.com/GARCl-A/Teste-Inoa/actions/workflows/ci.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=GARCl-A_Desafio-BT&metric=alert_status)](https://sonarcloud.io/summary/overall?id=GARCl-A_Desafio-BT)
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
- **Análise SonarQube**: Quality Gate ✅
- **CI/CD**: Build e deploy automatizados
- **Padrões**: Clean Code, SOLID, DRY

## 🏃‍♂️ Como Executar

```bash
# Configurar secrets
dotnet user-secrets set "DestinationEmail" "seu@email.com"
dotnet user-secrets set "ApiKey" "sua-api-key"
dotnet user-secrets set "EmailSettings:Password" "sua-senha"

# Executar
dotnet run -- PETR4 30.00 25.00
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
├── Services/           # Lógica de negócio
├── Models/            # Modelos de dados
├── Utils/             # Utilitários
└── Tests/             # Testes unitários
    ├── Unit/          # Testes unitários
    └── Integration/   # Testes de integração
```

## 🎯 Destaques Técnicos

- **Dependency Injection** nativo do .NET
- **Configuração** via Options Pattern
- **Logging** estruturado com ILogger
- **Async/Await** para operações I/O
- **Retry Policy** para chamadas de API
- **Validação** robusta de entrada
- **Testabilidade** com interfaces e mocks