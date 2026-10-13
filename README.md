# 🍮 CashFlow - Sistema de Gestão Financeira

Este repositório centraliza toda a engenharia, documentação e código-fonte do sistema **CashFlow**, projetado especificamente para modernizar e escalar a gestão financeira das operações do **Pudim do Will** e da linha **Will Fit Food**.

A solução visa proporcionar um controlo absoluto e em tempo real sobre as entradas (vendas de sobremesas, refeições preparadas e parcerias via iFood) e saídas (compras de insumos, embalagens e custos operacionais), substituindo o esforço manual por um ecossistema tecnológico de alta performance.

## 🧭 Evolução Arquitetural

A modernização tecnológica foi desenhada em três estágios para garantir entregas de valor contínuas sem paralisar o negócio:

* **Visão AS-IS (O Cenário Atual):** Representa o modelo tradicional de gestão. Operações baseadas em processos manuais, controlos fragmentados ou sistemas legados com acoplamento alto. Neste cenário, a extração de métricas financeiras consolidadas é lenta, suscetível a erros humanos e incapaz de escalar de forma fluida com o aumento do volume de pedidos diários.
* **Visão TÁTICA (O MVP Escalável):** É a solução implementada e disponível neste repositório. Resolve as dores imediatas do negócio utilizando padrões de arquitetura distribuída modernos (CQRS, Event-Driven com Apache Kafka, SQL Server, MongoDB e Redis). Segrega completamente o registo de novas vendas da consulta de saldos, entregando resiliência e tempos de resposta ultrarrápidos para o dia a dia da operação.
* **Visão TARGET (O Alvo Definitivo):** O estado futuro ideal. Uma plataforma 100% Cloud-Native (SaaS), com malha de serviços (Service Mesh) avançada, pipelines de CI/CD totalmente automatizados, observabilidade preditiva e capacidade de integrar automaticamente com fornecedores e plataformas externas de entrega em múltiplas regiões.

## 📁 Estrutura do Repositório

O repositório está organizado para separar a inteligência arquitetural da implementação técnica:

* `📁 documentacao/`
* Contém todos os artefatos de desenho do sistema.
* Inclui os **ADRs (Architecture Decision Records)**, que documentam o contexto, as consequências e os motivos das escolhas tecnológicas (ex: porquê usar Kafka para eventos e Redis para a leitura do saldo diário).
* Inclui diagramas de contexto, contentores e infraestrutura.


* `📁 projeto/`
* Contém a totalidade do código-fonte referente à **Visão Tática**.
* Inclui o `CashFlow.Gateway` (YARP e Rate Limiting), `CashFlow.EntryAPI` (Command/Escrita), `CashFlow.ConsolidationAPI` (Query/Leitura), o `ConsolidationWorker` (Processamento em Background) e a suite completa de testes de integração e testes unitários.
* Possui o `docker-compose.yml` e as instruções locais para levantar todo o ecossistema na máquina de desenvolvimento.
