namespace Clockz
{
    public interface ITicks
    {
        long Frequency { get; }
        long Ticks { get; }
    }
}