# 💰 CashFlow - Sistema de Gestão de Fluxo de Caixa

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Kafka](https://img.shields.io/badge/Apache_Kafka-231F20?style=for-the-badge&logo=apachekafka&logoColor=white)
![Redis](https://img.shields.io/badge/redis-%23DD0031.svg?style=for-the-badge&logo=redis&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-%234ea94b.svg?style=for-the-badge&logo=mongodb&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![YARP](https://img.shields.io/badge/YARP_Gateway-0081CB?style=for-the-badge)

## 🎯 Proposta de Solução (Visão Tática)

O **CashFlow** é uma solução de arquitetura distribuída desenhada para suportar alta disponibilidade e performance na gestão financeira diária (ex: controlo de vendas de produtos, gestão de fluxo e consolidação de caixa). 

A arquitetura foi baseada no padrão **CQRS (Command Query Responsibility Segregation)** e Orientação a Eventos (*Event-Driven*), garantindo que as operações de escrita (lançamentos) não impactam a performance das operações de leitura (consulta de saldos).

### 🏗️ Desenho da Arquitetura

1. **Gateway e Borda (YARP):** Ponto único de entrada (Porta 5000). Responsável pelo roteamento de APIs, validação de tokens JWT (Zero-Trust) e proteção contra ataques de sobrecarga através de *Rate Limiting* (Limite de 50 requisições por segundo).
2. **Gestão de Lançamentos (EntryAPI):** Lida exclusivamente com comandos (Writes). Valida as regras de negócio, garante a transação ACID guardando o registo financeiro num banco relacional (**SQL Server**) e emite um evento de domínio.
3. **Data Streaming (Apache Kafka):** Atua como o sistema nervoso central, recebendo os eventos da `EntryAPI` de forma assíncrona e resiliente.
4. **Consolidado Diário (Worker & ConsolidationAPI):** 
   - O **Worker** consome os eventos do Kafka em tempo real, guarda o histórico no **MongoDB** (Cold Data / Backup histórico) e incrementa o saldo diário atómicamente no **Redis** (Hot Data).
   - A **ConsolidationAPI** processa as leituras (Queries) consultando o saldo diretamente da memória cache do Redis, garantindo tempos de resposta em O(1) (ultrarrápidos).

---

## 🛠️ Tecnologias Utilizadas

* **Backend:** C# com .NET 8 (Web API, Worker Services)
* **API Gateway:** YARP (Yet Another Reverse Proxy)
* **Mensageria:** Apache Kafka
* **Bancos de Dados:** 
  * SQL Server (Dados Relacionais e Transacionais via EF Core)
  * MongoDB (Snapshots baseados em Documentos)
  * Redis (Cache de Saldo Consolidado)
* **Segurança:** Keycloak (OAuth2 / JWT Bearer)
* **Testes:** xUnit, Moq, WebApplicationFactory (In-Memory Integration Tests)

---

## 🚀 Como Executar o Projeto Localmente

### 1. Pré-requisitos
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) instalado.
* [Docker Desktop](https://www.docker.com/products/docker-desktop) instalado e a correr.
* EF Core CLI (`dotnet tool install --global dotnet-ef`).

### 2. Subir a Infraestrutura (Docker)
Na raiz do projeto, suba as bases de dados, o Kafka e o servidor de identidade usando o Docker Compose:
```bash
docker-compose up -d