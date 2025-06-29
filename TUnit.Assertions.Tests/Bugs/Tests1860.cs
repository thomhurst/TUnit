namespace TUnit.Assertions.Tests.Bugs;

public class Tests1860
{
    class Game
    {
        public Guid Id { get; set; }
        public List<Player> Players { get; } = [];
    }

    class Player
    {
        public Guid Id { get; set; }
        public Game Game { get; set; } = default!;
        public Guid GameId { get; set; }
    }

    [Test]
    public async Task GameInstancesShouldBeSame()
    {
        var game1 = CreateGame();
        var game2 = CreateGame();

        await Assert.That(game1).IsEquivalentTo(game2);
    }

    static Game CreateGame()
    {
        var game = new Game { Id = new("22D05F6C-DE9B-4B70-81B0-A54E0E83DA6D") };
        var player = new Player
        {
            Id = new("FE0FA471-AC98-4D1B-825B-4DDF64122022"),
            Game = game,
            GameId = game.Id
        };
        game.Players.Add(player);

        return game;
    }
}
