namespace Ticketing.command.Application.Models
{
  public class KafkaSettings
  {
    public required string Hostname {  get; set; }
    public required string Port { get; set; }
    public required string Topic { get; set; }

  }
}
