using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CashFlow.Domain.Entities;

public class ConsolidatedBalance
{
    /// <summary>
    /// Identificador único do documento no MongoDB (Equivalente à Primary Key).
    /// O BsonRepresentation garante que o Mongo grave como ObjectId, mas tratamos como string no C# para facilitar.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// O valor consolidado exato de todas as transações.
    /// </summary>
    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Data e hora da última vez que o saldo foi atualizado (útil para auditoria).
    /// </summary>
    [BsonElement("lastUpdatedAt")]
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// (Opcional, mas muito recomendado) Guarda o ID do último evento do Kafka processado.
    /// Isso garante a Idempotência: se o Kafka reenviar a mesma mensagem por engano, a API não soma o valor duas vezes.
    /// </summary>
    [BsonElement("lastProcessedMessageId")]
    public string? LastProcessedMessageId { get; set; }
}