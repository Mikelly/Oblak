namespace Oblak.Interfaces;

public interface IMessageHubb
{
    Task Notify();
    Task Status(int progress, string message);
}
