namespace PSTetris.Rendering
{
    public interface IGameRenderer
    {
        void Initialize(int boardWidth, int boardHeight);
        void RenderFrame(GameState state);
        void RenderInfo(GameState state);
        /// <summary>
        /// Displays the game-over overlay and waits for player input.
        /// Returns true if the player wants to restart, false to quit.
        /// </summary>
        bool RenderGameOver(GameState state);
        void Cleanup();
    }
}
