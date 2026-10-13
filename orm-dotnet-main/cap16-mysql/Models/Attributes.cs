namespace MeuOrm.Models;

// Propriedade calculada: é preenchida em consultas, mas nunca vai para INSERT/UPDATE
[AttributeUsage(AttributeTargets.Property)]
public class CalcFieldAttribute : Attribute { }

// Nome de tabela explícito, para quando a convenção não serve
[AttributeUsage(AttributeTargets.Class)]
public class TableAttribute : Attribute
{
    public string Name { get; }

    public TableAttribute(string name)
    {
        Name = name;
    }
}
