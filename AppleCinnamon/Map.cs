using AppleCinnamon.System;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.WIC;

namespace AppleCinnamon
{
    public class Map
	{
		private Effect _solidBlockEffect;
		private Effect _basicColorEffect;

        public ChunkManager ChunkManager { get; set; }
        public Camera Camera {get;set;}
        public BoxDrawer BoxDrawer { get; }
        public Game Game { get; }
		public Map(Game game)
		{
			
            Game = game;
            LoadContent();
            BoxDrawer = new BoxDrawer();
            // Camera = new Camera(BoxDrawer);
            ChunkManager = new ChunkManager(game.Device, BoxDrawer, this);
            ChunkManager.FirstChunkLoaded += (sender, args) =>
            {
                Camera = new Camera(BoxDrawer);
            };
        }

		protected void LoadContent()
		{

            var basicColorEffectByteCode = ShaderBytecode.CompileFromFile("Content/Effect/BasicEffect.fx", "fx_5_0", ShaderFlags.Debug, EffectFlags.AllowSlowOperations);
            _basicColorEffect = new Effect(Game.Device, basicColorEffectByteCode);
            var effectByteCode = ShaderBytecode.CompileFromFile("Content/Effect/SolidBlockEffect.fx", "fx_5_0");
            
            _solidBlockEffect = new Effect(Game.Device, effectByteCode);
            var w = TextureLoader.CreateTexture2DFromBitmap(Game.Device,
                TextureLoader.LoadBitmap(new ImagingFactory2(), "Content/Texture/terrain.png"));
            _solidBlockEffect.GetVariableByName("Textures").AsShaderResource().SetResource(new ShaderResourceView(Game.Device, w));

        }

        public void Draw()
        {
            if (Camera != null)
            {
                ChunkManager.Draw(_solidBlockEffect, Game.Device, Game.RenderForm);
                BoxDrawer.Draw(Game.Device, _basicColorEffect);
            }
        }

        public void Update(GameTime gameTime)
        {
            if (Game.RenderForm.Focused)
            {
                Camera?.Update(gameTime, Game.RenderForm, ChunkManager);
            }

            ChunkManager.Update(gameTime, Camera);
            UpdateSolidEffect();
        }

		public void UpdateSolidEffect()
		{
            if (Camera != null)
            {
                _solidBlockEffect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(Camera.WorldViewProjection);
                _basicColorEffect.GetVariableByName("WorldViewProjection").AsMatrix() .SetMatrix(Camera.WorldViewProjection);

                // _basicColorEffect.GetVariableByName("Projection").AsMatrix().SetMatrix(Camera.Projection);
                // _basicColorEffect.GetVariableByName("View").AsMatrix().SetMatrix(Camera.View);
                // _basicColorEffect.GetVariableByName("World").AsMatrix().SetMatrix(Camera.World);
            }
        }
	}
}
