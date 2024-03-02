namespace Trading.Bot.Models.DataTransferObjects;

public class EmailData
{
    public string EmailToAddress { get; set; }
    public string EmailToName { get; set; }
    public string EmailSubject { get; set; }
    public string EmailBody { get; set; }
}