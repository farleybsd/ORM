package meuorm;

import meuorm.core.IOrm;

public class Main {

    interface Tomada {
        int voltagem();
    }

    static class Hidreletrica implements Tomada {
        @Override
        public int voltagem() {
            return 127;
        }
    }

    public static void main(String[] args) {
        Tomada tomada = new Hidreletrica();
        System.out.println("Voltagem: " + tomada.voltagem() + " V");

        // O contrato do ORM também compila:
        System.out.println("Métodos que todo driver precisa ter: " + IOrm.class.getMethods().length);
    }
}
