package meuorm.models;

@Table("tb_fornecedor_legado")
public class Fornecedor extends Model {

    private String razaoSocial = "";

    public String getRazaoSocial() {
        return razaoSocial;
    }

    public void setRazaoSocial(String razaoSocial) {
        this.razaoSocial = razaoSocial;
    }
}
