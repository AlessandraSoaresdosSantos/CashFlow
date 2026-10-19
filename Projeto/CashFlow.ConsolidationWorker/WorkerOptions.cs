namespace CashFlow.ConsolidationWorker;

public class WorkerOptions
{
    public const string SectionName = "WorkerSettings";

    public string KafkaBootstrapServers { get; set; } = "localhost:9092";
    public string TopicName { get; set; } = "entry-events";
    public string DeadLetterTopicName { get; set; } = "entry-events-dlt";
    public string DatabaseName { get; set; } = "CashFlowDb";
    public string CollectionName { get; set; } = "EntrySnapshots";
}