using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Lifetime;  // in seconds
    public float MaxLifetime;
    public Microsoft.Xna.Framework.Color Color;
    public float Scale;
    
    public Particle(Vector2 position, Vector2 velocity, float lifetime, Microsoft.Xna.Framework.Color color, float scale)
    {
        Position = position;
        Velocity = velocity;
        Lifetime = lifetime;
        MaxLifetime = lifetime;
        Color = color;
        Scale = scale;
    }

    public bool IsDead => Lifetime <= 0f;

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Lifetime -= dt;
        // Apply simple gravity
        Velocity += new Vector2(0, 9.8f) * dt;  
        // Move
        Position += Velocity * dt;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D texture)
    {
        // alpha fades over lifetime
        float alpha = MathHelper.Clamp(Lifetime / MaxLifetime, 0f, 1f);

        spriteBatch.Draw(
            texture,
            Position,
            null,
            Color * alpha,
            0f,
            Vector2.Zero,
            Scale,
            SpriteEffects.None,
            0f
        );
    }
}
