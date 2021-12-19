namespace AppleCinnamon.Pipeline
{
    public interface IChunkTransformer
    {
        Chunk Transform(Chunk chunk);
    }
}