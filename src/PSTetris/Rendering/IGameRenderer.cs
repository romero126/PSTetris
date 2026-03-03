namespace PSTetris.Rendering
{
    public interface IGameRenderer
    {
        void Initialize(int boardWidth, int boardHeight);
        void RenderFrame(GameState state);
        void RenderInfo(GameState state);
        void RenderGameOver(GameState state);
        void Cleanup();
    }
}
