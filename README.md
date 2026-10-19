# 💰 CashFlow - Sistema de Gestão Financeira Corporativa

![Status](https://img.shields.io/badge/Status-MVP_Concluído-success?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Arquitetura](https://img.shields.io/badge/Architecture-Event--Driven_%7C_CQRS-231F20?style=for-the-badge)

Este repositório centraliza toda a engenharia, documentação arquitetural e o código-fonte do sistema **CashFlow**, projetado para modernizar e escalar a gestão financeira diária das operações de uma empresa. 

A solução visa proporcionar um controlo absoluto e em tempo real sobre as entradas e saídas, substituindo o esforço manual e sistemas legados por um ecossistema tecnológico distribuído, resiliente e de alta performance.

---

## 🧭 Evolução Arquitetural (Roadmap)

A modernização tecnológica foi desenhada em três estágios estratégicos para garantir entregas de valor contínuas sem paralisar o negócio:

*   **Visão AS-IS (O Cenário Atual):** Representa o modelo tradicional de gestão. Operações baseadas em processos manuais, controlos fragmentados ou sistemas legados com alto nível de acoplamento. Neste cenário, a extração de métricas financeiras consolidadas é lenta, suscetível a erros humanos e incapaz de escalar de forma fluida com o aumento do volume de transações.
*   **Visão TÁTICA (O MVP Escalável - *Foco deste Repositório*):** Resolve as dores imediatas do negócio utilizando padrões de arquitetura modernos (*CQRS*, *Event-Driven* com Apache Kafka, SQL Server, MongoDB e Redis). Segrega completamente o registo de novos lançamentos da consulta de saldos, entregando resiliência e tempos de resposta ultrarrápidos em O(1) para o dia a dia da operação.
*   **Visão TARGET (O Alvo Definitivo):** O estado futuro ideal. Uma plataforma 100% *Cloud-Native* (SaaS), com malha de serviços (*Service Mesh*) avançada, *pipelines* de CI/CD totalmente automatizados, observabilidade preditiva e capacidade de integração automática com fornecedores e plataformas externas em múltiplas regiões.

---

## 📂 Estrutura do Repositório

O repositório está organizado para separar a inteligência arquitetural (desenho) da implementação técnica (código):

### 1. `📁 documentacao/` (Desenho da Solução)
Contém todos os artefactos de engenharia de software e visão de produto:
*   **ADRs (Architecture Decision Records):** Documentam o contexto e os motivos das escolhas tecnológicas críticas (ex: porquê utilizar o Apache Kafka para orquestração de eventos e o Redis para leitura de saldos).
*   **Diagramas (C4 Model):** Representações visuais de Contexto, Contentores e Componentes de Infraestrutura.

### 2. `📁 projeto/` (Código-Fonte e Execução)
Contém a totalidade do código-fonte referente à **Visão Tática**. 
A arquitetura é composta por um *API Gateway* (YARP), microsserviços segregados para Escrita (`EntryAPI`) e Leitura (`ConsolidationAPI`), e processamento assíncrono em *background* (`ConsolidationWorker`).

> 🔗 **IMPORTANTE:** Para instruções detalhadas de como compilar, configurar a infraestrutura Docker, popular o banco de dados (*Database First*) e rodar a aplicação, **[consulte o README Técnico dentro da pasta do projeto](https://github.com/AlessandraSoaresdosSantos/CashFlow/blob/master/Projeto/README.md)**.
---

## ✅ Status de Implementação (O que está pronto para avaliação?)

O código contido na pasta `/projeto` é um MVP totalmente funcional, demonstrando a aplicação prática dos conceitos arquiteturais:

*   **Gateway e Segurança:** API Gateway configurado com limite de requisições (*Rate Limiting*).
*   **Gestão de Lançamentos:** API de comandos com validação ACID via EF Core e SQL Server.
*   **Mensageria Assíncrona:** Emissão e consumo de eventos em tempo real utilizando o Apache Kafka via Docker.
*   **Consolidação Resiliente:** Atualização automática de saldos no Redis (*Hot Data*) com *fallback* e retenção de histórico no MongoDB (*Cold Data*).

---

## 🧪 Resumo de Validação e Testes (Quick Start)

Um dos pilares deste projeto é a confiabilidade. O sistema conta com uma suite robusta de testes automatizados que podem ser executados com facilidade:

1.  **Testes Unitários:** Focados no isolamento de regras de negócio, *Handlers* de CQRS e lógica de *Fallback* dos Repositórios (utilizando *Moq* para contornar infraestrutura real). Podem ser rodados sem qualquer dependência externa.
2.  **Testes de Integração:** Validam o ecossistema de ponta a ponta (requisições HTTP simuladas com `WebApplicationFactory`, contornando autenticação dinamicamente e validando retornos reais). Requerem a infraestrutura Docker ativa.

Para avaliar a qualidade do código através dos testes na sua máquina, navegue até a pasta `/projeto` e utilize a CLI do .NET:
```bash
# Executar a bateria de testes unitários
dotnet test src/CashFlow.UnitTests/CashFlow.UnitTests.csproj

# Executar a bateria de testes de integração (Requer Docker-Compose ativo)
dotnet test src/CashFlow.IntegrationTests/CashFlow.IntegrationTests.csproj
