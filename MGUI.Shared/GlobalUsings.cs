// MonoGame's device-bound types come from Monadmind.Gfx.Fna, which renders through D3D12. C# resolves these aliases
// before the namespace imports of the same scope, so `using Microsoft.Xna.Framework.Graphics;` keeps serving the
// enums and state objects (BlendState, SamplerState, SpriteSortMode, ...) while every unqualified SpriteBatch,
// Texture2D, ... means the Fna type.
global using GraphicsDevice = Monadmind.Gfx.Fna.GraphicsDevice;
global using SpriteBatch = Monadmind.Gfx.Fna.SpriteBatch;
global using SpriteFont = Monadmind.Gfx.Fna.SpriteFont;
global using Effect = Monadmind.Gfx.Fna.Effect;
global using EffectParameter = Monadmind.Gfx.Fna.EffectParameter;
global using EffectTechnique = Monadmind.Gfx.Fna.EffectTechnique;
global using EffectPass = Monadmind.Gfx.Fna.EffectPass;
global using Texture = Monadmind.Gfx.Fna.Texture;
global using Texture2D = Monadmind.Gfx.Fna.Texture2D;
global using RenderTarget2D = Monadmind.Gfx.Fna.RenderTarget2D;
global using RenderTargetBinding = Monadmind.Gfx.Fna.RenderTargetBinding;
global using PrimitiveBatch = Monadmind.Gfx.Fna.PrimitiveBatch;
