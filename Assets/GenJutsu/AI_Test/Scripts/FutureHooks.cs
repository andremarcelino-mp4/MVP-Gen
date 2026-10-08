namespace GenJutsu.AITest
{
    // Pontos de extensão. Nenhum deles executa luz, porta, temperatura ou memória ainda.
    public interface IHouseControl
    {
    }

    public interface IUserMemory
    {
        void Remember(string key, string value);
        bool TryRecall(string key, out string value);
    }

    public interface IGenJutsuTool
    {
        string Name { get; }
    }
}
