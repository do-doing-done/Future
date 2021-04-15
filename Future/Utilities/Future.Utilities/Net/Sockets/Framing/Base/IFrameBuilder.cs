namespace Future.Utilities.Net.Sockets
{
    public interface IFrameBuilder
    {
        IFrameEncoder Encoder { get; }
        IFrameDecoder Decoder { get; }
    }
}
