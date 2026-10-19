# 💰 CashFlow - Sistema de Gestão de Fluxo de Caixa

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Apache Kafka](https://img.shields.io/badge/Apache_Kafka-231F20?style=for-the-badge&logo=apachekafka&logoColor=white)
![Redis](https://img.shields.io/badge/redis-%23DD0031.svg?style=for-the-badge&logo=redis&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-%234ea94b.svg?style=for-the-badge&logo=mongodb&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![YARP](https://img.shields.io/badge/YARP_Gateway-0081CB?style=for-the-badge)

Este repositório contém o código-fonte do **CashFlow**, uma solução de arquitetura distribuída desenvolvida para suportar alta disponibilidade e performance na gestão financeira diária (ex: controle de lançamentos, gestão de fluxo e consolidação de caixa).

---

## ✅ Status de Implementação (O que já está funcionando)

- [x] **YARP API Gateway:** Roteamento centralizado na porta 5000 e Rate Limiting configurado.
- [x] **EntryAPI:** Validação de regras de negócio, persistência ACID no SQL Server e emissão de eventos.
- [x] **Mensageria (Kafka):** Fluxo de eventos assíncrono totalmente configurado e operacional via Docker.
- [x] **Consolidation Worker:** Consumo em tempo real do Kafka, guardando histórico no MongoDB e saldo atualizado no Redis.
- [x] **ConsolidationAPI:** Consultas de saldo em O(1) direto do Redis, com rotina de *fallback* resiliente para o MongoDB.
- [x] **Testes Automatizados:** Cobertura de testes unitários focada em Repositórios e Controllers (Mock de Redis e Mongo 100% verdes).

---

## 🎯 Proposta de Solução (Visão Tática)

A arquitetura foi baseada nos padrões **CQRS (Command Query Responsibility Segregation)** e Orientação a Eventos (*Event-Driven*), garantindo que as operações de escrita (lançamentos) não impactem a performance das operações de leitura (consulta de saldos).

### 🏗️ Desenho da Arquitetura

1. **Gateway e Borda (YARP):** Ponto único de entrada (Porta 5000). Responsável pelo roteamento de APIs e proteção contra ataques de sobrecarga através de *Rate Limiting* (Limite de 50 requisições por segundo).
2. **Gestão de Lançamentos (EntryAPI):** Lida exclusivamente com comandos (Writes). Valida as regras de negócio, garante a transação ACID guardando o registro financeiro em um banco relacional (**SQL Server**) e emite um evento de domínio no Kafka.
3. **Data Streaming (Apache Kafka):** Atua como o sistema nervoso central, recebendo os eventos de forma assíncrona e resiliente.
4. **Consolidado Diário (Worker & ConsolidationAPI):** 
   - O **Worker** consome os eventos do Kafka, guarda o histórico no **MongoDB** (Cold Data) e incrementa o saldo atómicamente no **Redis** (Hot Data).
   - A **ConsolidationAPI** processa as leituras (Queries) consultando o saldo diretamente da memória cache do Redis, garantindo tempos de resposta ultrarrápidos.

---

## 📂 Estrutura do Projeto

A solução é composta por 9 projetos, organizados de forma a respeitar a separação de responsabilidades:

```text
📦 Solução 'CashFlow'
┣ 📜 README.md
┣ 📂 Regras de Negócio (Core)
┃ ┣ 🧠 CashFlow.Application          -> Interfaces, Commands, Queries e Handlers (CQRS).
┃ ┗ 🏛️ CashFlow.Domain               -> Entidades (Entry, ConsolidatedBalance, OutboxMessage), Enums e Eventos.
┣ 📂 Infraestrutura e Dados
┃ ┗ 🛠️ CashFlow.Infrastructure       -> Acesso a Dados (AppDbContext, EntryRepository, RedisBalanceRepository) e Mensageria (KafkaMessageBus).
┣ 📂 Apresentação (APIs e Workers)
┃ ┣ 🚀 CashFlow.EntryAPI             -> Controllers de Lançamentos e OutboxBackgroundWorker.
┃ ┣ 🚀 CashFlow.ConsolidationAPI     -> BalanceController para leitura rápida do consolidado.
┃ ┣ ⚙️ CashFlow.ConsolidationWorker  -> Worker Service em background (Worker.cs) para consumir mensagens.
┃ ┗ 🚪 CashFlow.Gateway              -> API Gateway (YARP) que unifica as rotas.
┗ 📂 Validação (Testes)
  ┣ 🧪 CashFlow.UnitTests            -> Testes de Controllers, Handlers, Repositórios e Workers isolados.
  ┗ 🔗 CashFlow.IntegrationTests     -> Testes de integração (Ponta a Ponta) das APIs.
```

🛠️ Tecnologias Utilizadas
```text
Backend: C# com .NET 8 (Web API, Worker Services)
API Gateway: YARP (Yet Another Reverse Proxy)
Mensageria: Apache Kafka
Bancos de Dados:
SQL Server: Dados Relacionais e Transacionais.
MongoDB: Snapshots baseados em Documentos.
Redis: Cache de Saldo Consolidado.
Testes: xUnit, Moq, WebApplicationFactory.
```

🚀 Guia Prático de Execução Local (Passo a Passo)
```text
Siga os passos abaixo exatamente nesta ordem para garantir a correta inicialização do ecossistema.

Passo 1: Instalação de Dependências (Pré-requisitos)
Certifique-se de que sua máquina possui as seguintes ferramentas instaladas:
.NET 8 SDK
Docker Desktop (rodando em background)
Cliente de banco de dados (ex: DBeaver, Azure Data Studio ou SQL Server Management Studio)

Passo 2: Subir a Infraestrutura com Docker
O projeto utiliza contêineres para provisionar os bancos de dados e o Kafka. Na raiz do repositório, execute:

Bash
docker-compose up -d
Nota: Aguarde cerca de 30 a 60 segundos para que serviços pesados como SQL Server e Kafka fiquem totalmente "health" (operacionais).

Passo 3: Configuração do Banco de Dados (Abordagem Database First)
Este projeto utiliza a abordagem Database First. Isso significa que as tabelas não são geradas via EF Core Migrations no startup; o schema deve existir previamente no SQL Server.

Conecte-se ao SQL Server local provisionado pelo Docker (via DBeaver ou SSMS):
Host: localhost,1433
User: sa
Password: (Insira_Aqui_A_Senha_Configurada_No_Docker)

Abra o script SQL localizado na pasta de infraestrutura do repositório (ex: scripts/init_database.sql).

Execute o script no SQL Server. Ele se encarregará de criar o banco de dados CashFlowDb, a tabela de lançamentos e inserir os dados iniciais de domínio (massa de dados).

Passo 4: Executar os Microsserviços
Com a infraestrutura no Docker e o banco SQL criado, abra múltiplos terminais (ou abas no seu IDE) na raiz do projeto e inicie os projetos na seguinte ordem:

1. Iniciar a API de Lançamentos:

Bash
dotnet run --project src/CashFlow.EntryAPI/CashFlow.EntryAPI.csproj
2. Iniciar o Worker Consumidor:

Bash
dotnet run --project src/CashFlow.ConsolidationWorker/CashFlow.ConsolidationWorker.csproj
3. Iniciar a API de Consolidação e Leitura:

Bash
dotnet run --project src/CashFlow.ConsolidationAPI/CashFlow.ConsolidationAPI.csproj
4. Iniciar o API Gateway (YARP):

Bash
dotnet run --project src/CashFlow.Gateway/CashFlow.Gateway.csproj
Após executar os comandos, todas as chamadas deverão ser feitas para a porta 5000 (Gateway).
```

🧪 Validação e Testes Automatizados
```text
A arquitetura do projeto possui uma robusta suíte de testes projetada para validar as integrações com os bancos, resiliência do cache e regras de negócio sem necessidade de infraestrutura manual para os testes isolados.

1. Testes Unitários
Os testes unitários focam nas regras da API e lógica dos Repositórios (utilizando Moq para simular comportamentos do StackExchange.Redis e do driver do MongoDB).
Eles não dependem do Docker e podem ser rodados a qualquer momento.

Na raiz do projeto, execute:

Bash
dotnet test src/CashFlow.UnitTests/CashFlow.UnitTests.csproj
2. Testes de Integração
Os testes de integração validam as conexões reais e o fluxo de ponta a ponta (Web API Endpoints).Aviso: Para rodar os testes de integração, a infraestrutura local (Passos 2 e 3) precisa estar rodando no Docker.

Na raiz do projeto, execute:

Bash
dotnet test src/CashFlow.IntegrationTests/CashFlow.IntegrationTests.csproj
```

Para informações técnicas de arquitetura, trade-offs e decisões arquiteturais de nível macro, consulte o README GERAL do portfólio.
