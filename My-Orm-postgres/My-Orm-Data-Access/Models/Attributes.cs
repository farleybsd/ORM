namespace My_Orm_Data_Access.Models;

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