package meuorm.models;

public class Cliente extends Model {

    private String nome = "";
    private String email = "";
    private boolean ativo = true;

    @CalcField
    private int totalPedidos;   // não existe coluna total_pedidos

    public String getNome() {
        return nome;
    }

    public void setNome(String nome) {
        this.nome = nome;
    }

    public String getEmail() {
        return email;
    }

    public void setEmail(String email) {
        this.email = email;
    }

    public boolean isAtivo() {
        return ativo;
    }

    public void setAtivo(boolean ativo) {
        this.ativo = ativo;
    }

    public int getTotalPedidos() {
        return totalPedidos;
    }
}
