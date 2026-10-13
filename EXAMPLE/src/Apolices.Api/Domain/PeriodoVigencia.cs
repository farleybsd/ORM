namespace Apolices.Api.Domain;

public sealed record PeriodoVigencia
{
    public DateOnly Inicio { get; }
    public DateOnly Fim { get; }

    public PeriodoVigencia(DateOnly inicio, DateOnly fim)
    {
        if (fim < inicio)
            throw new ArgumentException("A data de fim da vigência não pode ser anterior à data de início.");

        Inicio = inicio;
        Fim = fim;
    }

    public bool EstaVigenteEm(DateOnly data) => data >= Inicio && data <= Fim;
}
