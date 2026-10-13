// Capítulo 4: só o contrato. Ainda não há banco nenhum.
// O exemplo da tomada mostra a ideia de interface:

ITomada tomada = new Hidreletrica();
Console.WriteLine($"Voltagem: {tomada.Voltagem()} V");
Console.WriteLine("O contrato IOrm (Core/IOrm.cs) compilou. No capítulo 5 criamos a fachada.");

public interface ITomada
{
    int Voltagem();
}

public class Hidreletrica : ITomada
{
    public int Voltagem() => 127;
}
