// MonoGame's device-bound types come from Monadmind.Gfx.Zna, which renders through D3D12. C# resolves these aliases
// before the namespace imports of the same scope, so `using Microsoft.Xna.Framework.Graphics;` keeps serving the
// enums and state objects (BlendState, SamplerState, SpriteSortMode, ...) while every unqualified SpriteBatch,
// Texture2D, ... means the Zna type.
global using GraphicsDevice = Monadmind.Gfx.Zna.GraphicsDevice;
global using SpriteBatch = Monadmind.Gfx.Zna.SpriteBatch;
global using SpriteFont = Monadmind.Gfx.Zna.SpriteFont;
global using Effect = Monadmind.Gfx.Zna.Effect;
global using EffectParameter = Monadmind.Gfx.Zna.EffectParameter;
global using EffectTechnique = Monadmind.Gfx.Zna.EffectTechnique;
global using EffectPass = Monadmind.Gfx.Zna.EffectPass;
global using Texture = Monadmind.Gfx.Zna.Texture;
global using Texture2D = Monadmind.Gfx.Zna.Texture2D;
global using RenderTarget2D = Monadmind.Gfx.Zna.RenderTarget2D;
global using RenderTargetBinding = Monadmind.Gfx.Zna.RenderTargetBinding;
global using PrimitiveBatch = Monadmind.Gfx.Zna.PrimitiveBatch;
