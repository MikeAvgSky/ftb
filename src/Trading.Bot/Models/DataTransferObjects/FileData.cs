namespace Trading.Bot.Models.DataTransferObjects;

public class FileData<T>
{
    public string FileName { get; set; }
    public T Value { get; set; }

    public FileData(string fileName, T value)
    {
        FileName = fileName;
        Value = value;
    }
}