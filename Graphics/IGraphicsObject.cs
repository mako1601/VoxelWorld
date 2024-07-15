namespace VoxelWorld.Graphics
{
    public interface IGraphicsObject
    {
        public int ID { get; set; }
        public void Bind();
        public void Unbind();
        public void Delete();
    }
}
