namespace Future.Utilities.Net.Sockets
{
    public interface IFrameEncoder
    {
        void EncodeFrame(byte[] payload, int offset, int count, out byte[] frame_buffer, out int frame_buffer_offset, out int frame_buffer_length);
    }
}
