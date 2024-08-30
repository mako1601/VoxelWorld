namespace VoxelWorld.Graphics
{
    public interface IGraphicsObject
    {
        public int ID { get; }
        public void Bind();
        public void Unbind();
    }
}
