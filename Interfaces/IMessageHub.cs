namespace Oblak.Interfaces;

public interface IMessageHub
{
    Task Notify();
    Task Status(int progress, string message);
}
