using StbImageSharp;

namespace VoxelWorld.Graphics
{
    public class TextureManager
    {
        public static TextureManager Instance { get; } = new();
        public Dictionary<string, float[]> Textures { get; }
        public ImageResult Atlas { get; }

        public TextureManager()
        {
            Textures = [];
            Atlas = GenerateTextureAtlas();
        }

        // this method can be easily broken (please don't do that)
        public ImageResult GenerateTextureAtlas()
        {
            var textures = Directory
                .GetFiles("resources/textures/blocks", "*.*", SearchOption.TopDirectoryOnly)
                .Where(file => file.EndsWith(".png"))
                .ToDictionary(file => Path.GetFileName(file),
                              file => ImageResult.FromStream(File.OpenRead(file), ColorComponents.RedGreenBlueAlpha));

            int atlasWidth = 16 * textures.First().Value.Width;
            int atlasHeight = 16 * textures.First().Value.Height;
            byte[] atlasData = new byte[atlasWidth * atlasHeight * 4];

            int xOffset = 0;
            int yOffset = 0;

            foreach (var kvp in textures)
            {
                var file = kvp.Key;
                var texture = kvp.Value;

                if (xOffset + texture.Width > atlasWidth)
                {
                    xOffset = 0;
                    yOffset += texture.Height;
                }

                for (int y = 0; y < texture.Height; y++)
                {
                    for (int x = 0; x < texture.Width; x++)
                    {
                        int atlasIndex = ((yOffset + y) * atlasWidth + xOffset + x) * 4;
                        int textureIndex = (y * texture.Width + x) * 4;

                        atlasData[atlasIndex] = texture.Data[textureIndex];
                        atlasData[atlasIndex + 1] = texture.Data[textureIndex + 1];
                        atlasData[atlasIndex + 2] = texture.Data[textureIndex + 2];
                        atlasData[atlasIndex + 3] = texture.Data[textureIndex + 3];
                    }
                }

                float x1 = (float)xOffset / atlasWidth;
                float y1 = (float)yOffset / atlasHeight;
                float x2 = (float)(xOffset + texture.Width) / atlasWidth;
                float y2 = (float)(yOffset + texture.Height) / atlasHeight;

                Textures[file] = [x1, y1, x2, y2];

                xOffset += texture.Width;
            }
#if DEBUG   // if you want to see a result
            using (Stream stream = File.OpenWrite("atlas.png"))
            {
                var imageWriter = new StbImageWriteSharp.ImageWriter();
                imageWriter.WritePng(atlasData, atlasWidth, atlasHeight, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
            }
#endif
            return new ImageResult
            {
                Comp = ColorComponents.RedGreenBlueAlpha,
                Data = atlasData,
                Height = atlasHeight,
                SourceComp = ColorComponents.RedGreenBlueAlpha,
                Width = atlasWidth
            };
        }
    }
}
