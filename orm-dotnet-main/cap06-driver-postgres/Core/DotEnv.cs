namespace MeuOrm.Core;

public static class DotEnv
{
    // Lê o arquivo e coloca cada CHAVE=valor nas variáveis de ambiente do processo
    public static void Load(string path = ".env")
    {
        if (!File.Exists(path))
            return; // em produção as variáveis podem vir do próprio servidor

        foreach (string line in File.ReadAllLines(path))
        {
            string text = line.Trim();

            if (text == "" || text.StartsWith('#'))
                continue; // linha vazia ou comentário

            int pos = text.IndexOf('=');
            if (pos <= 0)
                continue; // linha sem "=" é ignorada

            string key = text[..pos].Trim();
            string value = text[(pos + 1)..].Trim();

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    // Busca uma variável obrigatória e explica o erro se ela não existir
    public static string Get(string key)
    {
        return Environment.GetEnvironmentVariable(key)
            ?? throw new InvalidOperationException(
                $"A variável '{key}' não foi encontrada. Confira o arquivo .env");
    }
}
